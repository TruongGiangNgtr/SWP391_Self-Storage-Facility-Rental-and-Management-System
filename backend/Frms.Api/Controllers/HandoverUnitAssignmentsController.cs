using Frms.Api.Authorization;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[ApiController]
[Route("api/v1/reservations/{reservationId:guid}/handover-unit-assignment")]
public sealed class HandoverUnitAssignmentsController(IHandoverUnitAssignmentService service)
    : ControllerBase {
    /// <summary>MWP-A01: Persist Manager assignment or reassignment.</summary>
    [HttpPut]
    [Authorize(Roles = RoleNames.FacilityManager)]
    public async Task<ActionResult<ApiResponse<HandoverUnitAssignmentResponse>>> Assign(
        Guid reservationId, [FromBody] AssignHandoverUnitRequest request,
        CancellationToken cancellationToken) {
        var result = await service.AssignAsync(reservationId, request.StorageUnitId, cancellationToken);
        return Ok(new ApiResponse<HandoverUnitAssignmentResponse>(
            HandoverUnitAssignmentResponse.From(result), "Unit assignment saved."));
    }

    /// <summary>MWP-A02: Read currently assigned unit for Manager or Staff.</summary>
    [HttpGet]
    [Authorize(Roles = RoleNames.FacilityManager + "," + RoleNames.FacilityStaff)]
    public async Task<ActionResult<ApiResponse<HandoverUnitAssignmentResponse?>>> Get(
        Guid reservationId, CancellationToken cancellationToken) {
        var result = await service.GetActiveAsync(reservationId, cancellationToken);
        return Ok(new ApiResponse<HandoverUnitAssignmentResponse?>(
            result is null ? null : HandoverUnitAssignmentResponse.From(result)));
    }

    /// <summary>MWP-A03: Release selection, retaining historical assignment event.</summary>
    [HttpDelete]
    [Authorize(Roles = RoleNames.FacilityManager)]
    public async Task<IActionResult> Release(Guid reservationId, CancellationToken cancellationToken) {
        await service.ReleaseAsync(reservationId, cancellationToken);
        return NoContent();
    }
}
