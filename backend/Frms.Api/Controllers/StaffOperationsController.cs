using Frms.Api.Authorization;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Frms.Business.Models.Commands;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Authorize(Roles = RoleNames.FacilityStaff)]
[Route("api/v1")]
public sealed class StaffOperationsController(
    IHandoverService handoverService,
    IStaffWorkItemService staffWorkItemService)
    : ScaffoldControllerBase {
    /// <summary>OPS-001: Derived daily Staff work list.</summary>
    [HttpGet("staff/work-items")]
    [ProducesResponseType(
        typeof(PaginatedResponse<StaffWorkItemResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<StaffWorkItemResponse>>>
        GetWorkItems(
            [FromQuery] DateOnly? date,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default) {
        var result = await staffWorkItemService.ListAsync(
            date,
            page,
            pageSize,
            cancellationToken);

        var data = result.Items.Select(x =>
            new StaffWorkItemResponse(
                x.WorkType,
                x.ReferenceId,
                x.EntityId,
                x.ScheduledDate,
                x.Status,
                x.CustomerId.HasValue
                    ? new StaffWorkItemCustomerResponse(
                        x.CustomerId.Value,
                        x.CustomerName!,
                        x.CustomerPhone!)
                    : null
            )).ToArray();

        var totalPages = result.TotalItems == 0
            ? 0
            : (int)Math.Ceiling(
                result.TotalItems / (double)result.PageSize);

        return Ok(new PaginatedResponse<StaffWorkItemResponse>(
            data,
            new PaginationResponse(
                result.Page,
                result.PageSize,
                result.TotalItems,
                totalPages)));
    }

    /// <summary>OPS-004: Atomic Complete Handover.</summary>
    [HttpPost("reservations/{reservationId:guid}/complete-handover")]
    [ProducesResponseType(
        typeof(ApiResponse<CompleteHandoverResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CompleteHandoverResponse>>>
        CompleteHandover(
            Guid reservationId,
            [FromBody] CompleteHandoverRequest request,
            CancellationToken cancellationToken) {
        var result =
            await handoverService.CompleteAsync(
                new CompleteHandoverCommand(
                    reservationId,
                    request.VisitId,
                    request.StorageUnitId,
                    request.DiscountId),
                cancellationToken);

        var response =
            new CompleteHandoverResponse(
                new HandoverContractResponse(
                    result.Contract.ContractId,
                    result.Contract.Status,
                    result.Contract.StorageUnitId,
                    result.Contract.StartMonth.ToString("yyyy-MM"),
                    result.Contract.EndMonth.ToString("yyyy-MM")),
                result.ReservationStatus,
                result.VisitStatus,
                result.StorageUnitStatus);

        return Ok(
            new ApiResponse<CompleteHandoverResponse>(
                response,
                "Handover completed."));
    }
}
