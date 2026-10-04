using System.ComponentModel.DataAnnotations;

namespace Frms.Api.DTOs.Requests;

/// <summary>Payload for starting a MoMo payment for an existing invoice (PAY-001).</summary>
public sealed record StartInvoiceMomoPaymentRequest
{
    [Required, Url]
    public required string ReturnUrl { get; init; }
}

/// <summary>Payload for starting a first-month pre-handover payment (PAY-002).</summary>
public sealed record StartFirstMonthMomoPaymentRequest
{
    [Required, Url]
    public required string ReturnUrl { get; init; }
}
