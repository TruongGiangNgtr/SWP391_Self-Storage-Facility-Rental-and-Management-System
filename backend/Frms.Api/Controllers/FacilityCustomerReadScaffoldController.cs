using Frms.Api.Authorization;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Route("api/v1/facilities/{facilityId:guid}/customers")]
[Authorize(Roles = RoleNames.FacilityStaff + "," + RoleNames.FacilityManager)]
public sealed class FacilityCustomerReadScaffoldController : ScaffoldControllerBase
{
    // UX-RBAC-12: Must resolve the current Employee.FacilityId and restrict linked
    // Reservation OR Contract rows BEFORE pagination/counting and DTO mapping.
    [HttpGet]
    public ActionResult<ApiErrorResponse> ListFacilityCustomers(Guid facilityId) =>
        ScaffoldNotImplemented("UX-RBAC-12-LIST");

    [HttpGet("{customerId:guid}")]
    public ActionResult<ApiErrorResponse> GetFacilityCustomer(Guid facilityId, Guid customerId) =>
        ScaffoldNotImplemented("UX-RBAC-12-DETAIL");
}
