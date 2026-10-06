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

    /// <summary>PAY-001: Start MoMo payment for an existing Invoice scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpPost("invoices/{invoiceId:guid}/payments/momo")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceMomoPaymentResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> StartInvoiceMomoPayment(
        Guid invoiceId,
        [FromBody] StartInvoiceMomoPaymentRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("PAY-001");

    /// <summary>PAY-003: Get own Payment status scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpGet("payments/{paymentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDetailResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> GetPayment(
        Guid paymentId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("PAY-003");

    /// <summary>PAY-004: Provider-defined MoMo callback scaffold; wire payload is intentionally unspecified.</summary>
    [AllowAnonymous]
    [HttpPost("payments/momo/callback")]
    public ActionResult<ApiErrorResponse> MomoCallback(CancellationToken cancellationToken) =>
        ScaffoldNotImplemented("PAY-004");
}
