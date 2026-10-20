using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IBusinessPortfolioRepository
{
    Task<BusinessPortfolioRecord> GetSnapshotAsync(CancellationToken cancellationToken);
}
