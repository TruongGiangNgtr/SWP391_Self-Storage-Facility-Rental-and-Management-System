using System.ComponentModel.DataAnnotations;
using Frms.Api.Validation;

namespace Frms.Api.DTOs.Requests;

/// <summary>AUTH-001: public Customer self-registration payload.</summary>
public sealed record RegisterCustomerRequest
{
    [Required, MaxLength(150)]
    public required string FullName { get; init; }

    [Required, MaxLength(30)]
    public required string PhoneNumber { get; init; }

    [Required, EmailAddress, MaxLength(254)]
    public required string Email { get; init; }

    [Required, FrmsPassword]
    public required string Password { get; init; }

    [MaxLength(1000)]
    public string? Address { get; init; }

    // Optional or blank at registration; nonblank values must be 12 ASCII digits after trim.
    [RegularExpression(@"^\s*(?:[0-9]{12})?\s*$",
        ErrorMessage = "CCCD must be exactly 12 ASCII digits when supplied.")]
    public string? Cccd { get; init; }
}
