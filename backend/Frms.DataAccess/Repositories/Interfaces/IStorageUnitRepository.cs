using Frms.DataAccess.Persistence.Entities;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IStorageUnitRepository {
    Task<bool> FacilityExistsAsync(
        Guid facilityId,
        CancellationToken cancellationToken);

    Task<bool> UnitTypeExistsAsync(
        Guid unitTypeId,
        CancellationToken cancellationToken);

    Task<bool> UnitCodeExistsAsync(
        Guid facilityId,
        string unitCode,
        CancellationToken cancellationToken);

    Task<Guid> CreateAsync(
        Guid facilityId,
        Guid unitTypeId,
        string unitCode,
        string? locationInfo,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<StorageUnit> Items, int TotalCount)>
        ListByFacilityAsync(
            Guid facilityId,
            int page,
            int pageSize,
            CancellationToken cancellationToken);

    Task<StorageUnit?> FindByIdAsync(
        Guid storageUnitId,
        CancellationToken cancellationToken);

    Task<bool> HasActiveContractAsync(
        Guid storageUnitId,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        Guid storageUnitId,
        Guid unitTypeId,
        string? locationInfo,
        CancellationToken cancellationToken);

    Task UpdateStatusAsync(
        Guid storageUnitId,
        string status,
        CancellationToken cancellationToken);
}
