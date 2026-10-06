using Microsoft.Extensions.DependencyInjection;

namespace Frms.Api.BackgroundJobs;

public static class BackgroundJobRegistration
{
    public static IServiceCollection AddFrmsBackgroundJobs(
        this IServiceCollection services)
    {
        services.AddHostedService<
            ExpirePendingReservationsJob>();

        services.AddHostedService<
            ReservationNoShowBackgroundService>();

        return services;
    }
}