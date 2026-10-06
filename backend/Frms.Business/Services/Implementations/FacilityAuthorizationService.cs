using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Services.Interfaces;

namespace Frms.Business.Services.Implementations;

public sealed class FacilityAuthorizationService(
    ICurrentUserContext currentUser,
    IAuthenticationService authenticationService)
    : IFacilityAuthorizationService {
    public async Task EnsureSameFacilityAsync(
        Guid targetFacilityId,
        CancellationToken cancellationToken = default) {
        if (!currentUser.IsAuthenticated) {
            throw new BusinessException(
                "UNAUTHORIZED",
                "Authentication is required.",
                401);
        }

        var account = await authenticationService.GetCurrentAccountAsync(
            currentUser.UserAccountId,
            cancellationToken);

        if (account.FacilityId is null ||
            account.FacilityId.Value != targetFacilityId) {
            throw new BusinessException(
                "FACILITY_ACCESS_DENIED",
                "You are not authorized to access this facility.",
                403);
        }
    }
}
