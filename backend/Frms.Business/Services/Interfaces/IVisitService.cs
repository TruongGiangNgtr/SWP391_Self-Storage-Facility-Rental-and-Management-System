using Frms.DataAccess.Persistence.Entities;

namespace Frms.Business.Services.Interfaces;

public interface IVisitService
{
    Task<(IReadOnlyList<Visit> Items, int TotalItems)> ListOwnAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Visit> GetOwnAsync(
        Guid visitId,
        CancellationToken cancellationToken = default);

    Task<Visit> RescheduleAsync(
        Guid visitId,
        DateOnly visitDate,
        CancellationToken cancellationToken = default);

    Task<Visit> CancelAsync(
        Guid visitId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<Visit> CheckInAsync(
        Guid visitId,
        CancellationToken cancellationToken = default);
}