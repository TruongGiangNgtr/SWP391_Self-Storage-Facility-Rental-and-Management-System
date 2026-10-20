namespace Frms.Api.DTOs.Responses;

public sealed record PublicPolicySectionResponse(string Heading, string Body);

public sealed record PublicPolicySummaryResponse(
    string PolicyKey, string Title, string Summary,
    Guid PolicyId, int Version, DateTime EffectiveFrom, DateTime? EffectiveTo);

public sealed record PublicPolicyDetailResponse(
    string PolicyKey, string Title, string Summary,
    Guid PolicyId, int Version, DateTime EffectiveFrom, DateTime? EffectiveTo,
    IReadOnlyList<PublicPolicySectionResponse> Sections);
