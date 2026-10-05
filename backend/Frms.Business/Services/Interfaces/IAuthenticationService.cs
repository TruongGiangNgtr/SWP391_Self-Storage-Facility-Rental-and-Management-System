using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface IAuthenticationService
{
    Task<AuthenticationResult> LoginCustomerAsync(CustomerLoginCommand command, CancellationToken cancellationToken);
    Task<AuthenticationResult> LoginEmployeeAsync(EmployeeLoginCommand command, CancellationToken cancellationToken);
    Task<CurrentAccountResult> GetCurrentAccountAsync(Guid userAccountId, CancellationToken cancellationToken);
    Task<bool> IsActiveAsync(Guid userAccountId, string role, CancellationToken cancellationToken);
}
