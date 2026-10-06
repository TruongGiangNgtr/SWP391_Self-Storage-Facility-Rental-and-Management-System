using Frms.DataAccess.DependencyInjection;
using Frms.DataAccess.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace Frms.IntegrationTests;

public sealed class DataAccessRegistrationTests
{
    [TestCase("Frms")]
    [TestCase("FrmsDb")]
    public void DAL_CONFIG_001_ConfigurationSelectsSqlServerAndConfiguredConnection(string name)
    {
        const string expected = "Server=<SERVER>;Database=<DATABASE>;Trusted_Connection=True;TrustServerCertificate=True";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{name}"] = expected
        }).Build();
        using var services = new ServiceCollection().AddDataAccess(configuration).BuildServiceProvider();
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FrmsDbContext>();

        Assert.Multiple(() =>
        {
            Assert.That(dbContext.Database.ProviderName, Is.EqualTo("Microsoft.EntityFrameworkCore.SqlServer"));
            AssertConfiguredConnection(dbContext, expected);
        });
    }

    [Test]
    public void DAL_CONFIG_002_DoubleUnderscoreEnvironmentVariableConfiguresConnection()
    {
        const string expected = "Server=<SERVER>;Database=<DATABASE>;Trusted_Connection=True;TrustServerCertificate=True";
        var prefix = $"FRMS_CONFIG_TEST_{Guid.NewGuid():N}_";
        var variable = prefix + "ConnectionStrings__FrmsDb";
        Environment.SetEnvironmentVariable(variable, expected);
        try
        {
            var configuration = new ConfigurationBuilder().AddEnvironmentVariables(prefix).Build();
            using var services = new ServiceCollection().AddDataAccess(configuration).BuildServiceProvider();
            using var scope = services.CreateScope();
            AssertConfiguredConnection(scope.ServiceProvider.GetRequiredService<FrmsDbContext>(), expected);
        }
        finally { Environment.SetEnvironmentVariable(variable, null); }
    }

    [Test]
    public void DAL_CONFIG_003_ExistingFrmsNameTakesPrecedenceOverAlias()
    {
        const string expected = "Server=<EXISTING_SERVER>;Database=<DATABASE>";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Frms"] = expected,
            ["ConnectionStrings:FrmsDb"] = "Server=<ALIAS_SERVER>;Database=<DATABASE>"
        }).Build();
        using var services = new ServiceCollection().AddDataAccess(configuration).BuildServiceProvider();
        using var scope = services.CreateScope();
        AssertConfiguredConnection(scope.ServiceProvider.GetRequiredService<FrmsDbContext>(), expected);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void DAL_CONFIG_004_MissingOrBlankConnectionFailsClearly(string? value)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:FrmsDb"] = value
        }).Build();
        Assert.That(() => new ServiceCollection().AddDataAccess(configuration),
            Throws.InvalidOperationException.With.Message.Contains("ConnectionStrings__FrmsDb"));
    }

    [TestCase(null)]
    [TestCase("Server=<SERVER>;Database=<DATABASE>;Trusted_Connection=True;TrustServerCertificate=True")]
    [NonParallelizable]
    public void DAL_CONFIG_005_DesignTimeFactoryRequiresExplicitDatabaseAndSupportsAlias(string? connection)
    {
        var names = new[] { "FRMS_CONNECTION_STRING", "ConnectionStrings__Frms", "ConnectionStrings__FrmsDb" };
        var previous = names.ToDictionary(name => name, Environment.GetEnvironmentVariable);
        try
        {
            foreach (var name in names) Environment.SetEnvironmentVariable(name, null);
            Environment.SetEnvironmentVariable("ConnectionStrings__FrmsDb", connection);
            var factory = new FrmsDbContextFactory();
            if (connection is null)
                Assert.That(() => factory.CreateDbContext([]), Throws.InvalidOperationException.With.Message.Contains("ConnectionStrings__FrmsDb"));
            else
            {
                using var database = factory.CreateDbContext([]);
                AssertConfiguredConnection(database, connection);
            }
        }
        finally
        {
            foreach (var pair in previous) Environment.SetEnvironmentVariable(pair.Key, pair.Value);
        }
    }

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

    private static void AssertConfiguredConnection(FrmsDbContext dbContext, string expected)
    {
        // SQL Server normalizes aliases and appends diagnostic defaults; compare connection semantics.
        var configured = new SqlConnectionStringBuilder(expected);
        var actual = new SqlConnectionStringBuilder(dbContext.Database.GetConnectionString());
        Assert.Multiple(() =>
        {
            Assert.That(actual.DataSource, Is.EqualTo(configured.DataSource));
            Assert.That(actual.InitialCatalog, Is.EqualTo(configured.InitialCatalog));
            Assert.That(actual.IntegratedSecurity, Is.EqualTo(configured.IntegratedSecurity));
            Assert.That(actual.TrustServerCertificate, Is.EqualTo(configured.TrustServerCertificate));
        });
    }
}
