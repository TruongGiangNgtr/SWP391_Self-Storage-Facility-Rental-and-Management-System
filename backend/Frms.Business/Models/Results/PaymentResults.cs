namespace Frms.Business.Models.Results;

/// <summary>Authoritative Payment state (PAY-003 and PAY-004 outcome). Timestamps are UTC.</summary>
public sealed record PaymentResult(
    Guid PaymentId,
    Guid InvoiceId,
    decimal Amount,
    string PaymentMethod,
    string? TransactionCode,
    string Status,
    DateTimeOffset? PaidAt,
    DateTimeOffset CreatedAt);

/// <summary>Pending payment attempt started for an existing Deposit/Rental Fee Invoice (PAY-001).</summary>
public sealed record InvoicePaymentStartResult(
    Guid PaymentId,
    Guid InvoiceId,
    decimal Amount,
    string PaymentMethod,
    string Status,
    string PaymentUrl);
