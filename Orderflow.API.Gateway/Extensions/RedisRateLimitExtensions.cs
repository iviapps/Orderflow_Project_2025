using RedisRateLimiting;
using RedisRateLimiting.AspNetCore;
using StackExchange.Redis;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace Orderflow.ApiGateway.Extensions;

public static class RedisRateLimitingExtensions
{
    private static readonly string KeyPrefix = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

    public static IServiceCollection AddRedisRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            // Anonymous policy for public endpoints (100 req/min per IP)
            options.AddPolicy("anonymous", context =>
            {
                var redis = context.RequestServices.GetRequiredService<IConnectionMultiplexer>();
                var ipAddress = GetClientIpAddress(context);

                return RedisRateLimitPartition.GetFixedWindowRateLimiter(
                    $"{KeyPrefix}:ip:{ipAddress}",
                    _ => new RedisFixedWindowRateLimiterOptions
                    {
                        ConnectionMultiplexerFactory = () => redis,
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1)
                    });
            });

            // Authenticated users policy (250 req/min per user)
            // Rate limiter runs after authentication, so context.User is populated
            options.AddPolicy("authenticated", context =>
            {
                var redis = context.RequestServices.GetRequiredService<IConnectionMultiplexer>();

                // Get userId from authenticated user claims
                var userId = context.User.FindFirst("sub")?.Value
                    ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // If we have a valid userId from the token, use user-based rate limiting
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    return RedisRateLimitPartition.GetFixedWindowRateLimiter(
                        $"{KeyPrefix}:user:{userId}",
                        _ => new RedisFixedWindowRateLimiterOptions
                        {
                            ConnectionMultiplexerFactory = () => redis,
                            PermitLimit = 250,
                            Window = TimeSpan.FromMinutes(1)
                        });
                }

                // Fallback: no valid token = stricter IP-based limiting
                var ipAddress = GetClientIpAddress(context);
                return RedisRateLimitPartition.GetFixedWindowRateLimiter(
                    $"{KeyPrefix}:unauth:{ipAddress}",
                    _ => new RedisFixedWindowRateLimiterOptions
                    {
                        ConnectionMultiplexerFactory = () => redis,
                        PermitLimit = 50, // Stricter limit for unauthenticated on protected routes
                        Window = TimeSpan.FromMinutes(1)
                    });
            });

            // Handle rate limit rejections
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
                    ? ((TimeSpan)retryAfterValue).TotalSeconds
                    : 60;

                context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString();

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too many requests",
                    message = "Rate limit exceeded. Please try again later.",
                    retryAfter = $"{retryAfter} seconds"
                }, cancellationToken);
            };
        });

        return services;
    }

    /// <summary>
    /// Extracts the real client IP address, handling proxy headers and IPv6 normalization.
    /// </summary>
    private static string GetClientIpAddress(HttpContext context)
    {
        string? ipAddress = null;

        // Check X-Forwarded-For header first (standard proxy header)
        // Format: "client, proxy1, proxy2" - we want the first (client) IP
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            ipAddress = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()?.Trim();
        }

        // Fallback: Check X-Real-IP header (used by some proxies like nginx)
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            ipAddress = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        }

        // Fallback: Check CF-Connecting-IP header (Cloudflare)
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            ipAddress = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
        }

        // Final fallback: Use connection remote IP
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            ipAddress = context.Connection.RemoteIpAddress?.ToString();
        }

        // Normalize the IP address
        return NormalizeIpAddress(ipAddress);
    }

    /// <summary>
    /// Normalizes IP addresses to ensure consistent key generation.
    /// Handles IPv6 normalization and IPv4-mapped IPv6 addresses.
    /// </summary>
    private static string NormalizeIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return "0.0.0.0"; // Safe fallback instead of "unknown"
        }

        // Try to parse as IP address for normalization
        if (IPAddress.TryParse(ipAddress, out var parsedIp))
        {
            // Handle IPv4-mapped IPv6 addresses (::ffff:192.168.1.1 -> 192.168.1.1)
            if (parsedIp.IsIPv4MappedToIPv6)
            {
                parsedIp = parsedIp.MapToIPv4();
            }

            // Return normalized string representation
            // For IPv6, this ensures consistent formatting (lowercase, compressed)
            return parsedIp.ToString();
        }

        // If parsing fails, return sanitized original (remove any dangerous characters)
        return ipAddress.Replace(":", "_").Replace("/", "_");
    }
}
