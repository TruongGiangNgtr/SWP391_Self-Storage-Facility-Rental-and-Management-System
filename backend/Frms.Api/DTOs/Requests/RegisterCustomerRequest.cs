using System.ComponentModel.DataAnnotations;

namespace Frms.Api.DTOs.Requests;

/// <summary>Contract-only payload for the unimplemented AUTH-001 registration endpoint.</summary>
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
