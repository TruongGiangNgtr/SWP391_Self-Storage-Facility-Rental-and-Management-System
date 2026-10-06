using Frms.Business.Abstractions.External;
using Frms.Business.Exceptions;

namespace Frms.Infrastructure.External;

internal sealed class UnconfiguredPaymentGateway : IPaymentGateway
{
    public Task EnsureConfiguredAsync(CancellationToken cancellationToken) => NotConfiguredAsync("Payment gateway");
    public Task<PaymentGatewaySession> CreatePaymentAsync(PaymentGatewayRequest request, CancellationToken cancellationToken) => Task.FromException<PaymentGatewaySession>(new BusinessException("EXTERNAL_PROVIDER_NOT_CONFIGURED", "Payment gateway is not configured.", 503));
    private static Task NotConfiguredAsync(string provider) => Task.FromException(new BusinessException("EXTERNAL_PROVIDER_NOT_CONFIGURED", $"{provider} is not configured.", 503));
}

internal sealed class UnconfiguredAiRecommendationProvider : IAiRecommendationProvider
{
    public Task EnsureConfiguredAsync(CancellationToken cancellationToken) => Task.FromException(new BusinessException("EXTERNAL_PROVIDER_NOT_CONFIGURED", "AI recommendation provider is not configured.", 503));
}

internal sealed class UnconfiguredEmailService : IEmailService
{
    public Task SendInitialCredentialAsync(string recipientEmail, string initialPassword, CancellationToken cancellationToken) => Task.FromException(new BusinessException("EXTERNAL_PROVIDER_NOT_CONFIGURED", "Email provider is not configured.", 503));
}

internal sealed class UnconfiguredNotificationSender : INotificationSender
{
    public Task EnsureConfiguredAsync(CancellationToken cancellationToken) => Task.FromException(new BusinessException("EXTERNAL_PROVIDER_NOT_CONFIGURED", "Notification provider is not configured.", 503));
}
