using Frms.Api.DTOs.Responses;

namespace Frms.Api.Middleware;

/// <summary>Logs and sanitizes unexpected request-pipeline exceptions.</summary>
public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var traceId = context.TraceIdentifier;
            logger.LogError(exception, "Unhandled API exception. TraceId: {TraceId}", traceId);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(
                new ApiErrorResponse(
                    "INTERNAL_SERVER_ERROR",
                    "An unexpected error occurred.",
                    traceId),
                context.RequestAborted);
        }
    }
}
