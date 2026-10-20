using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface IPublicPolicyContentProvider
{
    IReadOnlyList<PublishedPolicyArticle> PublishedArticles { get; }
}

public interface IPublicPolicyService
{
    Task<(IReadOnlyList<PublicPolicyDetailResult> Items, int TotalItems)> ListAsync(
        int page, int pageSize, CancellationToken cancellationToken);
    Task<PublicPolicyDetailResult> GetAsync(string policyKey, CancellationToken cancellationToken);
}
