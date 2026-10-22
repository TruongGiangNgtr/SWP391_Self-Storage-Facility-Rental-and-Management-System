using System.Globalization;
using System.Text.Json;
using Frms.Business.Abstractions.Security;
using Frms.Business.Abstractions.Time;
using Frms.Business.Exceptions;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Implementations;

internal sealed class StorageUnitTimelineService(
    IStorageUnitTimelineRepository repository,
    IAuthenticationService authentication,
    ICurrentUserContext currentUser,
    IClock clock) : IStorageUnitTimelineService
{
    public async Task<(IReadOnlyList<StorageUnitTimelineResult> Items, int TotalCount)> ListAsync(
        Guid facilityId, Guid? unitTypeId, string? month, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(page, pageSize);
        var (startUtc, endUtc) = ParseMonth(month);
        await AuthorizeFacilityAsync(facilityId, cancellationToken);
        if (!await repository.FacilityExistsAsync(facilityId, cancellationToken))
            throw new BusinessException("FACILITY_NOT_FOUND", "Facility was not found.", 404);
        if (unitTypeId.HasValue &&
            (unitTypeId == Guid.Empty || !await repository.UnitTypeExistsAsync(unitTypeId.Value, cancellationToken)))
            throw new BusinessException("UNIT_TYPE_NOT_FOUND", "Unit type was not found.", 404);

        var (units, total) = await repository.ListUnitsAsync(
            facilityId, unitTypeId, (page - 1) * pageSize, pageSize, cancellationToken);
        return (await PopulateAsync(units, startUtc, endUtc, cancellationToken), total);
    }

    public async Task<StorageUnitTimelineResult> GetAsync(
        Guid facilityId, Guid storageUnitId, string? month,
        CancellationToken cancellationToken = default)
    {
        var (startUtc, endUtc) = ParseMonth(month);
        await AuthorizeFacilityAsync(facilityId, cancellationToken);
        if (!await repository.FacilityExistsAsync(facilityId, cancellationToken))
            throw new BusinessException("FACILITY_NOT_FOUND", "Facility was not found.", 404);
        var unit = storageUnitId == Guid.Empty ? null :
            await repository.FindUnitAsync(facilityId, storageUnitId, cancellationToken);
        if (unit is null)
            throw new BusinessException("RESOURCE_NOT_FOUND", "Storage unit was not found in this facility.", 404);
        var items = await PopulateAsync([unit], startUtc, endUtc, cancellationToken);
        return items[0];
    }

    private async Task AuthorizeFacilityAsync(Guid facilityId, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            throw new BusinessException("UNAUTHORIZED", "Authentication is required.", 401);
        // GetCurrentAccountAsync rechecks persisted role, ACTIVE status and Facility assignment.
        var account = await authentication.GetCurrentAccountAsync(currentUser.UserAccountId, ct);
        if (account.Role is not ("FACILITY_MANAGER" or "BUSINESS_OPERATIONS_MANAGER" or "SYSTEM_ADMINISTRATOR") ||
            (account.Role == "FACILITY_MANAGER" && account.FacilityId != facilityId))
            throw new BusinessException("FORBIDDEN", "You are not authorized to view this facility's timeline.", 403);
    }

    private (DateTime StartUtc, DateTime EndUtc) ParseMonth(string? value)
    {
        if (value is null || value.Length != 7 || value[4] != '-' ||
            !int.TryParse(value.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out var year) ||
            !int.TryParse(value.AsSpan(5, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var month) ||
            year is < 1 or > 9998 || month is < 1 or > 12)
            throw new BusinessException("INVALID_MONTH", "Month must use YYYY-MM.", 400);

        var localStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var localEnd = localStart.AddMonths(1);
        return (TimeZoneInfo.ConvertTimeToUtc(localStart, clock.BusinessTimeZone),
                TimeZoneInfo.ConvertTimeToUtc(localEnd, clock.BusinessTimeZone));
    }

    private static void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100 || ((long)page - 1) * pageSize > int.MaxValue)
            throw new BusinessException("INVALID_PAGINATION", "Page must be >= 1 and pageSize between 1 and 100.", 400);
    }

    private async Task<IReadOnlyList<StorageUnitTimelineResult>> PopulateAsync(
        IReadOnlyList<StorageUnitTimelineUnitRecord> units, DateTime monthStartUtc,
        DateTime monthEndUtc, CancellationToken ct)
    {
        if (units.Count == 0) return [];
        // Timeline business rule: default unrecorded periods to AVAILABLE,
        // including past and future dates in the requested month.
        // A recorded state persists until the next status event.
        var end = monthEndUtc;
        IReadOnlyList<StorageUnitStatusAuditRecord> audit = end > monthStartUtc
            ? await repository.ListStatusEventsAsync(
                units.Select(x => x.StorageUnitId).ToArray(), monthStartUtc, end, ct)
            : [];
        var byUnit = audit.GroupBy(x => x.StorageUnitId).ToDictionary(x => x.Key, x => x.ToArray());
        return units.Select(unit => new StorageUnitTimelineResult(
            unit.StorageUnitId, unit.UnitCode, unit.UnitTypeName,
            unit.FacilityName, unit.LocationInfo,
            BuildIntervals(byUnit.TryGetValue(unit.StorageUnitId, out var events) ? events : [],
                monthStartUtc, end))).ToArray();
    }

    private static IReadOnlyList<StorageUnitStatusIntervalResult> BuildIntervals(
        IReadOnlyList<StorageUnitStatusAuditRecord> events, DateTime start, DateTime end)
    {
        if (start >= end) return [];
        var intervals = new List<StorageUnitStatusIntervalResult>();
        var known = "AVAILABLE";
        var cursor = start;
        foreach (var entry in events.OrderBy(x => x.CreatedAt).ThenBy(x => x.AuditLogId))
        {
            var at = entry.CreatedAt < start ? start : entry.CreatedAt;
            if (at > end) break;
            // Even if audit continuity is incomplete, a missing interval is AVAILABLE.
            var oldStatus = ParseStatus(entry.OldValue);
            if (entry.Action == "CHANGE_STORAGE_UNIT_STATUS" && oldStatus != known)
                known = "AVAILABLE";
            if (at > cursor)
                intervals.Add(new StorageUnitStatusIntervalResult(known, AsUtc(cursor), AsUtc(at)));
            known = ParseStatus(entry.NewValue) ?? "AVAILABLE";
            cursor = at;
        }
        if (cursor < end)
            intervals.Add(new StorageUnitStatusIntervalResult(known, AsUtc(cursor), AsUtc(end)));
        return intervals;
    }

    private static string? ParseStatus(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("status", out var property) ||
                property.ValueKind != JsonValueKind.String) return null;
            var status = property.GetString();
            return status is "AVAILABLE" or "IN_USE" or "INSPECTION" or "MAINTENANCE" ? status : null;
        }
        catch (JsonException) { return null; }
    }

    private static DateTimeOffset AsUtc(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
