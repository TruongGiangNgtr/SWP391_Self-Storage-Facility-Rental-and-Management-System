using Microsoft.Extensions.Options;

namespace Frms.Infrastructure.Payments;

public sealed class PayOsOptionsValidator : IValidateOptions<PayOsOptions>
{
    public ValidateOptionsResult Validate(string? name, PayOsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var failures = new List<string>();
        foreach (var (field, value) in new[] { ("ClientId", options.ClientId), ("ApiKey", options.ApiKey), ("ChecksumKey", options.ChecksumKey) })
            if (string.IsNullOrWhiteSpace(value) || value.Contains('\r') || value.Contains('\n'))
                failures.Add($"Payment:PayOS:{field} is required and must be a single line.");
        if (options.ApiBaseUrl != "https://api-merchant.payos.vn")
            failures.Add("Payment:PayOS:ApiBaseUrl must be the official HTTPS production origin.");
        if (!Url(options.ReturnUrl, false)) failures.Add("Payment:PayOS:ReturnUrl must be an absolute HTTP(S) URL without credentials.");
        if (!Url(options.CancelUrl, false)) failures.Add("Payment:PayOS:CancelUrl must be an absolute HTTP(S) URL without credentials.");
        if (options.ReturnUrl != options.CancelUrl) failures.Add("Payment:PayOS:ReturnUrl and CancelUrl must use the same approved result route.");
        if (!Url(options.WebhookUrl, true)) failures.Add("Payment:PayOS:WebhookUrl must be an absolute HTTPS URL without credentials.");
        if (options.ExpiryMinutes <= 0) failures.Add("Payment:PayOS:ExpiryMinutes must be greater than zero.");
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static bool Url(string? value, bool https) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Fragment)
        && (uri.Scheme == Uri.UriSchemeHttps || (!https && uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback));
}
