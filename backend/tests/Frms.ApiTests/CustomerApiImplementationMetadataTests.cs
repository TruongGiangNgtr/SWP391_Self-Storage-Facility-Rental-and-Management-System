using System.Reflection;
using Frms.Api.Controllers;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Frms.ApiTests;

[TestFixture]
public sealed class CustomerApiImplementationMetadataTests
{
    [TestCase("UpdateOwnProfile")]
    [TestCase("ListFacilityCustomers")]
    [TestCase("GetFacilityCustomer")]
    [TestCase("ListCustomers")]
    [TestCase("GetCustomer")]
    public void PreviouslyScaffoldedActions_HaveConcreteResponses(string name)
    {
        var action = typeof(CustomersController).GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.That(action, Is.Not.Null);
        Assert.That(action!.ReturnType, Is.Not.EqualTo(typeof(ActionResult<ApiErrorResponse>)));
    }

    [Test]
    public void DirectoryProjection_ExposesOnlyCustomerIdAndName()
    {
        var names = typeof(CustomerDirectoryResponse).GetProperties().Select(x => x.Name).ToArray();
        Assert.That(names, Is.EquivalentTo(new[] { "CustomerId", "FullName" }));
    }
}
