namespace Frms.Api.DTOs.Responses;

/// <summary>Represents a Facility summary from the SRS canonical schema.</summary>
public sealed record FacilitySummaryResponse(
    Guid FacilityId,
    string Name,
    string Address,
    string? ContactInfo,
    string? Description,
    string Status);

/// <summary>Represents a requested month period.</summary>
public sealed record RequestedPeriodResponse(string StartMonth, string EndMonth);

/// <summary>Represents UnitType details and server-authoritative capacity.</summary>
public sealed record UnitTypeAvailabilityResponse(
    Guid UnitTypeId,
    string Name,
    string Mode,
    string Size,
    decimal RentalPrice,
    string? Description,
    RequestedPeriodResponse? RequestedPeriod,
    int? AvailableCapacity);

/// <summary>Represents an AI recommendation alternative.</summary>
public sealed record UnitTypeRecommendationAlternativeResponse(
    Guid UnitTypeId,
    string Reason);

/// <summary>Represents the validated optional AI recommendation result.</summary>
public sealed record UnitTypeRecommendationResponse(
    Guid RecommendedUnitTypeId,
    string Reason,
    IReadOnlyList<UnitTypeRecommendationAlternativeResponse> Alternatives);
