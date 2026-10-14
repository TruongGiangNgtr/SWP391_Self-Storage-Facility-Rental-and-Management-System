using Frms.Api.Authorization;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

/// <summary>
/// Customer-specific API surface for own profile, Facility-scoped lookup,
/// and Business Operations Manager lookup. Keep RBAC on actions: the scopes
/// are distinct and must never be combined into one broad role check.
/// </summary>
[Route("api/v1")]
public sealed class CustomersController : ScaffoldControllerBase
{
    // AUTH-005: Implement only after profile field/update rules are wired through Service/Repository.
    [Authorize(Roles = RoleNames.Customer)]
    [HttpPatch("customers/me/profile")]
    public ActionResult<ApiErrorResponse> UpdateOwnProfile([FromBody] System.Text.Json.JsonElement request) =>
        ScaffoldNotImplemented("AUTH-005");

    // UX-RBAC-12: Must resolve current Employee.FacilityId and restrict linked
    // Reservation OR Contract rows BEFORE pagination/counting and DTO mapping.
    [Authorize(Roles = RoleNames.FacilityStaff + "," + RoleNames.FacilityManager)]
    [HttpGet("facilities/{facilityId:guid}/customers")]
    public ActionResult<ApiErrorResponse> ListFacilityCustomers(Guid facilityId) =>
        ScaffoldNotImplemented("UX-RBAC-12-LIST");

    [Authorize(Roles = RoleNames.FacilityStaff + "," + RoleNames.FacilityManager)]
    [HttpGet("facilities/{facilityId:guid}/customers/{customerId:guid}")]
    public ActionResult<ApiErrorResponse> GetFacilityCustomer(Guid facilityId, Guid customerId) =>
        ScaffoldNotImplemented("UX-RBAC-12-DETAIL");

    // UX-RBAC-14: Business-wide read-only customer directory. Never expose
    // credentials/secrets; final field-level PII projection awaits SRS approval.
    [Authorize(Roles = RoleNames.BusinessOperationsManager)]
    [HttpGet("business/customers")]
    public ActionResult<ApiErrorResponse> ListCustomers() =>
        ScaffoldNotImplemented("UX-RBAC-14-LIST");

    [Authorize(Roles = RoleNames.BusinessOperationsManager)]
    [HttpGet("business/customers/{customerId:guid}")]
    public ActionResult<ApiErrorResponse> GetCustomer(Guid customerId) =>
        ScaffoldNotImplemented("UX-RBAC-14-DETAIL");
}
