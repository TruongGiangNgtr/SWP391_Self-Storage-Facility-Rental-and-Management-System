using Microsoft.Extensions.DependencyInjection;

namespace Frms.Business.DependencyInjection;

public static class BusinessServiceCollectionExtensions
{
    public static IServiceCollection AddBusiness(this IServiceCollection services)
    {
        // Business implementations are intentionally absent from this scaffold.
        return services;
    }
}
