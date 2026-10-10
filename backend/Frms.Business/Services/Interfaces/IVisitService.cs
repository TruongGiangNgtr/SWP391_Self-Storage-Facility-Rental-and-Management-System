using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface IVisitService
{
    Task<(IReadOnlyList<VisitResult> Items, int TotalItems)> ListOwnAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<VisitResult> GetOwnAsync(
        Guid visitId,
        CancellationToken cancellationToken = default);

    Task<VisitResult> RescheduleAsync(
        Guid visitId,
        DateOnly visitDate,
        CancellationToken cancellationToken = default);

    Task<VisitResult> CancelAsync(
        Guid visitId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<VisitResult> CheckInAsync(
        Guid visitId,
        CancellationToken cancellationToken = default);

    Task<VisitResult> GetByIdAsync(
        Guid visitId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<VisitResult> Items, int TotalItems)> ListAccessibleAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<VisitResult> CreateAccessAsync(
        Guid contractId,
        DateOnly visitDate,
        CancellationToken cancellationToken = default);

    Task<VisitResult> CheckOutAsync(
        Guid visitId,
        CancellationToken cancellationToken = default);
}
