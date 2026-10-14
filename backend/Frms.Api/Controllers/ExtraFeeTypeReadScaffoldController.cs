using Frms.Api.Authorization;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Route("api/v1/business/extra-fee-types")]
public sealed class ExtraFeeTypeReadScaffoldController : ScaffoldControllerBase
{
    [Authorize(Roles = RoleNames.FacilityStaff + "," + RoleNames.FacilityManager + "," + RoleNames.BusinessOperationsManager)]
    [HttpGet]
    public ActionResult<ApiErrorResponse> ListExtraFeeTypes() =>
        ScaffoldNotImplemented("BOM-013");
}
