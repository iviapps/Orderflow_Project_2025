using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Orderflow.Catalog.Data;
using Orderflow.Catalog.Extensions;
using Orderflow.Catalog.Services;
using Orderflow.Shared.Extensions; // AddJwtAuthentication + OpenApiExtensions (si la moviste a Shared)
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
        "Orderflow Catalog API V1",
        "v1",
        "Catalog API using Controllers with JWT Bearer authentication");

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
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");

// ============================================
// JWT Authentication (shared across all microservices)
// ============================================
builder.Services.AddJwtAuthentication(builder.Configuration);

// ============================================
// SERVICES
// ============================================
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IStockService, StockService>();

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
    await app.Services.SeedDevelopmentDataAsync();

    app.MapOpenApi();
    app.MapScalarApiReference(options =>

    {
        options
            .WithTitle("Orderflow Catalog API")
            .AddDocument("v1", "V1 - Controllers", "/openapi/v1.json", isDefault: true);
    });

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Orderflow Catalog API V1");
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapControllers();

await app.RunAsync();
