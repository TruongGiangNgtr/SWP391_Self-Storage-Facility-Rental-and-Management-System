using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Frms.Api.Authorization;

public sealed class ApiAuthorizationMiddlewareResultHandler
    : IAuthorizationMiddlewareResultHandler {
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler =
        new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult) {
        if (authorizeResult.Challenged) {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            await context.Response.WriteAsJsonAsync(
                new ApiErrorResponse(
                    "UNAUTHORIZED",
                    "Authentication is required.",
                    context.TraceIdentifier));

            return;
        }

        if (authorizeResult.Forbidden) {
            context.Response.StatusCode =
                StatusCodes.Status403Forbidden;

            await context.Response.WriteAsJsonAsync(
                new ApiErrorResponse(
                    "FORBIDDEN",
                    "You are not authorized to access this resource.",
                    context.TraceIdentifier));

            return;
        }

        await _defaultHandler.HandleAsync(
            next,
            context,
            policy,
            authorizeResult);
    }
}
