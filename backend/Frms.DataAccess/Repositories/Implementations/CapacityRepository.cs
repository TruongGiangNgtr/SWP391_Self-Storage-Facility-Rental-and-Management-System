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

        // Both SQL queries above are restricted to the selected unit types and
        // requested period. A sweep over interval boundaries avoids re-scanning
        // every reservation/contract for every month and every unit type.
        var reservationHoldsByType = reservationHolds.ToLookup(x => x.UnitTypeId);
        var contractOccupanciesByType = contractOccupancies.ToLookup(x => x.UnitTypeId);
        var monthCount = (endMonth.Year - startMonth.Year) * 12
            + endMonth.Month - startMonth.Month + 1;
        var result = new Dictionary<Guid, int>();

        foreach (var unitTypeId in ids)
        {
            var physicalCapacity = eligibleCapacity.GetValueOrDefault(unitTypeId, 0);
            var reservationDeltas = new int[monthCount + 1];
            var contractDeltas = new int[monthCount + 1];

            foreach (var range in reservationHoldsByType[unitTypeId])
            {
                AddRange(reservationDeltas, startMonth, range, monthCount);
            }
            foreach (var range in contractOccupanciesByType[unitTypeId])
            {
                AddRange(contractDeltas, startMonth, range, monthCount);
            }

            var held = 0;
            var occupied = 0;
            var minimumAvailable = int.MaxValue;
            for (var index = 0; index < monthCount; index++)
            {
                held += reservationDeltas[index];
                occupied += contractDeltas[index];
                minimumAvailable = Math.Min(minimumAvailable,
                    Math.Max(physicalCapacity - held - occupied, 0));
            }
            result[unitTypeId] = minimumAvailable == int.MaxValue ? 0 : minimumAvailable;
        }

        return result;
    }

    private static void AddRange(int[] deltas, DateOnly startMonth,
        CapacityRangeRow range, int monthCount)
    {
        var startIndex = Math.Max(0, (range.StartMonth.Year - startMonth.Year) * 12
            + range.StartMonth.Month - startMonth.Month);
        var endIndex = Math.Min(monthCount - 1, (range.EndMonth.Year - startMonth.Year) * 12
            + range.EndMonth.Month - startMonth.Month);
        if (startIndex > endIndex) return;
        deltas[startIndex]++;
        deltas[endIndex + 1]--;
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