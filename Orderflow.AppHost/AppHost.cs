
var builder = DistributedApplication.CreateBuilder(args);

// ============================================
// SECRETS & PARAMETERS
// ============================================
// JWT Secret - compartido entre Identity y API Gateway para validar tokens
var jwtSecret = builder.AddParameter("jwt-secret", secret: true);

// Google OAuth - OPCIONAL para desarrollo
// Solo se configura si existen los valores en User Secrets
// Si no están configurados, el sistema funciona sin Google OAuth
var googleClientId = builder.Configuration["Parameters:google-client-id"] ?? "";
var googleClientSecret = builder.Configuration["Parameters:google-client-secret"] ?? "";

// ============================================
// INFRASTRUCTURE
// ============================================

// PostgreSQL - Database for microservices
// NOTA: No usar WithHostPort() para evitar conflictos de puertos
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("orderflow-postgres-data-v2")
    .WithPgAdmin()
    .WithLifetime(ContainerLifetime.Persistent);

// Databases for microservices,
var identityDb = postgres.AddDatabase("identitydb");
var catalogDb = postgres.AddDatabase("catalogdb");
var ordersDb = postgres.AddDatabase("ordersdb");

// Redis - Distributed cache for rate limiting only
var redis = builder.AddRedis("cache")
    .WithDataVolume("orderflow-redis-data-v2")
    .WithLifetime(ContainerLifetime.Persistent);

// RabbitMQ - Message broker for reliable event-driven communication
var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithDataVolume("orderflow-rabbitmq-data-v2")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent);

// MailDev - Local SMTP server for development
var maildev = builder.AddContainer("maildev", "maildev/maildev")
    .WithHttpEndpoint(targetPort: 1080, name: "web")
    .WithEndpoint(targetPort: 1025, name: "smtp")
    .WithLifetime(ContainerLifetime.Persistent);

// ============================================
// MICROSERVICES
// ============================================
var identityService = builder.AddProject<Projects.Orderflow_Identity>("orderflow-identity")
    .WithReference(identityDb)
    .WithReference(rabbitmq)
    .WithEnvironment("Jwt__Secret", jwtSecret)
    .WithEnvironment("Google__ClientId", googleClientId)      // Opcional - vacío si no configurado
    .WithEnvironment("Google__ClientSecret", googleClientSecret) // Opcional - vacío si no configurado
    .WaitFor(identityDb)
    .WaitFor(rabbitmq);


// Catalog Service - Products and Categories
var catalogService = builder.AddProject<Projects.Orderflow_Catalog>("orderflow-catalog")
    .WithReference(catalogDb)
    .WaitFor(catalogDb);
   


//// Notifications Worker - Listens to RabbitMQ events and sends emails
var notificationsService = builder.AddProject<Projects.Orderflow_Notifications>("Orderflow-notifications")
    .WithReference(rabbitmq)
    .WithEnvironment("Email__SmtpHost", maildev.GetEndpoint("smtp").Property(EndpointProperty.Host))
    .WithEnvironment("Email__SmtpPort", maildev.GetEndpoint("smtp").Property(EndpointProperty.Port))
    .WaitFor(rabbitmq);


//// Orders Service - Order management
var ordersService = builder.AddProject<Projects.Orderflow_Orders>("Orderflow-orders")
    .WithReference(ordersDb)
    .WithReference(rabbitmq)
    .WithReference(catalogService)
    .WaitFor(ordersDb)
    .WaitFor(rabbitmq);

//// ============================================
//// API GATEWAY
//// ============================================
// API Gateway acts as the single entry point for all client requests
// It handles authentication, authorization, rate limiting, and routes to microservices
var apiGateway = builder.AddProject<Projects.Orderflow_ApiGateway>("orderflow-apigateway")
    .WithReference(redis) // Redis for rate limiting and caching
    .WithReference(identityService)
    .WithReference(catalogService)
    .WithReference(ordersService)
    .WithEnvironment("Jwt__Secret", jwtSecret)
    .WaitFor(identityService)
    .WaitFor(catalogService)
    .WaitFor(ordersService);



//// ============================================
//// FRONTEND - React App
//// ============================================
// Frontend communicates ONLY with API Gateway (not directly with microservices)
var frontendApp = builder.AddNpmApp("Orderflow-web", "../Orderflow.web", "dev")
    .WithReference(apiGateway) // Frontend talks to Gateway, not to services directly
    .WithEnvironment("VITE_API_GATEWAY_URL", apiGateway.GetEndpoint("https")) // Gateway URL for frontend
    .WithHttpEndpoint(env: "VITE_PORT") // Vite uses VITE_PORT environment variable
    .WaitFor(apiGateway)
    .WithExternalHttpEndpoints() // Make endpoint accessible via Aspire dashboard
    .PublishAsDockerFile();



builder.Build().Run();