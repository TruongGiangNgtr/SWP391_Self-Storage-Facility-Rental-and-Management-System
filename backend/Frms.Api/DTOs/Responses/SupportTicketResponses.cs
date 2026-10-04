namespace Frms.Api.DTOs.Responses;

/// <summary>Represents the canonical SupportTicketDetail schema.</summary>
public sealed record SupportTicketDetailResponse(
    Guid SupportTicketId,
    Guid ContractId,
    Guid CustomerId,
    Guid? AssignedEmployeeId,
    string Category,
    string Description,
    string Status,
    string? ResultNote,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
