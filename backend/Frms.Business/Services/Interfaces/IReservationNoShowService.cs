namespace Frms.Business.Services.Interfaces;

public interface IReservationNoShowService
{
    Task ProcessAsync(
        CancellationToken cancellationToken = default);
}