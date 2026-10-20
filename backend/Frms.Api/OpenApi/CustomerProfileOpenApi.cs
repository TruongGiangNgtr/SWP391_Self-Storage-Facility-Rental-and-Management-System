using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Frms.Api.OpenApi;

/// <summary>
/// OpenAPI-only shape for AUTH-005. Runtime intentionally keeps JsonElement so it can
/// distinguish an omitted property from an explicit null, and reject unknown fields.
/// </summary>
public sealed class CustomerProfilePatchRequestSchema
{
    [JsonPropertyName("fullName"), MaxLength(150)]
    public string? FullName { get; init; }

    [JsonPropertyName("address"), MaxLength(1000)]
    public string? Address { get; init; }

    [JsonPropertyName("cccd"), MaxLength(20)]
    public string? Cccd { get; init; }
}

internal sealed class CustomerProfileOpenApi : IOperationFilter
{
    private const string Details =
        "AUTH-005: Customer profile partial update. Supply one or more of fullName, address, cccd. " +
        "fullName, when supplied, must be nonblank (max 150 characters). " +
        "address and cccd accept null to clear; cccd must be unique when present (max 20 characters). " +
        "Omitted properties remain unchanged. Email and phoneNumber cannot be updated here. " +
        "Updating cccd is not identity verification.";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!Matches(context.ApiDescription.RelativePath, context.ApiDescription.HttpMethod) ||
            operation.RequestBody is not OpenApiRequestBody requestBody) return;

        var schema = context.SchemaGenerator.GenerateSchema(
            typeof(CustomerProfilePatchRequestSchema), context.SchemaRepository);
        foreach (var media in requestBody.Content.Values)
            media.Schema = schema;
        operation.Description = Details;
    }

    internal static async Task ConfigureAsync(OpenApiOperation operation,
        OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (!Matches(context.Description.RelativePath, context.Description.HttpMethod) ||
            operation.RequestBody is not OpenApiRequestBody requestBody) return;

        var schema = await context.GetOrCreateSchemaAsync(
            typeof(CustomerProfilePatchRequestSchema), null, cancellationToken);
        foreach (var media in requestBody.Content.Values)
            media.Schema = schema;
        operation.Description = Details;
    }

    private static bool Matches(string? path, string? method) =>
        string.Equals(path?.TrimStart('/'), "api/v1/customers/me/profile", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(method, "PATCH", StringComparison.OrdinalIgnoreCase);
}
