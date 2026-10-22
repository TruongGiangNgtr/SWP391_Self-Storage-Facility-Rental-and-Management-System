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

internal sealed class ReportReadService(
    IReportReadRepository repository,
    IReportRevenueRepository revenueRepository,
    IReportRevenueService revenueService,
    IStorageUnitTimelineRepository timeline,
    IFacilityAuthorizationService facilityAuthorization,
    ICurrentUserContext currentUser,
    IClock clock) : IReportReadService
{
    private static readonly string[] ValidStatuses =
        ["AVAILABLE", "IN_USE", "INSPECTION", "MAINTENANCE"];

    public async Task<FacilityUnitReadReport> FacilityUnitsAsync(
        Guid facilityId, string? month, int page, int pageSize, CancellationToken ct)
    {
        Authorize("FACILITY_MANAGER");
        ValidatePagination(page, pageSize);
        await facilityAuthorization.EnsureSameFacilityAsync(facilityId, ct);
        if (!await revenueRepository.FacilityExistsAsync(facilityId, ct))
            throw new BusinessException("RESOURCE_NOT_FOUND", "Facility was not found.", 404);

        var selected = ParseManagerMonth(month);
        var groups = await repository.StatusGroupsAsync(facilityId, null, ct);
        var types = (await repository.UnitTypesAsync(ct)).ToDictionary(x => x.UnitTypeId);
        var byType = groups.OrderBy(x => types.TryGetValue(x.UnitTypeId, out var t) ? t.Name : "")
            .ThenBy(x => x.UnitTypeId)
            .Select(g => new ReportUnitTypeUtilization(g.UnitTypeId,
                types.GetValueOrDefault(g.UnitTypeId)?.Name ?? "Unknown",
                types.GetValueOrDefault(g.UnitTypeId)?.Size ?? "",
                g.TotalUnits, g.AvailableUnits, g.InUseUnits, g.InspectionUnits,
                g.MaintenanceUnits, Rate(g.InUseUnits, g.TotalUnits))).ToArray();
        var (inventory, total) = await repository.FacilityInventoryAsync(facilityId, page, pageSize, ct);
        var daily = await DailyTrendAsync(facilityId, selected, ct);
        var complete = daily.Where(x => x.UsageRate.HasValue).ToArray();
        var average = complete.Length == 0 ? (decimal?)null :
            Math.Round(complete.Average(x => x.UsageRate!.Value), 4, MidpointRounding.AwayFromZero);
        var peak = complete.OrderByDescending(x => x.UsageRate).ThenBy(x => x.Date)
            .Select(x => (DateOnly?)x.Date).FirstOrDefault();
        return new FacilityUnitReadReport(facilityId, selected.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            clock.UtcNow, "CURRENT", false, byType, inventory, Page(page, pageSize, total),
            daily, average, peak);
    }

    public async Task<BusinessReportBreakdown> BusinessBreakdownAsync(
        string view, Guid? facilityId, Guid? unitTypeId, int? year, int? quarter,
        int page, int pageSize, CancellationToken ct)
    {
        Authorize("BUSINESS_OPERATIONS_MANAGER");
        ValidatePagination(page, pageSize);
        if (view is not ("facility" or "unitType" or "status"))
            throw new BusinessException("VALIDATION_ERROR", "view must be facility, unitType or status.", 400);
        var (selectedYear, selectedQuarter, start, end) = Period(year, quarter);
        await ValidateFiltersAsync(facilityId, unitTypeId, ct);
        var inventory = await repository.StatusGroupsAsync(facilityId, unitTypeId, ct);
        var facilities = (await repository.FacilitiesAsync(ct)).Where(f => !facilityId.HasValue || f.FacilityId == facilityId).ToArray();
        var types = (await repository.UnitTypesAsync(ct)).Where(t => !unitTypeId.HasValue || t.UnitTypeId == unitTypeId).ToArray();
        var facilityNames = facilities.ToDictionary(x => x.FacilityId, x => x.Name);
        var unitTypeNames = types.ToDictionary(x => x.UnitTypeId);
        var revenue = await repository.RevenueGroupsAsync(facilityId, unitTypeId, start, end,
            UtcMonthStart(start), UtcMonthStart(end), ct);

        // Facility and UnitType tabs also expose zero-unit catalogue entries.
        var keys = view switch
        {
            "facility" => facilities.Select(f => (Facility: (Guid?)f.FacilityId, Type: (Guid?)null)),
            "unitType" => types.Select(t => (Facility: (Guid?)null, Type: (Guid?)t.UnitTypeId)),
            _ => inventory.Select(x => (Facility: (Guid?)x.FacilityId, Type: (Guid?)x.UnitTypeId))
        };
        var rows = keys.Distinct().Select(k =>
        {
            var physical = inventory.Where(g => (!k.Facility.HasValue || k.Facility == g.FacilityId)
                && (!k.Type.HasValue || k.Type == g.UnitTypeId)).ToArray();
            var financial = revenue.Where(g => (!k.Facility.HasValue || k.Facility == g.FacilityId)
                && (!k.Type.HasValue || k.Type == g.UnitTypeId)).ToArray();
            var total = physical.Sum(g => g.TotalUnits);
            var inUse = physical.Sum(g => g.InUseUnits);
            var first = financial.Sum(g => g.FirstMonthRecognizedRevenue);
            var later = financial.Sum(g => g.SubsequentPaidRentalRevenue);
            return new BusinessReportGroup(k.Facility,
                k.Facility.HasValue && facilityNames.TryGetValue(k.Facility.Value, out var fn) ? fn : null,
                k.Type, k.Type.HasValue ? unitTypeNames.GetValueOrDefault(k.Type.Value)?.Name : null,
                k.Type.HasValue ? unitTypeNames.GetValueOrDefault(k.Type.Value)?.Size : null,
                total, physical.Sum(g => g.AvailableUnits), inUse,
                physical.Sum(g => g.InspectionUnits), physical.Sum(g => g.MaintenanceUnits),
                Rate(inUse, total), first, later, first + later);
        }).OrderBy(x => x.FacilityName ?? x.UnitTypeName)
          .ThenBy(x => x.UnitTypeName).ThenBy(x => x.FacilityId).ThenBy(x => x.UnitTypeId).ToArray();
        var grandTotal = inventory.Sum(x => x.TotalUnits);
        var grandUse = inventory.Sum(x => x.InUseUnits);
        var grandFirst = revenue.Sum(x => x.FirstMonthRecognizedRevenue);
        var grandLater = revenue.Sum(x => x.SubsequentPaidRentalRevenue);
        var summary = new ReportSummary(facilities.Length,
            inventory.Select(x => x.UnitTypeId).Distinct().Count(), grandTotal,
            inventory.Sum(x => x.AvailableUnits), grandUse,
            inventory.Sum(x => x.InspectionUnits), inventory.Sum(x => x.MaintenanceUnits),
            Rate(grandUse, grandTotal), grandFirst, grandLater, grandFirst + grandLater);
        var pageRows = rows.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
        return new BusinessReportBreakdown(view, selectedYear, selectedQuarter, clock.UtcNow,
            summary, pageRows, Page(page, pageSize, rows.Length));
    }

    public async Task<BusinessReportMonthly> BusinessMonthlyAsync(
        Guid? facilityId, Guid? unitTypeId, int? year, int? quarter, CancellationToken ct)
    {
        Authorize("BUSINESS_OPERATIONS_MANAGER");
        var (selectedYear, selectedQuarter, start, end) = Period(year, quarter);
        await ValidateFiltersAsync(facilityId, unitTypeId, ct);
        // Reuse the canonical financial calculation: first month is recognized
        // from Contract/Reservation; subsequent rent comes from paid Invoice only.
        // Single-month calls preserve GMT+7 boundaries exactly.
        var monthly = new List<ReportRevenueMonth>();
        for (var month = start; month < end; month = month.AddMonths(1))
        {
            var value = await revenueService.GetBusinessAsync(facilityId, unitTypeId,
                month, month.AddMonths(1), ct);
            monthly.Add(new ReportRevenueMonth(month.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                value.FirstMonthRecognizedRevenue, value.SubsequentPaidRentalRevenue, value.RentalRevenue));
        }
        return new BusinessReportMonthly(selectedYear, selectedQuarter, facilityId, unitTypeId,
            monthly.Sum(x => x.FirstMonthRecognizedRevenue),
            monthly.Sum(x => x.SubsequentPaidRentalRevenue), monthly.Sum(x => x.RentalRevenue), monthly);
    }

    private async Task<IReadOnlyList<ReportDailyStatus>> DailyTrendAsync(
        Guid facilityId, DateOnly selectedMonth, CancellationToken ct)
    {
        var ids = await repository.FacilityUnitIdsAsync(facilityId, ct);
        if (ids.Length == 0) return [];
        var monthStart = UtcMonthStart(selectedMonth);
        var endMonth = UtcMonthStart(selectedMonth.AddMonths(1));
        var cutoff = clock.UtcNow.UtcDateTime;
        if (cutoff > endMonth) cutoff = endMonth;
        if (cutoff <= monthStart) return [];
        IReadOnlyList<StorageUnitStatusAuditRecord> events = ids.Length == 0 ? [] :
            await timeline.ListStatusEventsAsync(ids, monthStart, cutoff, ct);
        var byUnit = events.GroupBy(x => x.StorageUnitId)
            .ToDictionary(x => x.Key, x => x.OrderBy(e => e.CreatedAt).ThenBy(e => e.AuditLogId).ToArray());
        var rows = new List<ReportDailyStatus>();
        var lastDate = DateOnly.FromDateTime(clock.ToBusinessTime(new DateTimeOffset(cutoff, TimeSpan.Zero)).Date);
        for (var day = selectedMonth; day <= lastDate && day < selectedMonth.AddMonths(1); day = day.AddDays(1))
        {
            var dayCutoff = UtcMonthStart(day.AddDays(1));
            if (dayCutoff > cutoff) dayCutoff = cutoff;
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            var known = 0;
            foreach (var id in ids)
            {
                string? state = null;
                if (byUnit.TryGetValue(id, out var history))
                {
                    foreach (var e in history)
                    {
                        if (e.CreatedAt >= dayCutoff) break;
                        var previous = StatusFromJson(e.OldValue);
                        if (e.Action == "CHANGE_STORAGE_UNIT_STATUS" && state is not null && previous != state)
                            state = null;
                        state = StatusFromJson(e.NewValue);
                    }
                }
                if (state is null) continue;
                known++;
                counts[state] = counts.GetValueOrDefault(state) + 1;
            }
            var unknown = ids.Length - known;
            var used = counts.GetValueOrDefault("IN_USE");
            rows.Add(new ReportDailyStatus(day, counts.GetValueOrDefault("AVAILABLE"), used,
                counts.GetValueOrDefault("INSPECTION"), counts.GetValueOrDefault("MAINTENANCE"),
                known, unknown, unknown == 0 ? Rate(used, ids.Length) : null));
        }
        return rows;
    }

    private static string? StatusFromJson(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        try
        {
            using var doc = JsonDocument.Parse(value);
            if (doc.RootElement.ValueKind != JsonValueKind.Object ||
                !doc.RootElement.TryGetProperty("status", out var field) ||
                field.ValueKind != JsonValueKind.String) return null;
            var status = field.GetString();
            return Array.IndexOf(ValidStatuses, status) >= 0 ? status : null;
        }
        catch (JsonException) { return null; }
    }

    private void Authorize(string requiredRole)
    {
        if (!currentUser.IsAuthenticated)
            throw new BusinessException("UNAUTHORIZED", "Authentication is required.", 401);
        if (currentUser.Role != requiredRole)
            throw new BusinessException("FORBIDDEN", "Role is not authorized for this report.", 403);
    }

    private static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100 || ((long)page - 1) * pageSize > int.MaxValue)
            throw new BusinessException("INVALID_PAGINATION", "page must be >= 1; pageSize 1..100.", 400);
    }

    private DateOnly ParseManagerMonth(string? month)
    {
        var now = clock.ToBusinessTime(clock.UtcNow);
        if (string.IsNullOrWhiteSpace(month)) return new DateOnly(now.Year, now.Month, 1);
        if (month.Length != 7 || !DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) || date.Year is < 1 or > 9998)
            throw new BusinessException("VALIDATION_ERROR", "month must be yyyy-MM.", 400);
        if (date > new DateOnly(now.Year, now.Month, 1))
            throw new BusinessException("VALIDATION_ERROR", "Future month is not supported.", 400);
        return date;
    }

    private (int Year, int? Quarter, DateOnly From, DateOnly To) Period(int? year, int? quarter)
    {
        var selectedYear = year ?? clock.ToBusinessTime(clock.UtcNow).Year;
        if (selectedYear is < 1 or > 9998 || (quarter.HasValue && quarter.Value is < 1 or > 4))
            throw new BusinessException("VALIDATION_ERROR", "Invalid year or quarter.", 400);
        var start = new DateOnly(selectedYear, (quarter.GetValueOrDefault(1) - 1) * 3 + 1, 1);
        return (selectedYear, quarter, start, start.AddMonths(quarter.HasValue ? 3 : 12));
    }

    private async Task ValidateFiltersAsync(Guid? facilityId, Guid? unitTypeId, CancellationToken ct)
    {
        if (facilityId.HasValue && (facilityId.Value == Guid.Empty ||
            !await revenueRepository.FacilityExistsAsync(facilityId.Value, ct)))
            throw new BusinessException("RESOURCE_NOT_FOUND", "Facility was not found.", 404);
        if (unitTypeId.HasValue && (unitTypeId.Value == Guid.Empty ||
            !await revenueRepository.UnitTypeExistsAsync(unitTypeId.Value, ct)))
            throw new BusinessException("RESOURCE_NOT_FOUND", "Unit type was not found.", 404);
    }

    private DateTime UtcMonthStart(DateOnly day) =>
        TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), clock.BusinessTimeZone);

    private static decimal Rate(int used, int total) => total == 0 ? 0m :
        Math.Round((decimal)used / total, 4, MidpointRounding.AwayFromZero);

    private static ReportPagination Page(int page, int size, int total) =>
        new(page, size, total, total == 0 ? 0 : (int)Math.Ceiling(total / (double)size));
}
