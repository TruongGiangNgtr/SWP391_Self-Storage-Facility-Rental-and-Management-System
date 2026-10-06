namespace Frms.Business.Abstractions.External;

/// <summary>
/// Provider-neutral request to open a payment session for an internal Payment attempt.
/// </summary>
/// <param name="PaymentId">Internal Payment identifier used as the provider reference.</param>
/// <param name="Amount">Server-authoritative amount of the Payment attempt.</param>
/// <param name="ReturnUrl">Frontend URL the provider redirects to; redirect is not proof of success.</param>
public sealed record PaymentGatewayRequest(
    Guid PaymentId,
    decimal Amount,
    string ReturnUrl);

/// <summary>
/// Provider-neutral payment session returned by the payment provider adapter.
/// </summary>
/// <param name="PaymentUrl">Provider redirect URL for the customer.</param>
/// <param name="TransactionCode">Gateway transaction/reference when already issued; otherwise null.</param>
public sealed record PaymentGatewaySession(
    string PaymentUrl,
    string? TransactionCode);
