namespace Frms.UnitTests;

[TestFixture, Category("RegistrationProbe")]
public sealed class RegistrationPhoneCccdProbeMatrix2501Tests
{
    // Phone number and CCCD format: 100 generated probe cases (see TEST_CASES_3000.md).
    public static IEnumerable<TestCaseData> Cases()
    {
        yield return Row("TC_2501_phone_letters_only", "PhoneNumber", "abcdefghij", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2502_phone_trailing_letter", "PhoneNumber", "09000000a0", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2503_phone_letter_O_for_zero", "PhoneNumber", "090000000O", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2504_phone_dashes", "PhoneNumber", "0900-000-000", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2505_phone_parentheses", "PhoneNumber", "(090)0000000", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2506_phone_dots", "PhoneNumber", "0900.000.000", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2507_phone_inner_spaces", "PhoneNumber", "0900 000 000", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2508_phone_trailing_bang", "PhoneNumber", "090000000!", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2509_phone_words", "PhoneNumber", "phone-number", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2510_phone_fullwidth_digits", "PhoneNumber", "\uFF10\uFF19\uFF10\uFF10\uFF10\uFF10\uFF10\uFF10\uFF10\uFF10", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2511_phone_arabic_indic_digits", "PhoneNumber", "\u0660\u0669\u0660\u0660\u0660\u0660\u0660\u0660\u0660\u0660", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2512_phone_embedded_nul", "PhoneNumber", "09000000\u0000", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2513_phone_trailing_zwsp", "PhoneNumber", "0900000000\u200B", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2514_phone_slashes", "PhoneNumber", "0900/000/000", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2515_phone_digits_1", "PhoneNumber", "0", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2516_phone_digits_2", "PhoneNumber", "09", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2517_phone_digits_3", "PhoneNumber", "099", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2518_phone_digits_4", "PhoneNumber", "0999", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2519_phone_digits_5", "PhoneNumber", "09999", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2520_phone_digits_6", "PhoneNumber", "099999", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2521_phone_digits_7", "PhoneNumber", "0999999", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2522_phone_digits_8", "PhoneNumber", "09999999", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2523_phone_digits_9", "PhoneNumber", "099999999", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2524_phone_digits_16", "PhoneNumber", new string('9', 16), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2525_phone_digits_17", "PhoneNumber", new string('9', 17), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2526_phone_digits_18", "PhoneNumber", new string('9', 18), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2527_phone_digits_19", "PhoneNumber", new string('9', 19), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2528_phone_digits_20", "PhoneNumber", new string('9', 20), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2529_phone_digits_21", "PhoneNumber", new string('9', 21), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2530_phone_digits_22", "PhoneNumber", new string('9', 22), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2531_phone_digits_23", "PhoneNumber", new string('9', 23), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2532_phone_digits_24", "PhoneNumber", new string('9', 24), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2533_phone_digits_25", "PhoneNumber", new string('9', 25), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2534_phone_digits_26", "PhoneNumber", new string('9', 26), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2535_phone_digits_27", "PhoneNumber", new string('9', 27), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2536_phone_digits_28", "PhoneNumber", new string('9', 28), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2537_phone_digits_29", "PhoneNumber", new string('9', 29), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2538_phone_digits_30", "PhoneNumber", new string('9', 30), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2539_phone_ok_mobile_090", "PhoneNumber", "0900000000", RegistrationProbe.Accept, "0900000000");
        yield return Row("TC_2540_phone_ok_mobile_091", "PhoneNumber", "0912345678", RegistrationProbe.Accept, "0912345678");
        yield return Row("TC_2541_phone_ok_mobile_098", "PhoneNumber", "0987654321", RegistrationProbe.Accept, "0987654321");
        yield return Row("TC_2542_phone_ok_mobile_032", "PhoneNumber", "0321234567", RegistrationProbe.Accept, "0321234567");
        yield return Row("TC_2543_phone_ok_mobile_056", "PhoneNumber", "0561234567", RegistrationProbe.Accept, "0561234567");
        yield return Row("TC_2544_phone_ok_mobile_070", "PhoneNumber", "0701234567", RegistrationProbe.Accept, "0701234567");
        yield return Row("TC_2545_phone_ok_mobile_081", "PhoneNumber", "0811234567", RegistrationProbe.Accept, "0811234567");
        yield return Row("TC_2546_phone_ok_trim_spaces", "PhoneNumber", " 0900000000 ", RegistrationProbe.Accept, "0900000000");
        yield return Row("TC_2547_phone_ok_trim_tab_newline", "PhoneNumber", "\u00090900000000\u000A", RegistrationProbe.Accept, "0900000000");
        yield return Row("TC_2548_phone_ok_trim_nbsp", "PhoneNumber", "\u00A00900000000\u00A0", RegistrationProbe.Accept, "0900000000");
        yield return Row("TC_2549_phone_semicolon", "PhoneNumber", "0900000000;", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2550_phone_comma", "PhoneNumber", "0900000000,", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2551_phone_two_numbers_slash", "PhoneNumber", "0900000000/0911111111", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2552_phone_extension_word", "PhoneNumber", "0900000000 ext 12", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2553_phone_extension_x", "PhoneNumber", "0900000000x12", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2554_phone_two_numbers_space", "PhoneNumber", "0900000000 0911111111", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2555_cccd_letter_suffix", "Cccd", "00123456789A", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2556_cccd_letters_only", "Cccd", "ABCDEFGHIJKL", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2557_cccd_bang_suffix", "Cccd", "00123456789!", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2558_cccd_dashes", "Cccd", "0012-3456-7890", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2559_cccd_inner_spaces", "Cccd", "001 234 567 890", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2560_cccd_fullwidth_digits", "Cccd", "\uFF10\uFF10\uFF11\uFF12\uFF13\uFF14\uFF15\uFF16\uFF17\uFF18\uFF19\uFF10", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2561_cccd_embedded_nul", "Cccd", "00123456789\u0000", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2562_cccd_letter_O", "Cccd", "00123456789O", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2563_cccd_short_letters", "Cccd", "abc", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2564_cccd_single_dash", "Cccd", "-", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2565_cccd_dots", "Cccd", "........", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2566_cccd_trailing_semicolon", "Cccd", "001234567890;", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2567_cccd_digits_1", "Cccd", new string('1', 1), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2568_cccd_digits_2", "Cccd", new string('1', 2), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2569_cccd_digits_3", "Cccd", new string('1', 3), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2570_cccd_digits_4", "Cccd", new string('1', 4), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2571_cccd_digits_5", "Cccd", new string('1', 5), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2572_cccd_digits_6", "Cccd", new string('1', 6), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2573_cccd_digits_7", "Cccd", new string('1', 7), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2574_cccd_digits_8", "Cccd", new string('1', 8), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2575_cccd_digits_10", "Cccd", new string('1', 10), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2576_cccd_digits_11", "Cccd", new string('1', 11), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2577_cccd_digits_13", "Cccd", new string('1', 13), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2578_cccd_digits_14", "Cccd", new string('1', 14), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2579_cccd_digits_15", "Cccd", new string('1', 15), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2580_cccd_digits_16", "Cccd", new string('1', 16), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2581_cccd_digits_17", "Cccd", new string('1', 17), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2582_cccd_digits_18", "Cccd", new string('1', 18), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2583_cccd_digits_19", "Cccd", new string('1', 19), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2584_cccd_digits_20", "Cccd", new string('1', 20), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2585_cccd_ok_hanoi_12", "Cccd", "001234567890", RegistrationProbe.Accept, "001234567890");
        yield return Row("TC_2586_cccd_ok_hcm_12", "Cccd", "079123456789", RegistrationProbe.Accept, "079123456789");
        yield return Row("TC_2587_cccd_ok_cantho_12", "Cccd", "092123456789", RegistrationProbe.Accept, "092123456789");
        yield return Row("TC_2588_cccd_ok_legacy_9a", "Cccd", "001234567", RegistrationProbe.Accept, "001234567");
        yield return Row("TC_2589_cccd_ok_legacy_9b", "Cccd", "123456789", RegistrationProbe.Accept, "123456789");
        yield return Row("TC_2590_cccd_ok_trim_spaces", "Cccd", " 001234567890 ", RegistrationProbe.Accept, "001234567890");
        yield return Row("TC_2591_cccd_ok_trim_tab_newline", "Cccd", "\u0009001234567890\u000A", RegistrationProbe.Accept, "001234567890");
        yield return Row("TC_2592_cccd_ok_null_is_optional", "Cccd", (string?)null, RegistrationProbe.Accept, RegistrationProbe.NullStored);
        yield return Row("TC_2593_cccd_ok_empty_is_optional", "Cccd", "", RegistrationProbe.Accept, RegistrationProbe.NullStored);
        yield return Row("TC_2594_cccd_ok_whitespace_is_optional", "Cccd", "   ", RegistrationProbe.Accept, RegistrationProbe.NullStored);
        yield return Row("TC_2595_cccd_prefix_000", "Cccd", "000123456789", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2596_cccd_prefix_097", "Cccd", "097123456789", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2597_cccd_prefix_099", "Cccd", "099123456789", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2598_cccd_prefix_100", "Cccd", "100123456789", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2599_cccd_all_nine", "Cccd", "999999999999", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2600_cccd_all_zero", "Cccd", "000000000000", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
    }

    [TestCaseSource(nameof(Cases))]
    public Task Register_PhoneAndCccdProbe(string field, string? value, string expectation, string expectedStored)
        => RegistrationProbe.RunFieldProbe(field, value, expectation, expectedStored);

    private static TestCaseData Row(string name, string field, string? value, string expectation, string expectedStored)
        => new TestCaseData(field, value, expectation, expectedStored).SetName(name);
}
