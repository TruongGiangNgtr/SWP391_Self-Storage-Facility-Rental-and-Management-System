using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Frms.ApiTests;

public sealed class HealthEndpointTests
{
    [Test]
    public async Task API_HEALTH_001_UnreachableSqlServerReturnsServiceUnavailable()
    {
        await using var factory = new FrmsWebApplicationFactory().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // A closed loopback port tests an unavailable database without touching a real one.
                ["ConnectionStrings:Frms"] = "Server=tcp:127.0.0.1,1;Database=Frms_Test_Unavailable;Integrated Security=True;Connect Timeout=1;ConnectRetryCount=0"
            })));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        var body = await response.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
            Assert.That(body, Is.EqualTo("Unhealthy"));
            Assert.That(body, Does.Not.Contain("Server=").And.Not.Contain("Frms_Test_Unavailable"));
        });
    }
}
