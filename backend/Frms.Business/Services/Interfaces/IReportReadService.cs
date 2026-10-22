using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface IReportReadService
{
    Task<FacilityUnitReadReport> FacilityUnitsAsync(
        Guid facilityId, string? month, int page, int pageSize, CancellationToken ct);
    Task<BusinessReportBreakdown> BusinessBreakdownAsync(
        string view, Guid? facilityId, Guid? unitTypeId, int? year, int? quarter,
        int page, int pageSize, CancellationToken ct);
    Task<BusinessReportMonthly> BusinessMonthlyAsync(
        Guid? facilityId, Guid? unitTypeId, int? year, int? quarter, CancellationToken ct);
}
