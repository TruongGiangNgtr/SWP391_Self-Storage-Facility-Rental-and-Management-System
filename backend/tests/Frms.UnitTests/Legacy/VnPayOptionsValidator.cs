using Microsoft.Extensions.Options;

namespace Frms.Infrastructure.Payments;

public sealed class VnPayOptionsValidator : IValidateOptions<VnPayOptions>
{
    public ValidateOptionsResult Validate(string? name, VnPayOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        Required(options.TmnCode, "TmnCode", failures);
        Required(options.HashSecret, "HashSecret", failures);
        Required(options.CredentialSetId, "CredentialSetId", failures);

        if (!AbsoluteUrl(options.PaymentUrl, Uri.UriSchemeHttps))
            failures.Add("Payment:VnPay:PaymentUrl must be an absolute HTTPS URL.");
        if (!AbsoluteUrl(options.ReturnUrl, Uri.UriSchemeHttp, Uri.UriSchemeHttps))
            failures.Add("Payment:VnPay:ReturnUrl must be an absolute HTTP or HTTPS URL.");
        if (!AbsoluteUrl(options.IpnUrl, Uri.UriSchemeHttps))
            failures.Add("Payment:VnPay:IpnUrl must be an absolute HTTPS URL.");
        if (options.Locale is not ("vn" or "en"))
            failures.Add("Payment:VnPay:Locale must be either 'vn' or 'en'.");
        Required(options.OrderType, "OrderType", failures);
        if (options.ExpiryMinutes <= 0)
            failures.Add("Payment:VnPay:ExpiryMinutes must be greater than zero.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void Required(string? value, string propertyName, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
            failures.Add($"Payment:VnPay:{propertyName} is required.");
    }

    private static bool AbsoluteUrl(string? value, params string[] schemes) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && schemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase);
}
