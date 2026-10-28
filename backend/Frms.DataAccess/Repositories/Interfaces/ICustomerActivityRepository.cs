using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface ICustomerActivityRepository
{
    Task<Guid?> ResolveActiveCustomerIdAsync(Guid userAccountId, CancellationToken cancellationToken);
    Task<CustomerActivityPage<CustomerNotificationRecord>> ListInAppNotificationsAsync(
        Guid userAccountId, long skip, int take, CancellationToken cancellationToken);
    Task<CustomerActivityPage<CustomerPaymentHistoryRecord>> ListPaymentsAsync(
        Guid customerId, long skip, int take, CancellationToken cancellationToken);
}
