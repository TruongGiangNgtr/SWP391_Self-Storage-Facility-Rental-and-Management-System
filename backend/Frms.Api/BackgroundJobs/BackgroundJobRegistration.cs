namespace Frms.Api.BackgroundJobs;

public static class BackgroundJobRegistration
{
    public static IServiceCollection AddFrmsBackgroundJobs(
        this IServiceCollection services)
    {
        services.AddHostedService<
            ExpirePendingReservationsJob>();

        // Financial jobs run in a single ordered pipeline: generate -> overdue -> late fee.
        // Billing defaults on; NoShow/Notifications remain opt-in via configuration.
        services.AddHostedService<BillingMaintenanceJob>();
        services.AddHostedService<ProcessReservationNoShowJob>();
        services.AddHostedService<RetryNotificationsJob>();

        return services;
    }
}
