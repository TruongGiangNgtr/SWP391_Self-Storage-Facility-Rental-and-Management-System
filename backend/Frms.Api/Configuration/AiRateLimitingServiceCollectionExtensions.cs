using System.Threading.RateLimiting;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.RateLimiting;

namespace Frms.Api.Configuration;

/// <summary>AI-001 public request-rate policy. Configuration is read only at process startup.</summary>
public static class AiRateLimitingServiceCollectionExtensions
{
    public static IServiceCollection AddFrmsAiRateLimiting(
        this IServiceCollection services, IConfiguration configuration)
    {
        var limit = Math.Clamp(configuration.GetValue<int?>("Ai:RateLimitPerMinute") ?? 10, 1, 60);
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("public-ai", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(new ApiErrorResponse(
                    "RATE_LIMIT_EXCEEDED", "AI request limit exceeded. Try again later.",
                    context.HttpContext.TraceIdentifier), cancellationToken);
            };
        });
        return services;
    }
}
