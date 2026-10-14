using Frms.Api.Authorization;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Authorize(Roles = RoleNames.SystemAdministrator)]
[Route("api/v1/admin")]
public sealed class AdminSupplementController : ScaffoldControllerBase
{
    [HttpPost("customers/{customerId:guid}/activate")]
    public ActionResult<ApiErrorResponse> ActivateCustomer(Guid customerId) =>
        ScaffoldNotImplemented("ADM-003");

    [HttpPost("customers/{customerId:guid}/deactivate")]
    public ActionResult<ApiErrorResponse> DeactivateCustomer(Guid customerId) =>
        ScaffoldNotImplemented("ADM-004");

    [HttpGet("login-history")]
    public ActionResult<ApiErrorResponse> ListLoginHistory() =>
        ScaffoldNotImplemented("ADM-010");

    [HttpGet("audit-logs")]
    public ActionResult<ApiErrorResponse> ListAuditLogs() =>
        ScaffoldNotImplemented("ADM-011");

    // PROPOSED UX-RBAC-01/11: no operational/financial data exposure.
    [HttpGet("facilities/lookup")]
    public ActionResult<ApiErrorResponse> FacilityLookup() =>
        ScaffoldNotImplemented("UX-RBAC-01");

    [HttpGet("customers/{customerId:guid}/deactivation-eligibility")]
    public ActionResult<ApiErrorResponse> CustomerDeactivationEligibility(Guid customerId) =>
        ScaffoldNotImplemented("UX-RBAC-11");
}
