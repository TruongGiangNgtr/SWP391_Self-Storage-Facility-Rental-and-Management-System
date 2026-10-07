using Frms.Business.Models.Commands;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Interfaces;

public interface IRenewalService
{
    Task<RenewedContractRecord> RenewAsync(
        RenewContractCommand command,
        CancellationToken cancellationToken = default);
}