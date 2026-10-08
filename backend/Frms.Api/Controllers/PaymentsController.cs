using System.ComponentModel.DataAnnotations;
using Frms.Api.Authorization;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Frms.Business.Abstractions.External;
using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[ApiController]
[Route("api/v1")]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
public sealed class PaymentsController(IPaymentService service) : ControllerBase
{
    /// <summary>PAY-001: Start or recover the same authorized Invoice payment attempt.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpPost("invoices/{invoiceId:guid}/payments/vnpay")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceVnPayPaymentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceVnPayPaymentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<InvoiceVnPayPaymentResponse>>> StartInvoiceVnPayPayment(
        Guid invoiceId, [FromHeader(Name = "Idempotency-Key"), Required] string idempotencyKey,
        [FromBody] StartInvoiceVnPayPaymentRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(idempotencyKey, out var key) || key == Guid.Empty)
            throw new BusinessException("VALIDATION_ERROR", "A non-empty UUID Idempotency-Key is required.", 400);
        var clientIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var result = await service.StartInvoicePaymentAsync(
            invoiceId, new StartPaymentCommand(key, request.ReturnUrl, clientIpAddress), cancellationToken);
        var response = new ApiResponse<InvoiceVnPayPaymentResponse>(new(result.PaymentId, result.InvoiceId,
            result.Amount, result.PaymentMethod, result.Status, result.PaymentUrl));
        return result.NewlyInitiated && result.Outcome == PaymentStartOutcome.SessionAvailable
            ? CreatedAtAction(nameof(GetPayment), new { paymentId = result.PaymentId }, response)
            : Ok(response);
    }

    /// <summary>PAY-003: Read Payment detail within authoritative ownership/Facility scope.</summary>
    [Authorize(Roles = RoleNames.Customer + "," + RoleNames.FacilityStaff + "," + RoleNames.FacilityManager + "," + RoleNames.BusinessOperationsManager)]
    [HttpGet("payments/{paymentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PaymentDetailResponse>>> GetPayment(Guid paymentId, CancellationToken cancellationToken)
    {
        var p = await service.GetByIdAsync(paymentId, cancellationToken);
        return Ok(new ApiResponse<PaymentDetailResponse>(new(p.PaymentId, p.InvoiceId, p.Amount,
            p.PaymentMethod, p.TransactionCode, p.Status, p.PaidAt, p.CreatedAt)));
    }

    /// <summary>PAY-004: checksum-verified VNPay IPN acknowledgement.</summary>
    [AllowAnonymous]
    [HttpGet("payments/vnpay/ipn")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(VnPayIpnResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<VnPayIpnResponse>> VnPayIpn(CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.ProcessCallbackAsync(
                new PaymentGatewayCallbackRequest(Request.QueryString.Value ?? string.Empty), cancellationToken);
            return Ok(result.Outcome switch
            {
                PaymentApplicationOutcome.Applied
                    => new VnPayIpnResponse("00", "Confirm Success"),
                PaymentApplicationOutcome.Duplicate or PaymentApplicationOutcome.TerminalConflict
                    => new VnPayIpnResponse("02", "Order already confirmed"),
                PaymentApplicationOutcome.NotFound
                    => new VnPayIpnResponse("01", "Order not found"),
                PaymentApplicationOutcome.AmountMismatch
                    => new VnPayIpnResponse("04", "Invalid amount"),
                PaymentApplicationOutcome.VerificationRejected
                    => new VnPayIpnResponse("97", "Invalid signature"),
                _ => new VnPayIpnResponse("99", "Invalid request")
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            // RspCode 99 asks VNPay to retry and does not claim that persistence succeeded.
            return Ok(new VnPayIpnResponse("99", "Internal processing error"));
        }
    }
}
