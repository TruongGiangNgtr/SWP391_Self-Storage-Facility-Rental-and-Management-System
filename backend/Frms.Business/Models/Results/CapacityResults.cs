namespace Frms.Business.Models.Results;

public sealed record UnitTypeAvailabilityResult(
    Guid UnitTypeId,
    string Name,
    string Mode,
    string Size,
    decimal RentalPrice,
    string? Description,
    DateOnly? RequestedStartMonth,
    DateOnly? RequestedEndMonth,
    int? AvailableCapacity,
    string? ImageUrl = null,
    string? VideoUrl = null);

public sealed record CapacityPageResult(
    IReadOnlyList<UnitTypeAvailabilityResult> Items,
    int TotalItems);