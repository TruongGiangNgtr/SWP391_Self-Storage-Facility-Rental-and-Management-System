namespace Frms.Business.Services.Interfaces;

public interface IRecurringJobsService
{
    Task ProcessReservationNoShowAsync(CancellationToken cancellationToken);
    Task GenerateMonthlyInvoicesAsync(CancellationToken cancellationToken);
    Task MarkOverdueInvoicesAsync(CancellationToken cancellationToken);
    Task RecalculateOpenLateFeesAsync(CancellationToken cancellationToken);
    Task RetryOnePendingNotificationAsync(CancellationToken cancellationToken);
}
