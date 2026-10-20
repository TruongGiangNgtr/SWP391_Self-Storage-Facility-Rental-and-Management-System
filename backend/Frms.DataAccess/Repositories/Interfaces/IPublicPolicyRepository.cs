using Frms.DataAccess.Persistence.Entities;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IPublicPolicyRepository
{
    Task<Policy?> GetCurrentActiveAsync(CancellationToken cancellationToken);
}
