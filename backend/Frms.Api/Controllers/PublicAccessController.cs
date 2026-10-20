using Frms.Api.DTOs.Responses;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

/// <summary>Anonymous public catalogue and reviewed current policy content.</summary>
[Route("api/v1")]
public sealed class PublicAccessController(IOperationalCatalogService catalogue, IPublicPolicyService policies) : ScaffoldControllerBase
{
    [AllowAnonymous]
    [HttpGet("unit-types")]
    public async Task<ActionResult<PaginatedResponse<UnitTypeSummary>>> ListPublicUnitTypes(
        [FromQuery] string? mode = null, [FromQuery] string? size = null,
        [FromQuery] Guid? unitTypeId = null, [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var (items, total) = await catalogue.ListPublicUnitTypesAsync(mode, size, unitTypeId,
            sortBy, sortDirection, page, pageSize, cancellationToken);
        return Ok(new PaginatedResponse<UnitTypeSummary>(
            items.Select(x => new UnitTypeSummary(x.UnitTypeId, x.Name, x.Mode,
                x.Size, x.RentalPrice, x.Description, x.ImageUrl, x.VideoUrl)).ToArray(),
            new PaginationResponse(page, pageSize, total,
                total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize))));
    }

    [AllowAnonymous]
    [HttpGet("public/policies")]
    public async Task<ActionResult<PaginatedResponse<PublicPolicySummaryResponse>>> ListPublicPolicies(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await policies.ListAsync(page, pageSize, cancellationToken);
        var data = items.Select(x => new PublicPolicySummaryResponse(x.PolicyKey, x.Title,
            x.Summary, x.PolicyId, x.Version, x.EffectiveFrom, x.EffectiveTo)).ToArray();
        return Ok(new PaginatedResponse<PublicPolicySummaryResponse>(data,
            new PaginationResponse(page, pageSize, total,
                total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize))));
    }

    [AllowAnonymous]
    [HttpGet("public/policies/{policyKey}")]
    public async Task<ActionResult<ApiResponse<PublicPolicyDetailResponse>>> GetPublicPolicy(
        string policyKey, CancellationToken cancellationToken)
    {
        var x = await policies.GetAsync(policyKey, cancellationToken);
        var data = new PublicPolicyDetailResponse(x.PolicyKey, x.Title, x.Summary,
            x.PolicyId, x.Version, x.EffectiveFrom, x.EffectiveTo,
            x.Sections.Select(s => new PublicPolicySectionResponse(s.Heading, s.Body)).ToArray());
        return Ok(new ApiResponse<PublicPolicyDetailResponse>(data));
    }
}
