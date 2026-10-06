using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;

namespace Frms.Business.Services.Implementations;

internal sealed class FacilityService(
    IFacilityRepository facilityRepository) : IFacilityService {
    public Task<(IReadOnlyList<Facility> Items, int TotalItems)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => facilityRepository.GetPagedAsync(page, pageSize, cancellationToken);

    public async Task<Facility> CreateAsync(
        string name,
        string address,
        string? contactInfo,
        string? description,
        CancellationToken cancellationToken = default) {
        var facility = new Facility {
            FacilityId = Guid.NewGuid(),
            Name = name.Trim(),
            Address = address.Trim(),
            ContactInfo = NormalizeOptional(contactInfo),
            Description = NormalizeOptional(description),
            Status = "INACTIVE"
        };

        await facilityRepository.AddAsync(facility, cancellationToken);

        return facility;
    }

    public async Task<Facility?> UpdateAsync(
        Guid facilityId,
        string? name,
        string? address,
        string? contactInfo,
        string? description,
        bool contactInfoSupplied,
        bool descriptionSupplied,
        CancellationToken cancellationToken = default) {
        var facility = await facilityRepository.GetByIdAsync(
            facilityId,
            cancellationToken);

        if (facility is null)
            return null;

        if (name is not null)
            facility.Name = name.Trim();

        if (address is not null)
            facility.Address = address.Trim();

        if (contactInfoSupplied)
            facility.ContactInfo = NormalizeOptional(contactInfo);

        if (descriptionSupplied)
            facility.Description = NormalizeOptional(description);

        await facilityRepository.SaveChangesAsync(cancellationToken);

        return facility;
    }

    public async Task<Facility?> ActivateAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default) {
        var facility = await facilityRepository.GetByIdAsync(
            facilityId,
            cancellationToken);

        if (facility is null)
            return null;

        facility.Status = "ACTIVE";

        await facilityRepository.SaveChangesAsync(cancellationToken);

        return facility;
    }

    public async Task<Facility?> DeactivateAsync(
        Guid facilityId,
        CancellationToken cancellationToken = default) {
        var facility = await facilityRepository.GetByIdAsync(
            facilityId,
            cancellationToken);

        if (facility is null)
            return null;

        facility.Status = "INACTIVE";

        await facilityRepository.SaveChangesAsync(cancellationToken);

        return facility;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
