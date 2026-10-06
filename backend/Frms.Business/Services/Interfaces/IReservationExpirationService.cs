namespace Frms.Business.Services.Interfaces;

public interface IReservationExpirationService
{
    Task ExpirePendingAsync(
        CancellationToken cancellationToken = default);
}