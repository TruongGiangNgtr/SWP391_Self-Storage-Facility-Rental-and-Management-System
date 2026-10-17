using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IReportRevenueRepository
{
    Task<bool> FacilityExistsAsync(Guid facilityId, CancellationToken cancellationToken);
    Task<bool> UnitTypeExistsAsync(Guid unitTypeId, CancellationToken cancellationToken);

    // Contract.StartMonth is calendar-month based; Invoice.PaidAt is persisted UTC.
    // End boundaries are exclusive, preventing overlap between adjacent periods.
    Task<RentalRevenueRecord> CalculateAsync(
        Guid? facilityId,
        Guid? unitTypeId,
        DateOnly startMonth,
        DateOnly endMonthExclusive,
        DateTime paidStartUtc,
        DateTime paidEndUtcExclusive,
        CancellationToken cancellationToken);
}
