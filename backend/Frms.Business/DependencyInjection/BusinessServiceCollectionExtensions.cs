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
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IFacilityAuthorizationService, FacilityAuthorizationService>();
        services.AddScoped<IStorageUnitService, StorageUnitService>();
        services.AddScoped<IFacilityService, FacilityService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IVisitService, VisitService>();
        services.AddScoped<IInspectionService, InspectionService>();
        services.AddScoped<ISupportTicketService, SupportTicketService>();
        services.AddScoped<ICapacityService, CapacityService>();
        services.AddScoped<IReservationExpirationService,ReservationExpirationService>();
        services.AddScoped<IHandoverService, HandoverService>();
        services.AddScoped<IRenewalService, RenewalService>();
        services.AddScoped<IContractService, ContractService>();
        services.AddScoped<IFacilityOperationsReportService, FacilityOperationsReportService>();
        services.AddScoped<IUnitTypeService, UnitTypeService>();
        services.AddScoped<IPolicyService, PolicyService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminEmployeeService, AdminEmployeeService>();
        services.AddScoped<IFacilityCatalogService,FacilityCatalogService>();
        services.AddScoped<IDiscountService, DiscountService>();

        services.AddScoped<IStaffWorkItemService,StaffWorkItemService>();
        services.AddScoped<IReturnProcessingService, ReturnProcessingService>();
        services.AddScoped<IInspectionWorkflowService, InspectionWorkflowService>();
        return services;
    }
}
