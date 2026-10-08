using Frms.Business.Abstractions.External;
using Frms.Infrastructure.External;
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
        services.AddSingleton<IAiRecommendationProvider, UnconfiguredAiRecommendationProvider>();
        services.AddSingleton<IEmailService, UnconfiguredEmailService>();
        services.AddSingleton<INotificationSender, UnconfiguredNotificationSender>();
        return services;
    }
}
