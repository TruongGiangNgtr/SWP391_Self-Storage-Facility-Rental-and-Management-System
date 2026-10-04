using Microsoft.AspNetCore.Mvc.Testing;
using System.Security.Cryptography;
namespace Frms.ApiTests;

[TestFixture]
public sealed class OpenApiFoundationTests
{
    [Test]
    public async Task OpenApi_PhaseZeroHost_DocumentsOnlyImplementedAuthFoundation()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Frms", "Server=localhost;Database=Frms_TestHost;Trusted_Connection=True;TrustServerCertificate=True");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        try
        {
            await using var factory = new WebApplicationFactory<Program>(); using var client = factory.CreateClient(); var json = await client.GetStringAsync("/openapi/v1.json");
            Assert.Multiple(() => { Assert.That(json, Does.Contain("/api/v1/auth/customer/login")); Assert.That(json, Does.Contain("/api/v1/auth/employee/login")); Assert.That(json, Does.Contain("/api/v1/auth/me")); Assert.That(json, Does.Not.Contain("/api/v1/reservations")); });
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__Frms", null);
            Environment.SetEnvironmentVariable("Jwt__SigningKey", null);
        }
    }
}
