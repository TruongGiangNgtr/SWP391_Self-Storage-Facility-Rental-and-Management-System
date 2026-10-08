using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Frms.Api.Authorization;
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
    /// <summary>PAY-001: Start/retrieve an authorized Invoice payment, with server-owned redirects/amount.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpPost("invoices/{invoiceId:guid}/payments/payos")]
    [ProducesResponseType(typeof(ApiResponse<InvoicePayOsPaymentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<InvoicePayOsPaymentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<InvoicePayOsPaymentResponse>>> StartInvoicePayOsPayment(
        Guid invoiceId, [FromHeader(Name = "Idempotency-Key"), Required] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(idempotencyKey, out var key) || key == Guid.Empty)
            throw new BusinessException("VALIDATION_ERROR", "A non-empty UUID Idempotency-Key is required.", 400);
        var result = await service.StartInvoicePaymentAsync(
            invoiceId, new StartPaymentCommand(key, string.Empty, string.Empty), cancellationToken);
        var response = new ApiResponse<InvoicePayOsPaymentResponse>(new(result.PaymentId, result.InvoiceId,
            result.Amount, result.PaymentMethod, result.Status, result.PaymentUrl));
        return result.NewlyInitiated && result.Outcome == PaymentStartOutcome.SessionAvailable
            ? CreatedAtAction(nameof(GetPayment), new { paymentId = result.PaymentId }, response) : Ok(response);
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

    /// <summary>PAY-004: signature-verified JSON webhook. Browser return/cancel never mutate state.</summary>
    [AllowAnonymous]
    [HttpPost("payments/payos/webhook")]
    [RequestSizeLimit(65_536)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(PayOsWebhookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PayOsWebhook([FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        var raw = payload.GetRawText();
        if (Encoding.UTF8.GetByteCount(raw) > 65_536)
            return StatusCode(413, new ApiErrorResponse("PAYMENT_CALLBACK_TOO_LARGE", "The callback is too large.", HttpContext.TraceIdentifier));
        try
        {
            var result = await service.ProcessCallbackAsync(new PaymentGatewayCallbackRequest(raw), cancellationToken);
            return result.Outcome switch
            {
                PaymentApplicationOutcome.Applied or PaymentApplicationOutcome.Duplicate or PaymentApplicationOutcome.TerminalConflict
                    => Ok(new PayOsWebhookResponse("ACKNOWLEDGED")),
                PaymentApplicationOutcome.NotFound => Ok(new PayOsWebhookResponse("IGNORED_UNKNOWN_ORDER")),
                PaymentApplicationOutcome.AmountMismatch => Conflict(new ApiErrorResponse("AMOUNT_MISMATCH", "The payment amount does not match.", HttpContext.TraceIdentifier)),
                PaymentApplicationOutcome.ReferenceConflict => Conflict(new ApiErrorResponse("REFERENCE_CONFLICT", "The payment reference conflicts.", HttpContext.TraceIdentifier)),
                _ => BadRequest(new ApiErrorResponse("PAYMENT_CALLBACK_INVALID", "The payment callback could not be verified.", HttpContext.TraceIdentifier))
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            // Non-2xx permits delivery retry; never acknowledge a failed persistence call.
            return StatusCode(503, new ApiErrorResponse("PAYMENT_CALLBACK_UNAVAILABLE", "Payment processing is temporarily unavailable.", HttpContext.TraceIdentifier));
        }
    }
}
