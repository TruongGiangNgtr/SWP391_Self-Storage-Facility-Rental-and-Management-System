using Frms.Business.Abstractions.Time;
using Frms.Business.Services.Implementations;
using Frms.Business.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Frms.Business.DependencyInjection;

public static class BusinessServiceCollectionExtensions
{
    public static IServiceCollection AddBusiness(
        this IServiceCollection services,
        string? businessTimeZone = null)
    {
        services.AddSingleton<IClock>(_ => new SystemClock(businessTimeZone ?? "Asia/Ho_Chi_Minh"));
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ICustomerRegistrationService, CustomerRegistrationService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IFacilityAuthorizationService, FacilityAuthorizationService>();
        services.AddScoped<IStorageUnitService, StorageUnitService>();
        services.AddScoped<IStorageUnitTimelineService, StorageUnitTimelineService>();
        services.AddScoped<IFacilityService, FacilityService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IVisitService, VisitService>();
        services.AddScoped<IReturnVisitService, ReturnVisitService>();
        services.AddScoped<IInspectionService, InspectionService>();
        services.AddScoped<ISupportTicketService, SupportTicketService>();
        services.AddScoped<ICapacityService, CapacityService>();
        services.AddScoped<IReservationExpirationService,ReservationExpirationService>();
        services.AddScoped<IHandoverService, HandoverService>();
        services.AddScoped<IHandoverUnitAssignmentService, HandoverUnitAssignmentService>();
        services.AddScoped<IRenewalService, RenewalService>();
        services.AddScoped<IContractService, ContractService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IFacilityOperationsReportService, FacilityOperationsReportService>();
        services.AddScoped<IReportRevenueService, ReportRevenueService>();
        services.AddScoped<IBusinessOverviewService, BusinessOverviewService>();
        services.AddScoped<IUnitTypeService, UnitTypeService>();
        services.AddScoped<IPolicyService, PolicyService>();
        services.AddScoped<IPublicPolicyService, PublicPolicyService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminEmployeeService, AdminEmployeeService>();
        services.AddScoped<IFacilityCatalogService,FacilityCatalogService>();
        services.AddScoped<IDiscountService, DiscountService>();
        services.AddScoped<IOperationalCatalogService, OperationalCatalogService>();
        services.AddScoped<IEmployeeDirectoryService, EmployeeDirectoryService>();
        services.AddScoped<IAdminReadService, AdminReadService>();
        services.AddScoped<IAdminCustomerStatusService, AdminCustomerStatusService>();
        services.AddScoped<IBusinessDiscountService, BusinessDiscountService>();
        services.AddScoped<ICustomerActivityService, CustomerActivityService>();

        services.AddScoped<IStaffWorkItemService,StaffWorkItemService>();
        services.AddScoped<IReturnProcessingService, ReturnProcessingService>();
        services.AddScoped<IInspectionWorkflowService, InspectionWorkflowService>();
        return services;
    }
}
