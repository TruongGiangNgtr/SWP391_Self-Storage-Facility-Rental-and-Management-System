using System.ComponentModel.DataAnnotations;
using System.Text;
using Frms.Business.Abstractions.Security;
using Frms.Business.Abstractions.Time;
using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.Business.Validators;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Implementations;

internal sealed class CustomerRegistrationService(
    ICustomerRegistrationRepository repository,
    IPasswordHasher passwordHasher,
    IClock clock) : ICustomerRegistrationService
{
    public async Task<CustomerRegistrationResult> RegisterAsync(
        RegisterCustomerCommand command, CancellationToken cancellationToken)
    {
        // Identifiers use the same normalization as existing AUTH-002/003 lookup.
        var name = command.FullName?.Trim() ?? string.Empty;
        var phone = command.PhoneNumber?.Trim() ?? string.Empty;
        var email = command.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var address = string.IsNullOrWhiteSpace(command.Address) ? null : command.Address.Trim();
        var cccd = CustomerCccdRules.NormalizeOptional(command.Cccd);
        var password = command.Password;

        if (cccd is not null && !CustomerCccdRules.IsValid(cccd))
            throw CustomerCccdRules.InvalidField();
        if (name.Length is < 1 or > 150 || phone.Length is < 1 or > 30 ||
            email.Length is < 1 or > 254 || !new EmailAddressAttribute().IsValid(email) ||
            password is null || password.Length is < 8 or > 64 || Encoding.UTF8.GetByteCount(password) > 72 ||
            address?.Length > 1000)
            throw new BusinessException("VALIDATION_ERROR", "Customer registration fields are invalid.", 400);

        var accountId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        // Only BCrypt hash crosses the service -> repository boundary.
        var registration = new NewCustomerRegistration(
            accountId, customerId, name, phone, email,
            passwordHasher.Hash(password), address, cccd, clock.UtcNow.UtcDateTime);

        try
        {
            await repository.CreateAsync(registration, cancellationToken);
        }
        catch (CustomerRegistrationConflictException conflict)
        {
            throw conflict.Identifier switch
            {
                RegistrationConflictIdentifier.Email =>
                    new BusinessException("EMAIL_ALREADY_EXISTS", "The email is already registered.", 409),
                RegistrationConflictIdentifier.PhoneNumber =>
                    new BusinessException("PHONE_NUMBER_ALREADY_EXISTS", "The phone number is already registered.", 409),
                // OD-10: same validation envelope and errors.cccd key for duplicates.
                RegistrationConflictIdentifier.Cccd => CustomerCccdRules.DuplicateField(),
                _ => new InvalidOperationException("Unexpected registration conflict.")
            };
        }

        return new CustomerRegistrationResult(accountId, customerId);
    }
}
