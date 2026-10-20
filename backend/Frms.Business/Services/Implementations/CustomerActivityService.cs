using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Implementations;

internal sealed class CustomerActivityService(
    ICustomerActivityRepository repository, ICurrentUserContext currentUser) : ICustomerActivityService
{
    public async Task<CustomerActivityPage<CustomerNotificationRecord>> NotificationsAsync(
        int page, int pageSize, CancellationToken ct)
    {
        var (accountId, _) = await AuthorizedCustomerAsync(page, pageSize, ct);
        return await repository.ListSentNotificationsAsync(accountId, ((long)page - 1) * pageSize, pageSize, ct);
    }

    public async Task<CustomerActivityPage<CustomerPaymentHistoryRecord>> PaymentsAsync(
        int page, int pageSize, CancellationToken ct)
    {
        var (_, customerId) = await AuthorizedCustomerAsync(page, pageSize, ct);
        return await repository.ListPaymentsAsync(customerId, ((long)page - 1) * pageSize, pageSize, ct);
    }

    private async Task<(Guid AccountId, Guid CustomerId)> AuthorizedCustomerAsync(
        int page, int pageSize, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            throw new BusinessException("UNAUTHORIZED", "Authentication is required.", 401);
        if (currentUser.Role != "CUSTOMER")
            throw new BusinessException("FORBIDDEN", "Only Customers may view their own activity.", 403);
        if (page < 1 || pageSize is < 1 or > 100 || page > int.MaxValue / pageSize)
            throw new BusinessException("VALIDATION_ERROR", "Invalid pagination parameters.", 400);
        var accountId = currentUser.UserAccountId;
        var id = await repository.ResolveActiveCustomerIdAsync(accountId, ct);
        if (id is null)
            throw new BusinessException("FORBIDDEN", "An active Customer account is required.", 403);
        return (accountId, id.Value);
    }
}
