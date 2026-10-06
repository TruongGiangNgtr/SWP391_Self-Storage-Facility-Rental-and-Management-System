using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;

namespace Frms.Business.Services.Implementations;

internal sealed class ReservationNoShowService(
    IReservationNoShowRepository repository)
    : IReservationNoShowService
{
    public Task ProcessAsync(
        CancellationToken cancellationToken = default)
    {
        return repository.ProcessAsync(
            cancellationToken);
    }
}