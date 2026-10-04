using Frms.Api.Authorization;

namespace Frms.ApiTests;

public sealed class RoleNamesTests
{
    [Test]
    public void AllContainsExactlyFiveLockedRoles()
    {
        string[] expected =
        [
            "CUSTOMER",
            "FACILITY_STAFF",
            "FACILITY_MANAGER",
            "BUSINESS_OPERATIONS_MANAGER",
            "SYSTEM_ADMINISTRATOR",
        ];

        Assert.That(RoleNames.All, Is.EquivalentTo(expected));
        Assert.That(RoleNames.All, Has.Count.EqualTo(5));
    }
}
