using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace Frms.ApiTests;

public sealed class FrmsWebApplicationFactory : WebApplicationFactory<Program>
{
    static FrmsWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("ConnectionStrings__Frms", "Server=localhost;Database=Frms_ApiTests;Trusted_Connection=True;TrustServerCertificate=True");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "frms-api-tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "frms-api-tests");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
        Environment.SetEnvironmentVariable("Jwt__LifetimeMinutes", "60");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
    }
}
