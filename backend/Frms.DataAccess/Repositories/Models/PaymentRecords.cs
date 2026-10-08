using System.Text.Json.Serialization;

namespace Frms.DataAccess.Repositories.Models;

/// <summary>Server-owned Invoice values and scope; actor authorization belongs to Business.</summary>
public sealed record PaymentInvoiceRecord
{
    public Guid InvoiceId { get; init; }
    public required string InvoiceType { get; init; }
    public required string Status { get; init; }
    public decimal AmountDue { get; init; }
    public Guid CustomerId { get; init; }
    public Guid CustomerUserAccountId { get; init; }
    public Guid FacilityId { get; init; }
    public DateOnly? BillingMonth { get; init; }
    public DateOnly? ContractStartMonth { get; init; }
}

/// <summary>PAY-003 projection. Never includes an initiation key or a provider session URL.</summary>
public sealed record PaymentDetailRecord(
    Guid PaymentId, Guid InvoiceId, decimal Amount, string PaymentMethod,
    string? TransactionCode, string Status, DateTimeOffset? PaidAt, DateTimeOffset CreatedAt,
    Guid CustomerId, Guid CustomerUserAccountId, Guid FacilityId);

/// <summary>Internal PAY-001 session data, released by Business only after ownership validation.</summary>
public sealed record PaymentAttemptRecord(
    PaymentDetailRecord Detail, [property: JsonIgnore] Guid IdempotencyKey,
    [property: JsonIgnore] string? PaymentUrl, DateTimeOffset? PaymentUrlExpiresAt, long? ProviderOrderCode = null)
{
    // Provider redirect URLs can carry sensitive session data.
    public override string ToString() => $"PaymentAttempt {{ PaymentId = {Detail.PaymentId} }}";
}

public enum PaymentAttemptOutcome
{
    Created, Existing, NotFound, InvoiceNotPayable, IdempotencyConflict, SessionExpired, ReferenceConflict, AmountUnsupported
}

/// <summary>A conflict never exposes the attempt held by another Invoice.</summary>
public sealed record PaymentAttemptResult(PaymentAttemptOutcome Outcome, PaymentAttemptRecord? Attempt);

public sealed record PaymentSessionRecord(string? TransactionCode, [property: JsonIgnore] string PaymentUrl, DateTimeOffset? ExpiresAt)
{
    public override string ToString() => nameof(PaymentSessionRecord);
}

/// <summary>Only definitive verified outcomes enter the authoritative write path.</summary>
public enum PaymentResultSource { VerifiedCallback }

public enum PaymentFinalStatus { Success, Failed }

public sealed record NormalizedPaymentResult(
    Guid PaymentId, PaymentFinalStatus Status, string? TransactionCode,
    decimal? VerifiedAmount, DateTimeOffset? VerifiedPaidAt, PaymentResultSource Source);

public enum PaymentApplyOutcome
{
    Applied, Duplicate, NotFound, InvalidResult, AmountMismatch, ReferenceConflict, TerminalConflict
}

public sealed record PaymentApplyResult(
    PaymentApplyOutcome Outcome, PaymentDetailRecord? Payment, string? Reason);
