namespace Frms.Api.DTOs.Responses;

/// <summary>Represents the canonical InvoiceDetail schema.</summary>
public sealed record InvoiceDetailResponse(
    Guid InvoiceId,
    Guid EntityId,
    string InvoiceType,
    string? BillingMonth,
    decimal BaseAmount,
    Guid? DiscountId,
    decimal DiscountAmount,
    decimal AmountDue,
    DateTimeOffset DueDate,
    string Status,
    DateTimeOffset? PaidAt);

/// <summary>Represents the canonical PaymentDetail schema.</summary>
public sealed record PaymentDetailResponse(
    Guid PaymentId,
    Guid InvoiceId,
    decimal Amount,
    string PaymentMethod,
    string? TransactionCode,
    string Status,
    DateTimeOffset? PaidAt,
    DateTimeOffset CreatedAt);

/// <summary>Represents a VNPay payment attempt for an existing Invoice.</summary>
public sealed record InvoiceVnPayPaymentResponse(
    Guid PaymentId,
    Guid InvoiceId,
    decimal Amount,
    string PaymentMethod,
    string Status,
    string? PaymentUrl)
{
    public override string ToString() => $"InvoiceVnPayPaymentResponse {{ PaymentId = {PaymentId}, Status = {Status} }}";
}

/// <summary>Provider acknowledgement returned by PAY-004.</summary>
public sealed record VnPayIpnResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("RspCode")] string ResponseCode,
    [property: System.Text.Json.Serialization.JsonPropertyName("Message")] string Message);
