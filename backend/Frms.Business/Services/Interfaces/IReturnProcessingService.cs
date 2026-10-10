using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface IReturnProcessingService
{
    Task<ConfirmedReturnResult> ConfirmAsync(Guid visitId, DateOnly actualReturnDate,
        CancellationToken ct = default);
    Task<FinalizedReturnResult> FinalizeAsync(Guid contractId, string storageUnitStatus,
        CancellationToken ct = default);
}
