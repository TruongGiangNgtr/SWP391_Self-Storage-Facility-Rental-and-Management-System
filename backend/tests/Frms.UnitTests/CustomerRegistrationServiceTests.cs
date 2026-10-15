using Frms.Business.Abstractions.Security;
using Frms.Business.Abstractions.Time;
using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Services.Implementations;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;

namespace Frms.UnitTests;

[TestFixture]
public sealed class CustomerRegistrationServiceTests
{
    [Test]
    public async Task Register_ValidPayload_NormalizesIdentifiersAndPersistsHashOnly()
    {
        var repository = new FakeRepository();
        var result = await CreateService(repository).RegisterAsync(
            new RegisterCustomerCommand(" Nguyen Van A ", " 0900000000 ",
                " Customer@Example.Com ", "ValidPassword!", " HCM ", null), default);

        Assert.Multiple(() =>
        {
            Assert.That(result.UserAccountId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.CustomerId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(repository.Registration, Is.Not.Null);
            Assert.That(repository.Registration!.Email, Is.EqualTo("customer@example.com"));
            Assert.That(repository.Registration.PhoneNumber, Is.EqualTo("0900000000"));
            Assert.That(repository.Registration.FullName, Is.EqualTo("Nguyen Van A"));
            Assert.That(repository.Registration.PasswordHash, Is.EqualTo("bcrypt-hash"));
            Assert.That(repository.Registration.CreatedAtUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
        });
    }

    [TestCase(RegistrationConflictIdentifier.Email, "EMAIL_ALREADY_EXISTS", 409)]
    [TestCase(RegistrationConflictIdentifier.PhoneNumber, "PHONE_NUMBER_ALREADY_EXISTS", 409)]
    [TestCase(RegistrationConflictIdentifier.Cccd, "VALIDATION_ERROR", 400)]
    public void Register_DuplicateIdentifier_ReturnsStableCode(
        RegistrationConflictIdentifier identifier, string code, int status)
    {
        var repo = new FakeRepository { Conflict = identifier };
        var error = Assert.ThrowsAsync<BusinessException>(() => CreateService(repo).RegisterAsync(
            new RegisterCustomerCommand("Customer", "0900000000", "test@example.com",
                "ValidPassword!", null, "001234567890"), default));
        Assert.Multiple(() =>
        {
            Assert.That(error!.Code, Is.EqualTo(code));
            Assert.That(error.SuggestedStatusCode, Is.EqualTo(status));
        });
    }

    [Test]
    public void Register_Over72Utf8PasswordBytes_IsRejectedWithoutPersistence()
    {
        var repo = new FakeRepository();
        var password = new string('ế', 40); // 40 characters, 120 UTF-8 bytes.
        var error = Assert.ThrowsAsync<BusinessException>(() => CreateService(repo).RegisterAsync(
            new RegisterCustomerCommand("Customer", "0900000000", "test@example.com",
                password, null, null), default));
        Assert.Multiple(() =>
        {
            Assert.That(error!.Code, Is.EqualTo("VALIDATION_ERROR"));
            Assert.That(repo.Registration, Is.Null);
        });
    }

    private static CustomerRegistrationService CreateService(FakeRepository repository) =>
        new(repository, new FakeHasher(), new FakeClock());

    private sealed class FakeRepository : ICustomerRegistrationRepository
    {
        public NewCustomerRegistration? Registration { get; private set; }
        public RegistrationConflictIdentifier? Conflict { get; init; }
        public Task CreateAsync(NewCustomerRegistration registration, CancellationToken cancellationToken)
        {
            if (Conflict is not null) throw new CustomerRegistrationConflictException(Conflict.Value);
            Registration = registration;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => "bcrypt-hash";
        public bool Verify(string password, string passwordHash) => throw new NotSupportedException();
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 10, 14, 0, 0, 0, TimeSpan.Zero);
        public TimeZoneInfo BusinessTimeZone => TimeZoneInfo.Utc;
        public DateTimeOffset ToBusinessTime(DateTimeOffset utcTimestamp) => utcTimestamp;
    }
}
