using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class StorageUnitRepository(
    FrmsDbContext dbContext) : IStorageUnitRepository {
    public Task<bool> FacilityExistsAsync(
        Guid facilityId,
        CancellationToken cancellationToken) =>
        dbContext.Facilities
            .AsNoTracking()
            .AnyAsync(
                x => x.FacilityId == facilityId,
                cancellationToken);

    public Task<bool> UnitTypeExistsAsync(
        Guid unitTypeId,
        CancellationToken cancellationToken) =>
        dbContext.UnitTypes
            .AsNoTracking()
            .AnyAsync(
                x => x.UnitTypeId == unitTypeId,
                cancellationToken);

    public Task<bool> UnitCodeExistsAsync(
        Guid facilityId,
        string unitCode,
        CancellationToken cancellationToken) =>
        dbContext.StorageUnits
            .AsNoTracking()
            .AnyAsync(
                x => x.FacilityId == facilityId
                     && x.UnitCode == unitCode,
                cancellationToken);

    public async Task<Guid> CreateAsync(
        Guid facilityId,
        Guid unitTypeId,
        string unitCode,
        string? locationInfo,
        CancellationToken cancellationToken) {
        var storageUnit = new StorageUnit {
            StorageUnitId = Guid.NewGuid(),
            FacilityId = facilityId,
            UnitTypeId = unitTypeId,
            UnitCode = unitCode,
            LocationInfo = locationInfo,
            Status = "AVAILABLE"
        };

        dbContext.StorageUnits.Add(storageUnit);

        await dbContext.SaveChangesAsync(cancellationToken);

        return storageUnit.StorageUnitId;
    }

    public async Task<(IReadOnlyList<StorageUnit> Items, int TotalCount)> ListByFacilityAsync(
            Guid facilityId,
            int page,
            int pageSize,
            CancellationToken cancellationToken) {
        var query = dbContext.StorageUnits
            .AsNoTracking()
            .Where(x => x.FacilityId == facilityId);

        var totalCount =
            await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.UnitCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<StorageUnit?> FindByIdAsync(
        Guid storageUnitId,
        CancellationToken cancellationToken) =>
        dbContext.StorageUnits
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.StorageUnitId == storageUnitId,
                cancellationToken);

    public Task<bool> HasActiveContractAsync(
        Guid storageUnitId,
        CancellationToken cancellationToken) =>
        dbContext.Contracts
            .AsNoTracking()
            .AnyAsync(
                x => x.StorageUnitId == storageUnitId
                     && x.Status == "ACTIVE",
                cancellationToken);

    public async Task UpdateAsync(
        Guid storageUnitId,
        Guid unitTypeId,
        string? locationInfo,
        CancellationToken cancellationToken) {
        var storageUnit = await dbContext.StorageUnits
            .SingleAsync(
                x => x.StorageUnitId == storageUnitId,
                cancellationToken);

        storageUnit.UnitTypeId = unitTypeId;
        storageUnit.LocationInfo = locationInfo;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(
        Guid storageUnitId,
        string status,
        CancellationToken cancellationToken) {
        var storageUnit = await dbContext.StorageUnits
            .SingleAsync(
                x => x.StorageUnitId == storageUnitId,
                cancellationToken);

        storageUnit.Status = status;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
