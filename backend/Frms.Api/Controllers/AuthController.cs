using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Frms.Api.Mapping;
using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frms.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    IAuthenticationService authenticationService,
    ICustomerRegistrationService customerRegistrationService)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("customer/register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterCustomerResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<RegisterCustomerResponse>>> RegisterCustomer(
        [FromBody] RegisterCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await customerRegistrationService.RegisterAsync(
            new RegisterCustomerCommand(request.FullName, request.PhoneNumber, request.Email,
                request.Password, request.Address, request.Cccd), cancellationToken);
        return StatusCode(StatusCodes.Status201Created,
            new ApiResponse<RegisterCustomerResponse>(
                new RegisterCustomerResponse(result.UserAccountId, result.CustomerId),
                "Customer registered."));
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
