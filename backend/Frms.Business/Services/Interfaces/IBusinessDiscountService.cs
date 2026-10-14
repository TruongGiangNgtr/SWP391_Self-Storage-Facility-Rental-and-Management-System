using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Interfaces;

public interface IBusinessDiscountService
{
    Task<BusinessDiscountRecord> CreateAsync(Guid customerId, string name, decimal percentage,
        DateTimeOffset effectiveFrom, DateTimeOffset? effectiveTo, CancellationToken ct);
    Task<BusinessDiscountRecord> UpdateAsync(Guid id, string name, decimal percentage,
        string status, DateTimeOffset effectiveFrom, DateTimeOffset? effectiveTo, CancellationToken ct);
}
