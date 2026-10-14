using Frms.Api.Authorization;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

// All endpoints in this controller are read-only. BOM does not inherit Admin writes.
[Authorize(Roles = RoleNames.BusinessOperationsManager)]
[Route("api/v1/business")]
public sealed class BusinessReadController(IEmployeeDirectoryService employees,
    IOperationalCatalogService catalogue,
    IBusinessDiscountService discounts) : ScaffoldControllerBase
{
    // UX-RBAC-16: exclude credentials/secrets; field-level PII awaits SRS.
    [HttpGet("employees")]
    public async Task<ActionResult<PaginatedResponse<EmployeeDirectoryResponse>>> ListEmployees(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await employees.ListBusinessAsync(page, pageSize, cancellationToken);
        return Ok(new PaginatedResponse<EmployeeDirectoryResponse>(
            items.Select(ToEmployee).ToArray(),
            new PaginationResponse(page, pageSize, total,
                total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize))));
    }

    [HttpGet("employees/{employeeId:guid}")]
    public async Task<ActionResult<ApiResponse<EmployeeDirectoryResponse>>> GetEmployee(
        Guid employeeId, CancellationToken cancellationToken)
    {
        var row = await employees.GetBusinessAsync(employeeId, cancellationToken);
        return Ok(new ApiResponse<EmployeeDirectoryResponse>(ToEmployee(row)));
    }

    // BOM-011/012: business mutations NOT implemented; do not report a successful write.
    [HttpPost("customers/{customerId:guid}/discounts")]
    public async Task<ActionResult<ApiResponse<DiscountResponse>>> CreateCustomerDiscount(
        Guid customerId, [FromBody] CreateBusinessDiscountRequest request,
        CancellationToken cancellationToken)
    {
        var row = await discounts.CreateAsync(customerId, request.Name, request.Percentage,
            request.EffectiveFrom, request.EffectiveTo, cancellationToken);
        return StatusCode(StatusCodes.Status201Created,
            new ApiResponse<DiscountResponse>(MapDiscount(row)));
    }

    [HttpPatch("discounts/{discountId:guid}")]
    public async Task<ActionResult<ApiResponse<DiscountResponse>>> UpdateDiscount(
        Guid discountId, [FromBody] UpdateBusinessDiscountRequest request,
        CancellationToken cancellationToken)
    {
        var row = await discounts.UpdateAsync(discountId, request.Name, request.Percentage,
            request.Status, request.EffectiveFrom, request.EffectiveTo, cancellationToken);
        return Ok(new ApiResponse<DiscountResponse>(MapDiscount(row)));
    }

    [HttpPatch("extra-fee-types/{extraFeeTypeId:guid}")]
    public async Task<ActionResult<ApiResponse<ExtraFeeTypeCatalogResponse>>> UpdateExtraFeeType(
        Guid extraFeeTypeId, [FromBody] UpdateExtraFeeTypeRequest request,
        CancellationToken cancellationToken)
    {
        var row = await catalogue.UpdateExtraFeeTypeAsync(extraFeeTypeId,
            request.DefaultAmount, request.Status, cancellationToken);
        return Ok(new ApiResponse<ExtraFeeTypeCatalogResponse>(new(
            row.ExtraFeeTypeId, row.Name, row.DefaultAmount, row.Status)));
    }

    private static DiscountResponse MapDiscount(
        Frms.DataAccess.Repositories.Models.BusinessDiscountRecord row) =>
        new(row.DiscountId, row.CustomerId, row.Name, row.Percentage,
            row.Status, DateTime.SpecifyKind(row.EffectiveFrom, DateTimeKind.Utc),
            row.EffectiveTo.HasValue
                ? DateTime.SpecifyKind(row.EffectiveTo.Value, DateTimeKind.Utc) : null);

    private static EmployeeDirectoryResponse ToEmployee(
        Frms.DataAccess.Repositories.Models.EmployeeDirectoryRecord row) =>
        new(row.EmployeeId, row.FacilityId, row.FullName, row.Role, row.Status);
}
