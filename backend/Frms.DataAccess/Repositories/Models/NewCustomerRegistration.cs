namespace Frms.DataAccess.Repositories.Models;

/// <summary>Validated registration data; contains a BCrypt hash, not a plaintext password.</summary>
public sealed record NewCustomerRegistration(
    Guid UserAccountId,
    Guid CustomerId,
    string FullName,
    string PhoneNumber,
    string Email,
    string PasswordHash,
    string? Address,
    string? Cccd,
    DateTime CreatedAtUtc);

public enum RegistrationConflictIdentifier { Email, PhoneNumber, Cccd }

public sealed class CustomerRegistrationConflictException(RegistrationConflictIdentifier identifier)
    : Exception("A unique Customer registration identifier already exists.")
{
    public RegistrationConflictIdentifier Identifier { get; } = identifier;
}
