using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Frms.Business.Models.Commands;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace Frms.Api.Controllers;

[Authorize]
[Route("api/v1/reservations")]
public sealed class ReservationsController(
    IReservationService reservationService)
    : ScaffoldControllerBase {
    /// <summary>RES-001: Create Reservation and Deposit Invoice.</summary>
    [HttpPost]
    [Authorize(Roles = "CUSTOMER")]
    [ProducesResponseType(
        typeof(ApiResponse<ReservationDetailResponse>),
        StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ReservationDetailResponse>>> CreateReservation(
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

        var response = new ReservationDetailResponse(
            result.ReservationId,
            result.FacilityId,
            result.UnitTypeId,
            result.PolicyId,
            result.StartMonth.ToString("yyyy-MM"),
            result.EndMonth.ToString("yyyy-MM"),
            result.LockedRentalPrice,
            result.DepositAmount,
            result.Status,
            new DepositInvoiceSummaryResponse(
                result.DepositInvoice.InvoiceId,
                result.DepositInvoice.Status,
                result.DepositInvoice.AmountDue,
                ToUtcOffset(result.DepositInvoice.DueDate)),
            null,
            ToUtcOffset(result.CreatedAt));

        return StatusCode(
            StatusCodes.Status201Created,
            new ApiResponse<ReservationDetailResponse>(
                response,
                "Reservation created."));
    }

    /// <summary>RES-002: List authorized Reservations.</summary>
    [HttpGet]
    [Authorize(Roles = "CUSTOMER,FACILITY_MANAGER,BUSINESS_OPERATIONS_MANAGER")]
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
            .Select(MapReservationDetail)
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

    /// <summary>RES-003: Get authorized Reservation detail.</summary>
    [HttpGet("{reservationId:guid}")]
    [Authorize(Roles = "CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER,BUSINESS_OPERATIONS_MANAGER")]
    [ProducesResponseType(
        typeof(ApiResponse<ReservationDetailResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ReservationDetailResponse>>> GetReservation(
        Guid reservationId,
        CancellationToken cancellationToken) {
        var result = await reservationService.GetByIdAsync(
            reservationId,
            cancellationToken);

        return Ok(
            new ApiResponse<ReservationDetailResponse>(
                MapReservationDetail(result),
                "Reservation retrieved."));
    }

    /// <summary>RES-004: Confirm Reservation after successful Deposit payment.</summary>
    [HttpPost("{reservationId:guid}/confirm")]
    [Authorize(Roles = "CUSTOMER")]
    [ProducesResponseType(
        typeof(ApiResponse<ConfirmReservationResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ConfirmReservationResponse>>> ConfirmReservation(
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

        return Ok(
            new ApiResponse<ConfirmReservationResponse>(
                response,
                "Reservation confirmed."));
    }

    /// <summary>RES-005: Cancel eligible Reservation scaffold.</summary>
    [HttpPost("{reservationId:guid}/cancel")]
    [Authorize(Roles = "CUSTOMER")]
    [ProducesResponseType(
        typeof(ApiResponse<ReservationDetailResponse>),
        StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> CancelReservation(
        Guid reservationId,
        [FromBody] CancelReservationRequest request,
        CancellationToken cancellationToken)
        => ScaffoldNotImplemented("RES-005");

    private static ReservationDetailResponse MapReservationDetail(
        ReservationDetailRecord result) {
        return new ReservationDetailResponse(
            result.ReservationId,
            result.FacilityId,
            result.UnitTypeId,
            result.PolicyId,
            result.StartMonth.ToString("yyyy-MM"),
            result.EndMonth.ToString("yyyy-MM"),
            result.LockedRentalPrice,
            result.DepositAmount,
            result.Status,
            new DepositInvoiceSummaryResponse(
                result.DepositInvoice.InvoiceId,
                result.DepositInvoice.Status,
                result.DepositInvoice.AmountDue,
                ToUtcOffset(result.DepositInvoice.DueDate)),
            result.ReservationVisit is null
                ? null
                : new ReservationVisitSummaryResponse(
                    result.ReservationVisit.VisitId,
                    result.ReservationVisit.VisitType,
                    result.ReservationVisit.VisitDate,
                    result.ReservationVisit.Status),
            ToUtcOffset(result.CreatedAt));
    }

    private static DateTimeOffset ToUtcOffset(DateTime value) {
        return new DateTimeOffset(
            DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
