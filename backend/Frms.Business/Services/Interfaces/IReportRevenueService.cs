using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Interfaces;

// Shared, authorized domain calculation for REP-002, REP-003 and REP-004.
// Endpoint query/response contracts must be approved separately before wiring.
public interface IReportRevenueService
{
    Task<RentalRevenueRecord> GetFacilityAsync(
        Guid facilityId, DateOnly startMonth, DateOnly endMonthExclusive,
        CancellationToken cancellationToken = default);

    Task<RentalRevenueRecord> GetBusinessAsync(
        Guid? facilityId, Guid? unitTypeId, DateOnly startMonth, DateOnly endMonthExclusive,
        CancellationToken cancellationToken = default);
}
