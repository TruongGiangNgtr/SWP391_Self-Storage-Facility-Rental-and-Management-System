using System.Text.Json;
using Frms.Business.Abstractions.External;
using Frms.Business.Abstractions.Time;
using Frms.Business.Exceptions;
using Frms.Infrastructure.DependencyInjection;
using Frms.Infrastructure.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Frms.UnitTests;

[TestFixture, Category("PayOsConfiguration")]
public sealed class PayOsOptionsTests
{
    [Test]
    public void UT_PAYOS_ConfigBindsAllEightFieldsAndSelectsOnlyPayOs()
    {
        var values = new Dictionary<string, string?>
        {
            ["Payment:PayOS:ClientId"] = "fixture-client", ["Payment:PayOS:ApiKey"] = "fixture-api-key",
            ["Payment:PayOS:ChecksumKey"] = "fixture-checksum", ["Payment:PayOS:ApiBaseUrl"] = "https://api-merchant.payos.vn",
            ["Payment:PayOS:ReturnUrl"] = "http://localhost:5173/customer/payments/result",
            ["Payment:PayOS:CancelUrl"] = "http://localhost:5173/customer/payments/result",
            ["Payment:PayOS:WebhookUrl"] = "https://lying-ladder-showroom.ngrok-free.dev/api/v1/payments/payos/webhook",
            ["Payment:PayOS:ExpiryMinutes"] = "15"
        };
        using var provider = Services(values);
        var options = provider.GetRequiredService<IOptions<PayOsOptions>>().Value;
        Assert.Multiple(() =>
        {
            foreach (var property in typeof(PayOsOptions).GetProperties())
                Assert.That(Convert.ToString(property.GetValue(options)), Is.EqualTo(values[$"Payment:PayOS:{property.Name}"]));
            Assert.That(provider.GetRequiredService<IPaymentGateway>(), Is.TypeOf<PayOsPaymentGateway>());
            Assert.That(provider.GetServices<IPaymentGateway>().Count(), Is.EqualTo(1));
            Assert.That(provider.GetService<IValidateOptions<VnPayOptions>>(), Is.Null);
        });
    }

    [TestCase("ClientId")]
    [TestCase("ApiKey")]
    [TestCase("ChecksumKey")]
    public void UT_PAYOS_ConfigRejectsMissingCredentialWithoutSecretValues(string field)
    {
        var options = Valid();
        typeof(PayOsOptions).GetProperty(field)!.SetValue(options, " ");
        var result = new PayOsOptionsValidator().Validate(null, options);
        Assert.That(result.Failed, Is.True);
        Assert.That(string.Join(';', result.Failures!), Does.Contain(field).And.Not.Contain("fixture-api-key").And.Not.Contain("fixture-checksum"));
    }

    [TestCase("ApiBaseUrl", "http://api-merchant.payos.vn")]
    [TestCase("ApiBaseUrl", "https://attacker.invalid")]
    [TestCase("ReturnUrl", "/relative")]
    [TestCase("ReturnUrl", "http://attacker.invalid")]
    [TestCase("CancelUrl", "https://other.invalid")]
    [TestCase("WebhookUrl", "http://localhost/webhook")]
    [TestCase("WebhookUrl", "https://user:password@example.invalid/webhook")]
    [TestCase("ApiKey", "fixture\r\nheader")]
    public void UT_PAYOS_ConfigRejectsInvalidUrlsAndHeaderInjection(string field, string value)
    {
        var options = Valid();
        typeof(PayOsOptions).GetProperty(field)!.SetValue(options, value);
        var result = new PayOsOptionsValidator().Validate(null, options);
        Assert.That(result.Failed, Is.True);
        Assert.That(string.Join(';', result.Failures!), Does.Not.Contain(value));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void UT_PAYOS_ConfigRejectsNonPositiveExpiry(int minutes) =>
        Assert.That(new PayOsOptionsValidator().Validate(null, WithExpiry(minutes)).Failed, Is.True);

    [Test]
    public void UT_PAYOS_OptionsNeverSerializeOrPrintCredentials()
    {
        var options = Valid();
        var output = options + JsonSerializer.Serialize(options);
        Assert.That(output, Does.Not.Contain(options.ClientId).And.Not.Contain(options.ApiKey).And.Not.Contain(options.ChecksumKey));
    }

    [Test]
    public void UT_PAYOS_MissingConfigurationDoesNotPreventCompositionButGatewayFailsSafely()
    {
        using var provider = Services(new Dictionary<string, string?>());
        var gateway = provider.GetRequiredService<IPaymentGateway>();
        var error = Assert.ThrowsAsync<BusinessException>(() => gateway.EnsureConfiguredAsync(default));
        Assert.That(error!.Code, Is.EqualTo("EXTERNAL_PROVIDER_NOT_CONFIGURED"));
        Assert.That(error.SuggestedStatusCode, Is.EqualTo(503));
    }

    [Test]
    public void UT_PAYOS_InvalidExpiryBindingReturnsControlledErrorWithoutOriginalValue()
    {
        using var provider = Services(new Dictionary<string, string?> { ["Payment:PayOS:ExpiryMinutes"] = "fixture-invalid-integer" });
        var gateway = provider.GetRequiredService<IPaymentGateway>();
        var error = Assert.ThrowsAsync<BusinessException>(() => gateway.EnsureConfiguredAsync(default));
        Assert.That(error!.Code, Is.EqualTo("EXTERNAL_PROVIDER_NOT_CONFIGURED"));
        Assert.That(error.Message, Does.Not.Contain("fixture-invalid-integer"));
        Assert.That(error.InnerException, Is.Null);
    }

    private static PayOsOptions Valid() => new() { ClientId = "fixture-client", ApiKey = "fixture-api-key", ChecksumKey = "fixture-checksum" };
    private static PayOsOptions WithExpiry(int minutes) { var options = Valid(); options.ExpiryMinutes = minutes; return options; }
    private static ServiceProvider Services(Dictionary<string, string?> values)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton<IClock, SystemClock>();
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
        return services.BuildServiceProvider();
    }
}
