using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Implementations;

internal sealed class ContractService(
    IContractRepository repository,
    ICurrentUserContext currentUser)
    : IContractService
{
    public async Task<ContractPageRecord> ListOwnAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new BusinessException(
                "UNAUTHORIZED",
                "Authentication is required.",
                401);
        }

        if (page < 1 ||
            pageSize < 1 ||
            pageSize > 100)
        {
            throw new BusinessException(
                "VALIDATION_ERROR",
                "page must be >= 1 and pageSize must be between 1 and 100.",
                400);
        }

        var customerId =
            await repository.GetCustomerIdByUserAccountIdAsync(
                currentUser.UserAccountId,
                cancellationToken);

        if (customerId is null)
        {
            throw new BusinessException(
                "RESOURCE_NOT_FOUND",
                "Customer profile was not found.",
                404);
        }

        var totalItems =
            await repository.CountByCustomerAsync(
                customerId.Value,
                cancellationToken);

        var items =
            await repository.ListByCustomerAsync(
                customerId.Value,
                (page - 1) * pageSize,
                pageSize,
                cancellationToken);

        var totalPages =
            totalItems == 0
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

    public async Task<ContractDetailRecord> GetOwnAsync(
        Guid contractId,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new BusinessException(
                "UNAUTHORIZED",
                "Authentication is required.",
                401);
        }

        var customerId =
            await repository.GetCustomerIdByUserAccountIdAsync(
                currentUser.UserAccountId,
                cancellationToken);

        if (customerId is null)
        {
            throw new BusinessException(
                "RESOURCE_NOT_FOUND",
                "Customer profile was not found.",
                404);
        }

        return await repository.GetDetailAsync(
                   customerId.Value,
                   contractId,
                   cancellationToken)
               ?? throw new BusinessException(
                   "RESOURCE_NOT_FOUND",
                   "Contract was not found.",
                   404);
    }
}