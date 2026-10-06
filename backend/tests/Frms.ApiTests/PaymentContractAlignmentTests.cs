using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Frms.Business.Abstractions.External;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;

namespace Frms.ApiTests;

public sealed class PaymentContractAlignmentTests
{
    [Test]
    public void PaymentService_DoesNotExposeRetiredReservationPaymentOperation()
    {
        var retiredMethodName = "StartFirst" + "MonthPaymentAsync";
        var method = typeof(IPaymentService).GetMethod(retiredMethodName);

        Assert.That(method, Is.Null);
    }

    [Test]
    public void RetiredReservationPaymentResultType_IsAbsent()
    {
        var retiredTypeName = "Frms.Business.Models.Results.First" + "MonthPaymentStartResult";
        var resultType = typeof(PaymentResult).Assembly.GetType(
            retiredTypeName);

        Assert.That(resultType, Is.Null);
    }

    [Test]
    public void CurrentPaymentContracts_RequireInvoiceId()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(PaymentResult).GetProperty(nameof(PaymentResult.InvoiceId))?.PropertyType,
                Is.EqualTo(typeof(Guid)));
            Assert.That(typeof(PaymentDetailResponse).GetProperty(nameof(PaymentDetailResponse.InvoiceId))?.PropertyType,
                Is.EqualTo(typeof(Guid)));
        });
    }

    [Test]
    public void CompleteHandoverRequest_ContainsOnlyCurrentFields()
    {
        var retiredPropertyName = "FirstMonth" + "PaymentId";
        var property = typeof(CompleteHandoverRequest).GetProperty(retiredPropertyName);

        Assert.That(property, Is.Null);
    }

    [Test]
    public void PaymentGateway_ExposesProviderNeutralCallbackBoundary()
    {
        var method = typeof(IPaymentGateway).GetMethod(nameof(IPaymentGateway.VerifyAndNormalizeCallbackAsync));
        var requestProperties = typeof(PaymentGatewayCallbackRequest).GetProperties().Select(property => property.Name);
        var resultProperties = typeof(PaymentGatewayCallbackResult).GetProperties().Select(property => property.Name);

        Assert.That(method, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task<PaymentGatewayCallbackResult>)));
            Assert.That(method.GetParameters().Select(parameter => parameter.ParameterType),
                Is.EqualTo(new[] { typeof(PaymentGatewayCallbackRequest), typeof(CancellationToken) }));
            Assert.That(requestProperties, Is.EqualTo(new[] { "RawBody" }));
            Assert.That(resultProperties,
                Is.EquivalentTo(new[] { "PaymentId", "Amount", "TransactionCode", "Status" }));
        });
    }
}
