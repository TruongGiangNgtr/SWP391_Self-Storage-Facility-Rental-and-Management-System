using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Frms.Business.Abstractions.Security;
using Frms.Business.Abstractions.Time;
using Frms.Business.Models.Results;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Frms.Api.Authentication;

internal sealed class JwtTokenService(IOptions<JwtOptions> options, IClock clock) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public IssuedToken Generate(Guid userAccountId, string role)
    {
        var now = clock.UtcNow;
        var expiresAt = now.AddMinutes(_options.LifetimeMinutes);
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, userAccountId.ToString()), new Claim("userAccountId", userAccountId.ToString()), new Claim(ClaimTypes.Role, role), new Claim("role", role) };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, now.UtcDateTime, expiresAt.UtcDateTime, credentials);
        return new IssuedToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
