using Frms.Api.Authorization;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Authorize(Roles = RoleNames.FacilityManager)]
[Route("api/v1/facilities/{facilityId:guid}/staff/lookup")]
public sealed class ManagerStaffLookupScaffoldController : ScaffoldControllerBase
{
    // UX-RBAC-09: only Staff in current Manager's assigned Facility.
    [HttpGet]
    public ActionResult<ApiErrorResponse> StaffLookup(Guid facilityId) =>
        ScaffoldNotImplemented("UX-RBAC-09");
}
