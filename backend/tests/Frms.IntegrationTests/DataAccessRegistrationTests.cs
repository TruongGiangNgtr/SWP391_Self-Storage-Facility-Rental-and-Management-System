using Frms.DataAccess.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Frms.IntegrationTests;

public sealed class DataAccessRegistrationTests
{
    [Test]
    public void FrmsDbContextCanBeResolvedWithoutOpeningAConnection()
    {
        using var factory = new FrmsWebApplicationFactory();
        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<FrmsDbContext>();

        Assert.Multiple(() =>
        {
            Assert.That(dbContext.Database.ProviderName, Is.EqualTo("Microsoft.EntityFrameworkCore.SqlServer"));
            Assert.That(dbContext.Model.GetEntityTypes().ToArray(), Has.Length.EqualTo(27));
        });
    }
}
