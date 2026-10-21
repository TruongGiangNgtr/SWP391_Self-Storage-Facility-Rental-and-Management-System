using Frms.Business.Abstractions.External;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;

namespace Frms.Business.Services.Implementations;

internal sealed class RecurringJobsService(
    IRecurringJobsRepository repository, INotificationSender sender) : IRecurringJobsService
{
    public Task ProcessReservationNoShowAsync(CancellationToken token) =>
        repository.ProcessReservationNoShowAsync(token);

    public Task GenerateMonthlyInvoicesAsync(CancellationToken token) =>
        repository.GenerateMonthlyInvoicesAsync(token);

    public Task MarkOverdueInvoicesAsync(CancellationToken token) =>
        repository.MarkOverdueInvoicesAsync(token);

    public Task RecalculateOpenLateFeesAsync(CancellationToken token) =>
        repository.RecalculateOpenLateFeesAsync(token);

    public async Task RetryOnePendingNotificationAsync(CancellationToken token)
    {
        await RetryPendingNotificationsAsync(1, token);
    }

    public async Task<int> RetryPendingNotificationsAsync(int maxItems, CancellationToken token)
    {
        if (maxItems is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(maxItems));
        // Validate provider once per tick; never mark SENT on failure.
        await sender.EnsureConfiguredAsync(token);
        var sent = 0;
        for (var i = 0; i < maxItems; i++)
        {
            token.ThrowIfCancellationRequested();
            await using var lease = await repository.TryAcquireNotificationAsync(token);
            if (lease is null) break;
            var item = lease.Notification;
            await sender.SendAsync(new NotificationDeliveryRequest(
                item.NotificationLogId, item.UserAccountId, item.RecipientEmail,
                item.NotificationType, item.Content), token);
            await lease.MarkSentAsync(token);
            sent++;
        }
        return sent;
    }
}
