using Frms.Infrastructure.Payments;

namespace Frms.UnitTests;

[TestFixture, Category("PayOsConfiguration")]
public sealed class PayOsOptionsEdgeCaseTests
{
    public static IEnumerable<TestCaseData> InvalidReturnUrls()
    {
        foreach (var value in new[] { "", " ", "/relative/path", "ftp://example.test/path", "http://example.test/path", "javascript:alert(1)", "https://user:pass@example.test/path", "https://example.test/path#fragment", "not a url" })
            yield return new TestCaseData(value).SetName($"invalid_return_url_{Slug(value)}");
    }

    [TestCaseSource(nameof(InvalidReturnUrls))]
    public void Validate_InvalidReturnUrl_IsRejected(string value)
    {
        var options = Valid();
        options.ReturnUrl = value;
        options.CancelUrl = value;
        Assert.That(new PayOsOptionsValidator().Validate(null, options).Failed, Is.True);
    }

    public static IEnumerable<TestCaseData> InvalidWebhookUrls()
    {
        foreach (var value in new[] { "", " ", "/webhook", "http://localhost/webhook", "http://example.test/webhook", "ftp://example.test/webhook", "https://user:pass@example.test/webhook", "https://example.test/webhook#fragment", "javascript:alert(1)", "not a url" })
            yield return new TestCaseData(value).SetName($"invalid_webhook_url_{Slug(value)}");
    }

    [TestCaseSource(nameof(InvalidWebhookUrls))]
    public void Validate_InvalidWebhookUrl_IsRejected(string value)
    {
        var options = Valid();
        options.WebhookUrl = value;
        Assert.That(new PayOsOptionsValidator().Validate(null, options).Failed, Is.True);
    }

    public static IEnumerable<TestCaseData> InvalidApiOrigins()
    {
        foreach (var value in new[] { "http://api-merchant.payos.vn", "https://api-merchant.payos.vn/", "https://api-merchant.payos.vn.evil.test", "https://evil.test", "", " " })
            yield return new TestCaseData(value).SetName($"invalid_api_origin_{Slug(value)}");
    }

    [TestCaseSource(nameof(InvalidApiOrigins))]
    public void Validate_OnlyOfficialApiOriginIsAllowed(string value)
    {
        var options = Valid();
        options.ApiBaseUrl = value;
        Assert.That(new PayOsOptionsValidator().Validate(null, options).Failed, Is.True);
    }

    [TestCase("ClientId")]
    [TestCase("ApiKey")]
    [TestCase("ChecksumKey")]
    public void Validate_CredentialContainingCarriageReturnOrNewline_IsRejected(string property)
    {
        foreach (var value in new[] { "secret\rvalue", "secret\nvalue", "secret\r\nvalue" })
        {
            var options = Valid();
            typeof(PayOsOptions).GetProperty(property)!.SetValue(options, value);
            Assert.That(new PayOsOptionsValidator().Validate(null, options).Failed, Is.True, $"Expected {property} to reject CR/LF.");
        }
    }

    [TestCase(-100)]
    [TestCase(-1)]
    [TestCase(0)]
    public void Validate_NonPositiveExpiry_IsRejected(int expiry)
    {
        var options = Valid();
        options.ExpiryMinutes = expiry;
        Assert.That(new PayOsOptionsValidator().Validate(null, options).Failed, Is.True);
    }

    [TestCase(1)]
    [TestCase(5)]
    [TestCase(15)]
    [TestCase(60)]
    [TestCase(int.MaxValue)]
    public void Validate_PositiveExpiry_IsAccepted(int expiry)
    {
        var options = Valid();
        options.ExpiryMinutes = expiry;
        Assert.That(new PayOsOptionsValidator().Validate(null, options).Succeeded, Is.True);
    }

    [Test]
    public void Validate_MismatchedReturnAndCancelRoutes_IsRejected()
    {
        var options = Valid();
        options.CancelUrl = "http://localhost:5173/other/result";
        Assert.That(new PayOsOptionsValidator().Validate(null, options).Failed, Is.True);
    }

    [Test]
    public void Validate_NullOptions_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => new PayOsOptionsValidator().Validate(null, null!));

    private static PayOsOptions Valid() => new()
    {
        ClientId = "fixture-client",
        ApiKey = "fixture-api-key",
        ChecksumKey = "fixture-checksum",
        ApiBaseUrl = "https://api-merchant.payos.vn",
        ReturnUrl = "http://localhost:5173/customer/payments/result",
        CancelUrl = "http://localhost:5173/customer/payments/result",
        WebhookUrl = "https://hooks.example.test/api/v1/payments/payos/webhook",
        ExpiryMinutes = 15
    };

    private static string Slug(string value)
    {
        if (string.IsNullOrEmpty(value)) return "empty";
        if (string.IsNullOrWhiteSpace(value)) return "whitespace";
        var slug = new string(value.Where(char.IsLetterOrDigit).Take(24).ToArray());
        return string.IsNullOrEmpty(slug) ? $"symbols_{value.Length}" : slug;
    }
}
