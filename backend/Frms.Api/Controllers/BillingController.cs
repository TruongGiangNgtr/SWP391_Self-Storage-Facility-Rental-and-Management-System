using Frms.Api.Authorization;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Route("api/v1")]
public sealed class BillingController : ScaffoldControllerBase
{
    /// <summary>BIL-001: List own Invoices scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpGet("invoices")]
    [ProducesResponseType(typeof(PaginatedResponse<InvoiceDetailResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> ListInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) => ScaffoldNotImplemented("BIL-001");

    /// <summary>BIL-002: Get own Invoice detail scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpGet("invoices/{invoiceId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDetailResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> GetInvoice(
        Guid invoiceId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("BIL-002");

}
