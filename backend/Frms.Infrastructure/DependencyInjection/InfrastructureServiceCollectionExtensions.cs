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
        services.AddOptions<VnPayOptions>()
            .Bind(configuration.GetSection(VnPayOptions.SectionName));
        services.AddSingleton<IValidateOptions<VnPayOptions>, VnPayOptionsValidator>();

        services.AddSingleton<IPaymentGateway, VnPayPaymentGateway>();
        services.AddSingleton<IAiRecommendationProvider, UnconfiguredAiRecommendationProvider>();
        services.AddSingleton<IEmailService, UnconfiguredEmailService>();
        services.AddSingleton<INotificationSender, UnconfiguredNotificationSender>();
        return services;
    }
}
