using Asp.Versioning;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Orderflow.Shared.Extensions;
using Orderflow.Orders.Clients;
using Orderflow.Orders.Data;
using Orderflow.Orders.Services;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// ASPIRE SERVICE DEFAULTS
// ============================================
builder.AddServiceDefaults();

// ============================================
// OPENAPI / SCALAR + SEGURIDAD JWT EN DOCS
// ============================================
builder.Services.AddOpenApi("v1", options =>
{
    options.ConfigureDocumentInfo(
        "Orderflow Orders API V1",
        "v1",
        "Orders API using Controllers with JWT Bearer authentication");

    options.AddJwtBearerSecurity();
    options.FilterByApiVersion("v1");
});

// ============================================
// API VERSIONING (Asp.Versioning)
// ============================================
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader(); // /api/v{version}/...
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

// ============================================
// DATABASE (PostgreSQL)
// ============================================
builder.AddNpgsqlDbContext<OrdersDbContext>("ordersdb");

// ============================================
// MASS TRANSIT + RABBITMQ
// ============================================
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var configuration = context.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("messaging");

        if (!string.IsNullOrEmpty(connectionString))
            cfg.Host(new Uri(connectionString));

        cfg.ConfigureEndpoints(context);
    });
});

// ============================================
// HTTP CLIENTS
// ============================================
builder.Services.AddHttpClient("catalog", client =>
{
    client.BaseAddress = new Uri("https+http://orderflow-catalog");
});
builder.Services.AddScoped<ICatalogClient, CatalogClient>();

// ============================================
// JWT Authentication (shared across all microservices)
// ============================================
builder.Services.AddJwtAuthentication(builder.Configuration);

// ============================================
// SERVICES
// ============================================
builder.Services.AddScoped<IOrderService, OrderService>();

// ============================================
// CONTROLLERS + JSON ENUMS AS STRING
// ============================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

var app = builder.Build();

// ============================================
// DEV ONLY: MIGRATIONS + OPENAPI + SCALAR
// ============================================
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    await db.Database.MigrateAsync();

    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Orderflow Orders API")
            .AddDocument("v1", "V1 - Controllers", "/openapi/v1.json", isDefault: true);
    });

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Orderflow Orders API V1");
    });
}

app.UseHttpsRedirection();

//FIX: si tienes JWT, necesitas Authentication antes de Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapControllers();

await app.RunAsync();
