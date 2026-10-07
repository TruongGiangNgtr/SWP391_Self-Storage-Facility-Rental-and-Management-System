namespace Frms.Business.Models.Results;

public sealed record FacilityCatalogResult(
    Guid FacilityId,
    string Name,
    string Address,
    string? ContactInfo,
    string? Description,
    string Status);

public sealed record FacilityCatalogPageResult(
    IReadOnlyList<FacilityCatalogResult> Items,
    int Page,
    int PageSize,
    int TotalItems);
