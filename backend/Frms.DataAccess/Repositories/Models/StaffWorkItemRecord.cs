namespace Frms.DataAccess.Repositories.Models;

public sealed record StaffWorkItemRecord(
    string WorkType,
    Guid ReferenceId,
    Guid? EntityId,
    DateOnly? ScheduledDate,
    string Status,
    Guid? CustomerId,
    string? CustomerName,
    string? CustomerPhone);
