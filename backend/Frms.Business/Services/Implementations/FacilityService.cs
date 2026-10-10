using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;

namespace Frms.Business.Services.Implementations;

internal sealed class FacilityService(
    IFacilityRepository facilityRepository) : IFacilityService {
    private Task<(IReadOnlyList<Facility> Items, int TotalItems)> GetPagedCoreAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => facilityRepository.GetPagedAsync(page, pageSize, cancellationToken);

    private async Task<Facility> CreateCoreAsync(
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

    private async Task<Facility?> UpdateCoreAsync(
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

    private async Task<Facility?> ActivateCoreAsync(
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

    private async Task<Facility?> DeactivateCoreAsync(
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

    public async Task<(IReadOnlyList<FacilityResult> Items, int TotalItems)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var row = await GetPagedCoreAsync(page, pageSize, cancellationToken);
        return (row.Items.Select(ServiceResultProjection.Map).ToArray(), row.TotalItems);
    }

    public async Task<FacilityResult> CreateAsync(string name, string address, string? contactInfo, string? description, CancellationToken cancellationToken = default)
    {
        return ServiceResultProjection.Map(await CreateCoreAsync(name, address, contactInfo, description, cancellationToken));
    }

    public async Task<FacilityResult?> UpdateAsync(Guid facilityId, string? name, string? address, string? contactInfo, string? description, bool contactInfoSupplied, bool descriptionSupplied, CancellationToken cancellationToken = default)
    {
        var row = await UpdateCoreAsync(facilityId, name, address, contactInfo, description, contactInfoSupplied, descriptionSupplied, cancellationToken);
        return row is null ? null : ServiceResultProjection.Map(row);
    }

    public async Task<FacilityResult?> ActivateAsync(Guid facilityId, CancellationToken cancellationToken = default)
    {
        var row = await ActivateCoreAsync(facilityId, cancellationToken);
        return row is null ? null : ServiceResultProjection.Map(row);
    }

    public async Task<FacilityResult?> DeactivateAsync(Guid facilityId, CancellationToken cancellationToken = default)
    {
        var row = await DeactivateCoreAsync(facilityId, cancellationToken);
        return row is null ? null : ServiceResultProjection.Map(row);
    }
}
