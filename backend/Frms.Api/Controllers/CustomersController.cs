using System.Text.Json;
using Frms.Api.Authorization;
using Frms.Api.DTOs.Responses;
using Frms.Api.Mapping;
using Frms.Api.Validation;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

/// <summary>Customer operations. Route and role remain action-specific.</summary>
[Route("api/v1")]
public sealed class CustomersController(ICustomerService customers) : ControllerBase
{
    [Authorize(Roles = RoleNames.Customer)]
    [HttpPatch("customers/me/profile")]
    [ProducesResponseType(typeof(ApiResponse<UpdatedCustomerProfileResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UpdatedCustomerProfileResponse>>> UpdateOwnProfile(
        [FromBody] JsonElement request, CancellationToken cancellationToken = default)
    {
        var updated = await customers.UpdateOwnProfileAsync(
            CustomerProfilePatchParser.Parse(request), cancellationToken);
        return Ok(new ApiResponse<UpdatedCustomerProfileResponse>(updated.ToResponse(), "Customer profile updated."));
    }

    // Facility relationship includes a Reservation OR Contract (not just assigned tasks).
    [Authorize(Roles = RoleNames.FacilityStaff + "," + RoleNames.FacilityManager)]
    [HttpGet("facilities/{facilityId:guid}/customers")]
    public async Task<ActionResult<PaginatedResponse<CustomerDirectoryResponse>>> ListFacilityCustomers(
        Guid facilityId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await customers.ListFacilityAsync(facilityId, page, pageSize, cancellationToken);
        return Ok(result.ToResponse());
    }

    [Authorize(Roles = RoleNames.FacilityStaff + "," + RoleNames.FacilityManager)]
    [HttpGet("facilities/{facilityId:guid}/customers/{customerId:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerDirectoryResponse>>> GetFacilityCustomer(
        Guid facilityId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var result = await customers.GetFacilityAsync(facilityId, customerId, cancellationToken);
        return Ok(new ApiResponse<CustomerDirectoryResponse>(result.ToResponse()));
    }

    // Read-only business directory. Field-level PII is intentionally excluded.
    [Authorize(Roles = RoleNames.BusinessOperationsManager)]
    [HttpGet("business/customers")]
    public async Task<ActionResult<PaginatedResponse<CustomerDirectoryResponse>>> ListCustomers(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await customers.ListBusinessAsync(page, pageSize, cancellationToken);
        return Ok(result.ToResponse());
    }

    [Authorize(Roles = RoleNames.BusinessOperationsManager)]
    [HttpGet("business/customers/{customerId:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerDirectoryResponse>>> GetCustomer(
        Guid customerId, CancellationToken cancellationToken = default)
    {
        var result = await customers.GetBusinessAsync(customerId, cancellationToken);
        return Ok(new ApiResponse<CustomerDirectoryResponse>(result.ToResponse()));
    }
}
