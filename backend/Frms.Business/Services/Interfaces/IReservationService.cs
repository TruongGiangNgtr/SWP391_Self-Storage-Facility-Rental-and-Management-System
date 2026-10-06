using Frms.Business.Models.Commands;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Interfaces;

public interface IReservationService
{
    Task<CreatedReservationRecord> CreateAsync(
        CreateReservationCommand command,
        CancellationToken cancellationToken = default);
}