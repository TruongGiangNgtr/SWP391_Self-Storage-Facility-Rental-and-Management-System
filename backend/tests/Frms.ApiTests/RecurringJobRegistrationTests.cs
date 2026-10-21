using Frms.Api.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Frms.ApiTests;

public sealed class RecurringJobRegistrationTests
{
    [Test]
    public void ExistingExpirationAndAllNewJobPipelinesAreRegistered()
    {
        var services = new ServiceCollection();
        services.AddFrmsBackgroundJobs();
        var names = services.Where(x => x.ServiceType == typeof(IHostedService))
            .Select(x => x.ImplementationType?.Name).ToArray();
        Assert.That(names, Is.EquivalentTo(new[]
        {
            "ExpirePendingReservationsJob", "BillingMaintenanceJob",
            "ProcessReservationNoShowJob", "RetryNotificationsJob"
        }));
    }
}
