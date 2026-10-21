using Frms.Business.Abstractions.External;
using Frms.Infrastructure.External;
using Frms.Infrastructure.Ai;
using Frms.Infrastructure.Email;
using Frms.Infrastructure.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Frms.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PayOsOptions>()
            .Bind(configuration.GetSection(PayOsOptions.SectionName));
        services.AddSingleton<IValidateOptions<PayOsOptions>, PayOsOptionsValidator>();

        services.AddHttpClient<IPaymentGateway, PayOsPaymentGateway>().RemoveAllLoggers();
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        if (string.Equals(configuration["Ai:Provider"], "OpenAICompatible", StringComparison.OrdinalIgnoreCase))
            services.AddHttpClient<IAiRecommendationProvider, OpenAiCompatibleRecommendationProvider>()
                .RemoveAllLoggers();
        else
            services.AddSingleton<IAiRecommendationProvider, UnconfiguredAiRecommendationProvider>();

        var emailProvider = configuration["Email:Provider"];
        if (string.Equals(emailProvider, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            // Fail closed: reject incomplete SMTP settings at startup, without exposing secrets.
            services.AddOptions<SmtpEmailOptions>()
                .Bind(configuration.GetSection(SmtpEmailOptions.SectionName))
                .Validate(SmtpEmailOptions.IsValid, "Email:Smtp configuration is invalid or incomplete.")
                .ValidateOnStart();
            services.AddTransient<ISmtpEmailTransport, SmtpEmailTransport>();
            services.AddTransient<IEmailService, SmtpInitialCredentialEmailService>();
            services.AddTransient<INotificationSender, SmtpNotificationSender>();
        }
        else if (string.IsNullOrWhiteSpace(emailProvider) ||
                 string.Equals(emailProvider, "Unconfigured", StringComparison.OrdinalIgnoreCase))
        {
            // Legacy-safe default: never pretend that an email was sent.
            services.AddSingleton<IEmailService, UnconfiguredEmailService>();
            services.AddSingleton<INotificationSender, UnconfiguredNotificationSender>();
        }
        else
        {
            throw new InvalidOperationException("Unsupported Email:Provider. Expected Smtp or Unconfigured.");
        }
        return services;
    }
}
