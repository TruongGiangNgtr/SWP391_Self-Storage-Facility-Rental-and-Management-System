using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Frms.Api.Controllers;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
            .Where(item => typeof(IActionResult).IsAssignableFrom(item.Method.ReturnType)
                || item.Method.ReturnType.IsGenericType)
            .ToArray();

        Assert.That(actionMethods, Is.Not.Empty);

        foreach (var (controllerType, method) in actionMethods)
        {
            var controller = (ControllerBase)Activator.CreateInstance(controllerType)!;
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
            });
        }
    }
}
