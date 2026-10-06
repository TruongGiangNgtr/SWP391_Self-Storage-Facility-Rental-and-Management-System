using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;

namespace Frms.Business.Services.Implementations;

internal sealed class ReservationExpirationService(
    IReservationExpirationRepository repository)
    : IReservationExpirationService
{
    public Task ExpirePendingAsync(
        CancellationToken cancellationToken = default)
    {
        return repository.ExpirePendingAsync(
            cancellationToken);
    }
}