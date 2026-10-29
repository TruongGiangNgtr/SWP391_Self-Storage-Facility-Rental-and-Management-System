using Frms.Business.Exceptions;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;

namespace Frms.Business.Services.Implementations;

internal sealed class PublicPolicyService(
    IPublicPolicyRepository repository,
    IPublicPolicyContentProvider content) : IPublicPolicyService
{
    private static readonly string[] Order =
    [
        "reservation-cancellation", "deposit-payment-refund",
        "storage-access-handover-contract", "facility-rules-prohibited-items"
    ];

    public async Task<(IReadOnlyList<PublicPolicyDetailResult> Items, int TotalItems)> ListAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            throw new BusinessException("VALIDATION_ERROR", "Invalid public policy pagination.", 400);
        if ((long)(page - 1) * pageSize > int.MaxValue)
            throw new BusinessException("VALIDATION_ERROR", "Invalid public policy pagination.", 400);
        var policy = await CurrentAsync(cancellationToken);
        var byKey = content.PublishedArticles.ToDictionary(x => x.PolicyKey, StringComparer.Ordinal);
        var published = Order.Where(byKey.ContainsKey)
            .Select(key => Project(byKey[key], policy)).ToArray();
        return (published.Skip((page - 1) * pageSize).Take(pageSize).ToArray(), published.Length);
    }

    public async Task<PublicPolicyDetailResult> GetAsync(string policyKey, CancellationToken cancellationToken)
    {
        var policy = await CurrentAsync(cancellationToken);
        var article = content.PublishedArticles.FirstOrDefault(x => x.PolicyKey == policyKey);
        if (article is null)
            throw new BusinessException("RESOURCE_NOT_FOUND", "Published policy not found.", 404);
        return Project(article, policy);
    }

    private async Task<Policy> CurrentAsync(CancellationToken cancellationToken) =>
        await repository.GetCurrentActiveAsync(cancellationToken) ??
        throw new BusinessException("RESOURCE_NOT_FOUND", "Current public policy not found.", 404);

    private static PublicPolicyDetailResult Project(PublishedPolicyArticle article, Policy policy) =>
        new(article.PolicyKey, article.Title, article.Summary,
            policy.PolicyId, policy.Version,
            DateTime.SpecifyKind(policy.EffectiveFrom, DateTimeKind.Utc),
            policy.EffectiveTo.HasValue
                ? DateTime.SpecifyKind(policy.EffectiveTo.Value, DateTimeKind.Utc)
                : null,
            article.Sections);
}
