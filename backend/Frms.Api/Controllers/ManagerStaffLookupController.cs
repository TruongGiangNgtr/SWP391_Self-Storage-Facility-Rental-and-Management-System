using Frms.Api.Authorization;
using Frms.Api.DTOs.Responses;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Authorize(Roles = RoleNames.FacilityManager)]
[Route("api/v1/facilities/{facilityId:guid}/staff/lookup")]
public sealed class ManagerStaffLookupController(IEmployeeDirectoryService employees) : ScaffoldControllerBase
{
    // UX-RBAC-09: only Staff in current Manager's assigned Facility.
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<EmployeeDirectoryResponse>>> StaffLookup(
        Guid facilityId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await employees.ListStaffAsync(facilityId, page, pageSize, cancellationToken);
        return Ok(new PaginatedResponse<EmployeeDirectoryResponse>(
            items.Select(x => new EmployeeDirectoryResponse(x.EmployeeId, x.FacilityId,
                x.FullName, x.Role, x.Status)).ToArray(),
            new PaginationResponse(page, pageSize, total,
                total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize))));
    }
}
