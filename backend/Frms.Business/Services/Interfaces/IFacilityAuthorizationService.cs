namespace Frms.Business.Services.Interfaces;

public interface IFacilityAuthorizationService {
    Task EnsureSameFacilityAsync(
        Guid targetFacilityId,
        CancellationToken cancellationToken = default);
}
