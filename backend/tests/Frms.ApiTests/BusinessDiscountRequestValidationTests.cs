using Frms.Api.DTOs.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Frms.ApiTests;

[TestFixture]
public sealed class BusinessDiscountRequestValidationTests
{
    private static readonly DateTimeOffset EffectiveFrom =
        new(2026, 10, 29, 0, 0, 0, TimeSpan.Zero);

    [Test]
    public void Create_ValidPayload_MvcValidationDoesNotThrow()
    {
        using var services = CreateServices();
        var request = new CreateBusinessDiscountRequest("Loyalty", 15m, EffectiveFrom, null);
        Assert.That(Validate(services, request).IsValid, Is.True);
    }

    [Test]
    public void Create_InvalidPayload_MvcValidationReportsNameAndPercentage()
    {
        using var services = CreateServices();
        var request = new CreateBusinessDiscountRequest(new string('x', 151), 101m, EffectiveFrom, null);
        var modelState = Validate(services, request);
        Assert.Multiple(() =>
        {
            Assert.That(modelState.IsValid, Is.False);
            Assert.That(modelState.Keys, Does.Contain("Name"));
            Assert.That(modelState.Keys, Does.Contain("Percentage"));
        });
    }

    [Test]
    public void Update_ValidPayload_MvcValidationDoesNotThrow()
    {
        using var services = CreateServices();
        var request = new UpdateBusinessDiscountRequest("Loyalty", 15m, "ACTIVE", EffectiveFrom, null);
        Assert.That(Validate(services, request).IsValid, Is.True);
    }

    [Test]
    public void Update_InvalidPayload_MvcValidationReportsNamePercentageAndStatus()
    {
        using var services = CreateServices();
        var request = new UpdateBusinessDiscountRequest(null!, -1m, null!, EffectiveFrom, null);
        var modelState = Validate(services, request);
        Assert.Multiple(() =>
        {
            Assert.That(modelState.IsValid, Is.False);
            Assert.That(modelState.Keys, Does.Contain("Name"));
            Assert.That(modelState.Keys, Does.Contain("Percentage"));
            Assert.That(modelState.Keys, Does.Contain("Status"));
        });
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        return services.BuildServiceProvider();
    }

    private static ModelStateDictionary Validate(IServiceProvider services, object request)
    {
        var modelState = new ModelStateDictionary();
        var context = new ActionContext(
            new DefaultHttpContext { RequestServices = services },
            new RouteData(), new ActionDescriptor(), modelState);
        services.GetRequiredService<IObjectModelValidator>()
            .Validate(context, validationState: null, prefix: string.Empty, model: request);
        return modelState;
    }
}
