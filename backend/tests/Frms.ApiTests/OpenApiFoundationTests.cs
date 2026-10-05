namespace Frms.ApiTests;

[TestFixture]
public sealed class OpenApiFoundationTests
{
    [Test]
    public async Task OpenApi_PhaseZeroHost_DocumentsOnlyImplementedAuthFoundation()
    {
        await using var factory = new FrmsWebApplicationFactory();
        using var client = factory.CreateClient();
        var json = await client.GetStringAsync("/openapi/v1.json");
        Assert.Multiple(() => { Assert.That(json, Does.Contain("/api/v1/auth/customer/login")); Assert.That(json, Does.Contain("/api/v1/auth/employee/login")); Assert.That(json, Does.Contain("/api/v1/auth/me")); Assert.That(json, Does.Contain("/api/v1/reservations")); });
    }
}
