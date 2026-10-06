using Frms.Business.Abstractions.Time;
using Frms.Business.Services.Implementations;
using Frms.Business.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Frms.Business.DependencyInjection;

public static class BusinessServiceCollectionExtensions
{
    public static IServiceCollection AddBusiness(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IFacilityAuthorizationService, FacilityAuthorizationService>();
        services.AddScoped<IStorageUnitService, StorageUnitService>();
        services.AddScoped<IFacilityService, FacilityService>();
        services.AddScoped<IUnitTypeService, UnitTypeService>();
        services.AddScoped<IPolicyService, PolicyService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        return services;
    }
}
