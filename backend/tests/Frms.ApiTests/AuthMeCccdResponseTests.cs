using System.Text.Json;
using Frms.Api.Mapping;
using Frms.Business.Models.Results;

namespace Frms.ApiTests;

[TestFixture]
public sealed class AuthMeCccdResponseTests
{
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CustomerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [TestCase("012345678901")]
    [TestCase(null)]
    public void CustomerProfile_AlwaysSerializesCccd_EvenWhenNull(string? cccd)
    {
        using var doc = JsonDocument.Parse(Serialize(new CurrentAccountResult(
            UserId, "CUSTOMER", "ACTIVE", "customer@example.test", "0900000000",
            CustomerId, null, null, "Customer Name", cccd)));
        var profile = doc.RootElement.GetProperty("data").GetProperty("profile");
        Assert.Multiple(() =>
        {
            Assert.That(profile.GetProperty("customerId").GetGuid(), Is.EqualTo(CustomerId));
            Assert.That(profile.TryGetProperty("cccd", out var value), Is.True);
            if (cccd is null) Assert.That(value.ValueKind, Is.EqualTo(JsonValueKind.Null));
            else Assert.That(value.GetString(), Is.EqualTo(cccd));
            Assert.That(doc.RootElement.GetProperty("data").TryGetProperty("cccd", out _), Is.False);
        });
    }

    [TestCase("FACILITY_STAFF")]
    [TestCase("FACILITY_MANAGER")]
    [TestCase("BUSINESS_OPERATIONS_MANAGER")]
    [TestCase("SYSTEM_ADMINISTRATOR")]
    public void EmployeeProfiles_DoNotSerializeCccd_EvenIfResultContainsOne(string role)
    {
        using var doc = JsonDocument.Parse(Serialize(new CurrentAccountResult(
            UserId, role, "ACTIVE", "employee@example.test", "0900000001",
            null, Guid.NewGuid(), Guid.NewGuid(), "Employee", "should-not-leak")));
        var profile = doc.RootElement.GetProperty("data").GetProperty("profile");
        Assert.That(profile.TryGetProperty("cccd", out _), Is.False);
        Assert.That(doc.RootElement.GetRawText(), Does.Not.Contain("should-not-leak"));
    }

    private static string Serialize(CurrentAccountResult result) =>
        JsonSerializer.Serialize(result.ToResponse(), new JsonSerializerOptions(JsonSerializerDefaults.Web));
}
