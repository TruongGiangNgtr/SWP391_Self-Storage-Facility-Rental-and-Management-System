namespace Frms.DataAccess.Repositories.Models;

public sealed record HandoverContractRecord(
    Guid ContractId,
    string Status,
    Guid StorageUnitId,
    DateOnly StartMonth,
    DateOnly EndMonth);

public sealed record CompletedHandoverRecord(
    HandoverContractRecord Contract,
    string ReservationStatus,
    string VisitStatus,
    string StorageUnitStatus);