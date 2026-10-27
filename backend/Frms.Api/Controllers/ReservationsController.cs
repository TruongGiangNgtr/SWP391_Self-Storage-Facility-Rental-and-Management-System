
using System.Globalization;
using Frms.Api.Authorization;
using Frms.Api.Mapping;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Frms.Business.Models.Commands;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Authorize]
[Route("api/v1/reservations")]
public sealed class ReservationsController(IReservationService reservationService) : ScaffoldControllerBase
{
    /// <summary>RES-001: Create Reservation and Deposit Invoice; saved Customer CCCD must be 12 ASCII digits.</summary>
    [HttpPost]
    [Authorize(Roles = "CUSTOMER")]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<ReservationDetailResponse>),
        StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ReservationDetailResponse>>>
        CreateReservation(
            [FromBody] CreateReservationRequest request,
            CancellationToken cancellationToken) {
        var startMonth = DateOnly.ParseExact(
            $"{request.StartMonth}-01",
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);

        var endMonth = DateOnly.ParseExact(
            $"{request.EndMonth}-01",
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);

        var result = await reservationService.CreateAsync(
            new CreateReservationCommand(
                request.FacilityId,
                request.UnitTypeId,
                startMonth,
                endMonth),
            cancellationToken);

        var detail = await reservationService.GetByIdAsync(
            result.ReservationId,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new ApiResponse<ReservationDetailResponse>(
                detail.ToResponse(),
                "Reservation created."));
    }

    /// <summary>RES-002: List authorized Reservations.</summary>
    [HttpGet]
    [Authorize(
        Roles =
            RoleNames.Customer + "," +
            RoleNames.FacilityManager + "," +
            RoleNames.BusinessOperationsManager)]
    [ProducesResponseType(
        typeof(PaginatedResponse<ReservationDetailResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> ListReservations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) {
        var result = await reservationService.ListAsync(
            page,
            pageSize,
            cancellationToken);

        var data = result.Items
            .Select(x => x.ToResponse())
            .ToArray();

        return Ok(new {
            data,
            pagination = new {
                page = result.Page,
                pageSize = result.PageSize,
                totalItems = result.TotalItems,
                totalPages = result.TotalPages
            }
        });
    }

    /// <summary>RES-003: Authorized Reservation detail.</summary>
    [HttpGet("{reservationId:guid}")]
    [Authorize(
        Roles =
            "CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER,BUSINESS_OPERATIONS_MANAGER")]
    [ProducesResponseType(
        typeof(ApiResponse<ReservationDetailResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ReservationDetailResponse>>>
        GetReservation(
            Guid reservationId,
            CancellationToken cancellationToken) {
        var result = await reservationService.GetByIdAsync(
            reservationId,
            cancellationToken);

        return Ok(new ApiResponse<ReservationDetailResponse>(
            result.ToResponse(),
            "Reservation retrieved."));
    }

    /// <summary>RES-004: Confirm Reservation after Deposit payment.</summary>
    [HttpPost("{reservationId:guid}/confirm")]
    [Authorize(Roles = "CUSTOMER")]
    [ProducesResponseType(
        typeof(ApiResponse<ConfirmReservationResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ConfirmReservationResponse>>>
        ConfirmReservation(
            Guid reservationId,
            [FromBody] ConfirmReservationRequest request,
            CancellationToken cancellationToken) {
        var result = await reservationService.ConfirmAsync(
            new ConfirmReservationCommand(
                reservationId,
                request.ReservationVisitDate),
            cancellationToken);

        var response = new ConfirmReservationResponse(
            result.ReservationId,
            result.Status,
            new ReservationVisitSummaryResponse(
                result.ReservationVisit.VisitId,
                result.ReservationVisit.VisitType,
                result.ReservationVisit.VisitDate,
                result.ReservationVisit.Status));

        return Ok(new ApiResponse<ConfirmReservationResponse>(
            response,
            "Reservation confirmed."));
    }

    /// <summary>
    /// RES-005: Cancel eligible Reservation and scheduled Visit.
    /// </summary>
    [HttpPost("{reservationId:guid}/cancel")]
    [Authorize(Roles = "CUSTOMER")]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiErrorResponse),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        typeof(ApiResponse<ReservationDetailResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ReservationDetailResponse>>>
        CancelReservation(
            Guid reservationId,
            [FromBody] CancelReservationRequest request,
            CancellationToken cancellationToken) {
        // CancelAsync returns ReservationCancellationResult,
        // not ReservationDetailRecord.
        var result = await reservationService.CancelAsync(
            new CancelReservationCommand(
                reservationId,
                request.Reason),
            cancellationToken);

        // Read authorized persisted detail after cancellation.
        // This provides CustomerId from the Reservation record.
        var detail = await reservationService.GetByIdAsync(
            result.ReservationId,
            cancellationToken);

        return Ok(new ApiResponse<ReservationDetailResponse>(
            detail.ToResponse(),
            "Reservation cancelled."));
    }
}
