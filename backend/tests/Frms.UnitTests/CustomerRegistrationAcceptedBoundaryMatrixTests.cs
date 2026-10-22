using Frms.Business.Abstractions.Security;
using Frms.Business.Abstractions.Time;
using Frms.Business.Models.Commands;
using Frms.Business.Services.Implementations;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;

namespace Frms.UnitTests;

[TestFixture, Category("CustomerRegistration")]
public sealed class CustomerRegistrationAcceptedBoundaryMatrixTests
{
    public static IEnumerable<TestCaseData> Cases()
    {
        for (var length = 1; length <= 30; length++)
            yield return Case("FullName", new string('N', length), $"accepted_name_length_{length}");
        for (var length = 1; length <= 30; length++)
            yield return Case("PhoneNumber", new string('9', length), $"accepted_phone_length_{length}");
        for (var length = 8; length <= 27; length++)
            yield return Case("Password", new string('a', length), $"accepted_password_length_{length}");
        for (var i = 0; i < 10; i++)
            yield return Case("Email", i % 2 == 0 ? $" USER{i}@EXAMPLE.TEST " : $"user{i}@Example.Test", $"accepted_normalized_email_{i}");
        for (var i = 0; i < 5; i++)
            yield return Case("Address", i == 0 ? " " : new string('A', i == 4 ? 1000 : i * 10), $"accepted_optional_address_{i}");
        for (var i = 0; i < 5; i++)
            yield return Case("Cccd", i == 0 ? " " : new string('7', i == 4 ? 20 : i * 4), $"accepted_optional_cccd_{i}");
    }

    [TestCaseSource(nameof(Cases))]
    public async Task Register_AcceptedBoundaryMatrixPersistsNormalizedValues(string field, string value)
    {
        var repository = new FakeRepository();
        var command = new RegisterCustomerCommand("Khoa", "0900000000", "khoa@example.test", "ValidPassword!", "HCM", "001234567890");
        command = field switch
        {
            "FullName" => command with { FullName = value },
            "PhoneNumber" => command with { PhoneNumber = value },
            "Password" => command with { Password = value },
            "Email" => command with { Email = value },
            "Address" => command with { Address = value },
            "Cccd" => command with { Cccd = value },
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        };

        await CreateService(repository).RegisterAsync(command, default);
        Assert.That(repository.Registration, Is.Not.Null);
        switch (field)
        {
            case "FullName": Assert.That(repository.Registration!.FullName, Is.EqualTo(value.Trim())); break;
            case "PhoneNumber": Assert.That(repository.Registration!.PhoneNumber, Is.EqualTo(value.Trim())); break;
            case "Password": Assert.That(repository.Registration!.PasswordHash, Is.EqualTo("test-hash")); break;
            case "Email": Assert.That(repository.Registration!.Email, Is.EqualTo(value.Trim().ToLowerInvariant())); break;
            case "Address": Assert.That(repository.Registration!.Address, Is.EqualTo(string.IsNullOrWhiteSpace(value) ? null : value.Trim())); break;
            case "Cccd": Assert.That(repository.Registration!.Cccd, Is.EqualTo(string.IsNullOrWhiteSpace(value) ? null : value.Trim())); break;
        }
    }

    private static CustomerRegistrationService CreateService(FakeRepository repository) => new(repository, new FakeHasher(), new FakeClock());
    private sealed class FakeRepository : ICustomerRegistrationRepository
    {
        public NewCustomerRegistration? Registration { get; private set; }
        public Task CreateAsync(NewCustomerRegistration registration, CancellationToken cancellationToken)
        { Registration = registration; return Task.CompletedTask; }
    }
    private sealed class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => "test-hash";
        public bool Verify(string password, string passwordHash) => false;
    }
    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 10, 22, 0, 0, 0, TimeSpan.Zero);
        public TimeZoneInfo BusinessTimeZone => TimeZoneInfo.Utc;
        public DateTimeOffset ToBusinessTime(DateTimeOffset utcTimestamp) => utcTimestamp;
    }
    private static TestCaseData Case(string field, string value, string name) => new TestCaseData(field, value).SetName(name);
}
