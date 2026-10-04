using System.ComponentModel.DataAnnotations;

namespace Frms.Api.DTOs.Requests;

/// <summary>Payload for customer self-registration (AUTH-001).</summary>
public sealed record RegisterCustomerRequest
{
    [Required]
    public required string FullName { get; init; }

    [Required]
    public required string PhoneNumber { get; init; }

    [Required, EmailAddress]
    public required string Email { get; init; }

    [Required, StringLength(64, MinimumLength = 8)]
    public required string Password { get; init; }

    public string? Address { get; init; }

    public string? Cccd { get; init; }
}

/// <summary>Payload for customer login by phone number (AUTH-002).</summary>
public sealed record CustomerLoginRequest
{
    [Required]
    public required string PhoneNumber { get; init; }

    [Required, StringLength(64, MinimumLength = 8)]
    public required string Password { get; init; }
}

/// <summary>Payload for employee login by email (AUTH-003).</summary>
public sealed record EmployeeLoginRequest
{
    [Required, EmailAddress]
    public required string Email { get; init; }

    [Required, StringLength(64, MinimumLength = 8)]
    public required string Password { get; init; }
}
