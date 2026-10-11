using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface IReturnVisitService
{
    Task<VisitResult> CreateAsync(Guid contractId, DateOnly visitDate, CancellationToken ct);
    Task<VisitResult> RescheduleAsync(Guid visitId, DateOnly visitDate, CancellationToken ct);
    Task<VisitResult> CancelAsync(Guid visitId, string reason, CancellationToken ct);
    Task<ContractReturnSummaryResult> GetSummaryAsync(Guid contractId, CancellationToken ct);
}
