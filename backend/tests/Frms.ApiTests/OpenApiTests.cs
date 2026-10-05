using System.Net;
using System.Text.Json;

namespace Frms.ApiTests;

public sealed class OpenApiTests
{
    [Test]
    public async Task OpenApiDocumentContainsAuthAndPreservedContractScaffoldEndpoints()
    {
        await using var factory = new FrmsWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json");
        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        var paths = document.RootElement.GetProperty("paths");
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(paths.EnumerateObject().Count(), Is.GreaterThan(3));
            Assert.That(paths.TryGetProperty("/api/v1/auth/customer/login", out _), Is.True);
            Assert.That(paths.TryGetProperty("/api/v1/auth/employee/login", out _), Is.True);
            Assert.That(paths.TryGetProperty("/api/v1/reservations", out _), Is.True);
            Assert.That(paths.TryGetProperty("/api/v1/reports/business/export", out _), Is.True);
            Assert.That(paths.TryGetProperty("/api/v1/admin/employees/{employeeId}/assignment", out _), Is.True);
        });
    }
}
