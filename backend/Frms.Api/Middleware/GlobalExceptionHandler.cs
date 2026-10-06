using Frms.Api.DTOs.Responses;
using Frms.Business.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace Frms.Api.Middleware;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var business = exception as BusinessException;
        var status = business?.SuggestedStatusCode ?? StatusCodes.Status500InternalServerError;
        var code = business?.Code ?? "INTERNAL_SERVER_ERROR";
        var message = business?.SafeMessage ?? "An unexpected error occurred.";
        if (business is null) logger.LogError(exception, "Unhandled exception for trace {TraceId}.", context.TraceIdentifier);
        else logger.LogWarning("Request failed with code {Code} for trace {TraceId}.", code, context.TraceIdentifier);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ApiErrorResponse(code, message, context.TraceIdentifier), cancellationToken);
        return true;
    }
}
