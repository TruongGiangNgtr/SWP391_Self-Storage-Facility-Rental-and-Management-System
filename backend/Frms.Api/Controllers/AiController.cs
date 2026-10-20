using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using static System.Net.WebRequestMethods;

namespace Frms.Api.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("public-ai")]
[Route("api/v1/ai")]
public sealed class AiController(IAiRecommendationService service) : ControllerBase
{
    /// <summary>AI-001: Optional, non-authoritative public UnitType recommendation. </summary>
    // For using this function, it is require to apply API key to .env
    // Ai__Provider= OpenAICompatible
    // Ai__Endpoint = https://generativelanguage.googleapis.com/v1beta/openai/chat/completions
    // Ai__ApiKey= < GEMINI_API_KEY >
    // Ai__Model = gemini - 3.8 - flash
    // Ai__TimeoutSeconds = 60
    // Ai__RateLimitPerMinute = 10

    [HttpPost("unit-type-recommendations")]
    [ProducesResponseType(typeof(ApiResponse<UnitTypeRecommendationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<UnitTypeRecommendationResponse>>> RecommendUnitType(
        [FromBody] RecommendUnitTypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.RecommendAsync(request.StorageDescription, request.PreferredMode, cancellationToken);
        return Ok(new ApiResponse<UnitTypeRecommendationResponse>(new(
            result.RecommendedUnitTypeId,
            result.Reason,
            result.Alternatives.Select(x => new UnitTypeRecommendationAlternativeResponse(x.UnitTypeId, x.Reason)).ToArray())));
    }
}
