using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface ICustomerRegistrationRepository
{
    Task CreateAsync(NewCustomerRegistration registration, CancellationToken cancellationToken);
}
