using Frms.Api.Authorization;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

// All endpoints in this controller are read-only. BOM does not inherit Admin writes.
[Authorize(Roles = RoleNames.BusinessOperationsManager)]
[Route("api/v1/business")]
public sealed class BusinessReadScaffoldController : ScaffoldControllerBase
{
    // UX-RBAC-14 and UX-RBAC-16: exclude credentials/secrets; field-level PII awaits SRS.
    [HttpGet("customers")]
    public ActionResult<ApiErrorResponse> ListCustomers() =>
        ScaffoldNotImplemented("UX-RBAC-14-LIST");

    [HttpGet("customers/{customerId:guid}")]
    public ActionResult<ApiErrorResponse> GetCustomer(Guid customerId) =>
        ScaffoldNotImplemented("UX-RBAC-14-DETAIL");

    [HttpGet("employees")]
    public ActionResult<ApiErrorResponse> ListEmployees() =>
        ScaffoldNotImplemented("UX-RBAC-16-LIST");

    [HttpGet("employees/{employeeId:guid}")]
    public ActionResult<ApiErrorResponse> GetEmployee(Guid employeeId) =>
        ScaffoldNotImplemented("UX-RBAC-16-DETAIL");

    // BOM-011/012: business mutations NOT implemented; do not report a successful write.
    [HttpPost("customers/{customerId:guid}/discounts")]
    public ActionResult<ApiErrorResponse> CreateCustomerDiscount(
        Guid customerId, [FromBody] System.Text.Json.JsonElement request) =>
        ScaffoldNotImplemented("BOM-011");

    [HttpPatch("discounts/{discountId:guid}")]
    public ActionResult<ApiErrorResponse> UpdateDiscount(
        Guid discountId, [FromBody] System.Text.Json.JsonElement request) =>
        ScaffoldNotImplemented("BOM-012");

    [HttpPatch("extra-fee-types/{extraFeeTypeId:guid}")]
    public ActionResult<ApiErrorResponse> UpdateExtraFeeType(
        Guid extraFeeTypeId, [FromBody] System.Text.Json.JsonElement request) =>
        ScaffoldNotImplemented("BOM-014");
}
