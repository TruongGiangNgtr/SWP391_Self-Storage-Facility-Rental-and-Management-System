using Frms.Business.Models.Commands;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Interfaces;

public interface IHandoverService
{
    Task<CompletedHandoverRecord> CompleteAsync(
        CompleteHandoverCommand command,
        CancellationToken cancellationToken = default);
}