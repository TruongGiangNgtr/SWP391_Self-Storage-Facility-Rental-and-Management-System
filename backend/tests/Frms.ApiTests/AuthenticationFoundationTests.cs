using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Frms.Api.Authentication;
using Frms.Api.DTOs.Requests;
using Frms.Api.Mapping;
using Frms.Api.Middleware;
using Frms.Business.Abstractions.Time;
using Frms.Business.Exceptions;
using Frms.Business.Models.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Frms.ApiTests;

[TestFixture]
public sealed class AuthenticationFoundationTests
{
    [Test]
    public void BCryptPasswordHasher_ConfiguredWorkFactor_HashesAndVerifiesWithoutPersistingPlaintext()
    {
        var hasher = new BCryptPasswordHasher(Options.Create(new BCryptOptions { WorkFactor = 12 }));
        var hash = hasher.Hash("correct-horse-battery-staple");

        Assert.Multiple(() =>
        {
            Assert.That(hash, Is.Not.EqualTo("correct-horse-battery-staple"));
            Assert.That(hasher.Verify("correct-horse-battery-staple", hash), Is.True);
            Assert.That(hasher.Verify("wrong-password", hash), Is.False);
        });
    }

    [Test]
    public void JwtTokenService_ValidOptions_EmitsSubjectRoleAndConfiguredExpiry()
    {
        var now = new DateTimeOffset(2026, 10, 4, 0, 0, 0, TimeSpan.Zero);
        var accountId = Guid.NewGuid();
        var service = new JwtTokenService(
            Options.Create(new JwtOptions
            {
                Issuer = "frms-tests",
                Audience = "frms-tests",
                SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                LifetimeMinutes = 30,
            }),
            new FixedClock(now));

        var issued = service.Generate(accountId, "CUSTOMER");
        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.AccessToken);

        Assert.Multiple(() =>
        {
            Assert.That(issued.ExpiresAt, Is.EqualTo(now.AddMinutes(30)));
            Assert.That(token.Issuer, Is.EqualTo("frms-tests"));
            Assert.That(token.Audiences, Does.Contain("frms-tests"));
            Assert.That(token.Subject, Is.EqualTo(accountId.ToString()));
            Assert.That(token.Claims, Has.Some.Matches<Claim>(claim => claim.Type == ClaimTypes.Role && claim.Value == "CUSTOMER"));
        });
    }

    [Test]
    public async Task GlobalExceptionHandler_BusinessException_WritesStableSafeErrorEnvelope()
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-phase0";
        context.Response.Body = new MemoryStream();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);

        await handler.TryHandleAsync(context, new BusinessException("AUTH_INVALID_CREDENTIALS", "Invalid credentials.", StatusCodes.Status401Unauthorized), CancellationToken.None);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        Assert.Multiple(() =>
        {
            Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
            Assert.That(body, Does.Contain("AUTH_INVALID_CREDENTIALS"));
            Assert.That(body, Does.Contain("trace-phase0"));
        });
    }

    [Test]
    public void AuthenticationMapping_TransportTypes_MapOnlyAtApiBoundary()
    {
        var command = new CustomerLoginRequest { PhoneNumber = "+84900000000", Password = "correct-horse-battery-staple" }.ToCommand("127.0.0.1", "NUnit");
        var result = new AuthenticationResult("jwt-token", "Bearer", DateTimeOffset.UtcNow, new AuthenticatedUserResult(Guid.NewGuid(), "CUSTOMER", "ACTIVE"));
        var response = result.ToResponse();

        Assert.Multiple(() =>
        {
            Assert.That(command.PhoneNumber, Is.EqualTo("+84900000000"));
            Assert.That(command.IpAddress, Is.EqualTo("127.0.0.1"));
            Assert.That(response.Data.AccessToken, Is.EqualTo("jwt-token"));
            Assert.That(response.Data.User.Role, Is.EqualTo("CUSTOMER"));
        });
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => utcNow;
        public TimeZoneInfo BusinessTimeZone => TimeZoneInfo.Utc;
        public DateTimeOffset ToBusinessTime(DateTimeOffset timestamp) => timestamp;
    }
}
