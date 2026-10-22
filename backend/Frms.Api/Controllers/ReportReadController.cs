using Frms.Api.Authorization;
using Frms.Api.DTOs.Responses;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

/// <summary>
/// Read-only reporting projections required by the existing Manager/BOM report pages.
/// Existing REP-001..004 endpoints and contracts remain unchanged.
/// </summary>
[Route("api/v1/reports")]
public sealed class ReportReadController(IReportReadService service) : ControllerBase
{
    /// <summary>
    /// Current physical snapshot and evidence-based daily history. The inventory
    /// is CURRENT even when a historical month is selected; do not mislabel it.
    /// </summary>
    [Authorize(Roles = RoleNames.FacilityManager)]
    [HttpGet("facilities/{facilityId:guid}/units")]
    public async Task<ActionResult<ApiResponse<FacilityUnitReadReport>>> FacilityUnits(
        Guid facilityId, [FromQuery] string? month = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(new ApiResponse<FacilityUnitReadReport>(
            await service.FacilityUnitsAsync(facilityId, month, page, pageSize, cancellationToken)));

    /// <summary>
    /// BOM grouped physical snapshot plus recognized rent for selected period.
    /// view=facility|unitType|status; year/quarter apply to revenue only.
    /// </summary>
    [Authorize(Roles = RoleNames.BusinessOperationsManager)]
    [HttpGet("business/analysis")]
    public async Task<ActionResult<ApiResponse<BusinessReportBreakdown>>> BusinessAnalysis(
        [FromQuery] string view = "facility", [FromQuery] Guid? facilityId = null,
        [FromQuery] Guid? unitTypeId = null, [FromQuery] int? year = null,
        [FromQuery] int? quarter = null, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(new ApiResponse<BusinessReportBreakdown>(
            await service.BusinessBreakdownAsync(view, facilityId, unitTypeId,
                year, quarter, page, pageSize, cancellationToken)));

    /// <summary>One year/quarter of monthly revenue rows for BOM chart and table.</summary>
    [Authorize(Roles = RoleNames.BusinessOperationsManager)]
    [HttpGet("business/monthly")]
    public async Task<ActionResult<ApiResponse<BusinessReportMonthly>>> BusinessMonthly(
        [FromQuery] Guid? facilityId = null, [FromQuery] Guid? unitTypeId = null,
        [FromQuery] int? year = null, [FromQuery] int? quarter = null,
        CancellationToken cancellationToken = default) =>
        Ok(new ApiResponse<BusinessReportMonthly>(
            await service.BusinessMonthlyAsync(facilityId, unitTypeId, year, quarter, cancellationToken)));
}
