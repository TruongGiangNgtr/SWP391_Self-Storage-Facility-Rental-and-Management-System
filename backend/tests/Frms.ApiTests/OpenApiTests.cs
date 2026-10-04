using System.Net;
using System.Text.Json;

namespace Frms.ApiTests;

public sealed class OpenApiTests
{
    [Test]
    public async Task OpenApiDocumentContainsTheSrsEndpointCatalogue()
    {
        await using var factory = new FrmsWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json");
        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        var paths = document.RootElement.GetProperty("paths");
        var operationCount = paths
            .EnumerateObject()
            .Sum(path => path.Value
                .EnumerateObject()
                .Count(property => IsHttpMethod(property.Name)));
        var descriptions = paths
            .EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject())
            .Where(property => IsHttpMethod(property.Name))
            .Select(property => property.Value.GetProperty("description").GetString())
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(operationCount, Is.EqualTo(88));
            Assert.That(descriptions, Has.All.Contain("returns 501 Not Implemented"));
            Assert.That(paths.TryGetProperty("/api/v1/reservations", out _), Is.True);
            Assert.That(paths.TryGetProperty("/api/v1/reports/business/export", out _), Is.True);
            Assert.That(paths.TryGetProperty("/api/v1/admin/employees/{employeeId}/assignment", out _), Is.True);
        });
    }

    private static bool IsHttpMethod(string value) => value is
        "get" or "post" or "put" or "patch" or "delete" or "head" or "options" or "trace";
}
