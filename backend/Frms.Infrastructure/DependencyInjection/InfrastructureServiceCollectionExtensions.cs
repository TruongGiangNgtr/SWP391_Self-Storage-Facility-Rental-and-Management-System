using Frms.Business.Abstractions.External;
using Frms.Infrastructure.External;
using Microsoft.Extensions.DependencyInjection;

namespace Frms.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IPaymentGateway, UnconfiguredPaymentGateway>();
        services.AddSingleton<IAiRecommendationProvider, UnconfiguredAiRecommendationProvider>();
        services.AddSingleton<IEmailService, UnconfiguredEmailService>();
        services.AddSingleton<INotificationSender, UnconfiguredNotificationSender>();
        return services;
    }
}
