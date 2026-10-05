using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Frms.Api.Mapping;
using Frms.Business.Exceptions;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthenticationService authenticationService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("customer/register")]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status501NotImplemented)]
    public ActionResult<ApiErrorResponse> RegisterCustomer(RegisterCustomerRequest request)
    {
        return StatusCode(
            StatusCodes.Status501NotImplemented,
            new ApiErrorResponse(
                "ENDPOINT_NOT_IMPLEMENTED",
                "AUTH-001 is a contract scaffold and has not been implemented.",
                HttpContext.TraceIdentifier));
    }

    [AllowAnonymous]
    [HttpPost("customer/login")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenDataResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<AuthTokenDataResponse>>> CustomerLogin(CustomerLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.LoginCustomerAsync(request.ToCommand(HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()), cancellationToken);
        return Ok(result.ToResponse());
    }

    [AllowAnonymous]
    [HttpPost("employee/login")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenDataResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<AuthTokenDataResponse>>> EmployeeLogin(EmployeeLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.LoginEmployeeAsync(request.ToCommand(HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()), cancellationToken);
        return Ok(result.ToResponse());
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<CurrentAccountDataResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<CurrentAccountDataResponse>>> Me(CancellationToken cancellationToken)
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var userAccountId)) throw new BusinessException("UNAUTHORIZED", "Authentication is required.", 401);
        var result = await authenticationService.GetCurrentAccountAsync(userAccountId, cancellationToken);
        return Ok(result.ToResponse());
    }
}
