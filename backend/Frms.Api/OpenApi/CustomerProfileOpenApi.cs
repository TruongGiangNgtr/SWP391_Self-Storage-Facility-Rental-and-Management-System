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

    [JsonPropertyName("cccd")]
    [RegularExpression(@"^\s*(?:[0-9]{12})?\s*$",
        ErrorMessage = "CCCD must be exactly 12 ASCII digits when supplied.")]
    public string? Cccd { get; init; }
}

internal sealed class CustomerProfileOpenApi : IOperationFilter
{
    private const string Details =
        "AUTH-005: Customer profile partial update. Supply one or more of fullName, address, cccd. " +
        "fullName, when supplied, must be nonblank (max 150 characters). " +
        "address and cccd accept null to clear; cccd must be unique when present (max 20 characters). " +
        "Omitted properties remain unchanged. Email and phoneNumber cannot be updated here. " +
        "A supplied nonblank cccd must be exactly 12 ASCII digits after trimming. " +
        "Updating cccd is not identity verification.";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!Matches(context.ApiDescription.RelativePath, context.ApiDescription.HttpMethod)) return;
        operation.Description = Details;
        if (operation.RequestBody is not OpenApiRequestBody requestBody ||
            requestBody.Content is null || !requestBody.Content.Keys.Any(IsJsonMediaType)) return;

        var schema = context.SchemaGenerator.GenerateSchema(
            typeof(CustomerProfilePatchRequestSchema), context.SchemaRepository);
        foreach (var media in requestBody.Content)
            if (IsJsonMediaType(media.Key)) media.Value.Schema = schema;
    }

    internal static async Task ConfigureAsync(OpenApiOperation operation,
        OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (!Matches(context.Description.RelativePath, context.Description.HttpMethod)) return;
        operation.Description = Details;
        if (operation.RequestBody is not OpenApiRequestBody requestBody ||
            requestBody.Content is null || !requestBody.Content.Keys.Any(IsJsonMediaType)) return;

        var schema = await context.GetOrCreateSchemaAsync(
            typeof(CustomerProfilePatchRequestSchema), null, cancellationToken);
        foreach (var media in requestBody.Content)
            if (IsJsonMediaType(media.Key)) media.Value.Schema = schema;
    }

    private static bool IsJsonMediaType(string mediaType)
    {
        var type = mediaType.Split(';', 2)[0].Trim();
        return type.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
               type.Equals("text/json", StringComparison.OrdinalIgnoreCase) ||
               type.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
    }

    private static bool Matches(string? path, string? method) =>
        string.Equals(path?.TrimStart('/'), "api/v1/customers/me/profile", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(method, "PATCH", StringComparison.OrdinalIgnoreCase);
}
