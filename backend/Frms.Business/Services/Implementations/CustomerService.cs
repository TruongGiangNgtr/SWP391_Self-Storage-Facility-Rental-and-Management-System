using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.Business.Validators;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Implementations;

internal sealed class CustomerService(
    ICustomerRepository repository,
    ICurrentUserContext currentUser,
    IAuthenticationService authentication,
    IFacilityAuthorizationService facilityAuthorization) : ICustomerService
{
    public async Task<UpdatedCustomerProfileResult> UpdateOwnProfileAsync(
        UpdateCustomerProfileCommand command, CancellationToken ct)
    {
        var account = await EnsureRoleAsync("CUSTOMER", ct);
        var update = CustomerProfileValidator.ValidateAndNormalize(command);
        CustomerProfileRecord? profile;
        try
        {
            profile = await repository.UpdateOwnProfileAsync(currentUser.UserAccountId,
                update.UpdateFullName, update.FullName, update.UpdateAddress, update.Address,
                update.UpdateCccd, update.Cccd, ct);
        }
        catch (CustomerCccdConflictException)
        {
            throw new BusinessException("VALIDATION_ERROR", "The CCCD is already registered.", 400);
        }

        if (profile is null) throw NotFound();
        return new UpdatedCustomerProfileResult(account.UserAccountId, profile.CustomerId,
            account.Role, account.Status, profile.FullName);
    }

    public async Task<CustomerDirectoryPageResult> ListFacilityAsync(
        Guid facilityId, int page, int pageSize, CancellationToken ct)
    {
        await EnsureFacilityRoleAsync(ct);
        ValidatePage(page, pageSize);
        await facilityAuthorization.EnsureSameFacilityAsync(facilityId, ct);
        var total = await repository.CountByFacilityAsync(facilityId, ct);
        var items = await repository.ListByFacilityAsync(facilityId, (page - 1) * pageSize, pageSize, ct);
        return Page(items, page, pageSize, total);
    }

    public async Task<CustomerDirectoryRecord> GetFacilityAsync(
        Guid facilityId, Guid customerId, CancellationToken ct)
    {
        await EnsureFacilityRoleAsync(ct);
        await facilityAuthorization.EnsureSameFacilityAsync(facilityId, ct);
        return await repository.GetByFacilityAsync(facilityId, customerId, ct) ?? throw NotFound();
    }

    public async Task<CustomerDirectoryPageResult> ListBusinessAsync(int page, int pageSize, CancellationToken ct)
    {
        await EnsureRoleAsync("BUSINESS_OPERATIONS_MANAGER", ct);
        ValidatePage(page, pageSize);
        var total = await repository.CountAllAsync(ct);
        var items = await repository.ListAllAsync((page - 1) * pageSize, pageSize, ct);
        return Page(items, page, pageSize, total);
    }

    public async Task<CustomerDirectoryRecord> GetBusinessAsync(Guid customerId, CancellationToken ct)
    {
        await EnsureRoleAsync("BUSINESS_OPERATIONS_MANAGER", ct);
        return await repository.GetByIdAsync(customerId, ct) ?? throw NotFound();
    }

    private async Task EnsureFacilityRoleAsync(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            throw new BusinessException("UNAUTHORIZED", "Authentication is required.", 401);
        if (currentUser.Role is not ("FACILITY_STAFF" or "FACILITY_MANAGER"))
            throw Forbidden();
        var account = await authentication.GetCurrentAccountAsync(currentUser.UserAccountId, ct);
        if (account.Role != currentUser.Role) throw Forbidden();
    }

    private async Task<CurrentAccountResult> EnsureRoleAsync(string role, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            throw new BusinessException("UNAUTHORIZED", "Authentication is required.", 401);
        if (currentUser.Role != role) throw Forbidden();
        var account = await authentication.GetCurrentAccountAsync(currentUser.UserAccountId, ct);
        if (account.Role != role) throw Forbidden();
        return account;
    }

    private static void ValidatePage(int page, int size)
    {
        if (page < 1 || size < 1 || size > 100 || page > int.MaxValue / size)
            throw new BusinessException("VALIDATION_ERROR", "Invalid pagination parameters.", 400);
    }

    private static CustomerDirectoryPageResult Page(
        IReadOnlyList<CustomerDirectoryRecord> items, int page, int size, int total) =>
        new(items, page, size, total, total == 0 ? 0 : (int)Math.Ceiling(total / (double)size));

    private static BusinessException Forbidden() =>
        new("FORBIDDEN", "You are not authorized to access this resource.", 403);
    private static BusinessException NotFound() =>
        new("RESOURCE_NOT_FOUND", "Customer was not found.", 404);
}
