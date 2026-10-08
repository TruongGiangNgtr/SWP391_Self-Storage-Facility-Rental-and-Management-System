using Frms.Api.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Frms.ApiTests;

[NonParallelizable]
public sealed class DevelopmentEnvConfigurationTests
{
    private string directory = null!;
    private string? previousEnvironmentValue;
    private string? previousPayOsClientId;
    private const string EnvironmentKey = "ConnectionStrings__FrmsDb";
    private const string PayOsClientIdEnvironmentKey = "Payment__PayOS__ClientId";

    [SetUp]
    public void SetUp()
    {
        directory = Path.Combine(Path.GetTempPath(), "frms-dotenv-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        previousEnvironmentValue = Environment.GetEnvironmentVariable(EnvironmentKey);
        previousPayOsClientId = Environment.GetEnvironmentVariable(PayOsClientIdEnvironmentKey);
        Environment.SetEnvironmentVariable(EnvironmentKey, null);
        Environment.SetEnvironmentVariable(PayOsClientIdEnvironmentKey, null);
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable(EnvironmentKey, previousEnvironmentValue);
        Environment.SetEnvironmentVariable(PayOsClientIdEnvironmentKey, previousPayOsClientId);
        Directory.Delete(directory, recursive: true);
    }

    [Test]
    public void API_CONFIG_001_DevelopmentLoadsContentRootFileWithoutChangingProcessEnvironment()
    {
        File.WriteAllText(Path.Combine(directory, ".env"), "# local configuration\nConnectionStrings__FrmsDb=\"from-file;quoted-value\"\n");
        using var configuration = CreateConfiguration();
        configuration.AddDevelopmentEnvFile(CreateEnvironment(Environments.Development));

        Assert.Multiple(() =>
        {
            Assert.That(configuration.GetConnectionString("FrmsDb"), Is.EqualTo("from-file;quoted-value"));
            Assert.That(Environment.GetEnvironmentVariable(EnvironmentKey), Is.Null);
            Assert.That(directory, Is.Not.EqualTo(Directory.GetCurrentDirectory()));
        });
    }

    [Test]
    public void API_CONFIG_002_ProcessEnvironmentOverridesLocalFile()
    {
        File.WriteAllText(Path.Combine(directory, ".env"), "ConnectionStrings__FrmsDb=from-file\n");
        Environment.SetEnvironmentVariable(EnvironmentKey, "from-environment");
        using var configuration = CreateConfiguration();
        configuration.AddDevelopmentEnvFile(CreateEnvironment(Environments.Development));

        Assert.That(configuration.GetConnectionString("FrmsDb"), Is.EqualTo("from-environment"));
    }

    [Test]
    public void API_CONFIG_003_MissingFileKeepsExistingConfiguration()
    {
        using var configuration = CreateConfiguration();
        Assert.DoesNotThrow(() => configuration.AddDevelopmentEnvFile(CreateEnvironment(Environments.Development)));
        Assert.That(configuration.GetConnectionString("FrmsDb"), Is.EqualTo("from-settings"));
    }

    [TestCase("Production")]
    [TestCase("Staging")]
    [TestCase("Testing")]
    public void API_CONFIG_004_OtherEnvironmentsIgnoreLocalFile(string environmentName)
    {
        File.WriteAllText(Path.Combine(directory, ".env"),
            "ConnectionStrings__FrmsDb=from-file\nPayment__PayOS__ClientId=fake-file-terminal\n");
        using var configuration = CreateConfiguration();
        configuration.AddDevelopmentEnvFile(CreateEnvironment(environmentName));

        Assert.Multiple(() =>
        {
            Assert.That(configuration.GetConnectionString("FrmsDb"), Is.EqualTo("from-settings"));
            Assert.That(configuration["Payment:PayOS:ClientId"], Is.EqualTo("fake-settings-terminal"));
        });
    }

    [Test]
    public void API_CONFIG_005_CommandLineOverridesLocalFile()
    {
        File.WriteAllText(Path.Combine(directory, ".env"), "ConnectionStrings__FrmsDb=from-file\n");
        using var configuration = CreateConfiguration();
        configuration.AddCommandLine(["--ConnectionStrings:FrmsDb=from-command-line"]);
        configuration.AddDevelopmentEnvFile(CreateEnvironment(Environments.Development));

        Assert.That(configuration.GetConnectionString("FrmsDb"), Is.EqualTo("from-command-line"));
    }

    [Test]
    public void API_CONFIG_006_InvalidFileDoesNotExposeItsContents()
    {
        File.WriteAllText(Path.Combine(directory, ".env"), "=private-test-marker\n");
        using var configuration = CreateConfiguration();

        var exception = Assert.Throws<InvalidOperationException>(() => configuration.AddDevelopmentEnvFile(CreateEnvironment(Environments.Development)));
        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("dotenv syntax").And.Not.Contain("private-test-marker"));
            Assert.That(exception.InnerException, Is.Null);
        });
    }

    [Test]
    public void API_CONFIG_007_DevelopmentMapsPayOsKeyWithoutChangingProcessEnvironment()
    {
        File.WriteAllText(Path.Combine(directory, ".env"),
            "Payment__PayOS__ClientId=fake-file-terminal\n");
        using var configuration = CreateConfiguration();
        configuration.AddDevelopmentEnvFile(CreateEnvironment(Environments.Development));

        Assert.Multiple(() =>
        {
            Assert.That(configuration["Payment:PayOS:ClientId"], Is.EqualTo("fake-file-terminal"));
            Assert.That(Environment.GetEnvironmentVariable(PayOsClientIdEnvironmentKey), Is.Null);
        });
    }

    [Test]
    public void API_CONFIG_008_ProcessEnvironmentOverridesPayOsLocalFile()
    {
        File.WriteAllText(Path.Combine(directory, ".env"),
            "Payment__PayOS__ClientId=fake-file-terminal\n");
        Environment.SetEnvironmentVariable(PayOsClientIdEnvironmentKey, "fake-process-terminal");
        using var configuration = CreateConfiguration();
        configuration.AddDevelopmentEnvFile(CreateEnvironment(Environments.Development));

        Assert.Multiple(() =>
        {
            Assert.That(configuration["Payment:PayOS:ClientId"], Is.EqualTo("fake-process-terminal"));
            Assert.That(Environment.GetEnvironmentVariable(PayOsClientIdEnvironmentKey),
                Is.EqualTo("fake-process-terminal"));
        });
    }

    private static ConfigurationManager CreateConfiguration()
    {
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:FrmsDb"] = "from-settings",
            ["Payment:PayOS:ClientId"] = "fake-settings-terminal"
        });
        configuration.AddEnvironmentVariables();
        return configuration;
    }

    private IHostEnvironment CreateEnvironment(string name) => new HostEnvironment
    {
        EnvironmentName = name,
        ContentRootPath = directory
    };

    private sealed class HostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = "Frms.Api";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
