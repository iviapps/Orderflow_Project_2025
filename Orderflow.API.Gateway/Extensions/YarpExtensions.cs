namespace Orderflow.ApiGateway.Extensions;

public static class YarpExtensions
{
    public static IServiceCollection AddYarpReverseProxy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add service discovery for resolving service names
        services.AddServiceDiscovery();

        // Configure YARP with service discovery
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"))
            .AddServiceDiscoveryDestinationResolver();

        return services;
    }

    public static IServiceCollection AddGatewayCors(
        this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        return services;
    }
}
