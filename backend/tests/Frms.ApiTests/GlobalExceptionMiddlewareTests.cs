using System.Text.Json;
using Frms.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Frms.ApiTests;

public sealed class GlobalExceptionMiddlewareTests
{
    [Test]
    public async Task UnexpectedExceptionReturnsSanitizedTraceableResponse()
    {
        const string sensitiveMessage = "sensitive-diagnostic-detail";
        var middleware = new GlobalExceptionMiddleware(
            _ => throw new InvalidOperationException(sensitiveMessage),
            NullLogger<GlobalExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-test-001",
        };
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var payload = await JsonDocument.ParseAsync(context.Response.Body);
        var root = payload.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
            Assert.That(context.Response.ContentType, Does.StartWith("application/json"));
            Assert.That(root.GetProperty("code").GetString(), Is.EqualTo("INTERNAL_SERVER_ERROR"));
            Assert.That(root.GetProperty("traceId").GetString(), Is.EqualTo("trace-test-001"));
            Assert.That(root.GetProperty("message").GetString(), Does.Not.Contain(sensitiveMessage));
        });
    }
}
