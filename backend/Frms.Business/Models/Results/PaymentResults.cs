namespace Frms.Business.Models.Results;

/// <summary>Authoritative Payment state (PAY-003 and PAY-004 outcome). Timestamps are UTC.</summary>
public sealed record PaymentResult(
    Guid PaymentId,
    Guid? InvoiceId,
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

/// <summary>
/// Pending first-month pre-handover payment attempt (PAY-002).
/// <see cref="InvoiceId"/> is null until Complete Handover links the first Rental Fee Invoice.
/// <see cref="Amount"/> equals Reservation.LockedRentalPrice; Contract Discount is not applied.
/// </summary>
public sealed record FirstMonthPaymentStartResult(
    Guid PaymentId,
    Guid? InvoiceId,
    Guid ReservationId,
    decimal Amount,
    string Status,
    string PaymentUrl);
