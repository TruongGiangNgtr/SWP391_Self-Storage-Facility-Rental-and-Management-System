using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IHandoverUnitAssignmentRepository {
    Task<Reservation?> GetReservationAsync(Guid reservationId, CancellationToken cancellationToken);
    Task<HandoverUnitAssignmentRecord?> GetActiveAsync(Guid reservationId, CancellationToken cancellationToken);
    Task AssignAsync(Guid reservationId, Guid storageUnitId, Guid userAccountId, CancellationToken cancellationToken);
    Task ReleaseAsync(Guid reservationId, Guid userAccountId, CancellationToken cancellationToken);
}
