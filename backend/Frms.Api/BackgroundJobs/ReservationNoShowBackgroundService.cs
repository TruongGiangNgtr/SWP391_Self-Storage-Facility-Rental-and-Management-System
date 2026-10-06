using Frms.Business.Services.Interfaces;

namespace Frms.Api.BackgroundJobs;

internal sealed class ReservationNoShowBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<ReservationNoShowBackgroundService> logger)
    : BackgroundService
{
    private readonly TimeSpan _interval =
        TimeSpan.FromMinutes(
            GetIntervalMinutes(configuration));

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await RunOnceAsync(stoppingToken);

        using var timer =
            new PeriodicTimer(_interval);

        try
        {
            while (await timer.WaitForNextTickAsync(
                       stoppingToken))
            {
                await RunOnceAsync(
                    stoppingToken);
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
            using var scope =
                scopeFactory.CreateScope();

            var service =
                scope.ServiceProvider
                    .GetRequiredService<IReservationNoShowService>();

            await service.ProcessAsync(
                cancellationToken);

            logger.LogInformation(
                "Reservation no-show processing completed.");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Reservation no-show processing failed.");
        }
    }

    private static int GetIntervalMinutes(
        IConfiguration configuration)
    {
        var configured =
            configuration.GetValue<int?>(
                "BackgroundJobs:ReservationNoShowIntervalMinutes");

        return configured is > 0
            ? configured.Value
            : 15;
    }
}