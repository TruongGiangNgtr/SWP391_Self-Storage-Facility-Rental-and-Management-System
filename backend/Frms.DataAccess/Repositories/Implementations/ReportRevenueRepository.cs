using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class ReportRevenueRepository(FrmsDbContext dbContext) : IReportRevenueRepository
{
    public Task<bool> FacilityExistsAsync(Guid facilityId, CancellationToken cancellationToken) =>
        dbContext.Facilities.AsNoTracking().AnyAsync(x => x.FacilityId == facilityId, cancellationToken);

    public Task<bool> UnitTypeExistsAsync(Guid unitTypeId, CancellationToken cancellationToken) =>
        dbContext.UnitTypes.AsNoTracking().AnyAsync(x => x.UnitTypeId == unitTypeId, cancellationToken);

    public async Task<RentalRevenueRecord> CalculateAsync(
        Guid? facilityId,
        Guid? unitTypeId,
        DateOnly startMonth,
        DateOnly endMonthExclusive,
        DateTime paidStartUtc,
        DateTime paidEndUtcExclusive,
        CancellationToken cancellationToken)
    {
        // One Contract has one Reservation. The Reservation's UnitTypeId is a
        // historical snapshot; do not join the current StorageUnit.UnitTypeId
        // because the unit may have been reassigned after contract completion.
        var rentals = from contract in dbContext.Contracts.AsNoTracking()
                      join reservation in dbContext.Reservations.AsNoTracking()
                          on contract.ReservationId equals reservation.ReservationId
                      where (!facilityId.HasValue || contract.FacilityId == facilityId.Value)
                         && (!unitTypeId.HasValue || reservation.UnitTypeId == unitTypeId.Value)
                      select new { Contract = contract, Reservation = reservation };

        // Each Contract contributes its initial rent exactly once in StartMonth,
        // irrespective of whether the Contract is ACTIVE/COMPLETED/TERMINATED.
        var firstMonth = await rentals
            .Where(x => x.Contract.StartMonth >= startMonth &&
                        x.Contract.StartMonth < endMonthExclusive)
            .Select(x => (decimal?)x.Reservation.LockedRentalPrice)
            .SumAsync(cancellationToken) ?? 0m;

        // Sum Invoice rows rather than Payment attempts to avoid webhook/retry
        // amplification. PaidAt is the authoritative UTC timestamp. Ignore
        // historical first-month invoices via BillingMonth > Contract.StartMonth.
        var subsequent = await (
            from invoice in dbContext.Invoices.AsNoTracking()
            join rental in rentals on invoice.EntityId equals rental.Contract.ContractId
            where invoice.InvoiceType == "RENTAL_FEE"
               && invoice.Status == "PAID"
               && invoice.BillingMonth.HasValue
               && invoice.BillingMonth.Value > rental.Contract.StartMonth
               && invoice.PaidAt.HasValue
               && invoice.PaidAt.Value >= paidStartUtc
               && invoice.PaidAt.Value < paidEndUtcExclusive
            select (decimal?)invoice.AmountDue
        ).SumAsync(cancellationToken) ?? 0m;

        return new RentalRevenueRecord(firstMonth, subsequent);
    }
}
