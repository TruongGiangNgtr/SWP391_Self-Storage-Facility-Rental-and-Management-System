namespace Frms.DataAccess.Repositories.Interfaces;

public interface IReservationExpirationRepository
{
    Task ExpirePendingAsync(
        CancellationToken cancellationToken);
}