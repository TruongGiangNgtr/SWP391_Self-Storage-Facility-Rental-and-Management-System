using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IReservationRepository {
    Task<Guid?> GetCustomerIdByUserAccountIdAsync(
        Guid userAccountId,
        CancellationToken cancellationToken);

    Task<CreatedReservationRecord> CreateAsync(
        Guid customerId,
        Guid facilityId,
        Guid unitTypeId,
        DateOnly startMonth,
        DateOnly endMonth,
        CancellationToken cancellationToken);

    Task<ReservationPageRecord> ListByCustomerAsync(
        Guid customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ReservationPageRecord> ListByFacilityAsync(
        Guid facilityId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ReservationPageRecord> ListAllAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ReservationDetailRecord?> GetByIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken);

    Task<ConfirmedReservationRecord> ConfirmAsync(
        Guid customerId,
        Guid reservationId,
        DateOnly reservationVisitDate,
        CancellationToken cancellationToken);
}
