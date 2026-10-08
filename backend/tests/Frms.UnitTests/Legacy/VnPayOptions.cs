using System.Text.Json.Serialization;

namespace Frms.Infrastructure.Payments;

public sealed class VnPayOptions
{
    public const string SectionName = "Payment:VnPay";

    public string TmnCode { get; set; } = string.Empty;

    [JsonIgnore]
    public string HashSecret { get; set; } = string.Empty;

    public string CredentialSetId { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string IpnUrl { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; }

    public override string ToString() => nameof(VnPayOptions);
}
