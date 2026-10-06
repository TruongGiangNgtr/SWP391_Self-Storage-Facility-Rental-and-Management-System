namespace Frms.Api.DTOs.Responses;

public sealed record FacilitySummary(
    Guid FacilityId,
    string Name,
    string Address,
    string? ContactInfo,
    string? Description,
    string Status);
