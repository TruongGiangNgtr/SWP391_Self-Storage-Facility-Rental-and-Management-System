using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Services.Interfaces;

namespace Frms.Business.Services.Implementations;

public sealed class FacilityAuthorizationService(
    ICurrentUserContext currentUser,
    IAuthenticationService authenticationService)
    : IFacilityAuthorizationService {
    public async Task<Guid> GetAssignedFacilityIdAsync(
        CancellationToken cancellationToken = default) {
        if (!currentUser.IsAuthenticated) {
            throw new BusinessException(
                "UNAUTHORIZED",
                "Authentication is required.",
                401);
        }

        var account =
            await authenticationService.GetCurrentAccountAsync(
                currentUser.UserAccountId,
                cancellationToken);

        if (account.FacilityId is null) {
            throw new BusinessException(
                "FORBIDDEN",
                "You are not assigned to a facility.",
                403);
        }

        return account.FacilityId.Value;
    }

    public async Task EnsureSameFacilityAsync(
        Guid targetFacilityId,
        CancellationToken cancellationToken = default) {
        var assignedFacilityId =
            await GetAssignedFacilityIdAsync(
                cancellationToken);

        if (assignedFacilityId != targetFacilityId) {
            throw new BusinessException(
                "FORBIDDEN",
                "You are not authorized to access this facility.",
                403);
        }
    }
}
