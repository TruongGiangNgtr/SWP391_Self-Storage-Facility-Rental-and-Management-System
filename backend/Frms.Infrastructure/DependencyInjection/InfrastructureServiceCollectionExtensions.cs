using Microsoft.Extensions.DependencyInjection;

namespace Frms.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // External-provider adapters are intentionally absent from this scaffold.
        return services;
    }
}
