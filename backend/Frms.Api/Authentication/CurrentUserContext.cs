using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Frms.Business.Abstractions.Security;

namespace Frms.Api.Authentication;

internal sealed class CurrentUserContext(
    IHttpContextAccessor httpContextAccessor) : ICurrentUserContext {
    private ClaimsPrincipal? User =>
        httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated == true;

    public Guid UserAccountId {
        get {
            var value = User?.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!Guid.TryParse(value, out var userAccountId)) {
                throw new UnauthorizedAccessException(
                    "Authenticated user account claim is missing.");
            }

            return userAccountId;
        }
    }

    public string Role =>
        User?.FindFirstValue(ClaimTypes.Role)
        ?? throw new UnauthorizedAccessException(
            "Authenticated user role claim is missing.");
}
