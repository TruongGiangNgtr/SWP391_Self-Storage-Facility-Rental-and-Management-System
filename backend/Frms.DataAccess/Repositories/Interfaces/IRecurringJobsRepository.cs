namespace Frms.DataAccess.Repositories.Interfaces;

public sealed record PendingNotification(
    Guid NotificationLogId, Guid UserAccountId, string RecipientEmail,
    string NotificationType, string Content);

public interface INotificationDeliveryLease : IAsyncDisposable
{
    PendingNotification Notification { get; }
    Task MarkSentAsync(CancellationToken cancellationToken);
}

public interface IRecurringJobsRepository
{
    Task ProcessReservationNoShowAsync(CancellationToken cancellationToken);
    Task GenerateMonthlyInvoicesAsync(CancellationToken cancellationToken);
    Task MarkOverdueInvoicesAsync(CancellationToken cancellationToken);
    Task RecalculateOpenLateFeesAsync(CancellationToken cancellationToken);
    Task<INotificationDeliveryLease?> TryAcquireNotificationAsync(CancellationToken cancellationToken);
}
