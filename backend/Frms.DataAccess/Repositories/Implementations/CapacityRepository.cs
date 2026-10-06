using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class CapacityRepository(
    FrmsDbContext dbContext) : ICapacityRepository
{
    public Task<string?> GetFacilityStatusAsync(
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        return dbContext.Facilities
            .AsNoTracking()
            .Where(x => x.FacilityId == facilityId)
            .Select(x => x.Status)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<int> CountUnitTypesAsync(
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        return UnitTypesAtFacility(facilityId)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UnitType>> ListUnitTypesAsync(
        Guid facilityId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        return await UnitTypesAtFacility(facilityId)
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ThenBy(x => x.UnitTypeId)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetAvailableCapacitiesAsync(
        Guid facilityId,
        IReadOnlyCollection<Guid> unitTypeIds,
        DateOnly startMonth,
        DateOnly endMonth,
        CancellationToken cancellationToken)
    {
        var ids = unitTypeIds.Distinct().ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<Guid, int>();
        }

        // MAINTENANCE units are removed from rentable capacity.
        // IN_USE units stay in the physical-capacity base and are
        // deducted below through ACTIVE Contract occupancy.
        var eligibleCapacity = await dbContext.StorageUnits
            .AsNoTracking()
            .Where(x =>
                x.FacilityId == facilityId &&
                ids.Contains(x.UnitTypeId) &&
                x.Status != "MAINTENANCE")
            .GroupBy(x => x.UnitTypeId)
            .Select(g => new
            {
                UnitTypeId = g.Key,
                Count = g.Count()
            })
            .ToDictionaryAsync(
                x => x.UnitTypeId,
                x => x.Count,
                cancellationToken);

        // Reservation capacity holds.
        // COMPLETED/CANCELLED no longer hold capacity.
        var reservationHolds = await dbContext.Reservations
            .AsNoTracking()
            .Where(x =>
                x.FacilityId == facilityId &&
                ids.Contains(x.UnitTypeId) &&
                (x.Status == "PENDING_DEPOSIT" ||
                 x.Status == "CONFIRMED") &&
                x.StartMonth <= endMonth &&
                x.EndMonth >= startMonth)
            .Select(x => new CapacityRangeRow(
                x.UnitTypeId,
                x.StartMonth,
                x.EndMonth))
            .ToListAsync(cancellationToken);

        // ACTIVE Contract is the actual occupancy source after handover.
        var contractOccupancies = await (
                from contract in dbContext.Contracts.AsNoTracking()
                join unit in dbContext.StorageUnits.AsNoTracking()
                    on contract.StorageUnitId equals unit.StorageUnitId
                where contract.Status == "ACTIVE"
                      && contract.FacilityId == facilityId
                      && ids.Contains(unit.UnitTypeId)
                      && contract.StartMonth <= endMonth
                      && contract.EndMonth >= startMonth
                select new CapacityRangeRow(
                    unit.UnitTypeId,
                    contract.StartMonth,
                    contract.EndMonth))
            .ToListAsync(cancellationToken);

        var result = new Dictionary<Guid, int>();

        foreach (var unitTypeId in ids)
        {
            var physicalCapacity =
                eligibleCapacity.GetValueOrDefault(unitTypeId, 0);

            var minimumAvailable = int.MaxValue;

            var month = startMonth;

            while (month <= endMonth)
            {
                var heldReservations = reservationHolds.Count(x =>
                    x.UnitTypeId == unitTypeId &&
                    x.StartMonth <= month &&
                    x.EndMonth >= month);

                var occupiedContracts = contractOccupancies.Count(x =>
                    x.UnitTypeId == unitTypeId &&
                    x.StartMonth <= month &&
                    x.EndMonth >= month);

                var available =
                    physicalCapacity
                    - heldReservations
                    - occupiedContracts;

                minimumAvailable = Math.Min(
                    minimumAvailable,
                    Math.Max(available, 0));

                if (month == endMonth)
                {
                    break;
                }

                month = month.AddMonths(1);
            }

            result[unitTypeId] =
                minimumAvailable == int.MaxValue
                    ? 0
                    : minimumAvailable;
        }

        return result;
    }

    private IQueryable<UnitType> UnitTypesAtFacility(Guid facilityId)
    {
        return dbContext.UnitTypes.Where(unitType =>
            dbContext.StorageUnits.Any(unit =>
                unit.FacilityId == facilityId &&
                unit.UnitTypeId == unitType.UnitTypeId));
    }

    private sealed record CapacityRangeRow(
        Guid UnitTypeId,
        DateOnly StartMonth,
        DateOnly EndMonth);
}