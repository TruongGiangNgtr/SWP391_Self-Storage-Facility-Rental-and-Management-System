using Frms.Business.Abstractions.External;

namespace Frms.Infrastructure.Email;

/// <summary>ADM-005/ADM-012: send the generated initial password exactly as supplied by Business.</summary>
public sealed class SmtpInitialCredentialEmailService(ISmtpEmailTransport transport) : IEmailService
{
    public Task SendInitialCredentialAsync(string recipientEmail, string initialPassword,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(initialPassword);
        // Plain text: never treat the generated password as markup or a template token.
        var body = "An FRMS employee account has been provisioned.\n\n" +
                   "Initial password: " + initialPassword + "\n\n" +
                   "Sign in using this password and keep it confidential.\n" +
                   "If you did not expect this email, contact your administrator.\n";
        return transport.SendAsync(recipientEmail, "FRMS employee account credentials", body,
            cancellationToken);
    }
}
