using Frms.Api.Authorization;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Route("api/v1/customers")]
[Authorize(Roles = RoleNames.Customer)]
public sealed class CustomerProfileScaffoldController : ScaffoldControllerBase
{
    // AUTH-005: Implement only after profile field/update rules are wired through Service/Repository.
    [HttpPatch("me/profile")]
    public ActionResult<ApiErrorResponse> UpdateOwnProfile([FromBody] System.Text.Json.JsonElement request) =>
        ScaffoldNotImplemented("AUTH-005");
}
