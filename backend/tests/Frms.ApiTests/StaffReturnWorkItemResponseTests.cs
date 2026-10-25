using System.Text.Json;
using Frms.Api.DTOs.Responses;
using NUnit.Framework;

namespace Frms.ApiTests;

[TestFixture]
public sealed class StaffReturnWorkItemResponseTests
{
    private static readonly Guid VisitId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Test]
    public void ConfirmedReturnIncludesPhysicalReturnDate()
    {
        var response = new StaffWorkItemResponse(
            "RETURN_VISIT", VisitId, Guid.NewGuid(), new DateOnly(2026, 10, 22),
            "CHECKED_IN", null, new DateOnly(2026, 10, 22), Guid.NewGuid());
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.That(json, Does.Contain("\"actualReturnDate\":\"2026-10-22\""));
        Assert.That(json, Does.Contain("\"inspectionId\""));
    }

    [Test]
    public void UnconfirmedVisitOmitsPhysicalReturnDate()
    {
        var response = new StaffWorkItemResponse(
            "RETURN_VISIT", VisitId, Guid.NewGuid(), new DateOnly(2026, 10, 22),
            "CHECKED_IN", null, null, null);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.That(json, Does.Not.Contain("actualReturnDate"));
    }

    [Test]
    public void OtherWorkTypesDoNotExposeReturnDate()
    {
        var response = new StaffWorkItemResponse(
            "SUPPORT_TICKET", Guid.NewGuid(), null, null, "OPEN", null, null, null);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.That(json, Does.Not.Contain("actualReturnDate"));
    }
}
