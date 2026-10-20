using Frms.Infrastructure.Payments;

namespace Frms.UnitTests;

[TestFixture, Category("PayOsConfiguration")]
public sealed class PayOsOptionsBulkEdgeTests
{
    public static IEnumerable<TestCaseData> InvalidConfigurationCases()
    {
        var credentialValues = new[] { "", " ", "\t", "\r", "\n", "key\rvalue", "key\nvalue", "key\r\nvalue", "\rkey", "key\n" };
        foreach (var property in new[] { "ClientId", "ApiKey", "ChecksumKey" })
            for (var i = 0; i < credentialValues.Length; i++)
                yield return Case(property, credentialValues[i], false, $"{property}_invalid_credential_{i}");

        var apiOrigins = new[]
        {
            "", " ", "http://api-merchant.payos.vn", "https://api-merchant.payos.vn/",
            "https://api-merchant.payos.vn.evil.test", "https://evil.test", "http://localhost",
            "ftp://api-merchant.payos.vn", "https://user@api-merchant.payos.vn", "https://api-merchant.payos.vn?x=1"
        };
        for (var i = 0; i < apiOrigins.Length; i++)
            yield return Case("ApiBaseUrl", apiOrigins[i], false, $"api_origin_invalid_{i}");

        var resultUrls = new[]
        {
            "", " ", "/relative/path", "ftp://example.test/path", "http://example.test/path",
            "javascript:alert(1)", "https://user:pass@example.test/path", "https://example.test/path#fragment",
            "not a url", "//example.test/path"
        };
        for (var i = 0; i < resultUrls.Length; i++)
        {
            yield return Case("ReturnUrl", resultUrls[i], true, $"return_url_invalid_{i}");
            yield return Case("CancelUrl", resultUrls[i], true, $"cancel_url_invalid_{i}");
        }

        var webhookUrls = new[]
        {
            "", " ", "/webhook", "http://localhost/webhook", "http://example.test/webhook",
            "ftp://example.test/webhook", "https://user:pass@example.test/webhook",
            "https://example.test/webhook#fragment", "javascript:alert(1)", "not a url"
        };
        for (var i = 0; i < webhookUrls.Length; i++)
            yield return Case("WebhookUrl", webhookUrls[i], false, $"webhook_url_invalid_{i}");

        foreach (var value in Enumerable.Range(-29, 30))
            yield return Case("ExpiryMinutes", value.ToString(), false, $"expiry_minutes_{value}");
    }

    [TestCaseSource(nameof(InvalidConfigurationCases))]
    public void Validate_BulkMalformedConfiguration_IsRejected(string property, string value,
        bool synchronizeResultRoutes)
    {
        var options = Valid();
        if (property == "ExpiryMinutes")
        {
            options.ExpiryMinutes = int.Parse(value);
        }
        else
        {
            typeof(PayOsOptions).GetProperty(property)!.SetValue(options, value);
            if (synchronizeResultRoutes)
            {
                if (property == "ReturnUrl") options.CancelUrl = value;
                if (property == "CancelUrl") options.ReturnUrl = value;
            }
        }

        Assert.That(new PayOsOptionsValidator().Validate(null, options).Failed, Is.True,
            $"Expected {property} value '{value}' to be rejected.");
    }

    private static TestCaseData Case(string property, string value, bool synchronizeResultRoutes, string name) =>
        new TestCaseData(property, value, synchronizeResultRoutes).SetName(name);

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
}
