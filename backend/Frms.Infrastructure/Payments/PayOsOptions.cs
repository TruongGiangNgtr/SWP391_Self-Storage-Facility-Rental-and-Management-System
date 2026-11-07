using System.Text.Json.Serialization;

namespace Frms.Infrastructure.Payments;

public sealed class PayOsOptions
{
    public const string SectionName = "Payment:PayOS";
    [JsonIgnore] public string ClientId { get; set; } = string.Empty;
    [JsonIgnore] public string ApiKey { get; set; } = string.Empty;
    [JsonIgnore] public string ChecksumKey { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://api-merchant.payos.vn";
    public string ReturnUrl { get; set; } = "Payment__PayOS__ReturnUrl";
    public string CancelUrl { get; set; } = "Payment__PayOS__CancelUrl";
    public string WebhookUrl { get; set; } = "https://swp391-self-storage-facility-rental-and.onrender.com/api/v1/payments/payos/webhook";
    public int ExpiryMinutes { get; set; } = 15;
    public override string ToString() => nameof(PayOsOptions);
}
