using System.Text.Json;
using Frms.Api.Validation;
using Frms.Business.Exceptions;

namespace Frms.ApiTests;

[TestFixture]
public sealed class CustomerProfilePatchParserMatrix2101Tests
{
    public static IEnumerable<TestCaseData> Cases()
    {
        var id = 2101;
        yield return new TestCaseData("{}", true).SetName($"TC_{id++}_empty_patch_object");
        var allowed = new[] { "fullName", "address", "cccd" };
        for (var i = 0; i < 49; i++)
        {
            var payload = new Dictionary<string, object?>();
            var count = i % 4;
            for (var j = 0; j < count; j++)
            {
                var key = allowed[(i + j) % allowed.Length];
                payload[key] = (i + j) % 7 == 0 ? null : $" value-{i}-{j} ";
            }
            yield return new TestCaseData(JsonSerializer.Serialize(payload), true).SetName($"TC_{id++}_allowed_string_patch_{i + 1:00}");
        }

        var invalid = new List<string>
        {
            "null", "[]", "\"text\"", "42", "true", "1.5", "{} trailing", "{", "[{}]", "\"\"",
            "{\"FullName\":\"Khoa\"}", "{\"FULLNAME\":\"Khoa\"}", "{\"phoneNumber\":\"0900\"}",
            "{\"email\":\"a@example.test\"}", "{\"password\":\"secret\"}", "{\"role\":\"CUSTOMER\"}",
            "{\"userAccountId\":\"abc\"}", "{\"customerId\":\"abc\"}", "{\"status\":\"ACTIVE\"}", "{\"createdAt\":\"now\"}",
            "{\"fullName\":123}", "{\"fullName\":true}", "{\"fullName\":[]}", "{\"fullName\":{}}",
            "{\"address\":123}", "{\"address\":false}", "{\"address\":[]}", "{\"address\":{}}",
            "{\"cccd\":123}", "{\"cccd\":true}", "{\"cccd\":[]}", "{\"cccd\":{}}",
            "{\"fullName\":\"Khoa\",\"email\":null}", "{\"address\":\"HCM\",\"phone\":null}",
            "{\"cccd\":\"123\",\"isAdmin\":false}", "{\"address\":\"HCM\",\"status\":\"ACTIVE\"}",
            "{\"fullName\":\"A\",\"FullName\":\"B\"}", "{\"address\":\"HCM\",\"Address\":\"HN\"}",
            "{\"fullName\":\"A\",\"fullName\":\"B\"}", "{\"address\":\"A\",\"address\":\"B\"}",
            "{\"cccd\":\"1\",\"cccd\":\"2\"}", "{\"fullName\":null,\"fullName\":\"Khoa\"}",
            "{\"fullName\":\"Khoa\",\"extra\":\"x\"}", "{\"address\":null,\"unknown\":null}",
            "{\"cccd\":\"123\",\"email\":\"x@y.test\"}", "{\"fullName\":\"A\",\"accountStatus\":\"ACTIVE\"}",
            "{\"address\":\"HCM\",\"facilityId\":\"id\"}", "{\"cccd\":\"123\",\"permissions\":[]}",
            "{\"fullName\":\"Khoa\",\"phoneNumber\":\"0900\"}", "{\"fullName\":\"Khoa\",\"passwordHash\":\"x\"}"
        };
        foreach (var json in invalid)
            yield return new TestCaseData(json, false).SetName($"TC_{id++}_rejected_patch_payload_{id - 2101:00}");
        if (id != 2201) throw new InvalidOperationException($"Expected 100 cases; generated {id - 2101}.");
    }

    [TestCaseSource(nameof(Cases))]
    public void Parse_OnlyAllowsSupportedPatchFields(string json, bool valid)
    {
        if (!valid)
        {
            JsonDocument invalidDocument;
            try
            {
                invalidDocument = JsonDocument.Parse(json);
            }
            catch (JsonException)
            {
                Assert.Pass("Malformed JSON is rejected before the PATCH parser receives a JsonElement.");
                return;
            }
            using (invalidDocument)
            {
                var error = Assert.Throws<BusinessException>(() => CustomerProfilePatchParser.Parse(invalidDocument.RootElement));
                Assert.That(error!.Code, Is.EqualTo("VALIDATION_ERROR"));
            }
            return;
        }

        using var document = JsonDocument.Parse(json);
        var parsed = CustomerProfilePatchParser.Parse(document.RootElement);
        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed.UpdateFullName, Is.EqualTo(document.RootElement.TryGetProperty("fullName", out _)));
        Assert.That(parsed.UpdateAddress, Is.EqualTo(document.RootElement.TryGetProperty("address", out _)));
        Assert.That(parsed.UpdateCccd, Is.EqualTo(document.RootElement.TryGetProperty("cccd", out _)));
    }
}
