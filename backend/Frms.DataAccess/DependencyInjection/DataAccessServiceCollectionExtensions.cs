using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Implementations;
using Frms.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frms.DataAccess.DependencyInjection;

public static class DataAccessServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseAuditActor(this IServiceCollection services,
        Func<IServiceProvider, Guid?> resolveActor)
    {
        ArgumentNullException.ThrowIfNull(resolveActor);
        services.AddScoped(provider => new DatabaseAuditActor(() => resolveActor(provider)));
        return services;
    }

    public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration configuration)
    {
        // Preserve the existing name; FrmsDb also supports the documented .NET environment variable.
        var connectionString = configuration.GetConnectionString("Frms")
            ?? configuration.GetConnectionString("FrmsDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("PostgreSQL configuration is required. Set ConnectionStrings__Frms (existing convention) or ConnectionStrings__FrmsDb.");

        return services.AddDataAccess(connectionString);
    }

    public static IServiceCollection AddDataAccess(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        if (new NpgsqlConnectionStringBuilder(connectionString).Multiplexing)
            throw new InvalidOperationException("FRMS audit requires session-affine connections. Disable multiplexing and use direct or session pooling.");
        services.TryAddScoped<PostgreSqlAuditInterceptor>();
        services.AddDbContext<FrmsDbContext>((provider, options) => options.UseNpgsql(connectionString)
            .AddInterceptors(provider.GetRequiredService<PostgreSqlAuditInterceptor>()));
        services.AddHealthChecks().AddDbContextCheck<FrmsDbContext>("postgresql");
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<ICustomerRegistrationRepository, CustomerRegistrationRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IReportRevenueRepository, ReportRevenueRepository>();
        services.AddScoped<IStorageUnitRepository, StorageUnitRepository>();
        services.AddScoped<IStorageUnitTimelineRepository, StorageUnitTimelineRepository>();
        services.AddScoped<IFacilityRepository, FacilityRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IVisitRepository, VisitRepository>();
        services.AddScoped<IReturnVisitRepository, ReturnVisitRepository>();
        services.AddScoped<IInspectionRepository, InspectionRepository>();
        services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
        services.AddScoped<ICapacityRepository, CapacityRepository>();
        services.AddScoped<IReservationExpirationRepository, ReservationExpirationRepository>();
        services.AddScoped<IHandoverRepository, HandoverRepository>();
        services.AddScoped<IRenewalRepository, RenewalRepository>();
        services.AddScoped<IContractRepository, ContractRepository>();
        services.AddScoped<IBillingRepository, BillingRepository>();
        services.AddScoped<IFacilityOperationsReportRepository, FacilityOperationsReportRepository>();
        services.AddScoped<IUnitTypeRepository, UnitTypeRepository>();
        services.AddScoped<IPolicyRepository, PolicyRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IAdminEmployeeRepository, AdminEmployeeRepository>();
        services.AddScoped<IDiscountRepository, DiscountRepository>();
        services.AddScoped<IOperationalCatalogRepository, OperationalCatalogRepository>();
        services.AddScoped<IEmployeeDirectoryRepository, EmployeeDirectoryRepository>();
        services.AddScoped<IAdminReadRepository, AdminReadRepository>();
        services.AddScoped<IAdminCustomerStatusRepository, AdminCustomerStatusRepository>();
        services.AddScoped<IBusinessDiscountRepository, BusinessDiscountRepository>();
        services.AddScoped<IFacilityCatalogRepository,FacilityCatalogRepository>();
        services.AddScoped<IStaffWorkItemRepository,StaffWorkItemRepository>();

        services.AddScoped<ReturnSqlExecutor>();
        services.AddScoped<IReturnProcessingRepository, ReturnProcessingRepository>();
        services.AddScoped<IInspectionWorkflowRepository, InspectionWorkflowRepository>();
        return services;
    }
}
