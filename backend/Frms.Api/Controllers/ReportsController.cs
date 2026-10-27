using System.Globalization;
using System.Text;
using Frms.Api.Authorization;
using Frms.Api.DTOs.Responses;
using Frms.Business.Abstractions.Time;
using Frms.Business.Exceptions;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Route("api/v1/reports")]
public sealed class ReportsController(
    IFacilityOperationsReportService facilityOperationsReportService,
    IReportRevenueService revenueService,
    IBusinessOverviewService overviewService,
    IReportReadService reportReadService,
    IClock clock) : ControllerBase
{
    /// <summary>REP-001: Facility Operations, unchanged.</summary>
    [Authorize(Roles = RoleNames.FacilityManager)]
    [HttpGet("facilities/{facilityId:guid}/operations")]
    [ProducesResponseType(typeof(ApiResponse<FacilityOperationsReportResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<FacilityOperationsReportResponse>>> GetFacilityOperationsReport(
        Guid facilityId, CancellationToken cancellationToken)
    {
        var result = await facilityOperationsReportService.GetAsync(facilityId, cancellationToken);
        var data = result.Operations;
        return Ok(new ApiResponse<FacilityOperationsReportResponse>(
            new FacilityOperationsReportResponse(
                facilityId, result.AsOf, data.AvailableUnits, data.InUseUnits,
                data.InspectionUnits, data.MaintenanceUnits, result.UsageRate,
                data.OverdueContractCount)));
    }

    /// <summary>REP-002: Facility-scoped rental revenue in a business month.</summary>
    [Authorize(Roles = RoleNames.FacilityManager)]
    [HttpGet("facilities/{facilityId:guid}/revenue")]
    [ProducesResponseType(typeof(ApiResponse<FacilityRevenueReportResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<FacilityRevenueReportResponse>>> GetFacilityRevenueReport(
        Guid facilityId, [FromQuery] string? month = null,
        CancellationToken cancellationToken = default)
    {
        var businessDate = clock.ToBusinessTime(clock.UtcNow);
        var first = ParseMonth(month, new DateOnly(businessDate.Year, businessDate.Month, 1));
        var total = await revenueService.GetFacilityAsync(facilityId,
            first, first.AddMonths(1), cancellationToken);
        return Ok(new ApiResponse<FacilityRevenueReportResponse>(new(
            facilityId, first.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            total.FirstMonthRecognizedRevenue, total.SubsequentPaidRentalRevenue,
            total.RentalRevenue)));
    }

    /// <summary>REP-003: Current business-year-to-date revenue and global physical-unit snapshot.</summary>
    [Authorize(Roles = RoleNames.BusinessOperationsManager)]
    [HttpGet("business/overview")]
    [ProducesResponseType(typeof(ApiResponse<BusinessOverviewReportResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BusinessOverviewReportResponse>>> GetBusinessOverview(
        CancellationToken cancellationToken)
    {
        var result = await overviewService.GetAsync(cancellationToken);
        return Ok(new ApiResponse<BusinessOverviewReportResponse>(new(
            result.FacilityCount, result.ActiveContractCount,
            result.UsageRate, result.RentalRevenue)));
    }

    /// <summary>
    /// REP-004: UTF-8 CSV export. Default revenue is backward-compatible;
    /// facility/unitType/status export the same grouped rows as business/analysis.
    /// </summary>
    [Authorize(Roles = RoleNames.BusinessOperationsManager)]
    [HttpGet("business/export")]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportBusinessReport(
        [FromQuery] Guid? facilityId = null,
        [FromQuery] Guid? unitTypeId = null,
        [FromQuery] int? year = null,
        [FromQuery] int? quarter = null,
        CancellationToken cancellationToken = default,
        [FromQuery] string view = "revenue")
    {
        if (view is not ("revenue" or "facility" or "unitType" or "status"))
            throw new BusinessException("VALIDATION_ERROR", "view must be revenue, facility, unitType or status.", 400);

        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        if (view != "revenue")
            return await ExportGroupedCsvAsync(view, facilityId, unitTypeId, year, quarter, cancellationToken);

        var businessDate = clock.ToBusinessTime(clock.UtcNow);
        var selectedYear = year ?? businessDate.Year;
        if (selectedYear is < 1 or > 9998 || (quarter.HasValue && quarter.Value is < 1 or > 4))
            throw new BusinessException("VALIDATION_ERROR", "Invalid report year or quarter.", 400);

        var firstMonth = quarter.HasValue ? (quarter.Value - 1) * 3 + 1 : 1;
        var months = quarter.HasValue ? 3 : 12;
        var csv = new StringBuilder("month,firstMonthRecognizedRevenue,subsequentPaidRentalRevenue,rentalRevenue\r\n");
        for (var i = 0; i < months; i++)
        {
            var start = new DateOnly(selectedYear, firstMonth, 1).AddMonths(i);
            var end = start.AddMonths(1);
            var result = await revenueService.GetBusinessAsync(
                facilityId, unitTypeId, start, end, cancellationToken);
            csv.Append(start.ToString("yyyy-MM", CultureInfo.InvariantCulture)).Append(',')
                .Append(result.FirstMonthRecognizedRevenue.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(result.SubsequentPaidRentalRevenue.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(result.RentalRevenue.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
        }
        var fileName = $"frms-revenue-{selectedYear}-{(quarter.HasValue ? "Q" + quarter.Value : "full-year")}.csv";
        return File(new UTF8Encoding(false).GetBytes(csv.ToString()), "text/csv; charset=utf-8", fileName);
    }

    private async Task<IActionResult> ExportGroupedCsvAsync(
        string view, Guid? facilityId, Guid? unitTypeId, int? year, int? quarter,
        CancellationToken ct)
    {
        const int pageSize = 100;
        // Authorize and validate ALL inputs before response headers/body are committed.
        var first = await reportReadService.BusinessBreakdownAsync(
            view, facilityId, unitTypeId, year, quarter, 1, pageSize, ct);
        var suffix = first.Quarter.HasValue ? $"Q{first.Quarter.Value}" : "full-year";
        Response.ContentType = "text/csv; charset=utf-8";
        Response.Headers["Content-Disposition"] =
            $"attachment; filename=\"frms-{view}-{first.Year}-{suffix}.csv\"";
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        await Response.WriteAsync(
            "facilityId,facilityName,unitTypeId,unitTypeName,unitTypeSize,totalUnits,availableUnits,inUseUnits,inspectionUnits,maintenanceUnits,usageRate,firstMonthRecognizedRevenue,subsequentPaidRentalRevenue,rentalRevenue\r\n", ct);

        // The export covers ALL filtered rows, not just the first screen page.
        // Only one bounded report page is buffered as a CSV payload at a time.
        for (var page = 1; page <= first.Pagination.TotalPages; page++)
        {
            var batch = page == 1 ? first : await reportReadService.BusinessBreakdownAsync(
                view, facilityId, unitTypeId, year, quarter, page, pageSize, ct);
            foreach (var item in batch.Rows)
            {
                var cols = new[]
                {
                    item.FacilityId?.ToString("D") ?? "", CsvText(item.FacilityName),
                    item.UnitTypeId?.ToString("D") ?? "", CsvText(item.UnitTypeName),
                    CsvText(item.UnitTypeSize),
                    item.TotalUnits.ToString(CultureInfo.InvariantCulture),
                    item.AvailableUnits.ToString(CultureInfo.InvariantCulture),
                    item.InUseUnits.ToString(CultureInfo.InvariantCulture),
                    item.InspectionUnits.ToString(CultureInfo.InvariantCulture),
                    item.MaintenanceUnits.ToString(CultureInfo.InvariantCulture),
                    item.UsageRate.ToString(CultureInfo.InvariantCulture),
                    item.FirstMonthRecognizedRevenue.ToString(CultureInfo.InvariantCulture),
                    item.SubsequentPaidRentalRevenue.ToString(CultureInfo.InvariantCulture),
                    item.RentalRevenue.ToString(CultureInfo.InvariantCulture)
                };
                await Response.WriteAsync(string.Join(',', cols) + "\r\n", ct);
            }
        }
        return new EmptyResult();
    }

    private static string CsvText(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        // Neutralize formula injection in spreadsheet applications, including
        // cells with leading whitespace before a formula sigil.
        var leading = value.TrimStart();
        if (leading.Length > 0 && leading[0] is '=' or '+' or '-' or '@')
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static DateOnly ParseMonth(string? value, DateOnly fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        if (value.Length != 7 || !DateOnly.TryParseExact(value + "-01", "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var month))
            throw new BusinessException("VALIDATION_ERROR", "month must be yyyy-MM.", 400);
        return month;
    }
}
