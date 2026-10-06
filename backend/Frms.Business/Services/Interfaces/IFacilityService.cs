using Frms.DataAccess.Persistence.Entities;

namespace Frms.Business.Services.Interfaces;

public interface IFacilityService {
    Task<(IReadOnlyList<Facility> Items, int TotalItems)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Facility> CreateAsync(
        string name,
        string address,
        string? contactInfo,
        string? description,
        CancellationToken cancellationToken = default);

    Task<Facility?> UpdateAsync(
        Guid facilityId,
        string? name,
        string? address,
        string? contactInfo,
        string? description,
        bool contactInfoSupplied,
        bool descriptionSupplied,
        CancellationToken cancellationToken = default);

    Task<Facility?> ActivateAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default);

    Task<Facility?> DeactivateAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default);
}
