using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class OperationalCatalogRepository(FrmsDbContext db) : IOperationalCatalogRepository
{
    public async Task<(IReadOnlyList<ExtraFeeTypeRecord> Items, int Total)> ListExtraFeeTypesAsync(
        bool activeOnly, int page, int pageSize, CancellationToken ct)
    {
        var query = db.ExtraFeeTypes.AsNoTracking().Where(x => !activeOnly || x.Status == "ACTIVE");
        var count = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.Name).ThenBy(x => x.ExtraFeeTypeId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ExtraFeeTypeRecord(x.ExtraFeeTypeId, x.Name, x.DefaultAmount, x.Status))
            .ToArrayAsync(ct);
        return (rows, count);
    }

    public async Task<ExtraFeeTypeRecord?> UpdateExtraFeeTypeAsync(
        Guid id, decimal amount, string status, CancellationToken ct)
    {
        var entity = await db.ExtraFeeTypes.SingleOrDefaultAsync(x => x.ExtraFeeTypeId == id, ct);
        if (entity is null) return null;
        entity.DefaultAmount = amount;
        entity.Status = status;
        await db.SaveChangesAsync(ct);
        return new ExtraFeeTypeRecord(entity.ExtraFeeTypeId, entity.Name, entity.DefaultAmount, entity.Status);
    }

    public async Task<(IReadOnlyList<PublicUnitTypeRecord> Items, int Total)> ListFacilityOfferedUnitTypesAsync(
        Guid facilityId, string? mode, string? size, Guid? unitTypeId,
        string sortBy, bool descending, int? skip, int? take, CancellationToken ct)
    {
        IQueryable<UnitType> query = db.UnitTypes.AsNoTracking().Where(x =>
            db.StorageUnits.Any(u => u.FacilityId == facilityId && u.UnitTypeId == x.UnitTypeId));
        if (mode is not null) query = query.Where(x => x.Mode == mode);
        if (size is not null) query = query.Where(x => x.Size.ToLower() == size);
        if (unitTypeId.HasValue) query = query.Where(x => x.UnitTypeId == unitTypeId.Value);
        var total = await query.CountAsync(ct);
        query = sortBy == "rentalPrice"
            ? (descending ? query.OrderByDescending(x => x.RentalPrice).ThenBy(x => x.UnitTypeId)
                          : query.OrderBy(x => x.RentalPrice).ThenBy(x => x.UnitTypeId))
            : (descending ? query.OrderByDescending(x => x.Name).ThenBy(x => x.UnitTypeId)
                          : query.OrderBy(x => x.Name).ThenBy(x => x.UnitTypeId));
        if (skip.HasValue) query = query.Skip(skip.Value);
        if (take.HasValue) query = query.Take(take.Value);
        var rows = await query.Select(x => new PublicUnitTypeRecord(x.UnitTypeId,
            x.Name, x.Mode, x.Size, x.RentalPrice, x.Description)).ToArrayAsync(ct);
        return (rows, total);
    }

    public async Task<(IReadOnlyList<PublicUnitTypeRecord> Items, int Total)> ListPublicUnitTypesAsync(
        string? mode, string? size, Guid? unitTypeId, string sortBy, bool descending,
        int page, int pageSize, CancellationToken ct)
    {
        // Offered type = existing physical StorageUnit at an ACTIVE Facility.
        var query = db.UnitTypes.AsNoTracking().Where(x => db.StorageUnits.Any(unit =>
            unit.UnitTypeId == x.UnitTypeId && db.Facilities.Any(f =>
                f.FacilityId == unit.FacilityId && f.Status == "ACTIVE")));
        if (mode is not null) query = query.Where(x => x.Mode == mode);
        if (size is not null) query = query.Where(x => x.Size == size);
        if (unitTypeId.HasValue) query = query.Where(x => x.UnitTypeId == unitTypeId.Value);
        var total = await query.CountAsync(ct);
        query = sortBy == "rentalPrice"
            ? (descending ? query.OrderByDescending(x => x.RentalPrice).ThenBy(x => x.UnitTypeId)
                          : query.OrderBy(x => x.RentalPrice).ThenBy(x => x.UnitTypeId))
            : (descending ? query.OrderByDescending(x => x.Name).ThenBy(x => x.UnitTypeId)
                          : query.OrderBy(x => x.Name).ThenBy(x => x.UnitTypeId));
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new PublicUnitTypeRecord(x.UnitTypeId, x.Name, x.Mode, x.Size,
                x.RentalPrice, x.Description)).ToArrayAsync(ct);
        return (rows, total);
    }
}
