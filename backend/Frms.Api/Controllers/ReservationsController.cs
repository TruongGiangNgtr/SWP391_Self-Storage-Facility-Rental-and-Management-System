using Frms.Api.Authorization;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Frms.Business.Models.Commands;
using Frms.Business.Services.Interfaces;

namespace Frms.Api.Controllers;

[Authorize(Roles = RoleNames.Customer)]
[Route("api/v1/reservations")]
public sealed class ReservationsController(
    IReservationService reservationService)
    : ScaffoldControllerBase
{
    /// <summary>RES-001: Create Reservation and Deposit Invoice.</summary>
    [HttpPost]
    [ProducesResponseType(
        typeof(ApiResponse<ReservationDetailResponse>),
        StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ReservationDetailResponse>>> CreateReservation(
        [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
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
                new DateTimeOffset(
                    DateTime.SpecifyKind(
                        result.DepositInvoice.DueDate,
                        DateTimeKind.Utc))),
            null,
            new DateTimeOffset(
                DateTime.SpecifyKind(
                    result.CreatedAt,
                    DateTimeKind.Utc)));

        return StatusCode(
            StatusCodes.Status201Created,
            new ApiResponse<ReservationDetailResponse>(
                response,
                "Reservation created."));
    }

    /// <summary>RES-002: List own Reservations scaffold.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<ReservationDetailResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> ListReservations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) => ScaffoldNotImplemented("RES-002");

    /// <summary>RES-003: Get own Reservation detail scaffold.</summary>
    [HttpGet("{reservationId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ReservationDetailResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> GetReservation(
        Guid reservationId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("RES-003");

    /// <summary>RES-004: Confirm Reservation scaffold.</summary>
    [HttpPost("{reservationId:guid}/confirm")]
    [ProducesResponseType(typeof(ApiResponse<ConfirmReservationResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> ConfirmReservation(
        Guid reservationId,
        [FromBody] ConfirmReservationRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("RES-004");

    /// <summary>RES-005: Cancel eligible Reservation scaffold.</summary>
    [HttpPost("{reservationId:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<ReservationDetailResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> CancelReservation(
        Guid reservationId,
        [FromBody] CancelReservationRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("RES-005");
}
