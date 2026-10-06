using System.Globalization;
using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;

namespace Frms.Business.Services.Implementations;

internal sealed class CapacityService(
    ICapacityRepository repository,
    ICurrentUserContext currentUser,
    IFacilityAuthorizationService facilityAuthorizationService)
    : ICapacityService
{
    public async Task<CapacityPageResult> ListUnitTypesAsync(
        Guid facilityId,
        string? startMonth,
        string? endMonth,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new BusinessException(
                "UNAUTHORIZED",
                "Authentication is required.",
                401);
        }

        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            throw new BusinessException(
                "VALIDATION_ERROR",
                "Page must be at least 1 and pageSize must be between 1 and 100.",
                400);
        }

        var facilityStatus =
            await repository.GetFacilityStatusAsync(
                facilityId,
                cancellationToken);

        if (facilityStatus is null)
        {
            throw new BusinessException(
                "RESOURCE_NOT_FOUND",
                "Facility was not found.",
                404);
        }

        if (string.Equals(
                currentUser.Role,
                "CUSTOMER",
                StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(
                    facilityStatus,
                    "ACTIVE",
                    StringComparison.Ordinal))
            {
                throw new BusinessException(
                    "FACILITY_INACTIVE",
                    "The facility is inactive.",
                    409);
            }
        }
        else if (
            string.Equals(
                currentUser.Role,
                "FACILITY_STAFF",
                StringComparison.OrdinalIgnoreCase)
            ||
            string.Equals(
                currentUser.Role,
                "FACILITY_MANAGER",
                StringComparison.OrdinalIgnoreCase))
        {
            await facilityAuthorizationService.EnsureSameFacilityAsync(
                facilityId,
                cancellationToken);
        }
        else
        {
            throw new BusinessException(
                "FORBIDDEN",
                "You are not authorized to access this resource.",
                403);
        }

        var range = ParseRequestedPeriod(
            startMonth,
            endMonth);

        var totalItems =
            await repository.CountUnitTypesAsync(
                facilityId,
                cancellationToken);

        var unitTypes =
            await repository.ListUnitTypesAsync(
                facilityId,
                (page - 1) * pageSize,
                pageSize,
                cancellationToken);

        IReadOnlyDictionary<Guid, int> capacities =
            new Dictionary<Guid, int>();

        if (range.StartMonth is not null &&
            range.EndMonth is not null &&
            unitTypes.Count > 0)
        {
            capacities =
                await repository.GetAvailableCapacitiesAsync(
                    facilityId,
                    unitTypes
                        .Select(x => x.UnitTypeId)
                        .ToArray(),
                    range.StartMonth.Value,
                    range.EndMonth.Value,
                    cancellationToken);
        }

        var items = unitTypes
            .Select(unitType =>
                new UnitTypeAvailabilityResult(
                    unitType.UnitTypeId,
                    unitType.Name,
                    unitType.Mode,
                    unitType.Size,
                    unitType.RentalPrice,
                    unitType.Description,
                    range.StartMonth,
                    range.EndMonth,
                    range.StartMonth is null
                        ? null
                        : capacities.GetValueOrDefault(
                            unitType.UnitTypeId,
                            0)))
            .ToArray();

        return new CapacityPageResult(
            items,
            totalItems);
    }

    private static RequestedPeriod ParseRequestedPeriod(
        string? startMonth,
        string? endMonth)
    {
        var hasStart = !string.IsNullOrWhiteSpace(startMonth);
        var hasEnd = !string.IsNullOrWhiteSpace(endMonth);

        if (!hasStart && !hasEnd)
        {
            return new RequestedPeriod(null, null);
        }

        if (hasStart != hasEnd)
        {
            throw InvalidMonthRange();
        }

        if (!TryParseMonth(startMonth!, out var start) ||
            !TryParseMonth(endMonth!, out var end) ||
            end < start)
        {
            throw InvalidMonthRange();
        }

        return new RequestedPeriod(start, end);
    }

    private static bool TryParseMonth(
        string value,
        out DateOnly month)
    {
        return DateOnly.TryParseExact(
            $"{value}-01",
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out month);
    }

    private static BusinessException InvalidMonthRange()
    {
        return new BusinessException(
            "INVALID_MONTH_RANGE",
            "The requested month range is invalid.",
            400);
    }

    private sealed record RequestedPeriod(
        DateOnly? StartMonth,
        DateOnly? EndMonth);
}