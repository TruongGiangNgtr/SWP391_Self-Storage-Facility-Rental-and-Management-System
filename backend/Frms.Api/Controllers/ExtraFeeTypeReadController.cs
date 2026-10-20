using Frms.Api.Authorization;
using Frms.Api.DTOs.Responses;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Route("api/v1/business/extra-fee-types")]
public sealed class ExtraFeeTypeReadController(IOperationalCatalogService service) : ControllerBase
{
    [Authorize(Roles = RoleNames.FacilityStaff + "," + RoleNames.FacilityManager + "," + RoleNames.BusinessOperationsManager)]
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ExtraFeeTypeCatalogResponse>>> ListExtraFeeTypes(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await service.ListExtraFeeTypesAsync(page, pageSize, cancellationToken);
        return Ok(new PaginatedResponse<ExtraFeeTypeCatalogResponse>(
            items.Select(x => new ExtraFeeTypeCatalogResponse(x.ExtraFeeTypeId, x.Name,
                x.DefaultAmount, x.Status)).ToArray(),
            new PaginationResponse(page, pageSize, total,
                total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize))));
    }
}
