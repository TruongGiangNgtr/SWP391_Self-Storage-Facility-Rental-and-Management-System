using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Frms.Api.Authentication;
using Frms.Api.Authorization;
using Frms.Business.Abstractions.Time;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Frms.ApiTests;

[TestFixture]
public sealed class JwtTokenServiceMatrix2301Tests
{
    private const string SigningKey = "batch-five-signing-key-must-be-at-least-32-bytes-long";

    public static IEnumerable<TestCaseData> Cases()
    {
        var id = 2301;
        for (var i = 0; i < 100; i++)
        {
            var userId = Guid.Parse($"{i + 1:D8}-abcd-1234-5678-{i + 1:D12}");
            var role = RoleNames.All[i % RoleNames.All.Count];
            var lifetime = new[] { 1, 5, 15, 30, 60, 120, 1440 }[i % 7];
            yield return new TestCaseData(userId, role, lifetime).SetName($"TC_{id++}_jwt_claims_and_lifetime_{i + 1:000}");
        }
        if (id != 2401) throw new InvalidOperationException($"Expected 100 cases; generated {id - 2301}.");
    }

    [TestCaseSource(nameof(Cases))]
    public void Generate_EmitsExpectedSubjectRoleIssuerAudienceAndExpiry(Guid userId, string role, int lifetimeMinutes)
    {
        var now = new DateTimeOffset(2026, 10, 22, 3, 0, 0, TimeSpan.Zero);
        var clock = new FixedClock(now);
        var options = Options.Create(new JwtOptions
        {
            Issuer = "frms-matrix-issuer",
            Audience = "frms-matrix-audience",
            SigningKey = SigningKey,
            LifetimeMinutes = lifetimeMinutes
        });
        var issued = new JwtTokenService(options, clock).Generate(userId, role);
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(issued.AccessToken);

        Assert.Multiple(() =>
        {
            Assert.That(issued.AccessToken, Is.Not.Empty);
            Assert.That(issued.ExpiresAt, Is.EqualTo(now.AddMinutes(lifetimeMinutes)));
            Assert.That(token.Issuer, Is.EqualTo("frms-matrix-issuer"));
            Assert.That(token.Audiences, Does.Contain("frms-matrix-audience"));
            Assert.That(token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Sub).Value, Is.EqualTo(userId.ToString()));
            Assert.That(token.Claims.Single(claim => claim.Type == "userAccountId").Value, Is.EqualTo(userId.ToString()));
            Assert.That(token.Claims.Count(claim => (claim.Type == "role" || claim.Type == ClaimTypes.Role) && claim.Value == role), Is.GreaterThanOrEqualTo(1));
            Assert.That(token.ValidTo, Is.EqualTo(now.AddMinutes(lifetimeMinutes).UtcDateTime).Within(TimeSpan.FromSeconds(1)));
            Assert.That(token.Header.Alg, Is.EqualTo(SecurityAlgorithms.HmacSha256));
        });

        var validation = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "frms-matrix-issuer",
            ValidateAudience = true,
            ValidAudience = "frms-matrix-audience",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            ValidateLifetime = false,
            ClockSkew = TimeSpan.Zero
        };
        Assert.DoesNotThrow(() => handler.ValidateToken(issued.AccessToken, validation, out _));
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
        public TimeZoneInfo BusinessTimeZone => TimeZoneInfo.Utc;
        public DateTimeOffset ToBusinessTime(DateTimeOffset utcTimestamp) => utcTimestamp;
    }
}
