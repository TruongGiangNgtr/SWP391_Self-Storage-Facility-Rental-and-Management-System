using Frms.Api.Authorization;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[Route("api/v1")]
public sealed class AuthController : ScaffoldControllerBase
{
    /// <summary>AUTH-001: Customer self-registration scaffold.</summary>
    [AllowAnonymous]
    [HttpPost("auth/customer/register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<ApiErrorResponse> RegisterCustomer(
        [FromBody] RegisterCustomerRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("AUTH-001");

    /// <summary>AUTH-002: Customer login by PhoneNumber scaffold.</summary>
    [AllowAnonymous]
    [HttpPost("auth/customer/login")]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> CustomerLogin(
        [FromBody] CustomerLoginRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("AUTH-002");

    /// <summary>AUTH-003: Employee login by Email scaffold.</summary>
    [AllowAnonymous]
    [HttpPost("auth/employee/login")]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> EmployeeLogin(
        [FromBody] EmployeeLoginRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("AUTH-003");

    /// <summary>AUTH-004: Current authenticated account/profile scaffold.</summary>
    [Authorize]
    [HttpGet("auth/me")]
    public ActionResult<ApiErrorResponse> GetCurrentAccount(CancellationToken cancellationToken) =>
        ScaffoldNotImplemented("AUTH-004");

    /// <summary>AUTH-005: Update limited Customer profile fields scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpPatch("customers/me/profile")]
    public ActionResult<ApiErrorResponse> UpdateCustomerProfile(CancellationToken cancellationToken) =>
        ScaffoldNotImplemented("AUTH-005");
}
