using Frms.Api.Authorization;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Persistence.Entities;

namespace Frms.Api.Controllers;

[Route("api/v1/visits")]
public sealed class VisitsController(
    IVisitService visitService) : ScaffoldControllerBase
{
    /// <summary>VIS-003: List own Visits.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpGet]
    [ProducesResponseType(
        typeof(PaginatedResponse<VisitDetailResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> ListVisits(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await visitService.ListOwnAsync(
            page,
            pageSize,
            cancellationToken);

        var data = result.Items
            .Select(ToResponse)
            .ToArray();

        var totalPages = result.TotalItems == 0
            ? 0
            : (int)Math.Ceiling(
                result.TotalItems / (double)pageSize);

        return Ok(new
        {
            data,
            pagination = new
            {
                page,
                pageSize,
                totalItems = result.TotalItems,
                totalPages
            }
        });
    }

    /// <summary>VIS-004: Get own Visit detail.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpGet("{visitId:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<VisitDetailResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VisitDetailResponse>>> GetVisit(
        Guid visitId,
        CancellationToken cancellationToken)
    {
        var visit = await visitService.GetOwnAsync(
            visitId,
            cancellationToken);

        return Ok(
            new ApiResponse<VisitDetailResponse>(
                ToResponse(visit)));
    }

    /// <summary>VIS-005: Reschedule an eligible Visit.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpPatch("{visitId:guid}/schedule")]
    [ProducesResponseType(
        typeof(ApiResponse<VisitDetailResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VisitDetailResponse>>> RescheduleVisit(
        Guid visitId,
        [FromBody] RescheduleVisitRequest request,
        CancellationToken cancellationToken)
    {
        var visit = await visitService.RescheduleAsync(
            visitId,
            request.VisitDate,
            cancellationToken);

        return Ok(
            new ApiResponse<VisitDetailResponse>(
                ToResponse(visit),
                "Visit rescheduled."));
    }

    /// <summary>VIS-006: Cancel an eligible Visit.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpPost("{visitId:guid}/cancel")]
    [ProducesResponseType(
        typeof(ApiResponse<VisitDetailResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VisitDetailResponse>>> CancelVisit(
        Guid visitId,
        [FromBody] CancelVisitRequest request,
        CancellationToken cancellationToken)
    {
        var visit = await visitService.CancelAsync(
            visitId,
            request.Reason,
            cancellationToken);

        return Ok(
            new ApiResponse<VisitDetailResponse>(
                ToResponse(visit),
                "Visit cancelled."));
    }

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

    private static VisitDetailResponse ToResponse(Visit visit)
    {
        return new VisitDetailResponse(
            visit.VisitId,
            visit.EntityId,
            visit.VisitType,
            visit.VisitDate,
            visit.ActualReturnDate is null
                ? null
                : DateOnly.FromDateTime(visit.ActualReturnDate.Value),
            visit.Status,
            visit.EmployeeId);
    }
}
