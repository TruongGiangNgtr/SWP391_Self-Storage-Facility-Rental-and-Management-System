using Frms.Business.Exceptions;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;

namespace Frms.Business.Services.Implementations;

internal sealed class FacilityCatalogService(
    IFacilityCatalogRepository facilityCatalogRepository)
    : IFacilityCatalogService {
    public async Task<FacilityCatalogPageResult>
        GetActiveFacilitiesAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) {
        ValidatePagination(page, pageSize);

        var skip = (page - 1) * pageSize;

        var (items, totalItems) =
            await facilityCatalogRepository.GetActivePagedAsync(
                skip,
                pageSize,
                cancellationToken);

        return new FacilityCatalogPageResult(
            items.Select(Map).ToArray(),
            page,
            pageSize,
            totalItems);
    }

    public async Task<FacilityCatalogResult>
        GetActiveFacilityAsync(
            Guid facilityId,
            CancellationToken cancellationToken = default) {
        var facility =
            await facilityCatalogRepository.GetActiveByIdAsync(
                facilityId,
                cancellationToken);

        if (facility is null) {
            throw new BusinessException(
                "RESOURCE_NOT_FOUND",
                "Facility was not found.",
                404);
        }

        return Map(facility);
    }

    private static void ValidatePagination(
        int page,
        int pageSize) {
        if (page < 1 || pageSize is < 1 or > 100) {
            throw new BusinessException(
                "INVALID_PAGINATION",
                "Page must be at least 1 and pageSize must be between 1 and 100.",
                400);
        }
    }

    private static FacilityCatalogResult Map(
        Frms.DataAccess.Repositories.Models.FacilityCatalogRecord x)
        => new(
            x.FacilityId,
            x.Name,
            x.Address,
            x.ContactInfo,
            x.Description,
            x.Status);
}
