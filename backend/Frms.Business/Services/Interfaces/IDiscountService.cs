using Frms.Business.Models;

namespace Frms.Business.Services.Interfaces;

public interface IDiscountService {
    Task<(IReadOnlyList<DiscountResult> Items, int TotalCount)>
        ListAccessibleAsync(
            Guid customerId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
}
