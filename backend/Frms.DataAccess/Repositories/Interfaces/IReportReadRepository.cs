using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IReportReadRepository
{
    Task<IReadOnlyList<ReportFacilityLookup>> FacilitiesAsync(CancellationToken ct);
    Task<IReadOnlyList<ReportUnitTypeLookup>> UnitTypesAsync(CancellationToken ct);
    Task<IReadOnlyList<ReportStatusGroup>> StatusGroupsAsync(Guid? facilityId, Guid? unitTypeId, CancellationToken ct);
    Task<IReadOnlyList<ReportRevenueGroup>> RevenueGroupsAsync(
        Guid? facilityId, Guid? unitTypeId,
        DateOnly fromMonth, DateOnly toMonthExclusive,
        DateTime fromPaidUtc, DateTime toPaidUtcExclusive,
        CancellationToken ct);
    Task<(IReadOnlyList<ReportUnitDetail> Items, int Total)> FacilityInventoryAsync(
        Guid facilityId, int page, int pageSize, CancellationToken ct);
    Task<Guid[]> FacilityUnitIdsAsync(Guid facilityId, CancellationToken ct);
}
