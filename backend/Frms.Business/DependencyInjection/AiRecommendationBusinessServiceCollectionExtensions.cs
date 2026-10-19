using Frms.Business.Services.Implementations;
using Frms.Business.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Frms.Business.DependencyInjection;

/// <summary>Scoped registration for the optional AI-001 business service.</summary>
public static class AiRecommendationBusinessServiceCollectionExtensions
{
    public static IServiceCollection AddAiRecommendationBusiness(this IServiceCollection services)
    {
        services.AddScoped<IAiRecommendationService, AiRecommendationService>();
        return services;
    }
}
