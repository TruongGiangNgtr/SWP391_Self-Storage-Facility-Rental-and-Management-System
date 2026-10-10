using System.Text.Json;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Frms.Business.Services.Interfaces;
using Frms.Business.Models.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[ApiController]
[Authorize(Roles = "BUSINESS_OPERATIONS_MANAGER")]
[Route("api/v1/business/facilities")]
public sealed class BusinessFacilitiesController(
    IFacilityService facilityService) : ControllerBase {
    /// <summary>
    /// BOM-001: List all Facilities for Business Operations Manager.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetFacilities(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) {
        if (page < 1 || pageSize < 1 || pageSize > 100) {
            return ValidationError(
                "page/pageSize",
                "page must be >= 1 and pageSize must be between 1 and 100.");
        }

        var (items, totalItems) = await facilityService.GetPagedAsync(
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
    /// BOM-002: Create Facility.
    /// New Facility always starts INACTIVE.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateFacility(
        [FromBody] CreateFacilityRequest request,
        CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(request.Name)) {
            return ValidationError(
                "name",
                "Facility name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Address)) {
            return ValidationError(
                "address",
                "Facility address is required.");
        }

        var facility = await facilityService.CreateAsync(
            request.Name,
            request.Address,
            request.ContactInfo,
            request.Description,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new {
                data = Map(facility),
                message = "Facility created."
            });
    }

    /// <summary>
    /// BOM-003: Update Facility basic information.
    /// </summary>
    [HttpPatch("{facilityId:guid}")]
    public async Task<IActionResult> UpdateFacility(
        Guid facilityId,
        [FromBody] JsonElement body,
        CancellationToken cancellationToken = default) {
        if (body.ValueKind != JsonValueKind.Object) {
            return ValidationError(
                "body",
                "Request body must be a JSON object.");
        }

        string? name = null;
        string? address = null;
        string? contactInfo = null;
        string? description = null;

        var nameSupplied =
            body.TryGetProperty("name", out var nameElement);

        var addressSupplied =
            body.TryGetProperty("address", out var addressElement);

        var contactInfoSupplied =
            body.TryGetProperty("contactInfo", out var contactElement);

        var descriptionSupplied =
            body.TryGetProperty("description", out var descriptionElement);

        if (!nameSupplied
            && !addressSupplied
            && !contactInfoSupplied
            && !descriptionSupplied) {
            return ValidationError(
                "body",
                "At least one Facility field must be supplied.");
        }

        if (nameSupplied) {
            if (nameElement.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(nameElement.GetString())) {
                return ValidationError(
                    "name",
                    "Facility name must be a non-empty string.");
            }

            name = nameElement.GetString();
        }

        if (addressSupplied) {
            if (addressElement.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(addressElement.GetString())) {
                return ValidationError(
                    "address",
                    "Facility address must be a non-empty string.");
            }

            address = addressElement.GetString();
        }

        if (contactInfoSupplied) {
            if (contactElement.ValueKind is not JsonValueKind.String
                and not JsonValueKind.Null) {
                return ValidationError(
                    "contactInfo",
                    "contactInfo must be a string or null.");
            }

            contactInfo =
                contactElement.ValueKind == JsonValueKind.Null
                    ? null
                    : contactElement.GetString();
        }

        if (descriptionSupplied) {
            if (descriptionElement.ValueKind is not JsonValueKind.String
                and not JsonValueKind.Null) {
                return ValidationError(
                    "description",
                    "description must be a string or null.");
            }

            description =
                descriptionElement.ValueKind == JsonValueKind.Null
                    ? null
                    : descriptionElement.GetString();
        }

        var facility = await facilityService.UpdateAsync(
            facilityId,
            name,
            address,
            contactInfo,
            description,
            contactInfoSupplied,
            descriptionSupplied,
            cancellationToken);

        if (facility is null)
            return ResourceNotFound();

        return Ok(new {
            data = Map(facility),
            message = "Facility updated."
        });
    }

    /// <summary>
    /// BOM-004: Activate Facility.
    /// </summary>
    [HttpPost("{facilityId:guid}/activate")]
    public async Task<IActionResult> ActivateFacility(
        Guid facilityId,
        CancellationToken cancellationToken = default) {
        var facility = await facilityService.ActivateAsync(
            facilityId,
            cancellationToken);

        if (facility is null)
            return ResourceNotFound();

        return Ok(new {
            data = Map(facility),
            message = "Facility activated."
        });
    }

    /// <summary>
    /// BOM-005: Deactivate Facility.
    /// </summary>
    [HttpPost("{facilityId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateFacility(
        Guid facilityId,
        CancellationToken cancellationToken = default) {
        var facility = await facilityService.DeactivateAsync(
            facilityId,
            cancellationToken);

        if (facility is null)
            return ResourceNotFound();

        return Ok(new {
            data = Map(facility),
            message = "Facility deactivated."
        });
    }

    private static FacilitySummary Map(FacilityResult facility)
        => new(
            facility.FacilityId,
            facility.Name,
            facility.Address,
            facility.ContactInfo,
            facility.Description,
            facility.Status);

    private ObjectResult ResourceNotFound()
        => NotFound(new {
            code = "RESOURCE_NOT_FOUND",
            message = "Facility was not found.",
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
