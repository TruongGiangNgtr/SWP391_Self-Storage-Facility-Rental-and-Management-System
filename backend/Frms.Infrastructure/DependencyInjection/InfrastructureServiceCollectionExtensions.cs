using Microsoft.Extensions.DependencyInjection;

namespace Frms.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // SRS V10 defines provider ownership but does not define enough Phase 0 method-level
        // contracts or selected providers to register a truthful runtime adapter here.
        return services;
    }
}
