using Frms.Api.Authorization;
using Frms.Api.DTOs.Responses;
using Frms.Api.Mapping;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Route("api/v1")]
[ApiController]
[Authorize(Roles = RoleNames.Customer + "," + RoleNames.FacilityStaff + "," +
    RoleNames.FacilityManager + "," + RoleNames.BusinessOperationsManager)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
public sealed class BillingController(IBillingService service) : ControllerBase
{
    /// <summary>BIL-001: List authorized persisted Invoices; scope precedes count and paging.</summary>
    [HttpGet("invoices")]
    [ProducesResponseType(typeof(PaginatedResponse<InvoiceDetailResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<InvoiceDetailResponse>>> ListInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await service.ListInvoicesAsync(page, pageSize, cancellationToken);
        return Ok(new PaginatedResponse<InvoiceDetailResponse>(result.Items.Select(BillingMapping.ToResponse).ToArray(),
            new PaginationResponse(result.Page, result.PageSize, result.TotalItems, result.TotalPages)));
    }

    /// <summary>BIL-002: Get authorized Invoice snapshots, including its nullable persisted PaidAt.</summary>
    [HttpGet("invoices/{invoiceId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<InvoiceDetailResponse>>> GetInvoice(
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var result = await service.GetInvoiceAsync(invoiceId, cancellationToken);
        return Ok(new ApiResponse<InvoiceDetailResponse>(result.ToResponse()));
    }

}
