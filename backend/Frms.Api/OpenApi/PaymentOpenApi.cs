using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Frms.Api.OpenApi;

internal sealed class PaymentOpenApi : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context) =>
        Configure(operation, context.ApiDescription.RelativePath);

    internal static void Configure(OpenApiOperation operation, string? path)
    {
        if (path != "api/v1/payments/vnpay/ipn") return;
        operation.Security = [];
        operation.Description = "Provider-facing VNPay GET IPN. The adapter validates the HMAC-SHA512 checksum, merchant, transaction reference and amount before financial state changes. Returns VNPay RspCode/Message JSON; no customer JWT.";
        operation.RequestBody = null;
    }
}
