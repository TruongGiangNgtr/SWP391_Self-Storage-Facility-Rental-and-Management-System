using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IStorageUnitTimelineRepository
{
    Task<bool> FacilityExistsAsync(Guid facilityId, CancellationToken ct);
    Task<bool> UnitTypeExistsAsync(Guid unitTypeId, CancellationToken ct);
    Task<(IReadOnlyList<StorageUnitTimelineUnitRecord> Items, int TotalCount)> ListUnitsAsync(
        Guid facilityId, Guid? unitTypeId, int skip, int take, CancellationToken ct);
    Task<StorageUnitTimelineUnitRecord?> FindUnitAsync(
        Guid facilityId, Guid storageUnitId, CancellationToken ct);
    Task<IReadOnlyList<StorageUnitStatusAuditRecord>> ListStatusEventsAsync(
        Guid[] storageUnitIds, DateTime fromUtc, DateTime toUtc, CancellationToken ct);
}
