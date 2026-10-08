namespace Frms.DataAccess.Repositories.Models;

public sealed record FacilityCatalogRecord(
    Guid FacilityId,
    string Name,
    string Address,
    string? ContactInfo,
    string? Description,
    string Status);
