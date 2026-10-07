namespace Frms.Business.Services.Interfaces;

public interface IFacilityAuthorizationService {
    Task<Guid> GetAssignedFacilityIdAsync(
        CancellationToken cancellationToken = default);

    Task EnsureSameFacilityAsync(
        Guid targetFacilityId,
        CancellationToken cancellationToken = default);
}
