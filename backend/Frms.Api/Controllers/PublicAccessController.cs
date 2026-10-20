using Frms.Api.DTOs.Responses;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

/// <summary>Release 1 public read contracts pending a public-safe projection implementation.</summary>
[Route("api/v1")]
public sealed class PublicAccessController(IOperationalCatalogService catalogue) : ScaffoldControllerBase
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
    public ActionResult<ApiErrorResponse> ListPublicPolicies() =>
        ScaffoldNotImplemented("PUB-POL-001");

    [AllowAnonymous]
    [HttpGet("public/policies/{policyKey}")]
    public ActionResult<ApiErrorResponse> GetPublicPolicy(string policyKey) =>
        ScaffoldNotImplemented("PUB-POL-002");
}
