using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Interfaces;

public interface IHandoverUnitAssignmentService {
    Task<HandoverUnitAssignmentRecord?> GetActiveAsync(Guid reservationId, CancellationToken cancellationToken = default);
    Task<HandoverUnitAssignmentRecord> AssignAsync(Guid reservationId, Guid storageUnitId, CancellationToken cancellationToken = default);
    Task ReleaseAsync(Guid reservationId, CancellationToken cancellationToken = default);
}
