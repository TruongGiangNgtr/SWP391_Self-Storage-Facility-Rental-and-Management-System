using Frms.Business.Models.Results;

namespace Frms.Business.Abstractions.External;

/// <summary>AI-001 optional execution contract; extends the existing provider boundary without breaking it.</summary>
public interface IAiRecommendationRunner : IAiRecommendationProvider
{
    Task<AiProviderRecommendation> RecommendAsync(
        string storageDescription,
        string preferredMode,
        IReadOnlyList<AiPublicUnitType> offeredUnitTypes,
        CancellationToken cancellationToken);
}
