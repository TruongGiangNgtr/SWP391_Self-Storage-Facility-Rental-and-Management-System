using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IReturnVisitRepository
{
    Task<ReturnContractRecord?> GetContractAsync(Guid contractId, CancellationToken ct);
    Task<ReturnVisitRecord?> GetOwnedVisitAsync(Guid customerId, Guid visitId, CancellationToken ct);
    Task<ReturnVisitRecord> CreateAsync(Guid accountId, Guid customerId, Guid contractId,
        DateOnly visitDate, DateTimeOffset nowUtc, CancellationToken ct);
    Task<ReturnVisitRecord> RescheduleAsync(Guid accountId, Guid customerId, Guid visitId,
        DateOnly visitDate, DateTimeOffset nowUtc, CancellationToken ct);
    Task<ContractReturnRecord?> GetSummaryAsync(Guid contractId, BillingReadScope scope, CancellationToken ct);
}
