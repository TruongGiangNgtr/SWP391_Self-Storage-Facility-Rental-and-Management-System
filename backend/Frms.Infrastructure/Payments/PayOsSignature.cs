using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Frms.Infrastructure.Payments;

/// <summary>Official payment-request (not payouts) canonicalization; no secret/payload logging.</summary>
internal static class PayOsSignature
{
    public static string Sign(string text, string key) => Convert.ToHexStringLower(
        HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(text)));

    public static bool Verify(JsonElement data, string? signature, string key)
    {
        if (signature is null || signature.Length != 64 || signature.Any(c => !Uri.IsHexDigit(c))) return false;
        var expected = Convert.FromHexString(Sign(Canonical(data), key));
        return CryptographicOperations.FixedTimeEquals(expected, Convert.FromHexString(signature));
    }

    public static string Canonical(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object) throw new JsonException("Invalid signed data.");
        RejectDuplicates(data);
        return string.Join('&', data.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal)
            .Select(p => $"{p.Name}={Value(p.Value)}"));
    }

    public static void RejectDuplicates(JsonElement data)
    {
        if (data.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in data.EnumerateObject())
            {
                if (!names.Add(field.Name)) throw new JsonException("Duplicate JSON field.");
                RejectDuplicates(field.Value);
            }
        }
        else if (data.ValueKind == JsonValueKind.Array)
            foreach (var item in data.EnumerateArray()) RejectDuplicates(item);
    }

    private static string Value(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => string.Empty,
        JsonValueKind.String => value.GetString() is "undefined" or "null" ? string.Empty : value.GetString()!,
        JsonValueKind.Number => value.GetDecimal().ToString("G29", CultureInfo.InvariantCulture),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Array => ArrayJson(value),
        _ => throw new JsonException("Unsupported signed value.")
    };

    private static string ArrayJson(JsonElement value)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStartArray();
            foreach (var item in value.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) throw new JsonException("Unsupported signed array item.");
                writer.WriteStartObject();
                foreach (var property in item.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteJsonValue(writer, property.Value);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    // JSON stringify normalizes numeric tokens (e.g. 2.0 -> 2), while preserving
    // nested object order. Only the immediate array-element keys are sorted
    // by the official payment-requests canonicalization, not the payouts algorithm.
    private static void WriteJsonValue(Utf8JsonWriter writer, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Number:
                writer.WriteRawValue(value.GetDecimal().ToString("G29", CultureInfo.InvariantCulture));
                break;
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in value.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    WriteJsonValue(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in value.EnumerateArray()) WriteJsonValue(writer, item);
                writer.WriteEndArray();
                break;
            default:
                value.WriteTo(writer);
                break;
        }
    }
}
