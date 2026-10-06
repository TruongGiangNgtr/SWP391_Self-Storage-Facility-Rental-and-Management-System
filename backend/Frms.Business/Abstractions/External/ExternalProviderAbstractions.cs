namespace Frms.Business.Abstractions.External;

/// <summary>
/// Business-owned, provider-neutral boundary for the payment provider (EPS-01).
/// Provider wire DTOs, credentials, endpoints, result codes and signature handling
/// remain inside the Infrastructure adapter that implements this interface.
/// </summary>
public interface IPaymentGateway
{
    Task EnsureConfiguredAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Creates a provider payment session for an already persisted internal Payment attempt.
    /// A returned session is never proof of payment success; Payment state changes only
    /// through a verified and normalized payment result (PAY-004).
    /// </summary>
    Task<PaymentGatewaySession> CreatePaymentAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verifies a raw provider callback and returns only authenticated,
    /// provider-neutral payment data for PAY-004 processing.
    /// </summary>
    Task<PaymentGatewayCallbackResult> VerifyAndNormalizeCallbackAsync(
        PaymentGatewayCallbackRequest request,
        CancellationToken cancellationToken);
}

/// <summary>Configuration boundary for the optional, non-authoritative AI recommendation provider.</summary>
public interface IAiRecommendationProvider
{
    Task EnsureConfiguredAsync(CancellationToken cancellationToken);
}

/// <summary>Configuration boundary for retryable notification delivery.</summary>
public interface INotificationSender
{
    Task EnsureConfiguredAsync(CancellationToken cancellationToken);
}
