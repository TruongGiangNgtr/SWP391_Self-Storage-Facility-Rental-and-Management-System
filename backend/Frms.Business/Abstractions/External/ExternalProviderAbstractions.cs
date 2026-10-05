namespace Frms.Business.Abstractions.External;

/// <summary>Configuration boundary for the provider that initiates invoice payments.</summary>
public interface IPaymentGateway
{
    Task EnsureConfiguredAsync(CancellationToken cancellationToken);
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
