using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using Frms.Business.Exceptions;
using Microsoft.Extensions.Options;

namespace Frms.Infrastructure.Email;

/// <summary>Internal SMTP boundary exposed as an interface for offline unit tests.</summary>
public interface ISmtpEmailTransport
{
    void EnsureConfigured();
    Task SendAsync(string recipientEmail, string subject, string plainTextBody,
        CancellationToken cancellationToken);
}

public sealed class SmtpEmailTransport(IOptions<SmtpEmailOptions> options) : ISmtpEmailTransport
{
    public void EnsureConfigured()
    {
        // Validate even if a caller bypassed DI's ValidateOnStart registration.
        if (!SmtpEmailOptions.IsValid(options.Value))
            throw new BusinessException("EXTERNAL_PROVIDER_NOT_CONFIGURED",
                "SMTP email configuration is missing or invalid.", 503);
    }

    public async Task SendAsync(string recipientEmail, string subject, string plainTextBody,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConfigured();
        var config = options.Value;
        if (!SmtpEmailOptions.IsMailbox(recipientEmail))
            throw new BusinessException("EMAIL_RECIPIENT_INVALID", "Email recipient is invalid.", 400);
        if (string.IsNullOrWhiteSpace(subject) || subject.Any(char.IsControl))
            throw new BusinessException("EMAIL_SUBJECT_INVALID", "Email subject is invalid.", 400);

        using var message = new MailMessage
        {
            From = new MailAddress(config.FromEmail, "FRMS", Encoding.UTF8),
            Subject = subject,
            SubjectEncoding = Encoding.UTF8,
            Body = plainTextBody,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(recipientEmail));

        using var smtp = new SmtpClient(config.Host, config.Port)
        {
            EnableSsl = true, // Port 587 uses STARTTLS; never allow plaintext fallback.
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(config.Username, config.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(config.TimeoutSeconds));
        try
        {
            // SMTP acceptance is not a guarantee of arrival in the recipient inbox.
            await smtp.SendMailAsync(message, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw Unavailable();
        }
        catch (SmtpException)
        {
            throw Unavailable();
        }
        catch (IOException)
        {
            throw Unavailable();
        }
        catch (SocketException)
        {
            throw Unavailable();
        }
        catch (System.Security.Authentication.AuthenticationException)
        {
            throw Unavailable();
        }
        catch (InvalidOperationException)
        {
            throw Unavailable();
        }
    }

    private static BusinessException Unavailable() =>
        new("EXTERNAL_PROVIDER_UNAVAILABLE", "SMTP email delivery is unavailable.", 503);
}
