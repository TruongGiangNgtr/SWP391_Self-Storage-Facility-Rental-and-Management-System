using Frms.Business.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Frms.UnitTests;

public sealed class BusinessRegistrationTests
{
    [Test]
    public void AddBusinessDoesNotRegisterPlaceholderImplementations()
    {
        var services = new ServiceCollection();

        var returnedServices = services.AddBusiness();

        Assert.Multiple(() =>
        {
            Assert.That(returnedServices, Is.SameAs(services));
            Assert.That(services, Is.Empty);
        });
    }
}
