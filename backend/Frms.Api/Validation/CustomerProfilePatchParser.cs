using System.Text.Json;
using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;

namespace Frms.Api.Validation;

internal static class CustomerProfilePatchParser
{
    // Explicit allowlist: never accept PhoneNumber, Email or account fields in AUTH-005.
    public static UpdateCustomerProfileCommand Parse(JsonElement json)
    {
        if (json.ValueKind != JsonValueKind.Object)
            throw Invalid();
        foreach (var property in json.EnumerateObject())
        {
            if (property.Name is not ("fullName" or "address" or "cccd"))
                throw Invalid();
        }
        var name = Read(json, "fullName", out var updateName);
        var address = Read(json, "address", out var updateAddress);
        var cccd = Read(json, "cccd", out var updateCccd);
        return new UpdateCustomerProfileCommand(updateName, name, updateAddress, address, updateCccd, cccd);
    }

    private static string? Read(JsonElement json, string key, out bool supplied)
    {
        supplied = json.TryGetProperty(key, out var value);
        if (!supplied || value.ValueKind == JsonValueKind.Null) return null;
        if (value.ValueKind != JsonValueKind.String) throw Invalid();
        return value.GetString();
    }

    private static BusinessException Invalid() =>
        new("VALIDATION_ERROR", "Only fullName, address and cccd string fields are allowed.", 400);
}
