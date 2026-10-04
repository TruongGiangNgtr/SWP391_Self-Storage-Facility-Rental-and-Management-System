using System.ComponentModel.DataAnnotations;

namespace Frms.Api.DTOs.Requests;

/// <summary>Payload for creating a support ticket (SUP-001).</summary>
public sealed record CreateSupportTicketRequest
{
    public required Guid ContractId { get; init; }

    [Required]
    public required string Category { get; init; }

    [Required]
    public required string Description { get; init; }
}

/// <summary>Payload for cancelling a support ticket (SUP-004).</summary>
public sealed record CancelSupportTicketRequest
{
    [Required]
    public required string Reason { get; init; }
}

/// <summary>Payload for assigning Facility Staff to a support ticket (SUP-005).</summary>
public sealed record AssignSupportTicketRequest
{
    public required Guid EmployeeId { get; init; }
}

/// <summary>Payload for completing a support ticket (SUP-006).</summary>
public sealed record CompleteSupportTicketRequest
{
    [Required]
    public required string ResultNote { get; init; }
}
