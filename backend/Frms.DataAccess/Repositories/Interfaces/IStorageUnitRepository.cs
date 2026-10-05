namespace Frms.DataAccess.Repositories.Interfaces;

public interface IStorageUnitRepository {
    Task<bool> FacilityExistsAsync(
        Guid facilityId,
        CancellationToken cancellationToken);

    Task<bool> UnitTypeExistsAsync(
        Guid unitTypeId,
        CancellationToken cancellationToken);

    Task<Guid> CreateAsync(
        Guid facilityId,
        Guid unitTypeId,
        string unitCode,
        string? locationInfo,
        CancellationToken cancellationToken);
}
