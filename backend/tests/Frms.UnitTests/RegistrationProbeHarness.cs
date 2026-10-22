using System.Collections.Concurrent;
using System.Reflection;
using Frms.Business.Abstractions.Security;
using Frms.Business.Abstractions.Time;
using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;
using Frms.Business.Services.Implementations;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;

namespace Frms.UnitTests;

// Shared helpers for the RegistrationProbe test matrices (TC_2501 - TC_3000).
// This file contains no test cases itself.
internal sealed class ProbeRepository : ICustomerRegistrationRepository
{
    private readonly ConcurrentQueue<NewCustomerRegistration> _items = new();
    private int _calls;

    public Exception? ToThrow { get; set; }
    public CancellationToken LastToken { get; private set; }
    public int Calls => Volatile.Read(ref _calls);
    public NewCustomerRegistration? Registration => _items.LastOrDefault();
    public IReadOnlyCollection<NewCustomerRegistration> All => _items.ToArray();

    public Task CreateAsync(NewCustomerRegistration registration, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _calls);
        LastToken = cancellationToken;
        if (ToThrow is not null) throw ToThrow;
        _items.Enqueue(registration);
        return Task.CompletedTask;
    }
}

internal sealed class ProbeHasher : IPasswordHasher
{
    private int _calls;

    public string? HashToReturn { get; set; } = "test-hash";
    public Exception? ToThrow { get; set; }
    public string? LastPassword { get; private set; }
    public int Calls => Volatile.Read(ref _calls);

    public string Hash(string password)
    {
        Interlocked.Increment(ref _calls);
        LastPassword = password;
        if (ToThrow is not null) throw ToThrow;
        return HashToReturn!;
    }

    public bool Verify(string password, string passwordHash) => false;
}

internal sealed class ProbeClock : IClock
{
    public DateTimeOffset Now { get; set; } = new(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
    public DateTimeOffset UtcNow => Now;
    public TimeZoneInfo BusinessTimeZone => TimeZoneInfo.Utc;
    public DateTimeOffset ToBusinessTime(DateTimeOffset utcTimestamp) => utcTimestamp;
}

internal static class RegistrationProbe
{
    public const string Reject = "Reject";
    public const string Accept = "Accept";
    public const string AnyStored = "<ANY>";
    public const string NullStored = "<NULL>";

    private static readonly Dictionary<string, string> StoredKeyword = new()
    {
        ["FullName"] = "name",
        ["PhoneNumber"] = "phone",
        ["Email"] = "email",
        ["Address"] = "address",
        ["Cccd"] = "cccd"
    };

    public static RegisterCustomerCommand ValidCommand() =>
        new("Khoa", "0900000000", "khoa@example.test", "ValidPassword!", "HCM", "001234567890");

    // 0 default, 1 no address, 2 no cccd, 3 150-char name, 4 upper-case email
    public static RegisterCustomerCommand Variant(int variant) => variant switch
    {
        0 => ValidCommand(),
        1 => ValidCommand() with { Address = null! },
        2 => ValidCommand() with { Cccd = null! },
        3 => ValidCommand() with { FullName = new string('N', 150) },
        4 => ValidCommand() with { Email = "KHOA@EXAMPLE.TEST" },
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
    };

    public static CustomerRegistrationService Create(ProbeRepository repository, ProbeHasher hasher, ProbeClock? clock = null) =>
        new(repository, hasher, clock ?? new ProbeClock());

    public static RegisterCustomerCommand With(RegisterCustomerCommand command, string field, string? value) => field switch
    {
        "FullName" => command with { FullName = value! },
        "PhoneNumber" => command with { PhoneNumber = value! },
        "Email" => command with { Email = value! },
        "Password" => command with { Password = value! },
        "Address" => command with { Address = value! },
        "Cccd" => command with { Cccd = value! },
        "PasswordEqualsEmail" => command with { Password = command.Email! },
        "PasswordEqualsEmailUpper" => command with { Password = command.Email!.ToUpperInvariant() },
        "PasswordEqualsPhone" => command with { Password = command.PhoneNumber! },
        "PasswordEqualsCccd" => command with { Password = command.Cccd! },
        "PasswordEqualsName" => command with { FullName = value!, Password = value! },
        "OptionalsNull" => command with { Address = null!, Cccd = null! },
        "AllNull" => new RegisterCustomerCommand(null!, null!, null!, null!, null!, null!),
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
    };

    public static async Task RunFieldProbe(string field, string? value, string expectation, string expectedStored)
    {
        var repository = new ProbeRepository();
        var hasher = new ProbeHasher();
        var service = Create(repository, hasher);
        var command = With(ValidCommand(), field, value);

        if (expectation == Reject)
        {
            var error = Assert.ThrowsAsync<BusinessException>(() => service.RegisterAsync(command, default));
            Assert.Multiple(() =>
            {
                Assert.That(error!.Code, Is.EqualTo("VALIDATION_ERROR"));
                Assert.That(repository.Registration, Is.Null, "registration must not be persisted");
                Assert.That(hasher.Calls, Is.EqualTo(0), "password must not be hashed for rejected input");
            });
            return;
        }

        await service.RegisterAsync(command, default);
        Assert.That(repository.Registration, Is.Not.Null, "registration must be persisted");

        if (field == "Password")
        {
            Assert.That(hasher.LastPassword, Is.EqualTo(value), "hasher must receive the password exactly as typed");
        }
        else if (expectedStored != AnyStored && StoredKeyword.TryGetValue(field, out var keyword))
        {
            var expected = expectedStored == NullStored ? null : expectedStored;
            Assert.That(StringProp(repository.Registration!, keyword), Is.EqualTo(expected));
        }
    }

    // ---- reflection helpers (property names of NewCustomerRegistration / result are found by keyword) ----

    private static PropertyInfo[] Props(object target) =>
        target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

    private static InvalidOperationException Missing(object target, string keyword) =>
        new($"Probe harness: no matching property containing '{keyword}' on {target.GetType().Name}. " +
            $"Properties: {string.Join(", ", Props(target).Select(p => p.Name + ":" + p.PropertyType.Name))}");

    public static string? StringProp(object target, string keyword)
    {
        var property = Props(target).FirstOrDefault(p =>
            p.PropertyType == typeof(string) && p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        if (property is null) throw Missing(target, keyword);
        return (string?)property.GetValue(target);
    }

    public static IEnumerable<string?> AllStringValues(object target) =>
        Props(target).Where(p => p.PropertyType == typeof(string)).Select(p => (string?)p.GetValue(target)).ToArray();

    public static Guid GuidProp(object target, string keyword)
    {
        var property = Props(target).FirstOrDefault(p =>
            p.PropertyType == typeof(Guid) && p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        if (property is null) throw Missing(target, keyword);
        return (Guid)property.GetValue(target)!;
    }

    public static (DateTime Utc, DateTimeKind? Kind) CreatedAt(object target)
    {
        var property = Props(target).FirstOrDefault(p =>
            (p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTimeOffset)) &&
            p.Name.Contains("created", StringComparison.OrdinalIgnoreCase));
        if (property is null) throw Missing(target, "created");
        var value = property.GetValue(target)!;
        return value is DateTimeOffset offset
            ? (offset.UtcDateTime, null)
            : ((DateTime)value, ((DateTime)value).Kind);
    }
}
