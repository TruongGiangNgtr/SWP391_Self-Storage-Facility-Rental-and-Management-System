using Frms.Business.Abstractions.External;
using Frms.Business.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Frms.ApiTests;

[TestFixture]
public sealed class ExternalProviderRegistrationTests
{
    [Test]
    public async Task ExternalProviderBoundaries_ResolveAndFailExplicitlyWhenNotConfigured()
    {
        await using var factory = new FrmsWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var providers = new Func<Task>[]
        {
            () => scope.ServiceProvider.GetRequiredService<IPaymentGateway>().EnsureConfiguredAsync(default),
            () => scope.ServiceProvider.GetRequiredService<IAiRecommendationProvider>().EnsureConfiguredAsync(default),
            () => scope.ServiceProvider.GetRequiredService<IEmailService>().SendInitialCredentialAsync("test@example.invalid", "not-a-secret", default),
            () => scope.ServiceProvider.GetRequiredService<INotificationSender>().EnsureConfiguredAsync(default),
        };

        foreach (var providerCall in providers)
        {
            var exception = Assert.ThrowsAsync<BusinessException>(async () => await providerCall());
            Assert.Multiple(() =>
            {
                Assert.That(exception!.Code, Is.EqualTo("EXTERNAL_PROVIDER_NOT_CONFIGURED"));
                Assert.That(exception.SuggestedStatusCode, Is.EqualTo(503));
            });
        }
    }

    [Test]
    public async Task UnconfiguredPaymentGateway_CallbackFailsWithStableErrorCode()
    {
        await using var factory = new FrmsWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var paymentGateway = scope.ServiceProvider.GetRequiredService<IPaymentGateway>();

        var exception = Assert.ThrowsAsync<BusinessException>(async () =>
            await paymentGateway.VerifyAndNormalizeCallbackAsync(
                new PaymentGatewayCallbackRequest("{}"),
                default));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Code, Is.EqualTo("EXTERNAL_PROVIDER_NOT_CONFIGURED"));
            Assert.That(exception.SuggestedStatusCode, Is.EqualTo(503));
        });
    }
}
