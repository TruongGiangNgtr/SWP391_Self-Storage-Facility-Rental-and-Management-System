using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Frms.Api.OpenApi;

internal sealed class PaymentOpenApi : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context) =>
        Configure(operation, context.ApiDescription.RelativePath);

    internal static void Configure(OpenApiOperation operation, string? path)
    {
        if (path != "api/v1/payments/payos/webhook") return;
        operation.Security = [];
        operation.Description = "Provider-facing payOS POST JSON webhook. HMAC-SHA256 verification precedes order-code resolution and amount/reference validation. Applied/duplicate and verified unknown sample orders acknowledge HTTP200; no customer JWT. Browser return/cancel do not mutate state.";
    }
}
