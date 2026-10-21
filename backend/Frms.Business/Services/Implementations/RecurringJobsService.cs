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
        // A missing provider is not success. Do not claim or mark a row SENT.
        await sender.EnsureConfiguredAsync(token);
        await using var lease = await repository.TryAcquireNotificationAsync(token);
        if (lease is null) return;

        var item = lease.Notification;
        await sender.SendAsync(new NotificationDeliveryRequest(
            item.NotificationLogId, item.UserAccountId, item.RecipientEmail,
            item.NotificationType, item.Content), token);
        // Only an acknowledged send transitions the PENDING row to SENT.
        await lease.MarkSentAsync(token);
    }
}
