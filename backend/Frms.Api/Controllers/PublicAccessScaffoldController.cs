using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

/// <summary>Release 1 public read contracts pending a public-safe projection implementation.</summary>
[Route("api/v1")]
public sealed class PublicAccessScaffoldController : ScaffoldControllerBase
{
    [AllowAnonymous]
    [HttpGet("unit-types")]
    public ActionResult<ApiErrorResponse> ListPublicUnitTypes() =>
        ScaffoldNotImplemented("CAT-005");

    [AllowAnonymous]
    [HttpGet("public/policies")]
    public ActionResult<ApiErrorResponse> ListPublicPolicies() =>
        ScaffoldNotImplemented("PUB-POL-001");

    [AllowAnonymous]
    [HttpGet("public/policies/{policyKey}")]
    public ActionResult<ApiErrorResponse> GetPublicPolicy(string policyKey) =>
        ScaffoldNotImplemented("PUB-POL-002");
}
