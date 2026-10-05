using Frms.Api.Authorization;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Authorize(Roles = RoleNames.Customer)]
[Route("api/v1/ai")]
public sealed class AiController : ScaffoldControllerBase
{
    /// <summary>AI-001: Optional AI Size Guide scaffold.</summary>
    [HttpPost("unit-type-recommendations")]
    [ProducesResponseType(typeof(ApiResponse<UnitTypeRecommendationResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> RecommendUnitType(
        [FromBody] RecommendUnitTypeRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("AI-001");
}
