using Microsoft.EntityFrameworkCore;
using Orderflow.Catalog.Data;
using Orderflow.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);


builder.AddServiceDefaults();
// JWT Authentication (shared across all microservices)
builder.Services.AddJwtAuthentication(builder.Configuration);
// Add PostgreSQL DbContext
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");


// Register services <- NOT IMPLEMENTED YET
//builder.Services.AddScoped<ICategoryService, CategoryService>();
//
//builder.Services.AddScoped<IProductService, ProductService>();
//builder.Services.AddScoped<IStockService, StockService>(); <- beware, we are using table 

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await db.Database.MigrateAsync();

    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
