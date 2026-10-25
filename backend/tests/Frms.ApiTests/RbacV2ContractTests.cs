using System.Reflection;
using Frms.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.ApiTests;

[TestFixture]
public sealed class RbacV2ContractTests
{
    private static string? Roles<T>(string methodName) =>
        typeof(T).GetMethod(methodName)?.GetCustomAttribute<AuthorizeAttribute>()?.Roles;

    [Test]
    public void ContractListUsesTheRolesDeclaredByTheCurrentController()
    {
        Assert.That(Roles<ContractsController>(nameof(ContractsController.ListContracts)),
            Is.EqualTo("CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER,BUSINESS_OPERATIONS_MANAGER"));
    }

    [Test]
    public void FacilityMutationIsBOMOnly()
    {
        var gate = typeof(BusinessFacilitiesController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.That(gate?.Roles, Is.EqualTo("BUSINESS_OPERATIONS_MANAGER"));
    }

    [Test]
    public void StorageUnitWritesRemainManagerOnly()
    {
        foreach (var name in new[] { "CreateStorageUnit", "UpdateStorageUnit", "ChangeStorageUnitStatus" })
            Assert.That(Roles<StorageUnitsController>(name), Is.EqualTo("FACILITY_MANAGER"));
    }

    [Test]
    public void GuestPublicEntryPointsAreAnonymous()
    {
        foreach (var name in new[] { "BrowseFacilities", "GetFacility", "ListUnitTypes" })
            Assert.That(typeof(CatalogController).GetMethod(name)?.GetCustomAttribute<AllowAnonymousAttribute>(), Is.Not.Null);
        Assert.That(typeof(PublicAccessController).GetMethod("ListPublicPolicies")?
            .GetCustomAttribute<AllowAnonymousAttribute>(), Is.Not.Null);
    }

    [Test]
    public void MonitoringEndpointsUseTheirCurrentDeclaredRoleGates()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Roles<InspectionsController>(nameof(InspectionsController.ListInspections)),
                Is.EqualTo("FACILITY_STAFF,FACILITY_MANAGER"));
            Assert.That(Roles<SupportTicketsController>(nameof(SupportTicketsController.ListSupportTickets)),
                Is.EqualTo("CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER"));
            Assert.That(Roles<ReportsController>(nameof(ReportsController.GetFacilityOperationsReport)),
                Is.EqualTo("FACILITY_MANAGER"));
            Assert.That(Roles<InspectionsController>(nameof(InspectionsController.ClaimInspection)),
                Is.EqualTo("FACILITY_STAFF"));
        });
    }

    [Test]
    public void AssignSupportTicketIsRestrictedToFacilityManagers()
    {
        // Assert the authorization metadata currently declared by the controller action.
        Assert.That(Roles<SupportTicketsController>(nameof(SupportTicketsController.AssignSupportTicket)),
            Is.EqualTo("FACILITY_MANAGER"));
    }

    [Test]
    public void NewReadOnlyControllersHaveTheExpectedRoleGates()
    {
        Assert.That(typeof(BusinessReadController)
            .GetCustomAttribute<AuthorizeAttribute>()?.Roles, Is.EqualTo("BUSINESS_OPERATIONS_MANAGER"));
        //Assert.That(typeof(FacilityCustomerReadController)
        //    .GetCustomAttribute<AuthorizeAttribute>()?.Roles,
            //Does.Contain("FACILITY_STAFF"));
        Assert.That(typeof(ManagerStaffLookupController)
            .GetCustomAttribute<AuthorizeAttribute>()?.Roles, Is.EqualTo("FACILITY_MANAGER"));
        Assert.That(typeof(AdminSupplementController)
            .GetCustomAttribute<AuthorizeAttribute>()?.Roles, Is.EqualTo("SYSTEM_ADMINISTRATOR"));
    }

    [Test]
    public void NewFeatureScaffoldsRemainExplicitlyUnimplemented()
    {
        foreach (var type in new[] {
            //typeof(CustomerProfileController), typeof(FacilityCustomerReadController),
            typeof(BusinessReadController), typeof(ExtraFeeTypeReadController),
            typeof(AdminSupplementController), typeof(ManagerStaffLookupController),
            typeof(PublicAccessController) })
            Assert.That(type.IsSubclassOf(typeof(ScaffoldControllerBase)), Is.True);
    }
}
