using Frms.Business.Abstractions.Security;
using Frms.Business.Abstractions.Time;
using Frms.Business.Exceptions;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Implementations;

internal sealed class ReportRevenueService(
    IReportRevenueRepository repository,
    IFacilityAuthorizationService facilityAuthorizationService,
    ICurrentUserContext currentUser,
    IClock clock) : IReportRevenueService
{
    public async Task<RentalRevenueRecord> GetFacilityAsync(
        Guid facilityId, DateOnly startMonth, DateOnly endMonthExclusive,
        CancellationToken cancellationToken = default)
    {
        EnsureRole("FACILITY_MANAGER");
        await facilityAuthorizationService.EnsureSameFacilityAsync(facilityId, cancellationToken);
        if (!await repository.FacilityExistsAsync(facilityId, cancellationToken))
            throw new BusinessException("RESOURCE_NOT_FOUND", "Facility was not found.", 404);
        return await CalculateAsync(facilityId, null, startMonth, endMonthExclusive, cancellationToken);
    }

    public async Task<RentalRevenueRecord> GetBusinessAsync(
        Guid? facilityId, Guid? unitTypeId,
        DateOnly startMonth, DateOnly endMonthExclusive,
        CancellationToken cancellationToken = default)
    {
        EnsureRole("BUSINESS_OPERATIONS_MANAGER");
        if (facilityId.HasValue && !await repository.FacilityExistsAsync(facilityId.Value, cancellationToken))
            throw new BusinessException("RESOURCE_NOT_FOUND", "Facility was not found.", 404);
        if (unitTypeId.HasValue && !await repository.UnitTypeExistsAsync(unitTypeId.Value, cancellationToken))
            throw new BusinessException("RESOURCE_NOT_FOUND", "Unit Type was not found.", 404);
        return await CalculateAsync(facilityId, unitTypeId, startMonth, endMonthExclusive, cancellationToken);
    }

    private async Task<RentalRevenueRecord> CalculateAsync(
        Guid? facilityId, Guid? unitTypeId,
        DateOnly startMonth, DateOnly endMonthExclusive,
        CancellationToken cancellationToken)
    {
        if (startMonth.Day != 1 || endMonthExclusive.Day != 1 || endMonthExclusive <= startMonth)
            throw new BusinessException("VALIDATION_ERROR", "Reporting month range is invalid.", 400);

        // SQL stores paid timestamps in UTC. Build half-open bounds from the
        // business calendar before querying, not from the API server's timezone.
        var from = ToUtcStart(startMonth);
        var to = ToUtcStart(endMonthExclusive);
        return await repository.CalculateAsync(
            facilityId, unitTypeId, startMonth, endMonthExclusive,
            from, to, cancellationToken);
    }

    private DateTime ToUtcStart(DateOnly month) =>
        TimeZoneInfo.ConvertTimeToUtc(month.ToDateTime(TimeOnly.MinValue), clock.BusinessTimeZone);

    private void EnsureRole(string role)
    {
        if (!currentUser.IsAuthenticated)
            throw new BusinessException("UNAUTHORIZED", "Authentication is required.", 401);
        if (currentUser.Role != role)
            throw new BusinessException("FORBIDDEN", "This report is not authorized for the current role.", 403);
    }
}
