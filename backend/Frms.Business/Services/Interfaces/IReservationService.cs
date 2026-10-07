using Frms.Business.Models.Commands;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Interfaces;

public interface IReservationService {
    Task<CreatedReservationRecord> CreateAsync(
        CreateReservationCommand command,
        CancellationToken cancellationToken = default);

    Task<ReservationPageRecord> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ReservationDetailRecord> GetByIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<ConfirmedReservationRecord> ConfirmAsync(
        ConfirmReservationCommand command,
        CancellationToken cancellationToken = default);
}
