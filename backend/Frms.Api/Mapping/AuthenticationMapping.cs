using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;

namespace Frms.Api.Mapping;

internal static class AuthenticationMapping
{
    public static CustomerLoginCommand ToCommand(this CustomerLoginRequest request, string? ipAddress, string? deviceInfo) => new(request.PhoneNumber, request.Password, ipAddress, deviceInfo);
    public static EmployeeLoginCommand ToCommand(this EmployeeLoginRequest request, string? ipAddress, string? deviceInfo) => new(request.Email, request.Password, ipAddress, deviceInfo);
    public static ApiResponse<AuthTokenDataResponse> ToResponse(this AuthenticationResult result) => new(new AuthTokenDataResponse(result.AccessToken, result.TokenType, new AuthenticatedUserResponse(result.User.UserAccountId, result.User.Role, result.User.Status)), "Login successful.");
    public static ApiResponse<CurrentAccountDataResponse> ToResponse(this CurrentAccountResult result)
    {
        CurrentProfileResponse profile = result.Role == "CUSTOMER"
            ? new CurrentCustomerProfileResponse(result.CustomerId, result.FullName, result.Cccd)
            : new CurrentProfileResponse(result.CustomerId, result.EmployeeId, result.FullName, result.FacilityId);
        return new(new CurrentAccountDataResponse(result.UserAccountId, result.Role,
            result.Status, result.Email, result.PhoneNumber, profile));
    }
}
