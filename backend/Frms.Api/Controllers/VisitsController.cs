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
    /// <summary>VIS-003: List authorized Visits.</summary>
    [Authorize(
        Roles =
            RoleNames.Customer + "," +
            RoleNames.FacilityManager + "," +
            RoleNames.BusinessOperationsManager)]
    [HttpGet]
    [ProducesResponseType(
        typeof(PaginatedResponse<VisitDetailResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> ListVisits(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) {
        var result = await visitService.ListAccessibleAsync(
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

        return Ok(new {
            data,
            pagination = new {
                page,
                pageSize,
                totalItems = result.TotalItems,
                totalPages
            }
        });
    }

    /// <summary>VIS-004: Authorized Visit detail.</summary>
    [Authorize(
        Roles =
            RoleNames.Customer + "," +
            RoleNames.FacilityStaff + "," +
            RoleNames.FacilityManager + "," +
            RoleNames.BusinessOperationsManager)]
    [HttpGet("{visitId:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<VisitDetailResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VisitDetailResponse>>> GetVisit(
        Guid visitId,
        CancellationToken cancellationToken) {
        var visit = await visitService.GetByIdAsync(
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

    /// <summary>OPS-002: Check in a Visit.</summary>
    [Authorize(Roles = RoleNames.FacilityStaff)]
    [HttpPost("{visitId:guid}/check-in")]
    [ProducesResponseType(
        typeof(ApiResponse<VisitDetailResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VisitDetailResponse>>> CheckInVisit(
        Guid visitId,
        CancellationToken cancellationToken)
    {
        var visit =
            await visitService.CheckInAsync(
                visitId,
                cancellationToken);

        return Ok(
            new ApiResponse<VisitDetailResponse>(
                ToResponse(visit),
                "Visit checked in."));
    }

    /// <summary>OPS-003: Check out an ACCESS Visit.</summary>
    [Authorize(Roles = RoleNames.FacilityStaff)]
    [HttpPost("{visitId:guid}/check-out")]
    [ProducesResponseType(
        typeof(ApiResponse<VisitDetailResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VisitDetailResponse>>> CheckOutVisit(
        Guid visitId,
        CancellationToken cancellationToken)
    {
        var visit =
            await visitService.CheckOutAsync(
                visitId,
                cancellationToken);

        return Ok(
            new ApiResponse<VisitDetailResponse>(
                ToResponse(visit),
                "Visit checked out."));
    }

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
