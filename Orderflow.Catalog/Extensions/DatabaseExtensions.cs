using Microsoft.EntityFrameworkCore;
using Orderflow.Catalog.Data;
using Orderflow.Catalog.Entities;

namespace Orderflow.Catalog.Extensions
{
    public static class DatabaseExtensions
    {
        public static async Task SeedDevelopmentDataAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

            await context.Database.MigrateAsync();

            if (!context.Categories.Any())
            {
                // Crear categorías
                var electronics = new Category { Name = "Electronics", Description = "Fusibles Producto" };
                var hardware = new Category { Name = "Hardware", Description = "Componentes Hardware" };
                context.Categories.AddRange(electronics, hardware);
                await context.SaveChangesAsync();

                // Crear productos con stock
                var products = new[]
                {
                new Product
                {
                    Name = "Laptop", Price = 999.99m, CategoryId = electronics.Id,
                    Stock = new Stock { QuantityAvailable = 50 }
                }, new Product {
                    Name = "Smartphone", Price = 499.99m, CategoryId = electronics.Id,
                    Stock = new Stock { QuantityAvailable = 100 }
                }, new Product {
                    Name = "Motherboard", Price = 199.99m, CategoryId = hardware.Id,
                    Stock = new Stock { QuantityAvailable = 30 }
                },
                    new Product {
                        Name = "RAM 16GB", Price = 79.99m, CategoryId = hardware.Id,
                        Stock = new Stock { QuantityAvailable = 200 }
                    }
            };
                context.Products.AddRange(products);
                await context.SaveChangesAsync();
            }
        }
    }
}
