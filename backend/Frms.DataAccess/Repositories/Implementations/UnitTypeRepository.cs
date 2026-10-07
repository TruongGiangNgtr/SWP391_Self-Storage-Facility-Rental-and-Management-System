using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class UnitTypeRepository(
    FrmsDbContext dbContext) : IUnitTypeRepository {
    public async Task<(IReadOnlyList<UnitType> Items, int TotalItems)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) {
        var query = dbContext.UnitTypes
            .AsNoTracking();

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.UnitTypeId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public Task<UnitType?> GetByIdAsync(
        Guid unitTypeId,
        CancellationToken cancellationToken = default)
        => dbContext.UnitTypes.FirstOrDefaultAsync(
            x => x.UnitTypeId == unitTypeId,
            cancellationToken);

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default) {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(
    IReadOnlyList<UnitType> Items,
    int TotalItems,
    string? FacilityStatus)> GetFacilityPagedAsync(
        Guid facilityId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) {
        var facilityStatus = await dbContext.Facilities
            .AsNoTracking()
            .Where(x => x.FacilityId == facilityId)
            .Select(x => x.Status)
            .SingleOrDefaultAsync(cancellationToken);

        if (facilityStatus is null) {
            return (
                Array.Empty<UnitType>(),
                0,
                null);
        }

        var query = dbContext.UnitTypes
            .AsNoTracking()
            .Where(unitType =>
                dbContext.StorageUnits.Any(storageUnit =>
                    storageUnit.FacilityId == facilityId &&
                    storageUnit.UnitTypeId == unitType.UnitTypeId));

        var totalItems = await query.CountAsync(
            cancellationToken);

        var items = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.UnitTypeId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (
            items,
            totalItems,
            facilityStatus);
    }
}
