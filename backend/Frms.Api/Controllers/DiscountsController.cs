using Frms.Api.Authorization;
using Frms.Api.DTOs.Responses;
using Frms.Business.Models;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Route("api/v1/business/customers")]
public sealed class CustomerDiscountsController(
    IDiscountService discountService)
    : ScaffoldControllerBase {
    /// <summary>BOM-010: Authorized Customer Discount catalogue.</summary>
    [Authorize(
        Roles =
            RoleNames.Customer + "," +
            RoleNames.FacilityStaff + "," +
            RoleNames.FacilityManager + "," +
            RoleNames.BusinessOperationsManager)]
    [HttpGet("{customerId:guid}/discounts")]
    [ProducesResponseType(
        typeof(PaginatedResponse<DiscountResponse>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<PaginatedResponse<DiscountResponse>>>
        ListCustomerDiscounts(
            Guid customerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default) {
        var (items, totalCount) =
            await discountService.ListAccessibleAsync(
                customerId,
                page,
                pageSize,
                cancellationToken);

        var data = items
            .Select(ToResponse)
            .ToArray();

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(
                totalCount / (double)pageSize);

        return Ok(
            new PaginatedResponse<DiscountResponse>(
                data,
                new PaginationResponse(
                    page,
                    pageSize,
                    totalCount,
                    totalPages)));
    }

    private static DiscountResponse ToResponse(
        DiscountResult result) {
        return new DiscountResponse(
            result.DiscountId,
            result.CustomerId,
            result.Name,
            result.Percentage,
            result.Status,
            AsUtc(result.EffectiveFrom),
            result.EffectiveTo is null
                ? null
                : AsUtc(result.EffectiveTo.Value));
    }

    private static DateTime AsUtc(DateTime value) {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc);
    }
}
