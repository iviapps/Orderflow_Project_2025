using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Orderflow.Identity.Data;
using Orderflow.Identity.Data.Entities;

namespace Orderflow.Identity.Extensions;

public static class DatabaseExtensions
{
    public static async Task SeedDevelopmentDataAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        // Obtener el DbContext de Identity
        var context = services.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();

        // Crear roles si no existen
        await SeedRolesAsync(services);

        // Crear usuarios de desarrollo
        await SeedUserAsync(
            services,
            email: "admin@admin.com",
            password: "Test12345.",
            firstName: "Admin",
            lastName: "User",
            role: Roles.Admin
        );

        await SeedUserAsync(
            services,
            email: "customer@customer.com",
            password: "Test12345.",
            firstName: "Customer",
            lastName: "User",
            role: Roles.Customer
        );
    }

    private static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in Roles.GetAll())
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                Console.WriteLine($" Role created: {role}");
            }
        }
    }

    private static async Task SeedUserAsync(
        IServiceProvider services,
        string email,
        string password,
        string firstName,
        string lastName,
        string role
    )
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(email);
        if (user != null)
        {
            Console.WriteLine($" User already exists: {email}");
            return;
        }

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            Console.WriteLine($" Failed to create user {email}: {errors}");
            return;
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        Console.WriteLine($" User created: {email} ({role})");
    }
}
