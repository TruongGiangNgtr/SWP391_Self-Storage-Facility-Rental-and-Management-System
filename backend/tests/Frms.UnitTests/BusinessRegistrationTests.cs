using Frms.Business.Abstractions.Time;
using Frms.Business.DependencyInjection;
using Frms.Business.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Frms.UnitTests;

public sealed class BusinessRegistrationTests
{
    [Test]
    public void AddBusinessRegistersTechnicalFoundationServices()
    {
        var services = new ServiceCollection();

        var returnedServices = services.AddBusiness();

        Assert.Multiple(() =>
        {
            Assert.That(returnedServices, Is.SameAs(services));
            Assert.That(services.Any(service => service.ServiceType == typeof(IClock)), Is.True);
            Assert.That(services.Any(service => service.ServiceType == typeof(IAuthenticationService)), Is.True);
        });
    }
}
