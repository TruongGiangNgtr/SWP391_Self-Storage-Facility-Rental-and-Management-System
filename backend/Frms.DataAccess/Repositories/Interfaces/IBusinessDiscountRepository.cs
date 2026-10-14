using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IBusinessDiscountRepository
{
    Task<BusinessDiscountRecord?> CreateAsync(Guid customerId, string name, decimal percent,
        DateTime fromUtc, DateTime? toUtc, CancellationToken ct);
    Task<BusinessDiscountRecord?> UpdateAsync(Guid discountId, string name, decimal percent,
        string status, DateTime fromUtc, DateTime? toUtc, CancellationToken ct);
}
