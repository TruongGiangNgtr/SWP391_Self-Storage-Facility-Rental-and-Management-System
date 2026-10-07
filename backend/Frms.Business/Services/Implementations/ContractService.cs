using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Implementations;

internal sealed class ContractService(
    IContractRepository repository,
    ICurrentUserContext currentUser,
    IFacilityAuthorizationService facilityAuthorizationService)
    : IContractService {
    private const string CustomerRole = "CUSTOMER";
    private const string FacilityStaffRole = "FACILITY_STAFF";
    private const string FacilityManagerRole = "FACILITY_MANAGER";
    private const string BomRole = "BUSINESS_OPERATIONS_MANAGER";

    public async Task<ContractPageRecord> ListAccessibleAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) {
        EnsureAuthenticated();

        if (page < 1 || pageSize < 1 || pageSize > 100) {
            throw new BusinessException(
                "VALIDATION_ERROR",
                "page must be >= 1 and pageSize must be between 1 and 100.",
                400);
        }

        int totalItems;
        IReadOnlyList<ContractSummaryRecord> items;

        switch (currentUser.Role) {
            case CustomerRole: {
                var customerId =
                    await GetCustomerIdAsync(cancellationToken);

                totalItems =
                    await repository.CountByCustomerAsync(
                        customerId,
                        cancellationToken);

                items =
                    await repository.ListByCustomerAsync(
                        customerId,
                        (page - 1) * pageSize,
                        pageSize,
                        cancellationToken);

                break;
            }

            case FacilityManagerRole: {
                var facilityId =
                    await facilityAuthorizationService
                        .GetAssignedFacilityIdAsync(
                            cancellationToken);

                totalItems =
                    await repository.CountByFacilityAsync(
                        facilityId,
                        cancellationToken);

                items =
                    await repository.ListByFacilityAsync(
                        facilityId,
                        (page - 1) * pageSize,
                        pageSize,
                        cancellationToken);

                break;
            }

            case BomRole: {
                totalItems =
                    await repository.CountAllAsync(
                        cancellationToken);

                items =
                    await repository.ListAllAsync(
                        (page - 1) * pageSize,
                        pageSize,
                        cancellationToken);

                break;
            }

            default:
                throw Forbidden();
        }

        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(
                totalItems / (double)pageSize);

        return new ContractPageRecord(
            items,
            page,
            pageSize,
            totalItems,
            totalPages);
    }

    public async Task<ContractDetailRecord> GetAccessibleAsync(
        Guid contractId,
        CancellationToken cancellationToken = default) {
        EnsureAuthenticated();

        switch (currentUser.Role) {
            case CustomerRole: {
                var customerId =
                    await GetCustomerIdAsync(
                        cancellationToken);

                return await repository.GetDetailAsync(
                           customerId,
                           contractId,
                           cancellationToken)
                       ?? NotFound();
            }

            case FacilityStaffRole:
            case FacilityManagerRole: {
                var result =
                    await repository.GetDetailByIdAsync(
                        contractId,
                        cancellationToken)
                    ?? NotFound();

                await facilityAuthorizationService
                    .EnsureSameFacilityAsync(
                        result.FacilityId,
                        cancellationToken);

                return result;
            }

            case BomRole:
                return await repository.GetDetailByIdAsync(
                           contractId,
                           cancellationToken)
                       ?? NotFound();

            default:
                throw Forbidden();
        }
    }

    private async Task<Guid> GetCustomerIdAsync(
        CancellationToken cancellationToken) {
        return await repository
                   .GetCustomerIdByUserAccountIdAsync(
                       currentUser.UserAccountId,
                       cancellationToken)
               ?? throw new BusinessException(
                   "RESOURCE_NOT_FOUND",
                   "Customer profile was not found.",
                   404);
    }

    private void EnsureAuthenticated() {
        if (!currentUser.IsAuthenticated) {
            throw new BusinessException(
                "UNAUTHORIZED",
                "Authentication is required.",
                401);
        }
    }

    private static BusinessException Forbidden()
        => new(
            "FORBIDDEN",
            "You are not authorized to access this resource.",
            403);

    private static ContractDetailRecord NotFound()
        => throw new BusinessException(
            "RESOURCE_NOT_FOUND",
            "Contract was not found.",
            404);
}
