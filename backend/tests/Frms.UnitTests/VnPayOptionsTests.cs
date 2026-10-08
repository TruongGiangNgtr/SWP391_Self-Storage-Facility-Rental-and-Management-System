using Frms.Infrastructure.DependencyInjection;
using Frms.Infrastructure.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Frms.UnitTests;

[TestFixture]
[Category("VnPayConfiguration")]
public sealed class VnPayOptionsTests
{
    private const string FakeHashSecret = "fake-vnpay-hash-secret-for-tests-only";

    [Test]
    public void UT_VNPAY_CONFIG_001_ApprovedSandboxConfigurationBindsAllProperties()
    {
        using var provider = Provider(Values());
        var options = provider.GetRequiredService<IOptions<VnPayOptions>>().Value;

        Assert.Multiple(() =>
        {
            Assert.That(VnPayOptions.SectionName, Is.EqualTo("Payment:VnPay"));
            Assert.That(options.TmnCode, Is.EqualTo("FAKETMN1"));
            Assert.That(options.HashSecret, Is.EqualTo(FakeHashSecret));
            Assert.That(options.CredentialSetId, Is.EqualTo("sandbox-v1"));
            Assert.That(options.PaymentUrl, Is.EqualTo("https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"));
            Assert.That(options.ReturnUrl, Is.EqualTo("http://localhost:5173/customer/payments/result"));
            Assert.That(options.IpnUrl, Is.EqualTo("https://lying-ladder-showroom.ngrok-free.dev/api/v1/payments/vnpay/ipn"));
            Assert.That(options.Locale, Is.EqualTo("vn"));
            Assert.That(options.OrderType, Is.EqualTo("other"));
            Assert.That(options.ExpiryMinutes, Is.EqualTo(15));
        });
    }

    [Test]
    public void UT_VNPAY_CONFIG_002_ApprovedSandboxConfigurationPassesValidation()
    {
        var result = new VnPayOptionsValidator().Validate(null, ValidOptions());

        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public void UT_VNPAY_CONFIG_008_EnglishLocaleHttpsReturnAndMinimumPositiveExpiryPassValidation()
    {
        var options = ValidOptions();
        options.Locale = "en";
        options.ReturnUrl = "https://localhost:5173/customer/payments/result";
        options.ExpiryMinutes = 1;

        var result = new VnPayOptionsValidator().Validate(null, options);

        Assert.That(result.Succeeded, Is.True);
    }

    [TestCase("TmnCode", "Payment:VnPay:TmnCode is required.")]
    [TestCase("HashSecret", "Payment:VnPay:HashSecret is required.")]
    [TestCase("CredentialSetId", "Payment:VnPay:CredentialSetId is required.")]
    [TestCase("OrderType", "Payment:VnPay:OrderType is required.")]
    public void UT_VNPAY_CONFIG_003_RequiredTextRejectsWhitespace(string property, string expectedFailure)
    {
        var options = ValidOptions();
        switch (property)
        {
            case "TmnCode": options.TmnCode = " "; break;
            case "HashSecret": options.HashSecret = " "; break;
            case "CredentialSetId": options.CredentialSetId = " "; break;
            case "OrderType": options.OrderType = " "; break;
            default: Assert.Fail("Unknown test property."); break;
        }

        var result = new VnPayOptionsValidator().Validate(null, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Failures, Does.Contain(expectedFailure));
        });
    }

    [TestCase("PaymentUrl", "http://sandbox.vnpayment.vn/pay", "Payment:VnPay:PaymentUrl must be an absolute HTTPS URL.")]
    [TestCase("PaymentUrl", "/relative/pay", "Payment:VnPay:PaymentUrl must be an absolute HTTPS URL.")]
    [TestCase("IpnUrl", "http://example.invalid/ipn", "Payment:VnPay:IpnUrl must be an absolute HTTPS URL.")]
    [TestCase("IpnUrl", "/relative/ipn", "Payment:VnPay:IpnUrl must be an absolute HTTPS URL.")]
    [TestCase("ReturnUrl", "ftp://example.invalid/result", "Payment:VnPay:ReturnUrl must be an absolute HTTP or HTTPS URL.")]
    [TestCase("ReturnUrl", "/relative/result", "Payment:VnPay:ReturnUrl must be an absolute HTTP or HTTPS URL.")]
    public void UT_VNPAY_CONFIG_004_UrlsRejectUnsupportedOrRelativeValues(
        string property, string value, string expectedFailure)
    {
        var options = ValidOptions();
        switch (property)
        {
            case "PaymentUrl": options.PaymentUrl = value; break;
            case "IpnUrl": options.IpnUrl = value; break;
            case "ReturnUrl": options.ReturnUrl = value; break;
            default: Assert.Fail("Unknown test property."); break;
        }

        var result = new VnPayOptionsValidator().Validate(null, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Failures, Does.Contain(expectedFailure));
        });
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void UT_VNPAY_CONFIG_005_NonPositiveExpiryIsRejected(int expiryMinutes)
    {
        var options = ValidOptions();
        options.ExpiryMinutes = expiryMinutes;

        var result = new VnPayOptionsValidator().Validate(null, options);

        Assert.That(result.Failures,
            Does.Contain("Payment:VnPay:ExpiryMinutes must be greater than zero."));
    }

    [TestCase("")]
    [TestCase("fr")]
    [TestCase("VN")]
    public void UT_VNPAY_CONFIG_006_UnsupportedLocaleIsRejected(string locale)
    {
        var options = ValidOptions();
        options.Locale = locale;

        var result = new VnPayOptionsValidator().Validate(null, options);

        Assert.That(result.Failures,
            Does.Contain("Payment:VnPay:Locale must be either 'vn' or 'en'."));
    }

    [Test]
    public void UT_VNPAY_CONFIG_007_ValidationFailureDoesNotExposeSecret()
    {
        var values = Values();
        values["Payment:VnPay:PaymentUrl"] = "not-an-absolute-url";
        using var provider = Provider(values);

        var exception = Assert.Throws<OptionsValidationException>(() =>
            _ = provider.GetRequiredService<IOptions<VnPayOptions>>().Value);

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("Payment:VnPay:PaymentUrl"));
            Assert.That(exception.Message, Does.Not.Contain(FakeHashSecret));
            Assert.That(ValidOptions().ToString(), Is.EqualTo(nameof(VnPayOptions))
                .And.Not.Contain(FakeHashSecret));
        });
    }

    private static ServiceProvider Provider(IDictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    private static Dictionary<string, string?> Values() => new()
    {
        ["Payment:VnPay:TmnCode"] = "FAKETMN1",
        ["Payment:VnPay:HashSecret"] = FakeHashSecret,
        ["Payment:VnPay:CredentialSetId"] = "sandbox-v1",
        ["Payment:VnPay:PaymentUrl"] = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
        ["Payment:VnPay:ReturnUrl"] = "http://localhost:5173/customer/payments/result",
        ["Payment:VnPay:IpnUrl"] = "https://lying-ladder-showroom.ngrok-free.dev/api/v1/payments/vnpay/ipn",
        ["Payment:VnPay:Locale"] = "vn",
        ["Payment:VnPay:OrderType"] = "other",
        ["Payment:VnPay:ExpiryMinutes"] = "15"
    };

    private static VnPayOptions ValidOptions() => new()
    {
        TmnCode = "FAKETMN1",
        HashSecret = FakeHashSecret,
        CredentialSetId = "sandbox-v1",
        PaymentUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
        ReturnUrl = "http://localhost:5173/customer/payments/result",
        IpnUrl = "https://lying-ladder-showroom.ngrok-free.dev/api/v1/payments/vnpay/ipn",
        Locale = "vn",
        OrderType = "other",
        ExpiryMinutes = 15
    };
}
