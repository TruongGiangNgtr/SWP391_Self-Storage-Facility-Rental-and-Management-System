using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Frms.DataAccess.Repositories.Models;

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

    public async Task<IReadOnlyList<StorageUnitRecord>> ListByFacilityAsync(
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        return await dbContext.StorageUnits
            .AsNoTracking()
            .Where(x => x.FacilityId == facilityId)
            .OrderBy(x => x.UnitCode)
            .Select(x => new StorageUnitRecord(
                x.StorageUnitId,
                x.FacilityId,
                x.UnitTypeId,
                x.UnitCode,
                x.LocationInfo,
                x.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<StorageUnitRecord?> GetByIdAsync(
        Guid storageUnitId,
        CancellationToken cancellationToken)
    {
        return await dbContext.StorageUnits
            .AsNoTracking()
            .Where(x => x.StorageUnitId == storageUnitId)
            .Select(x => new StorageUnitRecord(
                x.StorageUnitId,
                x.FacilityId,
                x.UnitTypeId,
                x.UnitCode,
                x.LocationInfo,
                x.Status))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
