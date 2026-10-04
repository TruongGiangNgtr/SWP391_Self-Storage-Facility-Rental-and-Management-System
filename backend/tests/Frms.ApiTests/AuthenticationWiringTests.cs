using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace Frms.ApiTests;

public sealed class AuthenticationWiringTests
{
    [Test]
    public async Task JwtBearerIsTheDefaultAuthenticationScheme()
    {
        await using var factory = new FrmsWebApplicationFactory();
        var schemeProvider = factory.Services.GetRequiredService<IAuthenticationSchemeProvider>();

        var scheme = await schemeProvider.GetDefaultAuthenticateSchemeAsync();

        Assert.That(scheme?.Name, Is.EqualTo(JwtBearerDefaults.AuthenticationScheme));
    }
}
