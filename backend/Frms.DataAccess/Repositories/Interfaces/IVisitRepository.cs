using Frms.DataAccess.Persistence.Entities;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IVisitRepository
{
    Task<Guid?> GetCustomerIdByUserAccountIdAsync(
        Guid userAccountId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Visit>> ListOwnedAsync(
        Guid customerId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<int> CountOwnedAsync(
        Guid customerId,
        CancellationToken cancellationToken);

    Task<Visit?> GetOwnedByIdAsync(
        Guid customerId,
        Guid visitId,
        CancellationToken cancellationToken);

    Task<Reservation?> GetOwnedReservationForVisitAsync(
        Guid customerId,
        Guid visitId,
        CancellationToken cancellationToken);

    Task<Policy?> GetPolicyAsync(
        Guid policyId,
        CancellationToken cancellationToken);

    Task UpdateVisitDateAsync(
        Guid visitId,
        DateOnly visitDate,
        CancellationToken cancellationToken);

    Task<Visit> CancelAsync(
        Guid customerId,
        Guid visitId,
        string reason,
        CancellationToken cancellationToken);

    Task<Employee?> GetEmployeeByUserAccountIdAsync(
        Guid userAccountId,
        CancellationToken cancellationToken);

    Task<Visit?> GetByIdAsync(
        Guid visitId,
        CancellationToken cancellationToken);

    Task<Reservation?> GetReservationForVisitAsync(
        Guid visitId,
        CancellationToken cancellationToken);

    Task<Visit> CheckInAsync(
        Guid visitId,
        Guid employeeId,
        CancellationToken cancellationToken);

    Task<Contract?> GetOwnedContractAsync(
        Guid customerId,
        Guid contractId,
        CancellationToken cancellationToken);

    Task<Contract?> GetOwnedContractForVisitAsync(
        Guid customerId,
        Guid visitId,
        CancellationToken cancellationToken);

    Task<bool> HasPendingReturnVisitAsync(
        Guid contractId,
        CancellationToken cancellationToken);

    Task<Visit> CreateAccessAsync(
        Guid contractId,
        DateOnly visitDate,
        CancellationToken cancellationToken);

    Task<Contract?> GetContractForVisitAsync(
        Guid visitId,
        CancellationToken cancellationToken);

    Task<bool> TryCheckOutAccessAsync(
        Guid visitId,
        CancellationToken cancellationToken);
}