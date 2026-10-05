using System.Net;

namespace Frms.ApiTests;

public sealed class HealthEndpointTests
{
    [Test]
    public async Task GetHealthReturnsSuccess()
    {
        await using var factory = new FrmsWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
