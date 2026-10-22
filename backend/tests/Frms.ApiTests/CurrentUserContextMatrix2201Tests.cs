using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Frms.Api.Authentication;
using Frms.Api.Authorization;
using Frms.Business.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Frms.ApiTests;

[TestFixture]
public sealed class CurrentUserContextMatrix2201Tests
{
    public static IEnumerable<TestCaseData> Cases()
    {
        var id = 2201;
        for (var i = 0; i < 50; i++)
        {
            var userId = Guid.Parse($"{i + 1:D8}-1111-2222-3333-{i + 1:D12}");
            yield return new TestCaseData(userId.ToString(), RoleNames.All[i % RoleNames.All.Count], true, true, true)
                .SetName($"TC_{id++}_authenticated_claims_{i + 1:00}");
        }
        var badSubjects = new string?[] { null, "", " ", "not-a-guid", "123", "{00000000-0000-0000-0000-000000000000}", "00000000-0000-0000-0000-00000000000Z", "00000000-0000-0000-0000-000000000000x", "null", "undefined", "00000000-0000-0000-0000-00000000000", "000000000000000000000000000000000000", "00000000/0000/0000/0000/000000000000", "00000000-0000-0000-0000-000000000000 ", " 00000000-0000-0000-0000-000000000000", "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee-extra", "-1", "1.0", "CUSTOMER", "SYSTEM_ADMINISTRATOR", "\t", "\n", "00000000-0000-0000-0000-00000000000g", "00000000-0000-0000-0000-00000000000!", "00000000-0000-0000-0000-000000000000\n" };
        foreach (var subject in badSubjects)
            yield return new TestCaseData(subject, RoleNames.Customer, true, false, true).SetName($"TC_{id++}_invalid_subject_{id - 2226:00}");
        for (var i = 0; i < 15; i++)
        {
            var userId = Guid.Parse($"{i + 101:D8}-aaaa-bbbb-cccc-{i + 101:D12}");
            yield return new TestCaseData(userId.ToString(), null, true, true, false).SetName($"TC_{id++}_missing_role_{i + 1:00}");
        }
        for (var i = 0; i < 10; i++)
            yield return new TestCaseData(null, null, false, false, false).SetName($"TC_{id++}_anonymous_context_{i + 1:00}");
        if (id != 2301) throw new InvalidOperationException($"Expected 100 cases; generated {id - 2201}.");
    }

    [TestCaseSource(nameof(Cases))]
    public void Context_ResolvesOnlyAvailableIdentityClaims(string? subject, string? role, bool authenticated, bool subjectValid, bool roleValid)
    {
        var claims = new List<Claim>();
        if (subject is not null) claims.Add(new Claim(JwtRegisteredClaimNames.Sub, subject));
        if (role is not null) claims.Add(new Claim(ClaimTypes.Role, role));
        var identity = new ClaimsIdentity(claims, authenticated ? "matrix-auth" : null);
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var context = new CurrentUserContext(new HttpContextAccessor { HttpContext = http });

        Assert.That(context.IsAuthenticated, Is.EqualTo(authenticated));
        if (subjectValid)
        {
            Assert.That(context.UserAccountId, Is.EqualTo(Guid.Parse(subject!)));
        }
        else
        {
            var error = Assert.Throws<BusinessException>(() => _ = context.UserAccountId);
            Assert.That(error!.Code, Is.EqualTo("UNAUTHORIZED"));
            Assert.That(error.SuggestedStatusCode, Is.EqualTo(401));
        }
        if (roleValid)
        {
            Assert.That(context.Role, Is.EqualTo(role));
        }
        else
        {
            var error = Assert.Throws<BusinessException>(() => _ = context.Role);
            Assert.That(error!.Code, Is.EqualTo("UNAUTHORIZED"));
        }
    }
}
