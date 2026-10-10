namespace Frms.Business.Models.Results;

public sealed record StaffWorkItemResult(
    string WorkType,
    Guid ReferenceId,
    Guid? EntityId,
    DateOnly? ScheduledDate,
    string Status,
    Guid? CustomerId,
    string? CustomerName,
    string? CustomerPhone);

public sealed record StaffWorkItemPageResult(
    IReadOnlyList<StaffWorkItemResult> Items,
    int Page,
    int PageSize,
    int TotalItems);
