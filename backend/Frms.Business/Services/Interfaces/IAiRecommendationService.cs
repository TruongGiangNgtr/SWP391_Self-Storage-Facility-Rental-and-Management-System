using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface IAiRecommendationService
{
    Task<AiRecommendationResult> RecommendAsync(
        string storageDescription, string preferredMode, CancellationToken cancellationToken);
}
