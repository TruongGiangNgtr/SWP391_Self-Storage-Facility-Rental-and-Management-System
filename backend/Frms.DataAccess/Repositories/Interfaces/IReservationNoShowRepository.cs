namespace Frms.DataAccess.Repositories.Interfaces;

public interface IReservationNoShowRepository
{
    Task ProcessAsync(
        CancellationToken cancellationToken);
}