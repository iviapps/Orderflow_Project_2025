using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Orderflow.Identity.Data;
using Orderflow.Identity.Data.Entities;

namespace Orderflow.Identity.Extensions;

public static class IdentityExtensions
{
    /// <summary>
    /// Configures ASP.NET Core Identity with ApplicationUser and Google OAuth
    /// </summary>
    public static IServiceCollection AddIdentityWithGoogle(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        // ASP.NET Core Identity
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Password
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;

            // Lockout
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User
            options.User.RequireUniqueEmail = true;

            // Sign in
            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // Google OAuth (solo si está configurado)
        var googleClientId = configuration["Google:ClientId"];
        var googleClientSecret = configuration["Google:ClientSecret"];

        if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
        {
            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleClientSecret;
                    options.CallbackPath = "/signin-google";
                });
        }

        return services;
    }
}