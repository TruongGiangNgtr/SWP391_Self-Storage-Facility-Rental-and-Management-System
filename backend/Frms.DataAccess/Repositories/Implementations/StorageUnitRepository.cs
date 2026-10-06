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
}
