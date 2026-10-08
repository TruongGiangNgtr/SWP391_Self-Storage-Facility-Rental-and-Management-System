using System.Text.Json.Serialization;

namespace Frms.Business.Abstractions.External;

/// <summary>Provider-neutral validation input used before a Payment row is created.</summary>
public sealed record PaymentGatewayPreflight(
    decimal Amount,
    [property: JsonIgnore] string ReturnUrl,
    string ClientIpAddress)
{
    public override string ToString() => nameof(PaymentGatewayPreflight);
}

/// <summary>Provider-neutral request to create a redirect for a persisted Payment attempt.</summary>
public sealed record PaymentGatewayRequest(
    Guid PaymentId,
    decimal Amount,
    [property: JsonIgnore] string ReturnUrl,
    string ClientIpAddress,
    DateTimeOffset CreatedAt,
    long? ProviderOrderCode = null)
{
    public override string ToString() => $"PaymentGatewayRequest {{ PaymentId = {PaymentId} }}";
}

/// <summary>
/// Provider-neutral payment session returned by the payment provider adapter.
/// </summary>
/// <param name="PaymentUrl">Provider redirect URL for the customer.</param>
/// <param name="TransactionCode">Gateway transaction/reference when already issued; otherwise null.</param>
public sealed record PaymentGatewaySession(
    [property: JsonIgnore] string PaymentUrl,
    string? TransactionCode,
    DateTimeOffset? ExpiresAt)
{
    public override string ToString() => nameof(PaymentGatewaySession);
}

public enum PaymentGatewayCreationOutcome { Unknown, SessionCreated }

/// <summary>A locally generated redirect is not proof of payment success.</summary>
public sealed record PaymentGatewayCreationResult(
    PaymentGatewayCreationOutcome Outcome, PaymentGatewaySession? Session, string? TransactionCode)
{
    public override string ToString() => $"PaymentGatewayCreationResult {{ Outcome = {Outcome} }}";
}

/// <summary>
/// Provider-neutral transport envelope for a raw payment callback. Parsing and
/// authenticity checks remain the responsibility of the Infrastructure adapter.
/// </summary>
/// <param name="RawPayload">Unmodified provider callback payload.</param>
public sealed record PaymentGatewayCallbackRequest(
    [property: JsonIgnore] string RawPayload)
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
    DateTimeOffset? PaidAt,
    long? ProviderOrderCode = null)
{
    public override string ToString() => $"PaymentGatewayCallbackResult {{ PaymentId = {PaymentId}, Status = {Status} }}";
}
