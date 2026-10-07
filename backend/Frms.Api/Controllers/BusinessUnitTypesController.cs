using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Persistence.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[ApiController]
[Authorize(Roles = "BUSINESS_OPERATIONS_MANAGER")]
[Route("api/v1/business/unit-types")]
public sealed class BusinessUnitTypesController(
    IUnitTypeService unitTypeService) : ControllerBase {
    /// <summary>
    /// BOM-006: List global UnitTypes.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUnitTypes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) {
        if (page < 1 || pageSize < 1 || pageSize > 100) {
            return ValidationError(
                "page/pageSize",
                "page must be >= 1 and pageSize must be between 1 and 100.");
        }

        var (items, totalItems) = await unitTypeService.GetPagedAsync(
            page,
            pageSize,
            cancellationToken);

        var data = items
            .Select(Map)
            .ToList();

        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)pageSize);

        return Ok(new {
            data,
            pagination = new {
                page,
                pageSize,
                totalItems,
                totalPages
            }
        });
    }

    /// <summary>
    /// BOM-007: Update current global UnitType RentalPrice.
    /// </summary>
    [HttpPatch("{unitTypeId:guid}/price")]
    public async Task<IActionResult> UpdateUnitTypePrice(
        Guid unitTypeId,
        [FromBody] UpdateUnitTypePriceRequest request,
        CancellationToken cancellationToken = default) {
        if (request.RentalPrice < 0) {
            return ValidationError(
                "rentalPrice",
                "Rental price must be greater than or equal to 0.");
        }

        var unitType = await unitTypeService.UpdatePriceAsync(
            unitTypeId,
            request.RentalPrice,
            cancellationToken);

        if (unitType is null)
            return ResourceNotFound();

        return Ok(new {
            data = Map(unitType),
            message = "UnitType rental price updated."
        });
    }

    private static UnitTypeSummary Map(UnitType unitType)
        => new(
            unitType.UnitTypeId,
            unitType.Name,
            unitType.Mode,
            unitType.Size,
            unitType.RentalPrice,
            unitType.Description);

    private ObjectResult ResourceNotFound()
        => NotFound(new {
            code = "RESOURCE_NOT_FOUND",
            message = "UnitType was not found.",
            traceId = HttpContext.TraceIdentifier
        });

    private BadRequestObjectResult ValidationError(
        string field,
        string message)
        => BadRequest(new {
            code = "VALIDATION_ERROR",
            message = "Request validation failed.",
            errors = new Dictionary<string, string[]> {
                [field] = [message]
            },
            traceId = HttpContext.TraceIdentifier
        });
}
