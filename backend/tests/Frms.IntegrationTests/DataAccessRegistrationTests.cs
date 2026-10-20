using Frms.DataAccess.DependencyInjection;
using Frms.DataAccess.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Frms.IntegrationTests;

[TestFixture]
public sealed class DataAccessRegistrationTests
{
    private const string ProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";

    [TestCase("Frms")]
    [TestCase("FrmsDb")]
    public void DAL_CONFIG_001_ConfigurationSelectsPostgreSqlAndConfiguredConnection(string name)
    {
        const string expected = "Host=127.0.0.1;Port=5432;Database=frms_tests;Username=frms_test;Password=test-password;Pooling=false";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{name}"] = expected
        }).Build();
        using var services = new ServiceCollection().AddDataAccess(configuration).BuildServiceProvider();
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FrmsDbContext>();

        Assert.Multiple(() =>
        {
            Assert.That(dbContext.Database.ProviderName, Is.EqualTo(ProviderName));
            AssertConfiguredConnection(dbContext, expected);
        });
    }

    [Test]
    public void DAL_CONFIG_002_DoubleUnderscoreEnvironmentVariableConfiguresConnection()
    {
        const string expected = "Host=127.0.0.1;Port=5433;Database=frms_env_tests;Username=frms_test;Password=test-password;Pooling=false";
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
    public void DAL_CONFIG_003_MultiplexingIsRejectedForAuditConnectionSafety()
    {
        const string connection = "Host=127.0.0.1;Database=frms_tests;Username=frms_test;Password=test-password;Multiplexing=true";

        Assert.That(() => new ServiceCollection().AddDataAccess(connection),
            Throws.InvalidOperationException.With.Message.Contains("Disable multiplexing"));
    }

    [Test]
    public void DAL_CONFIG_004_ExistingFrmsNameTakesPrecedenceOverAlias()
    {
        const string expected = "Host=frms-primary;Port=5432;Database=frms_primary;Username=primary;Password=test-password";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Frms"] = expected,
            ["ConnectionStrings:FrmsDb"] = "Host=frms-alias;Port=5432;Database=frms_alias;Username=alias;Password=test-password"
        }).Build();
        using var services = new ServiceCollection().AddDataAccess(configuration).BuildServiceProvider();
        using var scope = services.CreateScope();
        AssertConfiguredConnection(scope.ServiceProvider.GetRequiredService<FrmsDbContext>(), expected);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void DAL_CONFIG_005_MissingOrBlankConnectionFailsClearly(string? value)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:FrmsDb"] = value
        }).Build();
        Assert.That(() => new ServiceCollection().AddDataAccess(configuration),
            Throws.InvalidOperationException.With.Message.Contains("ConnectionStrings__Frms"));
    }

    [TestCase(null)]
    [TestCase("Host=127.0.0.1;Port=5432;Database=frms_design_tests;Username=frms_test;Password=test-password;Pooling=false")]
    [NonParallelizable]
    public void DAL_CONFIG_006_DesignTimeFactoryRequiresExplicitDatabaseAndSupportsAlias(string? connection)
    {
        var names = new[] { "FRMS_CONNECTION_STRING", "ConnectionStrings__Frms", "ConnectionStrings__FrmsDb" };
        var previous = names.ToDictionary(name => name, Environment.GetEnvironmentVariable);
        try
        {
            foreach (var name in names) Environment.SetEnvironmentVariable(name, null);
            Environment.SetEnvironmentVariable("ConnectionStrings__FrmsDb", connection);
            var factory = new FrmsDbContextFactory();
            if (connection is null)
                Assert.That(() => factory.CreateDbContext([]), Throws.InvalidOperationException.With.Message.Contains("ConnectionStrings__Frms"));
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
            Assert.That(dbContext.Database.ProviderName, Is.EqualTo(ProviderName));
            Assert.That(dbContext.Model.GetEntityTypes().ToArray(), Has.Length.EqualTo(27));
        });
    }

    private static void AssertConfiguredConnection(FrmsDbContext dbContext, string expected)
    {
        var configured = new NpgsqlConnectionStringBuilder(expected);
        var actual = new NpgsqlConnectionStringBuilder(dbContext.Database.GetConnectionString() ?? string.Empty);
        Assert.Multiple(() =>
        {
            Assert.That(actual.Host, Is.EqualTo(configured.Host));
            Assert.That(actual.Port, Is.EqualTo(configured.Port));
            Assert.That(actual.Database, Is.EqualTo(configured.Database));
            Assert.That(actual.Username, Is.EqualTo(configured.Username));
            Assert.That(actual.Password, Is.EqualTo(configured.Password));
            Assert.That(actual.Pooling, Is.EqualTo(configured.Pooling));
        });
    }
}
