using System.Globalization;
using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Frms.DataAccess.StoredProcedures;

namespace Frms.Business.Services.Implementations;

internal sealed class RenewalService(
    IRenewalRepository repository,
    ICurrentUserContext currentUser)
    : IRenewalService
{
    public async Task<RenewedContractRecord> RenewAsync(
        RenewContractCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new BusinessException(
                "UNAUTHORIZED",
                "Authentication is required.",
                401);
        }

        if (!DateOnly.TryParseExact(
                $"{command.NewEndMonth}-01",
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var newEndMonth))
        {
            throw new BusinessException(
                "VALIDATION_ERROR",
                "newEndMonth must use yyyy-MM format.",
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

        var contract =
            await repository.GetOwnedContractAsync(
                customerId.Value,
                command.ContractId,
                cancellationToken);

        if (contract is null)
        {
            throw new BusinessException(
                "RESOURCE_NOT_FOUND",
                "Contract was not found.",
                404);
        }

        if (contract.Status != "ACTIVE")
        {
            throw new BusinessException(
                "CONTRACT_NOT_ACTIVE",
                "Only an active Contract may be renewed.",
                409);
        }

        if (newEndMonth <= contract.EndMonth)
        {
            throw new BusinessException(
                "RENEWAL_NOT_CONTIGUOUS",
                "New End Month must be after the current End Month.",
                400);
        }

        try
        {
            return await repository.RenewAsync(
                customerId.Value,
                command.ContractId,
                newEndMonth,
                cancellationToken);
        }
        catch (StoredProcedureBusinessException ex)
        {
            switch (ex.Code)
            {
                case "CONTRACT_NOT_ACTIVE":
                    throw new BusinessException(
                        ex.Code,
                        "Only an active Contract may be renewed.",
                        409);

                case "RETURN_VISIT_PENDING":
                    throw new BusinessException(
                        ex.Code,
                        "A pending RETURN Visit blocks Contract renewal.",
                        409);

                case "RENEWAL_NOT_CONTIGUOUS":
                    throw new BusinessException(
                        ex.Code,
                        "The requested renewal period is not contiguous.",
                        400);

                case "RENEWAL_CAPACITY_NOT_AVAILABLE":
                    throw new BusinessException(
                        ex.Code,
                        "Capacity is not available for every extension month.",
                        409);

                case "RESOURCE_NOT_FOUND":
                    throw new BusinessException(
                        ex.Code,
                        "A required renewal resource was not found.",
                        404);

                default:
                    throw;
            }
        }
    }
}