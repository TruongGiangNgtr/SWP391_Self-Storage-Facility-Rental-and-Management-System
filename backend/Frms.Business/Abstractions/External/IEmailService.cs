namespace Frms.Business.Abstractions.External;

public interface IEmailService
{
    Task SendInitialCredentialAsync(string recipientEmail, string initialPassword, CancellationToken cancellationToken);
}
