using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class StorageUnitTimelineRepository(FrmsDbContext db) : IStorageUnitTimelineRepository
{
    public Task<bool> FacilityExistsAsync(Guid facilityId, CancellationToken ct) =>
        db.Facilities.AsNoTracking().AnyAsync(x => x.FacilityId == facilityId, ct);

    public Task<bool> UnitTypeExistsAsync(Guid unitTypeId, CancellationToken ct) =>
        db.UnitTypes.AsNoTracking().AnyAsync(x => x.UnitTypeId == unitTypeId, ct);

    private IQueryable<StorageUnitTimelineUnitRecord> Units(Guid facilityId, Guid? unitTypeId)
    {
        var query = from storage in db.StorageUnits.AsNoTracking()
                    join type in db.UnitTypes.AsNoTracking() on storage.UnitTypeId equals type.UnitTypeId
                    join facility in db.Facilities.AsNoTracking() on storage.FacilityId equals facility.FacilityId
                    where storage.FacilityId == facilityId
                    select new { storage, type, facility };
        if (unitTypeId.HasValue) query = query.Where(x => x.storage.UnitTypeId == unitTypeId.Value);
        return query.Select(x => new StorageUnitTimelineUnitRecord(
            x.storage.StorageUnitId, x.storage.UnitCode, x.type.Name,
            x.facility.Name, x.storage.LocationInfo));
    }

    public async Task<(IReadOnlyList<StorageUnitTimelineUnitRecord> Items, int TotalCount)> ListUnitsAsync(
        Guid facilityId, Guid? unitTypeId, int skip, int take, CancellationToken ct)
    {
        var query = Units(facilityId, unitTypeId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.UnitCode).ThenBy(x => x.StorageUnitId)
            .Skip(skip).Take(take).ToArrayAsync(ct);
        return (items, total);
    }

    public Task<StorageUnitTimelineUnitRecord?> FindUnitAsync(
        Guid facilityId, Guid storageUnitId, CancellationToken ct) =>
        Units(facilityId, null).SingleOrDefaultAsync(x => x.StorageUnitId == storageUnitId, ct);

    public async Task<IReadOnlyList<StorageUnitStatusAuditRecord>> ListStatusEventsAsync(
        Guid[] storageUnitIds, DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        if (storageUnitIds.Length == 0) return [];
        var audit = db.AuditLogs.AsNoTracking().Where(x =>
            x.EntityType == "STORAGE_UNIT" && x.EntityId.HasValue &&
            storageUnitIds.Contains(x.EntityId.Value) &&
            (x.Action == "CREATE_STORAGE_UNIT" || x.Action == "CHANGE_STORAGE_UNIT_STATUS"));

        // Exactly one latest event before the month for each unit supplies a verified baseline.
        // This is a bounded, server-side grouped query rather than loading entire audit history.
        var prior = await audit.Where(x => x.CreatedAt < fromUtc)
            .GroupBy(x => x.EntityId)
            .Select(g => g.OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.AuditLogId)
                .Select(x => new StorageUnitStatusAuditRecord(
                    x.AuditLogId, x.EntityId!.Value, x.Action,
                    x.OldValue, x.NewValue, x.CreatedAt)).First())
            .ToArrayAsync(ct);

        var current = await audit.Where(x => x.CreatedAt >= fromUtc && x.CreatedAt < toUtc)
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.AuditLogId)
            .Select(x => new StorageUnitStatusAuditRecord(
                x.AuditLogId, x.EntityId!.Value, x.Action,
                x.OldValue, x.NewValue, x.CreatedAt))
            .ToArrayAsync(ct);
        return prior.Concat(current).ToArray();
    }
}
