using System.ComponentModel.DataAnnotations;

namespace Frms.Api.DTOs.Requests;

/// <summary>Payload for creating an Employee account/profile (ADM-005).</summary>
public sealed record CreateEmployeeRequest
{
    [Required]
    public required string FullName { get; init; }

    [Required, EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string PhoneNumber { get; init; }

    [Required]
    public required string Role { get; init; }

    public Guid? FacilityId { get; init; }
}


/// <summary>Payload for assigning an Employee role and Facility (ADM-009).</summary>
public sealed record AssignEmployeeRequest
{
    [Required]
    public required string Role { get; init; }

    public Guid? FacilityId { get; init; }
}

public sealed record UpdateAdminEmployeeRequest {
    [Required]
    public required string FullName { get; init; }
}
