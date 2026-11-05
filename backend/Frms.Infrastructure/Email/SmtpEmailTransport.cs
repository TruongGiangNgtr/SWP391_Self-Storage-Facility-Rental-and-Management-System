using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using Frms.Business.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Frms.Infrastructure.Email;

/// <summary>Internal SMTP boundary exposed as an interface for offline unit tests.</summary>
public interface ISmtpEmailTransport
{
    void EnsureConfigured();
    Task SendAsync(string recipientEmail, string subject, string plainTextBody,
        CancellationToken cancellationToken);
}

public sealed class SmtpEmailTransport(
    IOptions<SmtpEmailOptions> options,
    ILogger<SmtpEmailTransport> logger) : ISmtpEmailTransport
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
            logger.LogWarning("SMTP submission timed out after {TimeoutSeconds} seconds.",
                config.TimeoutSeconds);
            throw Unavailable();
        }
        catch (SmtpException ex)
        {
            // Do not log ex.Message/ex.ToString(): server replies can include sensitive content.
            var socketError = ex.InnerException is SocketException socket
                ? socket.SocketErrorCode.ToString()
                : "None";
            logger.LogWarning(
                "SMTP submission failed. Status={SmtpStatus}, InnerType={InnerType}, SocketError={SocketError}.",
                ex.StatusCode, ex.InnerException?.GetType().Name ?? "None", socketError);
            throw Unavailable();
        }
        catch (IOException)
        {
            logger.LogWarning("SMTP submission failed: network I/O error.");
            throw Unavailable();
        }
        catch (SocketException ex)
        {
            logger.LogWarning("SMTP submission failed: socket error {SocketError}.",
                ex.SocketErrorCode);
            throw Unavailable();
        }
        catch (System.Security.Authentication.AuthenticationException)
        {
            logger.LogWarning("SMTP submission failed: TLS authentication error.");
            throw Unavailable();
        }
        catch (InvalidOperationException)
        {
            logger.LogWarning("SMTP submission failed: invalid SMTP client operation.");
            throw Unavailable();
        }
    }

    private static BusinessException Unavailable() =>
        new("EXTERNAL_PROVIDER_UNAVAILABLE", "SMTP email delivery is unavailable.", 503);
}
