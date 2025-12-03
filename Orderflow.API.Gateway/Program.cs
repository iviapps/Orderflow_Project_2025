using Orderflow.ApiGateway.Extensions;
using Orderflow.Shared.Extensions; 

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults (telemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Redis for distributed rate limiting
builder.AddRedisClient("cache");

// CORS for frontend communication
builder.Services.AddGatewayCors();

// JWT authentication from shared extensions > jwauthenticationextensions. 
builder.Services.AddJwtAuthentication(builder.Configuration);

// Authorization policies (authenticated, admin, customer)
builder.Services.AddGatewayAuthorizationPolicies();

// Rate limiting with Redis (configured in appsettings.json)
builder.Services.AddRedisRateLimiting(builder.Configuration);

// YARP reverse proxy (routes to microservices)
builder.Services.AddYarpReverseProxy(builder.Configuration);

var app = builder.Build();

// Health check endpoints
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapReverseProxy();

app.Run();
