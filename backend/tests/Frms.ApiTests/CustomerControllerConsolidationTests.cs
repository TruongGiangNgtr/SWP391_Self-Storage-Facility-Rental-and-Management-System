using System.Reflection;
using Frms.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Frms.ApiTests;

[TestFixture]
public sealed class CustomerControllerConsolidationTests
{
    [TestCase("UpdateOwnProfile", "PATCH", "customers/me/profile", "CUSTOMER")]
    [TestCase("ListFacilityCustomers", "GET", "facilities/{facilityId:guid}/customers", "FACILITY_STAFF,FACILITY_MANAGER")]
    [TestCase("GetFacilityCustomer", "GET", "facilities/{facilityId:guid}/customers/{customerId:guid}", "FACILITY_STAFF,FACILITY_MANAGER")]
    [TestCase("ListCustomers", "GET", "business/customers", "BUSINESS_OPERATIONS_MANAGER")]
    [TestCase("GetCustomer", "GET", "business/customers/{customerId:guid}", "BUSINESS_OPERATIONS_MANAGER")]
    public void CustomerEndpoints_PreserveMethodRouteAndRole(string action, string verb, string route, string roles)
    {
        var controller = typeof(CustomersController);
        Assert.That(controller.GetCustomAttribute<RouteAttribute>()?.Template, Is.EqualTo("api/v1"));

        var method = controller.GetMethod(action, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.That(method, Is.Not.Null, action);
        var http = method!.GetCustomAttributes<HttpMethodAttribute>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(http.HttpMethods, Does.Contain(verb));
            Assert.That(http.Template, Is.EqualTo(route));
            Assert.That(method.GetCustomAttribute<AuthorizeAttribute>()?.Roles, Is.EqualTo(roles));
        });
    }

    [Test]
    public void RemainingBusinessScaffolds_KeepExistingBusinessRole()
    {
        var business = typeof(BusinessReadScaffoldController);
        Assert.That(business.GetCustomAttribute<AuthorizeAttribute>()?.Roles,
            Is.EqualTo("BUSINESS_OPERATIONS_MANAGER"));
        Assert.That(business.GetMethod("ListCustomers"), Is.Null);
        Assert.That(business.GetMethod("GetCustomer"), Is.Null);
        Assert.That(business.GetMethod("ListEmployees"), Is.Not.Null);
        Assert.That(business.GetMethod("CreateCustomerDiscount"), Is.Not.Null);
    }
}
