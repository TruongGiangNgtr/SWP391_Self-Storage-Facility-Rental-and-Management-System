namespace Frms.Business.Models.Results;

// This catalogue projection contains only public CAT-005 fields, no account history or capacity.
public sealed record AiPublicUnitType(Guid UnitTypeId, string Name, string Mode,
    string Size, decimal RentalPrice, string? Description);

public sealed record AiRecommendationCandidate(Guid UnitTypeId, string Reason);

public sealed record AiProviderRecommendation(Guid RecommendedUnitTypeId, string Reason,
    IReadOnlyList<AiRecommendationCandidate> Alternatives);

public sealed record AiRecommendationResult(Guid RecommendedUnitTypeId, string Reason,
    IReadOnlyList<AiRecommendationCandidate> Alternatives);
