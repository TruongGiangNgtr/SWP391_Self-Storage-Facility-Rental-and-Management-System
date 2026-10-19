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

    // Keep the filter as an EF entity query. Constructor projections are only
    // created after SQL filtering, ordering, pagination and metadata joins.
    private IQueryable<Frms.DataAccess.Persistence.Entities.StorageUnit> Units(
        Guid facilityId, Guid? unitTypeId)
    {
        var query = db.StorageUnits.AsNoTracking()
            .Where(storage => storage.FacilityId == facilityId);
        if (unitTypeId.HasValue)
            query = query.Where(storage => storage.UnitTypeId == unitTypeId.Value);
        return query;
    }

    public async Task<(IReadOnlyList<StorageUnitTimelineUnitRecord> Items, int TotalCount)> ListUnitsAsync(
        Guid facilityId, Guid? unitTypeId, int skip, int take, CancellationToken ct)
    {
        var query = Units(facilityId, unitTypeId);
        var total = await query.CountAsync(ct);

        // Order and paginate *before* projection. PostgreSQL gets LIMIT/OFFSET
        // for physical units, not for their status-history intervals.
        var page = query.OrderBy(storage => storage.UnitCode)
            .ThenBy(storage => storage.StorageUnitId)
            .Skip(skip).Take(take);
        var rows = await (from storage in page
                          join type in db.UnitTypes.AsNoTracking()
                              on storage.UnitTypeId equals type.UnitTypeId
                          join facility in db.Facilities.AsNoTracking()
                              on storage.FacilityId equals facility.FacilityId
                          select new
                          {
                              storage.StorageUnitId,
                              storage.UnitCode,
                              UnitTypeName = type.Name,
                              FacilityName = facility.Name,
                              storage.LocationInfo
                          }).ToArrayAsync(ct);

        return (rows.Select(x => new StorageUnitTimelineUnitRecord(
            x.StorageUnitId, x.UnitCode, x.UnitTypeName,
            x.FacilityName, x.LocationInfo)).ToArray(), total);
    }

    public async Task<StorageUnitTimelineUnitRecord?> FindUnitAsync(
        Guid facilityId, Guid storageUnitId, CancellationToken ct)
    {
        // Filter before projecting to avoid EF trying to translate a record
        // constructor's StorageUnitId back into SQL.
        var rows = from storage in Units(facilityId, null)
                       .Where(storage => storage.StorageUnitId == storageUnitId)
                   join type in db.UnitTypes.AsNoTracking()
                       on storage.UnitTypeId equals type.UnitTypeId
                   join facility in db.Facilities.AsNoTracking()
                       on storage.FacilityId equals facility.FacilityId
                   select new
                   {
                       storage.StorageUnitId,
                       storage.UnitCode,
                       UnitTypeName = type.Name,
                       FacilityName = facility.Name,
                       storage.LocationInfo
                   };
        var row = await rows.SingleOrDefaultAsync(ct);
        return row is null ? null : new StorageUnitTimelineUnitRecord(
            row.StorageUnitId, row.UnitCode, row.UnitTypeName,
            row.FacilityName, row.LocationInfo);
    }

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
                .ThenByDescending(x => x.AuditLogId).First())
            .ToArrayAsync(ct);

        var current = await audit.Where(x => x.CreatedAt >= fromUtc && x.CreatedAt < toUtc)
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.AuditLogId)
            .ToArrayAsync(ct);
        // Both database queries are bounded to the requested units and time
        // range. Map to records only after materialization, never mid-query.
        return prior.Concat(current).Select(x => new StorageUnitStatusAuditRecord(
            x.AuditLogId, x.EntityId!.Value, x.Action,
            x.OldValue, x.NewValue, x.CreatedAt)).ToArray();
    }
}
