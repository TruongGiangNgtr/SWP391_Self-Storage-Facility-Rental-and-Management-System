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

/// <summary>Application outcome, not a persisted Payment status or an HTTP contract.</summary>
public enum PaymentStartOutcome { SessionAvailable, SessionUnavailable, Terminal, ReferenceConflict }

/// <summary>An authorized attempt; only a usable PENDING session may expose its URL.</summary>
public sealed record InvoicePaymentStartResult(
    Guid PaymentId,
    Guid InvoiceId,
    decimal Amount,
    string PaymentMethod,
    string Status,
    [property: System.Text.Json.Serialization.JsonIgnore] string? PaymentUrl,
    PaymentStartOutcome Outcome,
    DateTimeOffset? PaymentUrlExpiresAt,
    bool NewlyInitiated = false)
{
    public override string ToString() => $"InvoicePaymentStartResult {{ PaymentId = {PaymentId}, Status = {Status}, Outcome = {Outcome} }}";
}

public enum PaymentApplicationOutcome
{
    Applied, Duplicate, NotFound, InvalidResult, AmountMismatch, ReferenceConflict, TerminalConflict,
    VerificationRejected, Unresolved
}

public sealed record PaymentApplicationResult(
    PaymentApplicationOutcome Outcome, PaymentResult? Payment, string? Reason);
