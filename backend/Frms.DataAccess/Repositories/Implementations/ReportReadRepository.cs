using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class ReportReadRepository(FrmsDbContext db) : IReportReadRepository
{
    public async Task<IReadOnlyList<ReportFacilityLookup>> FacilitiesAsync(CancellationToken ct) =>
        await db.Facilities.AsNoTracking()
            .Select(f => new ReportFacilityLookup(f.FacilityId, f.Name))
            .ToArrayAsync(ct);

    public async Task<IReadOnlyList<ReportUnitTypeLookup>> UnitTypesAsync(CancellationToken ct) =>
        await db.UnitTypes.AsNoTracking()
            .Select(t => new ReportUnitTypeLookup(t.UnitTypeId, t.Name, t.Size))
            .ToArrayAsync(ct);

    public async Task<IReadOnlyList<ReportStatusGroup>> StatusGroupsAsync(
        Guid? facilityId, Guid? unitTypeId, CancellationToken ct)
    {
        var units = db.StorageUnits.AsNoTracking().AsQueryable();
        if (facilityId.HasValue) units = units.Where(u => u.FacilityId == facilityId.Value);
        if (unitTypeId.HasValue) units = units.Where(u => u.UnitTypeId == unitTypeId.Value);
        return await units.GroupBy(u => new { u.FacilityId, u.UnitTypeId })
            .Select(g => new ReportStatusGroup(g.Key.FacilityId, g.Key.UnitTypeId,
                g.Count(), g.Count(u => u.Status == "AVAILABLE"),
                g.Count(u => u.Status == "IN_USE"),
                g.Count(u => u.Status == "INSPECTION"),
                g.Count(u => u.Status == "MAINTENANCE")))
            .ToArrayAsync(ct);
    }

    public async Task<IReadOnlyList<ReportRevenueGroup>> RevenueGroupsAsync(
        Guid? facilityId, Guid? unitTypeId,
        DateOnly fromMonth, DateOnly toMonthExclusive,
        DateTime fromPaidUtc, DateTime toPaidUtcExclusive, CancellationToken ct)
    {
        // Historical UnitType belongs to Reservation, not the CURRENT physical unit.
        var rentals = from c in db.Contracts.AsNoTracking()
                      join r in db.Reservations.AsNoTracking()
                          on c.ReservationId equals r.ReservationId
                      where (!facilityId.HasValue || c.FacilityId == facilityId.Value)
                          && (!unitTypeId.HasValue || r.UnitTypeId == unitTypeId.Value)
                      select new { c.ContractId, c.FacilityId, r.UnitTypeId,
                          c.StartMonth, r.LockedRentalPrice };
        var initial = await rentals.Where(x => x.StartMonth >= fromMonth && x.StartMonth < toMonthExclusive)
            .GroupBy(x => new { x.FacilityId, x.UnitTypeId })
            .Select(g => new { g.Key.FacilityId, g.Key.UnitTypeId,
                Amount = g.Sum(x => x.LockedRentalPrice) })
            .ToArrayAsync(ct);
        var subsequent = await (from i in db.Invoices.AsNoTracking()
            join r in rentals on i.EntityId equals r.ContractId
            where i.InvoiceType == "RENTAL_FEE" && i.Status == "PAID"
                && i.BillingMonth.HasValue && i.BillingMonth.Value > r.StartMonth
                && i.PaidAt.HasValue && i.PaidAt.Value >= fromPaidUtc
                && i.PaidAt.Value < toPaidUtcExclusive
            group i by new { r.FacilityId, r.UnitTypeId } into g
            select new { g.Key.FacilityId, g.Key.UnitTypeId, Amount = g.Sum(i => i.AmountDue) })
            .ToArrayAsync(ct);
        var totals = new Dictionary<(Guid, Guid), (decimal First, decimal Later)>();
        foreach (var row in initial)
            totals[(row.FacilityId, row.UnitTypeId)] = (row.Amount, 0m);
        foreach (var row in subsequent)
        {
            var key = (row.FacilityId, row.UnitTypeId);
            totals.TryGetValue(key, out var existing);
            totals[key] = (existing.First, row.Amount);
        }
        return totals.Select(x => new ReportRevenueGroup(x.Key.Item1, x.Key.Item2,
            x.Value.First, x.Value.Later)).ToArray();
    }

    public async Task<(IReadOnlyList<ReportUnitDetail> Items, int Total)> FacilityInventoryAsync(
        Guid facilityId, int page, int pageSize, CancellationToken ct)
    {
        var units = db.StorageUnits.AsNoTracking().Where(u => u.FacilityId == facilityId);
        var total = await units.CountAsync(ct);
        var pageRows = await (from u in units.OrderBy(x => x.UnitCode)
                .ThenBy(x => x.StorageUnitId).Skip((page - 1) * pageSize).Take(pageSize)
            join t in db.UnitTypes.AsNoTracking() on u.UnitTypeId equals t.UnitTypeId
            select new { u.StorageUnitId, u.UnitCode, u.UnitTypeId, UnitTypeName = t.Name,
                UnitTypeSize = t.Size, u.LocationInfo, u.Status }).ToArrayAsync(ct);
        if (pageRows.Length == 0) return ([], total);
        var ids = pageRows.Select(x => x.StorageUnitId).ToArray();
        // One batched query; never N+1 per inventory row.
        var contracts = await (from c in db.Contracts.AsNoTracking()
            join customer in db.Customers.AsNoTracking() on c.CustomerId equals customer.CustomerId
            where ids.Contains(c.StorageUnitId) && c.Status == "ACTIVE"
            orderby c.ContractId
            select new { c.StorageUnitId, c.ContractId, c.CustomerId,
                CustomerName = customer.FullName, c.EndMonth }).ToArrayAsync(ct);
        var byUnit = contracts.GroupBy(x => x.StorageUnitId)
            .ToDictionary(g => g.Key, g => g.First());
        return (pageRows.Select(u =>
        {
            byUnit.TryGetValue(u.StorageUnitId, out var contract);
            return new ReportUnitDetail(u.StorageUnitId, u.UnitCode, u.UnitTypeId,
                u.UnitTypeName, u.UnitTypeSize, u.LocationInfo, u.Status,
                contract?.ContractId, contract?.CustomerId, contract?.CustomerName,
                contract?.EndMonth);
        }).ToArray(), total);
    }

    public Task<Guid[]> FacilityUnitIdsAsync(Guid facilityId, CancellationToken ct) =>
        db.StorageUnits.AsNoTracking().Where(u => u.FacilityId == facilityId)
            .OrderBy(u => u.StorageUnitId).Select(u => u.StorageUnitId).ToArrayAsync(ct);
}
