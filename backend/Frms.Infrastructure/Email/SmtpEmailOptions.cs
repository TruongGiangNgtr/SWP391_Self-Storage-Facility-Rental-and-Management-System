using System.Net.Mail;
using System.Text.Json.Serialization;

namespace Frms.Infrastructure.Email;

/// <summary>SMTP submission over STARTTLS; inject credentials through secret storage or environment.</summary>
public sealed class SmtpEmailOptions
{
    public const string SectionName = "Email:Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string FromEmail { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    [JsonIgnore] public string Password { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;

    public static bool IsValid(SmtpEmailOptions options) =>
        options is not null &&
        !string.IsNullOrWhiteSpace(options.Host) &&
        !options.Host.Any(char.IsWhiteSpace) &&
        !options.Host.Any(char.IsControl) &&
        options.Port is > 0 and <= 65535 and not 465 &&
        IsMailbox(options.FromEmail) &&
        !string.IsNullOrWhiteSpace(options.Username) &&
        !options.Username.Any(char.IsControl) &&
        !string.IsNullOrWhiteSpace(options.Password) &&
        !options.Password.Any(char.IsControl) &&
        options.TimeoutSeconds is >= 5 and <= 120;

    internal static bool IsMailbox(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        !value.Any(char.IsControl) &&
        MailAddress.TryCreate(value, out var parsed) &&
        parsed is not null &&
        string.Equals(parsed.Address, value, StringComparison.Ordinal);

    // Never disclose the secret if this configuration object is logged or serialized.
    public override string ToString() => nameof(SmtpEmailOptions);
}
