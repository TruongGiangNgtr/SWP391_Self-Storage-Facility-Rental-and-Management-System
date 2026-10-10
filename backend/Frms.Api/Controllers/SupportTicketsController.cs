using Frms.Api.Authorization;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Persistence.Entities;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Route("api/v1/support-tickets")]
public sealed class SupportTicketsController(ISupportTicketService supportTicketService) : ScaffoldControllerBase
{
    /// <summary>SUP-001: Create a Contract-related SupportTicket scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SupportTicketDetailResponse>), StatusCodes.Status201Created)]
    public ActionResult<ApiErrorResponse> CreateSupportTicket(
        [FromBody] CreateSupportTicketRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("SUP-001");

    /// <summary>SUP-002: List accessible SupportTickets scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer + "," + RoleNames.FacilityStaff + "," + RoleNames.FacilityManager)]
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<SupportTicketDetailResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSupportTickets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await supportTicketService.ListAccessibleAsync(page, pageSize, cancellationToken);
        var response = result.Items.Select(ToResponse).ToArray();
        return Ok(new PaginatedResponse<SupportTicketDetailResponse>(
            response, new PaginationResponse(page, pageSize, result.Total,
                result.Total == 0 ? 0 : (int)Math.Ceiling(result.Total / (double)pageSize))));
    }

    /// <summary>SUP-003: SupportTicket detail scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer + "," + RoleNames.FacilityStaff + "," + RoleNames.FacilityManager)]
    [HttpGet("{ticketId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SupportTicketDetailResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SupportTicketDetailResponse>>> GetSupportTicket(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var ticket = await supportTicketService.GetAccessibleAsync(ticketId, cancellationToken);
        return Ok(new ApiResponse<SupportTicketDetailResponse>(ToResponse(ticket)));
    }

    /// <summary>SUP-004: Cancel an eligible Customer-owned SupportTicket scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpPost("{ticketId:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<SupportTicketDetailResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SupportTicketDetailResponse>>> CancelSupportTicket(
        Guid ticketId,
        [FromBody] CancelSupportTicketRequest request,
        CancellationToken cancellationToken)
    {
        var ticket = await supportTicketService.CancelAsync(ticketId, request.Reason, cancellationToken);
        return Ok(new ApiResponse<SupportTicketDetailResponse>(ToResponse(ticket), "SupportTicket cancelled."));
    }

    /// <summary>SUP-005: Assign same-Facility Staff scaffold.</summary>
    [Authorize(Roles = RoleNames.FacilityManager)]
    [HttpPost("{ticketId:guid}/assign")]
    [ProducesResponseType(typeof(ApiResponse<SupportTicketDetailResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SupportTicketDetailResponse>>> AssignSupportTicket(
        Guid ticketId,
        [FromBody] AssignSupportTicketRequest request,
        CancellationToken cancellationToken)
    {
        var ticket = await supportTicketService.AssignAsync(ticketId, request.EmployeeId, cancellationToken);
        return Ok(new ApiResponse<SupportTicketDetailResponse>(ToResponse(ticket), "SupportTicket assigned."));
    }

    /// <summary>SUP-006: Complete an assigned SupportTicket scaffold.</summary>
    [Authorize(Roles = RoleNames.FacilityStaff)]
    [HttpPost("{ticketId:guid}/complete")]
    public ActionResult<ApiErrorResponse> CompleteSupportTicket(
        Guid ticketId,
        [FromBody] CompleteSupportTicketRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("SUP-006");
    private static SupportTicketDetailResponse ToResponse(SupportTicket ticket) => new(
        ticket.SupportTicketId, ticket.ContractId, ticket.CustomerId, ticket.AssignedEmployeeId,
        ticket.Category, ticket.Description, ticket.Status, ticket.ResultNote,
        new DateTimeOffset(DateTime.SpecifyKind(ticket.CreatedAt, DateTimeKind.Utc)),
        ticket.CompletedAt.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(ticket.CompletedAt.Value, DateTimeKind.Utc))
            : null);

}
