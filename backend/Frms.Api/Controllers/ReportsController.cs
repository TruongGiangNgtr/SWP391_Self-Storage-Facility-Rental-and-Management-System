using Frms.Api.Authorization;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Route("api/v1/reports")]
public sealed class ReportsController : ScaffoldControllerBase
{
    /// <summary>REP-001: Facility Operations report scaffold.</summary>
    [Authorize(Roles = RoleNames.FacilityManager)]
    [HttpGet("facilities/{facilityId:guid}/operations")]
    [ProducesResponseType(typeof(ApiResponse<FacilityOperationsReportResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> GetFacilityOperationsReport(
        Guid facilityId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("REP-001");

    /// <summary>REP-002: Facility revenue report scaffold.</summary>
    [Authorize(Roles = RoleNames.FacilityManager)]
    [HttpGet("facilities/{facilityId:guid}/revenue")]
    public ActionResult<ApiErrorResponse> GetFacilityRevenueReport(
        Guid facilityId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("REP-002");

    /// <summary>REP-003: System-wide Business Overview scaffold.</summary>
    [Authorize(Roles = RoleNames.BusinessOperationsManager)]
    [HttpGet("business/overview")]
    [ProducesResponseType(typeof(ApiResponse<BusinessOverviewReportResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> GetBusinessOverview(CancellationToken cancellationToken) =>
        ScaffoldNotImplemented("REP-003");

    /// <summary>REP-004: UTF-8 CSV business-report export scaffold.</summary>
    [Authorize(Roles = RoleNames.BusinessOperationsManager)]
    [HttpGet("business/export")]
    [Produces("text/csv")]
    public ActionResult<ApiErrorResponse> ExportBusinessReport(CancellationToken cancellationToken) =>
        ScaffoldNotImplemented("REP-004");
}
