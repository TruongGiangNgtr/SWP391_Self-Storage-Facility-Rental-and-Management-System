using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Interfaces;

public interface ICustomerActivityService
{
    Task<CustomerActivityPage<CustomerNotificationRecord>> NotificationsAsync(int page, int pageSize, CancellationToken ct);
    Task<CustomerActivityPage<CustomerPaymentHistoryRecord>> PaymentsAsync(int page, int pageSize, CancellationToken ct);
}
