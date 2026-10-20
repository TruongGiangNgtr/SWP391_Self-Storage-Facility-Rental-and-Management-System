using Frms.Business.Abstractions.Security;
using Frms.Business.Abstractions.Time;
using Frms.Business.Models.Results;
using Frms.Business.Rules;
using Frms.Business.Exceptions;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Implementations;

internal sealed class OperationalCatalogService(
    IOperationalCatalogRepository repository, ICurrentUserContext user,
    IAuthenticationService authentication, IFacilityAuthorizationService facilityAuth,
    ICapacityRepository capacityRepository, IClock clock) : IOperationalCatalogService
{
    public async Task<(IReadOnlyList<ExtraFeeTypeRecord> Items, int Total)> ListExtraFeeTypesAsync(
        int page, int pageSize, CancellationToken ct)
    {
        CheckPage(page, pageSize);
        await RequireActiveAsync(ct);
        if (user.Role != "BUSINESS_OPERATIONS_MANAGER")
        {
            if (user.Role is not ("FACILITY_STAFF" or "FACILITY_MANAGER")) throw Forbidden();
            // A facility assignment is mandatory, even though the catalogue is global.
            await facilityAuth.GetAssignedFacilityIdAsync(ct);
        }
        return await repository.ListExtraFeeTypesAsync(user.Role != "BUSINESS_OPERATIONS_MANAGER",
            page, pageSize, ct);
    }

    public async Task<ExtraFeeTypeRecord> UpdateExtraFeeTypeAsync(
        Guid id, decimal amount, string status, CancellationToken ct)
    {
        await RequireActiveAsync(ct);
        if (user.Role != "BUSINESS_OPERATIONS_MANAGER") throw Forbidden();
        if (amount < 0 || (status != "ACTIVE" && status != "INACTIVE"))
            throw new BusinessException("VALIDATION_ERROR", "Invalid ExtraFeeType update.", 400);
        return await repository.UpdateExtraFeeTypeAsync(id, amount, status, ct)
            ?? throw new BusinessException("RESOURCE_NOT_FOUND", "ExtraFeeType not found.", 404);
    }

    public async Task<(IReadOnlyList<PublicUnitTypeRecord> Items, int Total)> ListPublicUnitTypesAsync(
        string? mode, string? size, Guid? unitTypeId, string? sortBy, string? sortDirection,
        int page, int pageSize, CancellationToken ct)
    {
        CheckPage(page, pageSize);
        if (mode is not null && mode is not ("PUBLIC" or "PRIVATE") ||
            sortBy is not null && sortBy is not ("name" or "rentalPrice") ||
            sortDirection is not null && sortDirection is not ("asc" or "desc") ||
            size is { Length: > 150 })
            throw new BusinessException("VALIDATION_ERROR", "Unsupported public catalogue filter.", 400);
        return await repository.ListPublicUnitTypesAsync(mode, size, unitTypeId,
            sortBy ?? "name", sortDirection == "desc", page, pageSize, ct);
    }

    // CAT-003 public projection. Operational view continues through CapacityService/RBAC.
    public async Task<CapacityPageResult> ListFacilityPublicUnitTypesAsync(
        Guid facilityId, string? mode, string? size, Guid? unitTypeId,
        string? startMonth, string? endMonth, string? sortBy, string? sortDirection,
        string? availableOnly, int page, int pageSize, CancellationToken ct)
    {
        CheckPage(page, pageSize);
        if (page > int.MaxValue / pageSize)
            throw new BusinessException("VALIDATION_ERROR", "Invalid pagination.", 400);

        mode = mode?.Trim().ToUpperInvariant();
        size = string.IsNullOrWhiteSpace(size) ? null : size.Trim().ToLowerInvariant();
        if ((mode is not null && mode is not ("PUBLIC" or "PRIVATE")) ||
            (sortBy is not null && sortBy is not ("name" or "rentalPrice")) ||
            (sortDirection is not null && sortDirection is not ("asc" or "desc")) ||
            size?.Length > 150)
            throw new BusinessException("VALIDATION_ERROR", "Unsupported public catalogue filter.", 400);

        if (availableOnly is not null && !bool.TryParse(availableOnly, out _))
            throw new BusinessException("VALIDATION_ERROR", "Invalid availableOnly flag.", 400);
        var onlyAvailable = availableOnly is not null && bool.Parse(availableOnly);
        var period = RequestedPeriodRule.Parse(startMonth, endMonth);
        if (onlyAvailable && !period.StartMonth.HasValue)
            throw new BusinessException("VALIDATION_ERROR", "availableOnly requires both months.", 400);
        if (period.StartMonth.HasValue)
        {
            var now = clock.ToBusinessTime(clock.UtcNow);
            if (period.StartMonth.Value <= new DateOnly(now.Year, now.Month, 1))
                throw new BusinessException("INVALID_MONTH_RANGE", "StartMonth must be after the current business month.", 400);
        }

        var status = await capacityRepository.GetFacilityStatusAsync(facilityId, ct);
        if (status != "ACTIVE")
            throw new BusinessException("RESOURCE_NOT_FOUND", "Active Facility was not found.", 404);

        // For availableOnly, capacity must be calculated on ALL matching offerings
        // before filtering, totalItems and pagination. Other reads page in SQL first.
        var offset = (page - 1) * pageSize;
        var (items, total) = await repository.ListFacilityOfferedUnitTypesAsync(
            facilityId, mode, size, unitTypeId, sortBy ?? "name", sortDirection == "desc",
            onlyAvailable ? null : offset, onlyAvailable ? null : pageSize, ct);

        var capacities = new Dictionary<Guid, int>();
        if (period.StartMonth.HasValue && items.Count > 0)
        {
            // SQL IN parameters are bounded; use the existing multi-month capacity engine.
            foreach (var batch in items.Select(x => x.UnitTypeId).Chunk(400))
            {
                var values = await capacityRepository.GetAvailableCapacitiesAsync(
                    facilityId, batch, period.StartMonth.Value, period.EndMonth!.Value, ct);
                foreach (var pair in values) capacities[pair.Key] = pair.Value;
            }
        }

        IEnumerable<PublicUnitTypeRecord> selected = items;
        if (onlyAvailable)
        {
            selected = items.Where(x => capacities.GetValueOrDefault(x.UnitTypeId) > 0);
            var filtered = selected.ToArray();
            total = filtered.Length;
            selected = filtered.Skip(offset).Take(pageSize);
        }

        var rows = selected.Select(x => new UnitTypeAvailabilityResult(
            x.UnitTypeId, x.Name, x.Mode, x.Size, x.RentalPrice, x.Description,
            period.StartMonth, period.EndMonth,
            period.StartMonth.HasValue ? capacities.GetValueOrDefault(x.UnitTypeId) : null,
            x.ImageUrl, x.VideoUrl)).ToArray();
        return new CapacityPageResult(rows, total);
    }

    private async Task RequireActiveAsync(CancellationToken ct)
    {
        if (!user.IsAuthenticated) throw new BusinessException("UNAUTHORIZED", "Authentication is required.", 401);
        var account = await authentication.GetCurrentAccountAsync(user.UserAccountId, ct);
        if (account.Role != user.Role) throw Forbidden();
    }

    private static void CheckPage(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            throw new BusinessException("VALIDATION_ERROR", "Invalid pagination.", 400);
    }

    private static BusinessException Forbidden() => new("FORBIDDEN", "Access denied.", 403);
}
