using Frms.Business.Abstractions.Security;
using Frms.Business.Abstractions.Time;
using Frms.Business.Exceptions;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;

namespace Frms.Business.Services.Implementations;

internal sealed class BusinessOverviewService(
    IBusinessPortfolioRepository portfolio,
    IReportRevenueService revenue,
    ICurrentUserContext currentUser,
    IClock clock) : IBusinessOverviewService
{
    public async Task<BusinessOverviewResult> GetAsync(CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
            throw new BusinessException("UNAUTHORIZED", "Authentication required.", 401);
        if (currentUser.Role != "BUSINESS_OPERATIONS_MANAGER")
            throw new BusinessException("FORBIDDEN", "Business Operations Manager required.", 403);

        var now = clock.ToBusinessTime(clock.UtcNow);
        // Default no-filter overview = current business calendar year to date.
        // A business month is included through its last day; the end is exclusive.
        var start = new DateOnly(now.Year, 1, 1);
        var exclusiveEnd = new DateOnly(now.Year, now.Month, 1).AddMonths(1);
        var values = await revenue.GetBusinessAsync(null, null, start, exclusiveEnd, cancellationToken);
        var current = await portfolio.GetSnapshotAsync(cancellationToken);
        var rate = current.TotalUnits == 0 ? 0m :
            Math.Round((decimal)current.InUseUnits / current.TotalUnits, 4,
                MidpointRounding.AwayFromZero);
        return new BusinessOverviewResult(current.FacilityCount,
            current.ActiveContractCount, rate, values.RentalRevenue);
    }
}
