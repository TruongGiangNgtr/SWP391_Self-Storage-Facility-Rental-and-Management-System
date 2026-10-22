using System.Text.Json;
using Frms.Api.Authorization;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace Frms.ApiTests;

[TestFixture]
public sealed class AuthorizationMiddlewareResultMatrix2401Tests
{
    public static IEnumerable<TestCaseData> Cases()
    {
        var id = 2401;
        for (var i = 0; i < 50; i++)
            yield return new TestCaseData(true, $"trace-challenge-{i + 1:000}").SetName($"TC_{id++}_challenge_returns_401_{i + 1:00}");
        for (var i = 0; i < 50; i++)
            yield return new TestCaseData(false, $"trace-forbidden-{i + 1:000}").SetName($"TC_{id++}_forbidden_returns_403_{i + 1:00}");
        if (id != 2501) throw new InvalidOperationException($"Expected 100 cases; generated {id - 2401}.");
    }

    [TestCaseSource(nameof(Cases))]
    public async Task HandleAsync_ProducesStableUnauthorizedOrForbiddenEnvelope(bool challenged, string traceId)
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = traceId;
        context.Response.Body = new MemoryStream();
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        var result = challenged ? PolicyAuthorizationResult.Challenge() : PolicyAuthorizationResult.Forbid();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        await new ApiAuthorizationMiddlewareResultHandler().HandleAsync(next, context, policy, result);
        context.Response.Body.Position = 0;
        var envelope = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Multiple(() =>
        {
            Assert.That(context.Response.StatusCode, Is.EqualTo(challenged ? StatusCodes.Status401Unauthorized : StatusCodes.Status403Forbidden));
            Assert.That(context.Response.ContentType, Does.StartWith("application/json"));
            Assert.That(envelope, Is.Not.Null);
            Assert.That(envelope!.Code, Is.EqualTo(challenged ? "UNAUTHORIZED" : "FORBIDDEN"));
            Assert.That(envelope.Message, Is.EqualTo(challenged ? "Authentication is required." : "You are not authorized to access this resource."));
            Assert.That(envelope.TraceId, Is.EqualTo(traceId));
            Assert.That(nextCalled, Is.False);
        });
    }
}
