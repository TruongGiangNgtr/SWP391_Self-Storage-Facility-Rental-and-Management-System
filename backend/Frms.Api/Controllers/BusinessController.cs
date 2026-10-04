using Frms.Api.Authorization;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Authorize(Roles = RoleNames.BusinessOperationsManager)]
[Route("api/v1/business")]
public sealed class BusinessController : ScaffoldControllerBase
{
    /// <summary>BOM-001: List all Facilities for BOM scaffold.</summary>
    [HttpGet("facilities")]
    public ActionResult<ApiErrorResponse> ListFacilities(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) => ScaffoldNotImplemented("BOM-001");

    /// <summary>BOM-002: Create Facility scaffold.</summary>
    [HttpPost("facilities")]
    [ProducesResponseType(typeof(ApiResponse<FacilitySummaryResponse>), StatusCodes.Status201Created)]
    public ActionResult<ApiErrorResponse> CreateFacility(
        [FromBody] CreateFacilityRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("BOM-002");

    /// <summary>BOM-003: Update Facility basic information scaffold; exact request schema is pending.</summary>
    [HttpPatch("facilities/{facilityId:guid}")]
    public ActionResult<ApiErrorResponse> UpdateFacility(
        Guid facilityId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("BOM-003");

    /// <summary>BOM-004: Activate Facility scaffold.</summary>
    [HttpPost("facilities/{facilityId:guid}/activate")]
    public ActionResult<ApiErrorResponse> ActivateFacility(
        Guid facilityId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("BOM-004");

    /// <summary>BOM-005: Deactivate Facility scaffold.</summary>
    [HttpPost("facilities/{facilityId:guid}/deactivate")]
    public ActionResult<ApiErrorResponse> DeactivateFacility(
        Guid facilityId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("BOM-005");

    /// <summary>BOM-006: List global UnitTypes scaffold.</summary>
    [HttpGet("unit-types")]
    public ActionResult<ApiErrorResponse> ListUnitTypes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) => ScaffoldNotImplemented("BOM-006");

    /// <summary>BOM-007: Update current UnitType RentalPrice scaffold.</summary>
    [HttpPatch("unit-types/{unitTypeId:guid}/price")]
    public ActionResult<ApiErrorResponse> UpdateUnitTypePrice(
        Guid unitTypeId,
        [FromBody] UpdateUnitTypePriceRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("BOM-007");

    /// <summary>BOM-008: List Policy versions scaffold.</summary>
    [HttpGet("policies")]
    public ActionResult<ApiErrorResponse> ListPolicies(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) => ScaffoldNotImplemented("BOM-008");

    /// <summary>BOM-009: Create a new Policy version scaffold.</summary>
    [HttpPost("policies")]
    public ActionResult<ApiErrorResponse> CreatePolicyVersion(
        [FromBody] CreatePolicyVersionRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("BOM-009");

    /// <summary>BOM-010: List Customer Discounts scaffold.</summary>
    [HttpGet("customers/{customerId:guid}/discounts")]
    public ActionResult<ApiErrorResponse> ListCustomerDiscounts(
        Guid customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) => ScaffoldNotImplemented("BOM-010");

    /// <summary>BOM-011: Create Customer Discount scaffold.</summary>
    [HttpPost("customers/{customerId:guid}/discounts")]
    public ActionResult<ApiErrorResponse> CreateCustomerDiscount(
        Guid customerId,
        [FromBody] CreateCustomerDiscountRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("BOM-011");

    /// <summary>BOM-012: Update allowed Discount master fields scaffold.</summary>
    [HttpPatch("discounts/{discountId:guid}")]
    public ActionResult<ApiErrorResponse> UpdateDiscount(
        Guid discountId,
        [FromBody] UpdateDiscountRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("BOM-012");

    /// <summary>BOM-013: List ExtraFeeTypes scaffold.</summary>
    [HttpGet("extra-fee-types")]
    public ActionResult<ApiErrorResponse> ListExtraFeeTypes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) => ScaffoldNotImplemented("BOM-013");

    /// <summary>BOM-014: Update ExtraFeeType default amount/status scaffold.</summary>
    [HttpPatch("extra-fee-types/{extraFeeTypeId:guid}")]
    public ActionResult<ApiErrorResponse> UpdateExtraFeeType(
        Guid extraFeeTypeId,
        [FromBody] UpdateExtraFeeTypeRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("BOM-014");
}
