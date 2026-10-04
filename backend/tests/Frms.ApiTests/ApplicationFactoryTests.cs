using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Frms.ApiTests;

public sealed class ApplicationFactoryTests
{
    [Test]
    public void ApplicationStarts()
    {
        using var factory = new FrmsWebApplicationFactory();

        var environment = factory.Services.GetRequiredService<IHostEnvironment>();

        Assert.That(environment.EnvironmentName, Is.EqualTo("Testing"));
    }
}
