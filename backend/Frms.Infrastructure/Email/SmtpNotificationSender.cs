using Frms.Business.Abstractions.External;

namespace Frms.Infrastructure.Email;

/// <summary>Delivery adapter only. Queue claiming, retries and SENT state belong to Business/Repository.</summary>
public sealed class SmtpNotificationSender(ISmtpEmailTransport transport) : INotificationSender
{
    public Task EnsureConfiguredAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        transport.EnsureConfigured();
        return Task.CompletedTask;
    }

    public Task SendAsync(NotificationDeliveryRequest request, CancellationToken cancellationToken)
    {
        // Constant header avoids header injection from persisted notification type/content.
        // SMTP is at-least-once: retry after acceptance/crash can deliver duplicates.
        return transport.SendAsync(request.RecipientEmail, "FRMS notification", request.Content,
            cancellationToken);
    }
}
