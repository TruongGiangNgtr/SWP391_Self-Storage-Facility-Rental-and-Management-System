using Frms.Business.Abstractions.External;
using Frms.Business.Exceptions;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Frms.Business.Services.Implementations;

internal sealed class AiRecommendationService(
    IOperationalCatalogService catalogue,
    IAiRecommendationProvider provider,
    AiStorageItemsCatalog storageRules,
    ILogger<AiRecommendationService> logger) : IAiRecommendationService
{
    private const int PageSize = 100;
    private const int MaxOfferedTypes = 200; // Technical payload/cost guard, not a business inventory rule.

    public async Task<AiRecommendationResult> RecommendAsync(
        string storageDescription, string preferredMode, CancellationToken cancellationToken)
    {
        var description = storageDescription?.Trim() ?? "";
        if (description.Length is < 1 or > 1000 ||
            (preferredMode != "PUBLIC" && preferredMode != "PRIVATE"))
            throw new BusinessException("VALIDATION_ERROR", "Invalid AI recommendation input.", 400);

        // Server-side rule screen precedes catalogue and external AI calls.
        // The JSON remains a draft; errors are FRMS intake warnings, not legal judgments.
        var screening = storageRules.Assess(description);
        if (screening == AiStorageDecision.Prohibited)
            throw new BusinessException("PROHIBITED_STORAGE_ITEM",
                storageRules.PublicView.ProhibitedWarning, 422);
        if (screening == AiStorageDecision.NeedsReview)
            throw new BusinessException("STORAGE_ITEM_REVIEW_REQUIRED",
                storageRules.PublicView.NeedsReviewWarning, 422);

        try
        {
            await provider.EnsureConfiguredAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception) { throw Unavailable(); }

        // Use exactly CAT-005's authoritative public offering query; no account or rental data.
        var candidates = new List<AiPublicUnitType>();
        for (var page = 1; page <= MaxOfferedTypes / PageSize; page++)
        {
            var (items, total) = await catalogue.ListPublicUnitTypesAsync(
                preferredMode, null, null, "name", "asc", page, PageSize, cancellationToken);
            if (total > MaxOfferedTypes) throw Unavailable();
            candidates.AddRange(items.Select(x => new AiPublicUnitType(
                x.UnitTypeId, x.Name, x.Mode, x.Size, x.RentalPrice, x.Description)));
            if (candidates.Count >= total) break;
        }
        if (candidates.Count == 0) throw Unavailable();

        AiProviderRecommendation proposed;
        try
        {
            if (provider is not IAiRecommendationRunner runner) throw Unavailable();
            proposed = await runner.RecommendAsync(description, preferredMode, candidates, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception) { throw Unavailable(); }

        // Reject the whole answer on hallucinated IDs, duplicate IDs, invalid mode or unsafe/empty reasons.
        var offeredIds = candidates.Where(x => x.Mode == preferredMode)
            .Select(x => x.UnitTypeId).ToHashSet();
        if (!offeredIds.Contains(proposed.RecommendedUnitTypeId) || !ValidReason(proposed.Reason)
            || proposed.Alternatives is null || proposed.Alternatives.Count > 3)
        {
            logger.LogWarning("AI-001 provider output rejected: invalid selection, reason, or alternative count.");
            throw Unavailable();
        }

        var used = new HashSet<Guid> { proposed.RecommendedUnitTypeId };
        foreach (var alt in proposed.Alternatives)
        {
            if (!offeredIds.Contains(alt.UnitTypeId) || !used.Add(alt.UnitTypeId)
                || !ValidReason(alt.Reason))
            {
                logger.LogWarning("AI-001 provider output rejected: invalid or duplicate alternative.");
                throw Unavailable();
            }
        }

        return new AiRecommendationResult(proposed.RecommendedUnitTypeId,
            proposed.Reason.Trim(), proposed.Alternatives
                .Select(x => new AiRecommendationCandidate(x.UnitTypeId, x.Reason.Trim())).ToArray());
    }

    private static bool ValidReason(string? reason) =>
        !string.IsNullOrWhiteSpace(reason) && reason.Length <= 300 &&
        !reason.Any(char.IsControl);

    private static BusinessException Unavailable() => new(
        "EXTERNAL_PROVIDER_UNAVAILABLE",
        "AI recommendation is temporarily unavailable. Choose a unit type manually.", 503);
}
