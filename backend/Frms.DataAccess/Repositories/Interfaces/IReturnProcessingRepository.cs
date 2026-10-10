using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IReturnProcessingRepository
{
    Task<Visit?> GetVisitAsync(Guid visitId, CancellationToken ct);
    Task<Contract?> GetContractAsync(Guid contractId, CancellationToken ct);
    Task<Guid?> GetEmployeeIdAsync(Guid accountId, Guid facilityId, CancellationToken ct);
    Task<ActualReturnRecord> ConfirmAsync(Guid visitId, DateOnly date, Guid staffId, CancellationToken ct);
    Task<ReturnSettlementRecord> FinalizeAsync(Guid contractId, string unitStatus, Guid staffId, CancellationToken ct);
}
