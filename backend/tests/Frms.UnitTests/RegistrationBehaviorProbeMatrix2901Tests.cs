using System.Diagnostics;
using System.Globalization;
using Frms.Business.Exceptions;
using Frms.Business.Models.Commands;

namespace Frms.UnitTests;

[TestFixture, Category("RegistrationProbe")]
public sealed class RegistrationBehaviorProbeMatrix2901Tests
{
    // ---- Hasher output / failure ----
    public static IEnumerable<TestCaseData> HasherOutputCases()
    {
        yield return new TestCaseData("").SetName("TC_2901_hasher_returns_empty_string");
        yield return new TestCaseData(" ").SetName("TC_2902_hasher_returns_whitespace");
        yield return new TestCaseData(new object?[] { null }).SetName("TC_2903_hasher_returns_null");
    }

    [TestCaseSource(nameof(HasherOutputCases))]
    public void Hasher_ReturningNoHash_IsNeverPersisted(string? hash)
    {
        var repository = new ProbeRepository();
        var hasher = new ProbeHasher { HashToReturn = hash };
        var service = RegistrationProbe.Create(repository, hasher);
        Assert.CatchAsync(() => service.RegisterAsync(RegistrationProbe.ValidCommand(), default));
        Assert.That(repository.Registration, Is.Null);
    }

    public static IEnumerable<TestCaseData> HasherThrowsCases()
    {
        yield return new TestCaseData(new InvalidOperationException("hasher down")).SetName("TC_2904_hasher_throws_invalid_operation");
        yield return new TestCaseData(new OperationCanceledException("hasher cancelled")).SetName("TC_2905_hasher_throws_operation_cancelled");
    }

    [TestCaseSource(nameof(HasherThrowsCases))]
    public void Hasher_Exception_PropagatesAndNothingIsPersisted(Exception failure)
    {
        var repository = new ProbeRepository();
        var hasher = new ProbeHasher { ToThrow = failure };
        var service = RegistrationProbe.Create(repository, hasher);
        var caught = Assert.CatchAsync(() => service.RegisterAsync(RegistrationProbe.ValidCommand(), default));
        Assert.That(caught, Is.SameAs(failure));
        Assert.That(repository.Registration, Is.Null);
    }

    // ---- Repository exceptions ----
    public static IEnumerable<TestCaseData> RepositoryFailureCases()
    {
        yield return new TestCaseData(new InvalidOperationException("db down")).SetName("TC_2906_repository_throws_invalid_operation");
        yield return new TestCaseData(new TimeoutException("db timeout")).SetName("TC_2907_repository_throws_timeout");
        yield return new TestCaseData(new ArgumentException("bad argument")).SetName("TC_2908_repository_throws_argument");
        yield return new TestCaseData(new NotSupportedException("nope")).SetName("TC_2909_repository_throws_not_supported");
        yield return new TestCaseData(new OperationCanceledException("cancelled")).SetName("TC_2910_repository_throws_operation_cancelled");
        yield return new TestCaseData(new IOException("disk")).SetName("TC_2911_repository_throws_io");
        yield return new TestCaseData(new UnauthorizedAccessException("denied")).SetName("TC_2912_repository_throws_unauthorized");
        yield return new TestCaseData(new Exception("generic")).SetName("TC_2913_repository_throws_base_exception");
        yield return new TestCaseData(new KeyNotFoundException("missing")).SetName("TC_2914_repository_throws_key_not_found");
        yield return new TestCaseData(new DivideByZeroException("math")).SetName("TC_2915_repository_throws_divide_by_zero");
    }

    [TestCaseSource(nameof(RepositoryFailureCases))]
    public void Repository_UnexpectedException_PropagatesUnchanged(Exception failure)
    {
        var repository = new ProbeRepository { ToThrow = failure };
        var service = RegistrationProbe.Create(repository, new ProbeHasher());
        var caught = Assert.CatchAsync(() => service.RegisterAsync(RegistrationProbe.ValidCommand(), default));
        Assert.That(caught, Is.SameAs(failure));
    }

    // ---- Cancellation ----
    public static IEnumerable<TestCaseData> TokenCases()
    {
        yield return new TestCaseData(default(CancellationToken)).SetName("TC_2916_token_default_reaches_repository");
        yield return new TestCaseData(new CancellationTokenSource().Token).SetName("TC_2917_token_live_reaches_repository");
        yield return new TestCaseData(new CancellationToken(true)).SetName("TC_2918_token_cancelled_reaches_repository");
    }

    [TestCaseSource(nameof(TokenCases))]
    public async Task Repository_ReceivesCallerCancellationToken(CancellationToken token)
    {
        var repository = new ProbeRepository();
        var service = RegistrationProbe.Create(repository, new ProbeHasher());
        await service.RegisterAsync(RegistrationProbe.ValidCommand(), token);
        Assert.That(repository.LastToken, Is.EqualTo(token));
    }

    public static IEnumerable<TestCaseData> PreCancelledCases()
    {
        yield return new TestCaseData(0).SetName("TC_2919_precancelled_token_default_command");
        yield return new TestCaseData(1).SetName("TC_2920_precancelled_token_address_null");
        yield return new TestCaseData(2).SetName("TC_2921_precancelled_token_long_name");
    }

    [TestCaseSource(nameof(PreCancelledCases))]
    public void PreCancelledToken_ShouldNotHashOrPersist(int variant)
    {
        var repository = new ProbeRepository();
        var hasher = new ProbeHasher();
        var service = RegistrationProbe.Create(repository, hasher);
        Assert.CatchAsync<OperationCanceledException>(() => service.RegisterAsync(RegistrationProbe.Variant(variant), new CancellationToken(true)));
        Assert.That(repository.Registration, Is.Null);
    }

    // ---- Generated ids ----
    public static IEnumerable<TestCaseData> IdCountCases()
    {
        yield return new TestCaseData(2).SetName("TC_2922_ids_unique_over_2_registrations");
        yield return new TestCaseData(3).SetName("TC_2923_ids_unique_over_3_registrations");
        yield return new TestCaseData(4).SetName("TC_2924_ids_unique_over_4_registrations");
        yield return new TestCaseData(5).SetName("TC_2925_ids_unique_over_5_registrations");
        yield return new TestCaseData(6).SetName("TC_2926_ids_unique_over_6_registrations");
        yield return new TestCaseData(7).SetName("TC_2927_ids_unique_over_7_registrations");
        yield return new TestCaseData(8).SetName("TC_2928_ids_unique_over_8_registrations");
        yield return new TestCaseData(9).SetName("TC_2929_ids_unique_over_9_registrations");
        yield return new TestCaseData(10).SetName("TC_2930_ids_unique_over_10_registrations");
        yield return new TestCaseData(11).SetName("TC_2931_ids_unique_over_11_registrations");
    }

    [TestCaseSource(nameof(IdCountCases))]
    public async Task GeneratedIds_AreUniqueAndNonEmpty(int count)
    {
        var repository = new ProbeRepository();
        var service = RegistrationProbe.Create(repository, new ProbeHasher());
        for (var i = 0; i < count; i++)
            await service.RegisterAsync(RegistrationProbe.ValidCommand(), default);
        var all = repository.All;
        var accountIds = all.Select(r => RegistrationProbe.GuidProp(r, "account")).ToArray();
        var customerIds = all.Select(r => RegistrationProbe.GuidProp(r, "customer")).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(all, Has.Count.EqualTo(count));
            Assert.That(accountIds, Is.Unique);
            Assert.That(customerIds, Is.Unique);
            Assert.That(accountIds, Does.Not.Contain(Guid.Empty));
            Assert.That(customerIds, Does.Not.Contain(Guid.Empty));
            Assert.That(accountIds.Intersect(customerIds), Is.Empty);
        });
    }

    // ---- Timestamp ----
    public static IEnumerable<TestCaseData> ClockCases()
    {
        yield return new TestCaseData(2026, 10, 20, 0, 0).SetName("TC_2932_created_at_utc_baseline");
        yield return new TestCaseData(2026, 1, 1, 23, 420).SetName("TC_2933_created_at_plus7_new_year");
        yield return new TestCaseData(2026, 12, 31, 23, -720).SetName("TC_2934_created_at_minus12_year_end");
        yield return new TestCaseData(2024, 2, 29, 12, 840).SetName("TC_2935_created_at_plus14_leap_day");
        yield return new TestCaseData(2026, 3, 8, 2, -300).SetName("TC_2936_created_at_minus5_dst_day");
        yield return new TestCaseData(2000, 1, 1, 0, 0).SetName("TC_2937_created_at_year_2000");
        yield return new TestCaseData(2099, 12, 31, 23, 0).SetName("TC_2938_created_at_year_2099");
        yield return new TestCaseData(1970, 1, 1, 0, 0).SetName("TC_2939_created_at_epoch");
        yield return new TestCaseData(2026, 6, 15, 6, 330).SetName("TC_2940_created_at_plus530_india");
        yield return new TestCaseData(2026, 11, 1, 1, -420).SetName("TC_2941_created_at_minus7_november");
    }

    [TestCaseSource(nameof(ClockCases))]
    public async Task CreatedAt_IsClockUtcTime(int year, int month, int day, int hour, int offsetMinutes)
    {
        var clock = new ProbeClock { Now = new DateTimeOffset(year, month, day, hour, 0, 0, TimeSpan.FromMinutes(offsetMinutes)) };
        var repository = new ProbeRepository();
        var service = RegistrationProbe.Create(repository, new ProbeHasher(), clock);
        await service.RegisterAsync(RegistrationProbe.ValidCommand(), default);
        var created = RegistrationProbe.CreatedAt(repository.Registration!);
        Assert.That(created.Utc, Is.EqualTo(clock.Now.UtcDateTime));
        if (created.Kind is not null) Assert.That(created.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    // ---- Hash stored, plain password never stored ----
    public static IEnumerable<TestCaseData> PasswordStorageCases()
    {
        yield return new TestCaseData("ValidPassword!").SetName("TC_2942_stores_hash_only_default");
        yield return new TestCaseData("Aa1!Aa1!").SetName("TC_2943_stores_hash_only_mixed");
        yield return new TestCaseData("correct horse battery staple").SetName("TC_2944_stores_hash_only_passphrase");
        yield return new TestCaseData("M\u1EADt kh\u1EA9u m\u1EA1nh 123!").SetName("TC_2945_stores_hash_only_vietnamese");
        yield return new TestCaseData("Zx9#mK2$pL").SetName("TC_2946_stores_hash_only_random");
    }

    [TestCaseSource(nameof(PasswordStorageCases))]
    public async Task Registration_StoresHashNotPlainPassword(string password)
    {
        var repository = new ProbeRepository();
        var hasher = new ProbeHasher { HashToReturn = "stored-hash-value" };
        var service = RegistrationProbe.Create(repository, hasher);
        await service.RegisterAsync(RegistrationProbe.ValidCommand() with { Password = password }, default);
        var registration = repository.Registration!;
        Assert.Multiple(() =>
        {
            Assert.That(RegistrationProbe.StringProp(registration, "hash"), Is.EqualTo("stored-hash-value"));
            Assert.That(RegistrationProbe.AllStringValues(registration), Does.Not.Contain(password));
        });
    }

    // ---- Validation failure side effects ----
    public static IEnumerable<TestCaseData> SideEffectCases()
    {
        yield return new TestCaseData("FullName", "").SetName("TC_2947_invalid_name_empty_no_side_effects");
        yield return new TestCaseData("PhoneNumber", new string('9', 31)).SetName("TC_2948_invalid_phone_31_digits_no_side_effects");
        yield return new TestCaseData("Email", "not-an-email").SetName("TC_2949_invalid_email_no_at_no_side_effects");
        yield return new TestCaseData("Password", "short").SetName("TC_2950_invalid_password_short_no_side_effects");
        yield return new TestCaseData("Address", new string('A', 1001)).SetName("TC_2951_invalid_address_1001_no_side_effects");
        yield return new TestCaseData("Cccd", new string('7', 21)).SetName("TC_2952_invalid_cccd_21_digits_no_side_effects");
        yield return new TestCaseData("Email", "").SetName("TC_2953_invalid_email_empty_no_side_effects");
        yield return new TestCaseData("Password", (string?)null).SetName("TC_2954_invalid_password_null_no_side_effects");
        yield return new TestCaseData("FullName", (string?)null).SetName("TC_2955_invalid_name_null_no_side_effects");
        yield return new TestCaseData("PhoneNumber", (string?)null).SetName("TC_2956_invalid_phone_null_no_side_effects");
    }

    [TestCaseSource(nameof(SideEffectCases))]
    public void InvalidInput_DoesNotCallHasherOrRepository(string field, string? value)
    {
        var repository = new ProbeRepository();
        var hasher = new ProbeHasher();
        var service = RegistrationProbe.Create(repository, hasher);
        var command = RegistrationProbe.With(RegistrationProbe.ValidCommand(), field, value);
        Assert.ThrowsAsync<BusinessException>(() => service.RegisterAsync(command, default));
        Assert.Multiple(() =>
        {
            Assert.That(hasher.Calls, Is.EqualTo(0));
            Assert.That(repository.Calls, Is.EqualTo(0));
        });
    }

    public static IEnumerable<TestCaseData> MultiInvalidCases()
    {
        yield return new TestCaseData(63).SetName("TC_2957_all_invalid_mask_111111");
        yield return new TestCaseData(3).SetName("TC_2958_all_invalid_mask_000011");
        yield return new TestCaseData(48).SetName("TC_2959_all_invalid_mask_110000");
        yield return new TestCaseData(42).SetName("TC_2960_all_invalid_mask_101010");
        yield return new TestCaseData(21).SetName("TC_2961_all_invalid_mask_010101");
        yield return new TestCaseData(60).SetName("TC_2962_all_invalid_mask_111100");
    }

    [TestCaseSource(nameof(MultiInvalidCases))]
    public void SeveralInvalidFields_AreRejectedTogether(int mask)
    {
        var repository = new ProbeRepository();
        var hasher = new ProbeHasher();
        var service = RegistrationProbe.Create(repository, hasher);
        var command = RegistrationProbe.ValidCommand();
        if ((mask & 1) != 0) command = command with { FullName = "" };
        if ((mask & 2) != 0) command = command with { PhoneNumber = "" };
        if ((mask & 4) != 0) command = command with { Email = "" };
        if ((mask & 8) != 0) command = command with { Password = "" };
        if ((mask & 16) != 0) command = command with { Address = new string('A', 1001) };
        if ((mask & 32) != 0) command = command with { Cccd = new string('7', 21) };
        var error = Assert.ThrowsAsync<BusinessException>(() => service.RegisterAsync(command, default));
        Assert.Multiple(() =>
        {
            Assert.That(error!.Code, Is.EqualTo("VALIDATION_ERROR"));
            Assert.That(repository.Calls, Is.EqualTo(0));
        });
    }

    // ---- Null fields (reuses the field probe harness) ----
    public static IEnumerable<TestCaseData> NullFieldCases()
    {
        yield return new TestCaseData("FullName", (string?)null, RegistrationProbe.Reject, RegistrationProbe.AnyStored).SetName("TC_2963_null_name");
        yield return new TestCaseData("PhoneNumber", (string?)null, RegistrationProbe.Reject, RegistrationProbe.AnyStored).SetName("TC_2964_null_phone");
        yield return new TestCaseData("Email", (string?)null, RegistrationProbe.Reject, RegistrationProbe.AnyStored).SetName("TC_2965_null_email");
        yield return new TestCaseData("Password", (string?)null, RegistrationProbe.Reject, RegistrationProbe.AnyStored).SetName("TC_2966_null_password");
        yield return new TestCaseData("Address", (string?)null, RegistrationProbe.Accept, RegistrationProbe.NullStored).SetName("TC_2967_null_address");
        yield return new TestCaseData("Cccd", (string?)null, RegistrationProbe.Accept, RegistrationProbe.NullStored).SetName("TC_2968_null_cccd");
        yield return new TestCaseData("OptionalsNull", (string?)null, RegistrationProbe.Accept, RegistrationProbe.AnyStored).SetName("TC_2969_null_optionals_both");
        yield return new TestCaseData("AllNull", (string?)null, RegistrationProbe.Reject, RegistrationProbe.AnyStored).SetName("TC_2970_null_everything");
    }

    [TestCaseSource(nameof(NullFieldCases))]
    public Task NullFields_AreHandledAsEmpty(string field, string? value, string expectation, string expectedStored)
        => RegistrationProbe.RunFieldProbe(field, value, expectation, expectedStored);

    // ---- Result object ----
    public static IEnumerable<TestCaseData> ResultRunCases()
    {
        yield return new TestCaseData(1).SetName("TC_2971_result_ids_match_stored_run_1");
        yield return new TestCaseData(2).SetName("TC_2972_result_ids_match_stored_run_2");
        yield return new TestCaseData(3).SetName("TC_2973_result_ids_match_stored_run_3");
        yield return new TestCaseData(4).SetName("TC_2974_result_ids_match_stored_run_4");
        yield return new TestCaseData(5).SetName("TC_2975_result_ids_match_stored_run_5");
    }

    [TestCaseSource(nameof(ResultRunCases))]
    public async Task Result_ReturnsTheStoredIds(int runs)
    {
        var repository = new ProbeRepository();
        var service = RegistrationProbe.Create(repository, new ProbeHasher());
        for (var i = 0; i < runs; i++)
        {
            var result = await service.RegisterAsync(RegistrationProbe.ValidCommand(), default);
            var stored = repository.Registration!;
            Assert.Multiple(() =>
            {
                Assert.That(RegistrationProbe.GuidProp(result!, "account"), Is.EqualTo(RegistrationProbe.GuidProp(stored, "account")));
                Assert.That(RegistrationProbe.GuidProp(result!, "customer"), Is.EqualTo(RegistrationProbe.GuidProp(stored, "customer")));
            });
        }
    }

    // ---- Parallel registrations ----
    public static IEnumerable<TestCaseData> ParallelCases()
    {
        yield return new TestCaseData(2).SetName("TC_2976_parallel_2_registrations");
        yield return new TestCaseData(5).SetName("TC_2977_parallel_5_registrations");
        yield return new TestCaseData(10).SetName("TC_2978_parallel_10_registrations");
        yield return new TestCaseData(25).SetName("TC_2979_parallel_25_registrations");
        yield return new TestCaseData(50).SetName("TC_2980_parallel_50_registrations");
    }

    [TestCaseSource(nameof(ParallelCases))]
    public async Task ParallelRegistrations_AllSucceedWithUniqueIds(int count)
    {
        var repository = new ProbeRepository();
        var hasher = new ProbeHasher();
        var service = RegistrationProbe.Create(repository, hasher);
        await Task.WhenAll(Enumerable.Range(0, count).Select(_ => service.RegisterAsync(RegistrationProbe.ValidCommand(), default)));
        var accountIds = repository.All.Select(r => RegistrationProbe.GuidProp(r, "account")).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(repository.All, Has.Count.EqualTo(count));
            Assert.That(hasher.Calls, Is.EqualTo(count));
            Assert.That(accountIds, Is.Unique);
        });
    }

    // ---- Call counts ----
    public static IEnumerable<TestCaseData> HasherCountCases()
    {
        yield return new TestCaseData("ValidPassword!").SetName("TC_2981_hasher_called_once_default");
        yield return new TestCaseData("Aa1!Aa1!").SetName("TC_2982_hasher_called_once_min");
        yield return new TestCaseData(string.Concat(Enumerable.Repeat("Aa1!", 16))).SetName("TC_2983_hasher_called_once_max");
        yield return new TestCaseData("M\u1EADt kh\u1EA9u 123!").SetName("TC_2984_hasher_called_once_unicode");
        yield return new TestCaseData(" ValidPassword! ").SetName("TC_2985_hasher_called_once_padded");
    }

    [TestCaseSource(nameof(HasherCountCases))]
    public async Task ValidRegistration_HashesExactlyOnce(string password)
    {
        var hasher = new ProbeHasher();
        var service = RegistrationProbe.Create(new ProbeRepository(), hasher);
        await service.RegisterAsync(RegistrationProbe.ValidCommand() with { Password = password }, default);
        Assert.That(hasher.Calls, Is.EqualTo(1));
    }

    public static IEnumerable<TestCaseData> RepositoryCountCases()
    {
        yield return new TestCaseData(0).SetName("TC_2986_repository_called_once_default");
        yield return new TestCaseData(1).SetName("TC_2987_repository_called_once_address_null");
        yield return new TestCaseData(2).SetName("TC_2988_repository_called_once_cccd_null");
        yield return new TestCaseData(3).SetName("TC_2989_repository_called_once_name_150");
        yield return new TestCaseData(4).SetName("TC_2990_repository_called_once_email_upper");
    }

    [TestCaseSource(nameof(RepositoryCountCases))]
    public async Task ValidRegistration_PersistsExactlyOnce(int variant)
    {
        var repository = new ProbeRepository();
        var service = RegistrationProbe.Create(repository, new ProbeHasher());
        await service.RegisterAsync(RegistrationProbe.Variant(variant), default);
        Assert.That(repository.Calls, Is.EqualTo(1));
    }

    // ---- Huge inputs ----
    public static IEnumerable<TestCaseData> HugeInputCases()
    {
        yield return new TestCaseData("FullName").SetName("TC_2991_huge_input_fullname_1m_chars");
        yield return new TestCaseData("PhoneNumber").SetName("TC_2992_huge_input_phonenumber_1m_chars");
        yield return new TestCaseData("Email").SetName("TC_2993_huge_input_email_1m_chars");
        yield return new TestCaseData("Password").SetName("TC_2994_huge_input_password_1m_chars");
        yield return new TestCaseData("Address").SetName("TC_2995_huge_input_address_1m_chars");
        yield return new TestCaseData("Cccd").SetName("TC_2996_huge_input_cccd_1m_chars");
    }

    [TestCaseSource(nameof(HugeInputCases))]
    public void HugeInput_IsRejectedQuicklyWithoutHashing(string field)
    {
        var repository = new ProbeRepository();
        var hasher = new ProbeHasher();
        var service = RegistrationProbe.Create(repository, hasher);
        var command = RegistrationProbe.With(RegistrationProbe.ValidCommand(), field, new string('x', 1_000_000));
        var watch = Stopwatch.StartNew();
        Assert.ThrowsAsync<BusinessException>(() => service.RegisterAsync(command, default));
        watch.Stop();
        Assert.Multiple(() =>
        {
            Assert.That(hasher.Calls, Is.EqualTo(0));
            Assert.That(repository.Calls, Is.EqualTo(0));
            Assert.That(watch.ElapsedMilliseconds, Is.LessThan(2000));
        });
    }

    // ---- Culture ----
    public static IEnumerable<TestCaseData> CultureCases()
    {
        yield return new TestCaseData("tr-TR").SetName("TC_2997_email_lowercase_culture_tr_tr");
        yield return new TestCaseData("az-Latn-AZ").SetName("TC_2998_email_lowercase_culture_az_latn_az");
        yield return new TestCaseData("lt-LT").SetName("TC_2999_email_lowercase_culture_lt_lt");
        yield return new TestCaseData("en-US").SetName("TC_3000_email_lowercase_culture_en_us");
    }

    [TestCaseSource(nameof(CultureCases))]
    public async Task Email_LowerCasingIsCultureInvariant(string cultureName)
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultureName);
            var repository = new ProbeRepository();
            var service = RegistrationProbe.Create(repository, new ProbeHasher());
            await service.RegisterAsync(RegistrationProbe.ValidCommand() with { Email = "TITLE.INFO@EXAMPLE.TEST" }, default);
            Assert.That(RegistrationProbe.StringProp(repository.Registration!, "email"), Is.EqualTo("title.info@example.test"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
