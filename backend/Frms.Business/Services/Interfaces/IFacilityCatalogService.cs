using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface IFacilityCatalogService {
    Task<FacilityCatalogPageResult> SearchActiveFacilitiesAsync(
        string? location, Guid? facilityId, string? mode, string? size, Guid? unitTypeId,
        string? startMonth, string? endMonth, string? sortBy, string? sortDirection,
        int page, int pageSize, CancellationToken cancellationToken = default);

    Task<FacilityCatalogPageResult> GetActiveFacilitiesAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<FacilityCatalogResult> GetActiveFacilityAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default);
}
