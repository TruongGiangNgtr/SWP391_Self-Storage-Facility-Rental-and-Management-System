using Frms.Api.Authorization;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Route("api/v1/visits")]
public sealed class VisitsController : ScaffoldControllerBase
{
    /// <summary>VIS-003: List own Visits scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<VisitDetailResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> ListVisits(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) => ScaffoldNotImplemented("VIS-003");

    /// <summary>VIS-004: Get own Visit detail scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpGet("{visitId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VisitDetailResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> GetVisit(
        Guid visitId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("VIS-004");

    /// <summary>VIS-005: Reschedule an eligible Visit scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpPatch("{visitId:guid}/schedule")]
    public ActionResult<ApiErrorResponse> RescheduleVisit(
        Guid visitId,
        [FromBody] RescheduleVisitRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("VIS-005");

    /// <summary>VIS-006: Cancel an eligible Visit scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpPost("{visitId:guid}/cancel")]
    public ActionResult<ApiErrorResponse> CancelVisit(
        Guid visitId,
        [FromBody] CancelVisitRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("VIS-006");

    /// <summary>OPS-002: Check in a Visit scaffold.</summary>
    [Authorize(Roles = RoleNames.FacilityStaff)]
    [HttpPost("{visitId:guid}/check-in")]
    public ActionResult<ApiErrorResponse> CheckInVisit(
        Guid visitId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("OPS-002");

    /// <summary>OPS-003: Check out an eligible non-handover Visit scaffold.</summary>
    [Authorize(Roles = RoleNames.FacilityStaff)]
    [HttpPost("{visitId:guid}/check-out")]
    public ActionResult<ApiErrorResponse> CheckOutVisit(
        Guid visitId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("OPS-003");

    /// <summary>OPS-005: Confirm actual return and create Inspection scaffold.</summary>
    [Authorize(Roles = RoleNames.FacilityStaff)]
    [HttpPost("{visitId:guid}/confirm-return")]
    [ProducesResponseType(typeof(ApiResponse<ConfirmActualReturnResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> ConfirmReturn(
        Guid visitId,
        [FromBody] ConfirmActualReturnRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("OPS-005");
}
