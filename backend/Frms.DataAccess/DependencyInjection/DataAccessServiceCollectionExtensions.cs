using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Implementations;
using Frms.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frms.DataAccess.DependencyInjection;

public static class DataAccessServiceCollectionExtensions
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration configuration)
    {
        // Preserve the existing name; FrmsDb also supports the documented .NET environment variable.
        var connectionString = configuration.GetConnectionString("Frms")
            ?? configuration.GetConnectionString("FrmsDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("SQL Server configuration is required. Set ConnectionStrings__Frms (existing convention) or ConnectionStrings__FrmsDb.");

        return services.AddDataAccess(connectionString);
    }

    public static IServiceCollection AddDataAccess(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        services.AddDbContext<FrmsDbContext>(options => options.UseSqlServer(connectionString));
        services.AddHealthChecks().AddDbContextCheck<FrmsDbContext>("sqlserver");
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<IStorageUnitRepository, StorageUnitRepository>();
        services.AddScoped<IFacilityRepository, FacilityRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IVisitRepository, VisitRepository>();
        services.AddScoped<ICapacityRepository, CapacityRepository>();
        services.AddScoped<IReservationExpirationRepository, ReservationExpirationRepository>();
        services.AddScoped<IHandoverRepository, HandoverRepository>();
        services.AddScoped<IRenewalRepository, RenewalRepository>();
        services.AddScoped<IUnitTypeRepository, UnitTypeRepository>();
        services.AddScoped<IPolicyRepository, PolicyRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IAdminEmployeeRepository, AdminEmployeeRepository>();
        services.AddScoped<IDiscountRepository, DiscountRepository>();
        return services;
    }
}
