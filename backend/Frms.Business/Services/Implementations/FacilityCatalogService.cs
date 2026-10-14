using Frms.Business.Abstractions.Time;
using Frms.Business.Exceptions;
using Frms.Business.Rules;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;

namespace Frms.Business.Services.Implementations;

internal sealed class FacilityCatalogService(
    IFacilityCatalogRepository facilityCatalogRepository,
    ICapacityRepository capacityRepository,
    IClock clock)
    : IFacilityCatalogService {
    public async Task<FacilityCatalogPageResult> SearchActiveFacilitiesAsync(
        string? location, Guid? facilityId, string? mode, string? size, Guid? unitTypeId,
        string? startMonth, string? endMonth, string? sortBy, string? sortDirection,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ValidatePagination(page, pageSize);
        location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
        size = string.IsNullOrWhiteSpace(size) ? null : size.Trim().ToLowerInvariant();
        mode = mode?.Trim().ToUpperInvariant();

        if (location?.Length > 1000 || size?.Length > 150 ||
            (mode is not null && mode is not ("PRIVATE" or "PUBLIC")) ||
            (sortBy is not null && sortBy != "name") ||
            (sortDirection is not null && sortDirection is not ("asc" or "desc")))
            throw new BusinessException("VALIDATION_ERROR", "Unsupported facility catalogue filter or sort.", 400);

        // Public reservation search must start strictly after the current business month.
        var period = RequestedPeriodRule.Parse(startMonth, endMonth);
        if (period.StartMonth.HasValue)
        {
            var today = clock.ToBusinessTime(clock.UtcNow).Date;
            var businessMonth = new DateOnly(today.Year, today.Month, 1);
            if (period.StartMonth.Value <= businessMonth)
                throw new BusinessException("INVALID_MONTH_RANGE", "The public start month must be in the future.", 400);
        }

        var descending = sortDirection == "desc";
        // Long arithmetic prevents paging overflow for a large but syntactically valid page.
        var skip = (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue);

        if (!period.StartMonth.HasValue)
        {
            var (rows, total) = await facilityCatalogRepository.SearchActiveAsync(
                location, facilityId, mode, size, unitTypeId, descending,
                skip, pageSize, cancellationToken);
            return new FacilityCatalogPageResult(rows.Select(Map).ToArray(), page, pageSize, total);
        }

        // Period availability must be calculated BEFORE totalItems and pagination.
        // First select only ACTIVE Facilities and matching offered UnitTypes.
        var (candidates, _) = await facilityCatalogRepository.SearchActiveAsync(
            location, facilityId, mode, size, unitTypeId, descending,
            null, null, cancellationToken);
        if (candidates.Count == 0)
            return new FacilityCatalogPageResult([], page, pageSize, 0);

        var offeredIds = await facilityCatalogRepository.GetMatchingOfferedUnitTypesAsync(
            candidates.Select(x => x.FacilityId).ToArray(), mode, size, unitTypeId, cancellationToken);

        var matched = new List<Frms.DataAccess.Repositories.Models.FacilityCatalogRecord>();
        foreach (var candidate in candidates)
        {
            if (!offeredIds.TryGetValue(candidate.FacilityId, out var typeIds) || typeIds.Count == 0)
                continue;

            // Reuse CALC-CAP-01: availableCapacity is minimum across every requested month.
            var capacities = await capacityRepository.GetAvailableCapacitiesAsync(
                candidate.FacilityId, typeIds, period.StartMonth.Value,
                period.EndMonth!.Value, cancellationToken);
            if (typeIds.Any(id => capacities.GetValueOrDefault(id, 0) > 0))
                matched.Add(candidate);
        }

        var results = matched.Skip(skip).Take(pageSize).Select(Map).ToArray();
        return new FacilityCatalogPageResult(results, page, pageSize, matched.Count);
    }

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
                "VALIDATION_ERROR",
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
