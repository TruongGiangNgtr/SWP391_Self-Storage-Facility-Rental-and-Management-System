using System.ComponentModel.DataAnnotations;
using Frms.Api.Validation;

namespace Frms.Api.DTOs.Requests;

/// <summary>Customer phone-number login payload.</summary>
public sealed record CustomerLoginRequest
{
    [Required, MaxLength(30)]
    public required string PhoneNumber { get; init; }

    [Required, FrmsPassword]
    public required string Password { get; init; }
}

/// <summary>Employee email login payload.</summary>
public sealed record EmployeeLoginRequest
{
    [Required, EmailAddress, MaxLength(254)]
    public required string Email { get; init; }

    [Required, FrmsPassword]
    public required string Password { get; init; }
}
