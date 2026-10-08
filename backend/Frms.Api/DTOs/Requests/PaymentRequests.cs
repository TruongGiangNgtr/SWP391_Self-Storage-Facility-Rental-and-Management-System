using System.ComponentModel.DataAnnotations;

namespace Frms.Api.DTOs.Requests;

/// <summary>Payload for starting a VNPay payment for an existing invoice (PAY-001).</summary>
public sealed record StartInvoiceVnPayPaymentRequest
{
    [Required, Url]
    public required string ReturnUrl { get; init; }
}
