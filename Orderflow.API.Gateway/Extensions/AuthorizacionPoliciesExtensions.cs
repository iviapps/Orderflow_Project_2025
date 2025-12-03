namespace Orderflow.ApiGateway.Extensions;

public static class AuthorizationPoliciesExtensions
{
    public static IServiceCollection AddGatewayAuthorizationPolicies(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Requires valid JWT token
            options.AddPolicy("authenticated", policy =>
                policy.RequireAuthenticatedUser());

            // NOTE: "anonymous" is YARP built-in - don't define it here

            // Admin role required
            options.AddPolicy("admin", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Admin");
            });

            // Customer role required
            options.AddPolicy("customer", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Customer");
            });

            // Let YARP handle route-level authorization
            options.FallbackPolicy = null;
        });

        return services;
    }
}
