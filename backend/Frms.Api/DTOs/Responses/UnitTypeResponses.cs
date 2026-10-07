namespace Frms.Api.DTOs.Responses;

public sealed record UnitTypeSummary(
    Guid UnitTypeId,
    string Name,
    string Mode,
    string Size,
    decimal RentalPrice,
    string? Description);
