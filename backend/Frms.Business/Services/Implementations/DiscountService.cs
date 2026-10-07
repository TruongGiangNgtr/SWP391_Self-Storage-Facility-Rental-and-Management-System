using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Models;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;

namespace Frms.Business.Services.Implementations;

internal sealed class DiscountService(
    IDiscountRepository repository,
    ICurrentUserContext currentUser)
    : IDiscountService {
    public async Task<
        (IReadOnlyList<DiscountResult> Items, int TotalCount)>
        ListAccessibleAsync(
            Guid customerId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) {
        if (customerId == Guid.Empty) {
            throw new BusinessException(
                "VALIDATION_ERROR",
                "customerId is required.",
                400);
        }

        if (page < 1 || pageSize < 1 || pageSize > 100) {
            throw new BusinessException(
                "VALIDATION_ERROR",
                "Page must be at least 1 and pageSize must be between 1 and 100.",
                400);
        }

        if (!currentUser.IsAuthenticated) {
            throw new BusinessException(
                "UNAUTHORIZED",
                "Authentication is required.",
                401);
        }

        switch (currentUser.Role) {
            case "CUSTOMER": {
                var ownCustomerId =
                    await repository.GetCustomerIdByUserAccountIdAsync(
                        currentUser.UserAccountId,
                        cancellationToken);

                if (ownCustomerId is null) {
                    throw new BusinessException(
                        "RESOURCE_NOT_FOUND",
                        "Customer profile was not found.",
                        404);
                }

                if (ownCustomerId.Value != customerId) {
                    throw new BusinessException(
                        "FORBIDDEN",
                        "You cannot access another Customer's Discounts.",
                        403);
                }

                break;
            }

            case "FACILITY_STAFF":
            case "FACILITY_MANAGER": {
                if (!await repository.CustomerExistsAsync(
                        customerId,
                        cancellationToken)) {
                    throw new BusinessException(
                        "RESOURCE_NOT_FOUND",
                        "Customer was not found.",
                        404);
                }

                var employee =
                    await repository.GetEmployeeByUserAccountIdAsync(
                        currentUser.UserAccountId,
                        cancellationToken);

                if (employee?.FacilityId is null) {
                    throw new BusinessException(
                        "FORBIDDEN",
                        "Facility assignment is not available.",
                        403);
                }

                var related =
                    await repository.HasFacilityRelationshipAsync(
                        customerId,
                        employee.FacilityId.Value,
                        cancellationToken);

                if (!related) {
                    throw new BusinessException(
                        "FORBIDDEN",
                        "Customer is outside your assigned Facility.",
                        403);
                }

                break;
            }

            case "BUSINESS_OPERATIONS_MANAGER": {
                if (!await repository.CustomerExistsAsync(
                        customerId,
                        cancellationToken)) {
                    throw new BusinessException(
                        "RESOURCE_NOT_FOUND",
                        "Customer was not found.",
                        404);
                }

                break;
            }

            default:
                throw new BusinessException(
                    "FORBIDDEN",
                    "You are not authorized to access Customer Discounts.",
                    403);
        }

        var totalCount =
            await repository.CountByCustomerAsync(
                customerId,
                cancellationToken);

        var discounts =
            await repository.ListByCustomerAsync(
                customerId,
                (page - 1) * pageSize,
                pageSize,
                cancellationToken);

        var items = discounts
            .Select(x => new DiscountResult(
                x.DiscountId,
                x.CustomerId,
                x.Name,
                x.Percentage,
                x.Status,
                x.EffectiveFrom,
                x.EffectiveTo))
            .ToArray();

        return (items, totalCount);
    }
}
