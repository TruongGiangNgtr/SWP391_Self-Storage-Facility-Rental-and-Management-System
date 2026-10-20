namespace Frms.Business.Models.Results;

public sealed record PublicPolicySection(string Heading, string Body);
public sealed record PublishedPolicyArticle(
    string PolicyKey, string Title, string Summary,
    IReadOnlyList<PublicPolicySection> Sections);

public sealed record PublicPolicyDetailResult(
    string PolicyKey, string Title, string Summary, Guid PolicyId,
    int Version, DateTime EffectiveFrom, DateTime? EffectiveTo,
    IReadOnlyList<PublicPolicySection> Sections);
