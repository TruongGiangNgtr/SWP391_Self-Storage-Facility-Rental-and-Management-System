using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Frms.ApiTests;

[TestFixture]
public sealed class HealthEndpointTests
{
    [Test]
    public async Task API_HEALTH_001_UnreachablePostgreSqlReturnsServiceUnavailable()
    {
        await using var factory = new FrmsWebApplicationFactory().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Closed loopback port: the health check must fail without contacting a real database.
                ["ConnectionStrings:Frms"] = "Host=127.0.0.1;Port=1;Database=frms_test_unavailable;Username=frms_test;Password=not-a-secret;Timeout=1;Pooling=false"
            })));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        var body = await response.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
            Assert.That(body, Is.EqualTo("Unhealthy"));
            Assert.That(body, Does.Not.Contain("Host=").And.Not.Contain("frms_test_unavailable"));
        });
    }
}
