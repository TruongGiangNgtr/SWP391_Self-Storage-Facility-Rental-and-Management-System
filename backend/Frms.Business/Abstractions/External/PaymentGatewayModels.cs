namespace Frms.Business.Abstractions.External;

/// <summary>
/// Provider-neutral request to open a payment session for an internal Payment attempt.
/// </summary>
/// <param name="PaymentId">Stable internal attempt correlation; not a provider transaction code.</param>
/// <param name="Amount">Server-authoritative amount of the Payment attempt.</param>
/// <param name="ReturnUrl">Frontend URL the provider redirects to; redirect is not proof of success.</param>
public sealed record PaymentGatewayRequest(
    Guid PaymentId,
    decimal Amount,
    string ReturnUrl)
{
    public override string ToString() => $"PaymentGatewayRequest {{ PaymentId = {PaymentId} }}";
}

/// <summary>
/// Provider-neutral payment session returned by the payment provider adapter.
/// </summary>
/// <param name="PaymentUrl">Provider redirect URL for the customer.</param>
/// <param name="TransactionCode">Gateway transaction/reference when already issued; otherwise null.</param>
public sealed record PaymentGatewaySession(
    string PaymentUrl,
    string? TransactionCode,
    DateTimeOffset? ExpiresAt)
{
    public override string ToString() => nameof(PaymentGatewaySession);
}

public enum PaymentGatewayCreationOutcome { Unknown, SessionCreated, DefinitiveRejection }

/// <summary>Only DefinitiveRejection is proof of failure before a session exists.</summary>
public sealed record PaymentGatewayCreationResult(
    PaymentGatewayCreationOutcome Outcome, PaymentGatewaySession? Session, string? TransactionCode)
{
    public override string ToString() => $"PaymentGatewayCreationResult {{ Outcome = {Outcome} }}";
}

/// <summary>
/// Provider-neutral transport envelope for a raw payment callback. Parsing and
/// authenticity checks remain the responsibility of the Infrastructure adapter.
/// </summary>
/// <param name="RawBody">Unmodified callback body supplied by the provider.</param>
public sealed record PaymentGatewayCallbackRequest(
    string RawBody)
{
    public override string ToString() => nameof(PaymentGatewayCallbackRequest);
}

/// <summary>Only verified, definitive Success/Failed outcomes may reach persistence.</summary>
public enum PaymentGatewayCallbackOutcome { Unverified, Unknown, Pending, Success, Failed }

/// <summary>
/// Authenticated and normalized callback data returned by the payment provider adapter.
/// </summary>
/// <param name="PaymentId">Internal Payment identifier resolved from the provider reference.</param>
/// <param name="Amount">Provider-reported amount retained for server-side comparison.</param>
/// <param name="TransactionCode">Gateway transaction/reference used for idempotency.</param>
/// <param name="Status">Normalized payment outcome.</param>
public sealed record PaymentGatewayCallbackResult(
    Guid PaymentId,
    decimal? Amount,
    string? TransactionCode,
    PaymentGatewayCallbackOutcome Status,
    DateTimeOffset? PaidAt)
{
    public override string ToString() => $"PaymentGatewayCallbackResult {{ PaymentId = {PaymentId}, Status = {Status} }}";
}
