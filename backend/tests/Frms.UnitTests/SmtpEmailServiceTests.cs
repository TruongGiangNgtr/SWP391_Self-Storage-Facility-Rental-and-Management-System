using Frms.Business.Abstractions.External;
using Frms.Infrastructure.DependencyInjection;
using Frms.Infrastructure.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Frms.UnitTests;

[TestFixture]
public sealed class SmtpEmailServiceTests
{
    [Test]
    public async Task InitialCredential_SendsPlainTextWithoutExternalSmtp()
    {
        var transport = new RecordingTransport();
        var service = new SmtpInitialCredentialEmailService(transport);
        await service.SendInitialCredentialAsync("employee@example.test", "GENERATED-SECRET-ONLY-FOR-TEST", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(transport.Recipient, Is.EqualTo("employee@example.test"));
            Assert.That(transport.Subject, Is.EqualTo("FRMS employee account credentials"));
            Assert.That(transport.Body, Does.Contain("GENERATED-SECRET-ONLY-FOR-TEST"));
            Assert.That(transport.Body, Does.Contain("keep it confidential"));
            Assert.That(transport.Body, Does.Not.Contain("<html"));
            Assert.That(transport.CallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void InitialCredential_PropagatesTransportFailure()
    {
        var service = new SmtpInitialCredentialEmailService(new RecordingTransport { Fail = true });
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.SendInitialCredentialAsync("employee@example.test", "TEST-PASSWORD", CancellationToken.None));
    }

    [Test]
    public async Task NotificationSender_UsesFixedSubjectAndPersistedContent()
    {
        var transport = new RecordingTransport();
        var sender = new SmtpNotificationSender(transport);
        await sender.EnsureConfiguredAsync(CancellationToken.None);
        await sender.SendAsync(new NotificationDeliveryRequest(
            Guid.NewGuid(), Guid.NewGuid(), "recipient@example.test", "TYPE\r\nBcc:bad", "Thông báo FRMS"),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(transport.ConfigChecks, Is.EqualTo(1));
            Assert.That(transport.Subject, Is.EqualTo("FRMS notification"));
            Assert.That(transport.Body, Is.EqualTo("Thông báo FRMS"));
        });
    }

    [Test]
    public void NotificationSender_PropagatesFailureForQueueRetry()
    {
        var sender = new SmtpNotificationSender(new RecordingTransport { Fail = true });
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sender.SendAsync(new NotificationDeliveryRequest(
                Guid.NewGuid(), Guid.NewGuid(), "recipient@example.test", "EVENT", "BODY"),
                CancellationToken.None));
    }

    [Test]
    public void SmtpOptions_RejectMissingCredentialsAndHeaderInjection()
    {
        var valid = ValidOptions();
        Assert.That(SmtpEmailOptions.IsValid(valid), Is.True);
        Assert.That(SmtpEmailOptions.IsValid(new SmtpEmailOptions()), Is.False);
        valid.FromEmail = "valid@example.test\r\nBcc: attacker@example.test";
        Assert.That(SmtpEmailOptions.IsValid(valid), Is.False);
        valid = ValidOptions();
        valid.Password = "BAD\nPASSWORD";
        Assert.That(SmtpEmailOptions.IsValid(valid), Is.False);
        valid = ValidOptions();
        valid.Port = 0;
        Assert.That(SmtpEmailOptions.IsValid(valid), Is.False);
        valid.Port = 465; // SmtpClient cannot negotiate implicit SMTPS.
        Assert.That(SmtpEmailOptions.IsValid(valid), Is.False);
    }

    [Test]
    public void Transport_RejectsHeaderInjectionBeforeAnyNetworkSend()
    {
        var transport = new SmtpEmailTransport(Options.Create(ValidOptions()));
        Assert.ThrowsAsync<Frms.Business.Exceptions.BusinessException>(async () =>
            await transport.SendAsync("victim@example.test\r\nBcc:attacker@example.test",
                "FRMS notice", "Body", CancellationToken.None));
        Assert.ThrowsAsync<Frms.Business.Exceptions.BusinessException>(async () =>
            await transport.SendAsync("victim@example.test", "Subject\r\nBcc:attacker@example.test",
                "Body", CancellationToken.None));
    }

    [Test]
    public void Transport_RejectsMissingConfigurationWithoutNetworkSend()
    {
        var transport = new SmtpEmailTransport(Options.Create(new SmtpEmailOptions()));
        Assert.Throws<Frms.Business.Exceptions.BusinessException>(() => transport.EnsureConfigured());
    }

    [Test]
    public void SmtpOptions_RedactPasswordFromSerializationAndDisplay()
    {
        var options = ValidOptions();
        Assert.That(options.ToString(), Does.Not.Contain("SYNTHETIC-SECRET"));
        var json = System.Text.Json.JsonSerializer.Serialize(options);
        Assert.That(json, Does.Not.Contain("SYNTHETIC-SECRET"));
        Assert.That(json, Does.Not.Contain("Password"));
    }

    [Test]
    public void DependencyInjection_UsesSafeUnconfiguredDefault()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build());
        Assert.Multiple(() =>
        {
            Assert.That(services.Last(x => x.ServiceType == typeof(IEmailService))
                .ImplementationType?.Name, Is.EqualTo("UnconfiguredEmailService"));
            Assert.That(services.Last(x => x.ServiceType == typeof(INotificationSender))
                .ImplementationType?.Name, Is.EqualTo("UnconfiguredNotificationSender"));
        });
    }

    [Test]
    public void DependencyInjection_SmtpOptInWiresBothAdapters()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Email:Provider"] = "Smtp",
            ["Email:Smtp:Host"] = "smtp.example.test",
            ["Email:Smtp:Port"] = "587",
            ["Email:Smtp:FromEmail"] = "frms@example.test",
            ["Email:Smtp:Username"] = "frms@example.test",
            ["Email:Smtp:Password"] = "SYNTHETIC-SECRET"
        }).Build();
        var services = new ServiceCollection();
        services.AddInfrastructure(config);
        using var provider = services.BuildServiceProvider();

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetRequiredService<IEmailService>(), Is.InstanceOf<SmtpInitialCredentialEmailService>());
            Assert.That(provider.GetRequiredService<INotificationSender>(), Is.InstanceOf<SmtpNotificationSender>());
            Assert.That(provider.GetRequiredService<IOptions<SmtpEmailOptions>>().Value.Host, Is.EqualTo("smtp.example.test"));
        });
    }

    [Test]
    public void DependencyInjection_IncompleteSmtpSettingsFailOptionsValidation()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Email:Provider"] = "Smtp",
            ["Email:Smtp:Host"] = "smtp.example.test",
            ["Email:Smtp:FromEmail"] = "frms@example.test",
            ["Email:Smtp:Username"] = "frms@example.test"
            // Password intentionally missing.
        }).Build();
        var services = new ServiceCollection();
        services.AddInfrastructure(config);
        using var provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() =>
            _ = provider.GetRequiredService<IOptions<SmtpEmailOptions>>().Value);
    }

    [Test]
    public void CredentialSend_CancellationPropagates()
    {
        var service = new SmtpInitialCredentialEmailService(new RecordingTransport());
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await service.SendInitialCredentialAsync("employee@example.test", "TEST-PASSWORD", cancelled.Token));
    }

    [Test]
    public void DependencyInjection_InvalidProviderFailsClosed()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Email:Provider"] = "UnknownProvider"
        }).Build();
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddInfrastructure(config));
    }

    private static SmtpEmailOptions ValidOptions() => new()
    {
        Host = "smtp.example.test", Port = 587, FromEmail = "frms@example.test",
        Username = "frms@example.test", Password = "SYNTHETIC-SECRET", TimeoutSeconds = 30
    };

    private sealed class RecordingTransport : ISmtpEmailTransport
    {
        public int ConfigChecks { get; private set; }
        public int CallCount { get; private set; }
        public bool Fail { get; set; }
        public string? Recipient { get; private set; }
        public string? Subject { get; private set; }
        public string? Body { get; private set; }

        public void EnsureConfigured() => ConfigChecks++;

        public Task SendAsync(string recipientEmail, string subject, string plainTextBody,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            if (Fail) throw new InvalidOperationException("Synthetic delivery failure");
            Recipient = recipientEmail;
            Subject = subject;
            Body = plainTextBody;
            return Task.CompletedTask;
        }
    }
}
