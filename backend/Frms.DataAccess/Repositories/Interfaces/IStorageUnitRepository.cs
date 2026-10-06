namespace Frms.DataAccess.Repositories.Interfaces;

using Frms.DataAccess.Persistence.Entities;

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

    Task<(IReadOnlyList<StorageUnit> Items, int TotalCount)> ListByFacilityAsync(
        Guid facilityId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<StorageUnit?> FindByIdAsync(
        Guid storageUnitId,
        CancellationToken cancellationToken);
}
