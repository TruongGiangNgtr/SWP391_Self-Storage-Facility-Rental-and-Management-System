using Frms.Business.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Frms.Api.BackgroundJobs;

/// <summary>
/// Scoped, failure-isolated recurring work. Each database routine enforces its own
/// PostgreSQL concurrency/idempotency invariants; no HTTP actor is impersonated.
/// Disabled by default until deployment review and integration tests pass.
/// </summary>
internal abstract class RecurringBackgroundJob(
    IServiceScopeFactory scopes, IConfiguration configuration, ILogger logger) : BackgroundService
{
    protected abstract string EnabledSetting { get; }
    protected abstract TimeSpan Interval { get; }
    protected abstract Task RunAsync(IRecurringJobsService jobs, CancellationToken token);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue<bool>(EnabledSetting))
        {
            logger.LogInformation("Recurring background job {Job} disabled ({Setting}).",
                GetType().Name, EnabledSetting);
            return;
        }

        // Immediate catch-up on startup; then periodic reruns. Database routines
        // guarantee that retries do not create duplicate billing state.
        await RunOnceAsync(stoppingToken);
        using var timer = new PeriodicTimer(Interval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await RunOnceAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal host shutdown.
        }
    }

    private async Task RunOnceAsync(CancellationToken token)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var jobs = scope.ServiceProvider.GetRequiredService<IRecurringJobsService>();
            await RunAsync(jobs, token);
            logger.LogInformation("Recurring job {Job} completed.", GetType().Name);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Normal host shutdown.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Recurring job {Job} failed; retry at next interval.", GetType().Name);
        }
    }
}

/// <summary>Three required financial tasks run sequentially, never independently raced.</summary>
internal sealed class BillingMaintenanceJob(
    IServiceScopeFactory scopes, IConfiguration config, ILogger<BillingMaintenanceJob> logger)
    : RecurringBackgroundJob(scopes, config, logger)
{
    protected override string EnabledSetting => "BackgroundJobs:Billing:Enabled";
    protected override TimeSpan Interval => TimeSpan.FromDays(1);

    protected override async Task RunAsync(IRecurringJobsService jobs, CancellationToken token)
    {
        await jobs.GenerateMonthlyInvoicesAsync(token);
        await jobs.MarkOverdueInvoicesAsync(token);
        await jobs.RecalculateOpenLateFeesAsync(token);
    }
}

internal sealed class ProcessReservationNoShowJob(
    IServiceScopeFactory scopes, IConfiguration config, ILogger<ProcessReservationNoShowJob> logger)
    : RecurringBackgroundJob(scopes, config, logger)
{
    protected override string EnabledSetting => "BackgroundJobs:NoShow:Enabled";
    protected override TimeSpan Interval => TimeSpan.FromDays(1);
    protected override Task RunAsync(IRecurringJobsService jobs, CancellationToken token) =>
        jobs.ProcessReservationNoShowAsync(token);
}

internal sealed class RetryNotificationsJob(
    IServiceScopeFactory scopes, IConfiguration config, ILogger<RetryNotificationsJob> logger)
    : RecurringBackgroundJob(scopes, config, logger)
{
    protected override string EnabledSetting => "BackgroundJobs:Notifications:Enabled";
    protected override TimeSpan Interval => TimeSpan.FromMinutes(5);
    protected override Task RunAsync(IRecurringJobsService jobs, CancellationToken token) =>
        jobs.RetryOnePendingNotificationAsync(token);
}
