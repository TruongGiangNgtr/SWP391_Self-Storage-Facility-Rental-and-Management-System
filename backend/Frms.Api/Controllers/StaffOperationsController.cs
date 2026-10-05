using Frms.Api.Authorization;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Authorize(Roles = RoleNames.FacilityStaff)]
[Route("api/v1")]
public sealed class StaffOperationsController : ScaffoldControllerBase
{
    /// <summary>OPS-001: Derived daily Staff work list scaffold.</summary>
    [HttpGet("staff/work-items")]
    [ProducesResponseType(typeof(PaginatedResponse<StaffWorkItemResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> GetWorkItems(
        [FromQuery] DateOnly? date,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) => ScaffoldNotImplemented("OPS-001");

    /// <summary>OPS-004: Atomic Complete Handover scaffold.</summary>
    [HttpPost("reservations/{reservationId:guid}/complete-handover")]
    [ProducesResponseType(typeof(ApiResponse<CompleteHandoverResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> CompleteHandover(
        Guid reservationId,
        [FromBody] CompleteHandoverRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("OPS-004");
}
