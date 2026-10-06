using Frms.Business.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Frms.Api.BackgroundJobs;

internal sealed class ExpirePendingReservationsJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ExpirePendingReservationsJob> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval =
        TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await RunOnceAsync(stoppingToken);

        using var timer =
            new PeriodicTimer(Interval);

        try
        {
            while (await timer.WaitForNextTickAsync(
                       stoppingToken))
            {
                await RunOnceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
    }

    private async Task RunOnceAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await using var scope =
                scopeFactory.CreateAsyncScope();

            var service =
                scope.ServiceProvider
                    .GetRequiredService<
                        IReservationExpirationService>();

            await service.ExpirePendingAsync(
                cancellationToken);

            logger.LogInformation(
                "Reservation expiration job completed.");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Reservation expiration job failed.");
        }
    }
}