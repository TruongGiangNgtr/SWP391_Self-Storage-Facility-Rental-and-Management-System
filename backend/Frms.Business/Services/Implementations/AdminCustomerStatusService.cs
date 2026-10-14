using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Implementations;

internal sealed class AdminCustomerStatusService(IAdminCustomerStatusRepository repository,
    ICurrentUserContext user, IAuthenticationService auth) : IAdminCustomerStatusService
{
    public async Task<CustomerAccountStatusRecord> SetStatusAsync(
        Guid customerId, string status, CancellationToken ct)
    {
        if (!user.IsAuthenticated)
            throw new BusinessException("UNAUTHORIZED", "Authentication is required.", 401);
        var current = await auth.GetCurrentAccountAsync(user.UserAccountId, ct);
        if (user.Role != "SYSTEM_ADMINISTRATOR" || current.Role != "SYSTEM_ADMINISTRATOR")
            throw new BusinessException("FORBIDDEN", "Access denied.", 403);
        if (customerId == Guid.Empty || status is not ("ACTIVE" or "INACTIVE"))
            throw new BusinessException("VALIDATION_ERROR", "Invalid status request.", 400);
        try
        {
            return await repository.SetStatusAsync(customerId, status, ct)
                ?? throw new BusinessException("RESOURCE_NOT_FOUND", "Customer not found.", 404);
        }
        catch (CustomerStatusProcedureException exception)
        {
            throw exception.Code switch
            {
                "CUSTOMER_HAS_ACTIVE_LIFECYCLE" => new BusinessException(
                    exception.Code, "Customer has a confirmed reservation or active contract.", 409),
                "USER_ACCOUNT_NOT_FOUND" => new BusinessException(
                    "RESOURCE_NOT_FOUND", "Customer account not found.", 404),
                _ => new BusinessException("CONFLICT", "Status could not be changed.", 409)
            };
        }
    }
}
