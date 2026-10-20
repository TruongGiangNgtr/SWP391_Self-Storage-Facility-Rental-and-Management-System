using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class PublicPolicyRepository(FrmsDbContext dbContext) : IPublicPolicyRepository
{
    public Task<Policy?> GetCurrentActiveAsync(CancellationToken cancellationToken) =>
        dbContext.Policies.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Status == "ACTIVE", cancellationToken);
}
