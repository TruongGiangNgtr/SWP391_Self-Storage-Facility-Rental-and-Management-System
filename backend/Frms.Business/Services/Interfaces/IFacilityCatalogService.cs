using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface IFacilityCatalogService {
    Task<FacilityCatalogPageResult> GetActiveFacilitiesAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<FacilityCatalogResult> GetActiveFacilityAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default);
}
