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

/// <summary>Represents a pending MoMo payment for an existing Invoice.</summary>
public sealed record InvoiceMomoPaymentResponse(
    Guid PaymentId,
    Guid InvoiceId,
    decimal Amount,
    string PaymentMethod,
    string Status,
    string PaymentUrl);
