using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IHandoverRepository
{
    Task<Reservation?> GetReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken);

    Task<CompletedHandoverRecord> CompleteAsync(
        Guid reservationId,
        Guid visitId,
        Guid storageUnitId,
        Guid firstMonthPaymentId,
        Guid? discountId,
        CancellationToken cancellationToken);
}