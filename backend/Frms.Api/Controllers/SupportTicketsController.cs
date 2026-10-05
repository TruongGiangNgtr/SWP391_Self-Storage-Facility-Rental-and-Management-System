using Frms.Api.Authorization;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Route("api/v1/support-tickets")]
public sealed class SupportTicketsController : ScaffoldControllerBase
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
    public ActionResult<ApiErrorResponse> ListSupportTickets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) => ScaffoldNotImplemented("SUP-002");

    /// <summary>SUP-003: SupportTicket detail scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer + "," + RoleNames.FacilityStaff + "," + RoleNames.FacilityManager)]
    [HttpGet("{ticketId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SupportTicketDetailResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> GetSupportTicket(
        Guid ticketId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("SUP-003");

    /// <summary>SUP-004: Cancel an eligible Customer-owned SupportTicket scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpPost("{ticketId:guid}/cancel")]
    public ActionResult<ApiErrorResponse> CancelSupportTicket(
        Guid ticketId,
        [FromBody] CancelSupportTicketRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("SUP-004");

    /// <summary>SUP-005: Assign same-Facility Staff scaffold.</summary>
    [Authorize(Roles = RoleNames.FacilityManager)]
    [HttpPost("{ticketId:guid}/assign")]
    public ActionResult<ApiErrorResponse> AssignSupportTicket(
        Guid ticketId,
        [FromBody] AssignSupportTicketRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("SUP-005");

    /// <summary>SUP-006: Complete an assigned SupportTicket scaffold.</summary>
    [Authorize(Roles = RoleNames.FacilityStaff)]
    [HttpPost("{ticketId:guid}/complete")]
    public ActionResult<ApiErrorResponse> CompleteSupportTicket(
        Guid ticketId,
        [FromBody] CompleteSupportTicketRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("SUP-006");
}
