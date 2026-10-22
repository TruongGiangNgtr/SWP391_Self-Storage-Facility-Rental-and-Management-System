using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class FacilityCatalogRepository(
    FrmsDbContext dbContext)
    : IFacilityCatalogRepository {
    public async Task<(IReadOnlyList<FacilityCatalogRecord> Items, int TotalItems)> SearchActiveAsync(
        string? location, Guid? facilityId, string? mode, string? size, Guid? unitTypeId,
        bool descending, int? skip, int? take, CancellationToken cancellationToken)
    {
        var query = dbContext.Facilities.AsNoTracking().Where(x => x.Status == "ACTIVE");
        if (location is not null)
        {
            var needle = location.ToLowerInvariant();
            // Literal substring, not LIKE wildcard matching.
            query = query.Where(x => x.Name.ToLower().Contains(needle) ||
                                     x.Address.ToLower().Contains(needle));
        }
        if (facilityId.HasValue)
            query = query.Where(x => x.FacilityId == facilityId.Value);
        if (mode is not null || size is not null || unitTypeId.HasValue)
        {
            query = query.Where(f => dbContext.StorageUnits.Any(u =>
                u.FacilityId == f.FacilityId && dbContext.UnitTypes.Any(t =>
                    t.UnitTypeId == u.UnitTypeId &&
                    (mode == null || t.Mode == mode) &&
                    // Size uses the canonical frms_ci_as case-insensitive collation.
                    // Avoid LOWER(column) so a suitable index can be considered.
                    (size == null || t.Size == size) &&
                    (!unitTypeId.HasValue || t.UnitTypeId == unitTypeId.Value))));
        }

        var total = await query.CountAsync(cancellationToken);
        var ordered = descending
            ? query.OrderByDescending(x => x.Name).ThenBy(x => x.FacilityId)
            : query.OrderBy(x => x.Name).ThenBy(x => x.FacilityId);
        IQueryable<Frms.DataAccess.Persistence.Entities.Facility> paged = ordered;
        if (skip.HasValue) paged = paged.Skip(skip.Value);
        if (take.HasValue) paged = paged.Take(take.Value);

        var rows = await paged.Select(x => new FacilityCatalogRecord(
            x.FacilityId, x.Name, x.Address, x.ContactInfo, x.Description, x.Status))
            .ToArrayAsync(cancellationToken);
        return (rows, total);
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> GetMatchingOfferedUnitTypesAsync(
        IReadOnlyCollection<Guid> facilityIds, string? mode, string? size, Guid? unitTypeId,
        CancellationToken cancellationToken)
    {
        var ids = facilityIds.Distinct().ToArray();
        if (ids.Length == 0) return new Dictionary<Guid, IReadOnlyList<Guid>>();

        var rows = await (
            from unit in dbContext.StorageUnits.AsNoTracking()
            join type in dbContext.UnitTypes.AsNoTracking()
                on unit.UnitTypeId equals type.UnitTypeId
            where ids.Contains(unit.FacilityId) &&
                  (mode == null || type.Mode == mode) &&
                  (size == null || type.Size == size) &&
                  (!unitTypeId.HasValue || type.UnitTypeId == unitTypeId.Value)
            select new { unit.FacilityId, type.UnitTypeId })
            .Distinct()
            .ToArrayAsync(cancellationToken);

        return rows.GroupBy(x => x.FacilityId).ToDictionary(
            group => group.Key,
            group => (IReadOnlyList<Guid>)group.Select(x => x.UnitTypeId).ToArray());
    }

    public async Task<(
        IReadOnlyList<FacilityCatalogRecord> Items,
        int TotalItems)> GetActivePagedAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default) {
        var query = dbContext.Facilities
            .AsNoTracking()
            .Where(x => x.Status == "ACTIVE");

        var totalItems =
            await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.FacilityId)
            .Skip(skip)
            .Take(take)
            .Select(x => new FacilityCatalogRecord(
                x.FacilityId,
                x.Name,
                x.Address,
                x.ContactInfo,
                x.Description,
                x.Status))
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public Task<FacilityCatalogRecord?> GetActiveByIdAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default) {
        return dbContext.Facilities
            .AsNoTracking()
            .Where(x =>
                x.FacilityId == facilityId &&
                x.Status == "ACTIVE")
            .Select(x => new FacilityCatalogRecord(
                x.FacilityId,
                x.Name,
                x.Address,
                x.ContactInfo,
                x.Description,
                x.Status))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
