using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Frms.Api.Controllers;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Frms.ApiTests;

public sealed class ScaffoldEndpointTests
{
    [Test]
    public async Task RegisterCustomerReturnsNotImplemented()
    {
        await using var factory = new FrmsWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/customer/register",
            new
            {
                fullName = "Scaffold User",
                phoneNumber = "0900000000",
                email = "scaffold@example.invalid",
                password = "not-a-real-password",
                address = "Ho Chi Minh City",
                cccd = (string?)null,
            });
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotImplemented));
            Assert.That(error, Is.Not.Null);
            Assert.That(error!.Code, Is.EqualTo("ENDPOINT_NOT_IMPLEMENTED"));
            Assert.That(error.Message, Does.Contain("AUTH-001"));
            Assert.That(error.TraceId, Is.Not.Empty);
        });
    }

    [Test]
    public void EveryScaffoldActionReturns501WhenInvokedDirectly()
    {
        var actionMethods = typeof(ScaffoldControllerBase).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(ScaffoldControllerBase).IsAssignableFrom(type))
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Select(method => (ControllerType: type, Method: method)))
            // Implemented actions may still share ScaffoldControllerBase. The error-only
            // ActionResult contract identifies the remaining synchronous scaffold actions.
            .Where(item => item.Method.ReturnType == typeof(ActionResult<ApiErrorResponse>))
            .ToArray();

        Assert.That(actionMethods, Is.Not.Empty);

        using var factory = new FrmsWebApplicationFactory();
        using var scope = factory.Services.CreateScope();

        foreach (var (controllerType, method) in actionMethods)
        {
            var controller = (ControllerBase)ActivatorUtilities.CreateInstance(scope.ServiceProvider, controllerType);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            };

            var arguments = method.GetParameters()
                .Select(parameter => parameter.ParameterType.IsValueType
                    ? Activator.CreateInstance(parameter.ParameterType)
                    : null)
                .ToArray();

            var result = method.Invoke(controller, arguments);
            var actionResult = result as ActionResult<ApiErrorResponse>;
            var objectResult = actionResult?.Result as ObjectResult;

            Assert.Multiple(() =>
            {
                Assert.That(actionResult, Is.Not.Null, $"{controllerType.Name}.{method.Name}");
                Assert.That(objectResult?.StatusCode, Is.EqualTo(StatusCodes.Status501NotImplemented),
                    $"{controllerType.Name}.{method.Name}");
                Assert.That(objectResult?.Value, Is.TypeOf<ApiErrorResponse>(),
                    $"{controllerType.Name}.{method.Name}");
                var error = objectResult?.Value as ApiErrorResponse;
                Assert.That(error?.Code, Is.EqualTo("ENDPOINT_NOT_IMPLEMENTED"));
                Assert.That(error?.TraceId, Is.Not.Empty);
            });
        }
    }
}
