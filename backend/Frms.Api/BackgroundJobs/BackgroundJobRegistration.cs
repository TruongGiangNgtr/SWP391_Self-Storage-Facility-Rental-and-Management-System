using Microsoft.Extensions.DependencyInjection;

namespace Frms.Api.BackgroundJobs;

public static class BackgroundJobRegistration
{
    public static IServiceCollection AddFrmsBackgroundJobs(this IServiceCollection services)
    {
        // No Phase 0 job has authoritative scheduling semantics. Future jobs belong here and
        // must depend on Business service interfaces only.
        return services;
    }
}
