using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface IFacilityService {
    Task<(IReadOnlyList<FacilityResult> Items, int TotalItems)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<FacilityResult> CreateAsync(
        string name,
        string address,
        string? contactInfo,
        string? description,
        CancellationToken cancellationToken = default);

    Task<FacilityResult?> UpdateAsync(
        Guid facilityId,
        string? name,
        string? address,
        string? contactInfo,
        string? description,
        bool contactInfoSupplied,
        bool descriptionSupplied,
        CancellationToken cancellationToken = default);

    Task<FacilityResult?> ActivateAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default);

    Task<FacilityResult?> DeactivateAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default);
}
