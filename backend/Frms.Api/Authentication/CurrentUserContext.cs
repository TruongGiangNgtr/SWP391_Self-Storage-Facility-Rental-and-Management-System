using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;

namespace Frms.Api.Authentication;

internal sealed class CurrentUserContext(
    IHttpContextAccessor httpContextAccessor) : ICurrentUserContext {
    private ClaimsPrincipal? User =>
        httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated == true;

    public Guid UserAccountId {
        get {
            var value = User?.FindFirstValue(
                JwtRegisteredClaimNames.Sub);

            if (!Guid.TryParse(value, out var userAccountId)) {
                throw Unauthorized();
            }

            return userAccountId;
        }
    }

    public string Role =>
        User?.FindFirstValue(ClaimTypes.Role)
        ?? throw Unauthorized();

    private static BusinessException Unauthorized()
        => new(
            "UNAUTHORIZED",
            "Authentication is required.",
            401);
}
