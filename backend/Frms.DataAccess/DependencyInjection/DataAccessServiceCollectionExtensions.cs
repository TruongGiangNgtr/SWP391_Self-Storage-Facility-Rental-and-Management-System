using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Implementations;
using Frms.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Frms.DataAccess.DependencyInjection;

public static class DataAccessServiceCollectionExtensions
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<FrmsDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        return services;
    }
}
