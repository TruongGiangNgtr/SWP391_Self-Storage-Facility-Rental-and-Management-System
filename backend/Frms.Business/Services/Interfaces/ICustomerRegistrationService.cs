using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface ICustomerRegistrationService
{
    Task<CustomerRegistrationResult> RegisterAsync(RegisterCustomerCommand command, CancellationToken cancellationToken);
}
