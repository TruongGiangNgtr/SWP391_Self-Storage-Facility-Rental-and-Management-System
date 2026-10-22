namespace Frms.UnitTests;

[TestFixture, Category("RegistrationProbe")]
public sealed class RegistrationPasswordProbeMatrix2801Tests
{
    // Password policy: 100 generated probe cases (see TEST_CASES_3000.md).
    public static IEnumerable<TestCaseData> Cases()
    {
        yield return Row("TC_2801_password_ws_spaces_8", "Password", "        ", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2802_password_ws_spaces_64", "Password", "                                                                ", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2803_password_ws_tabs_8", "Password", "\u0009\u0009\u0009\u0009\u0009\u0009\u0009\u0009", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2804_password_ws_nbsp_8", "Password", "\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2805_password_ws_mixed_ws", "Password", " \u0009 \u0009 \u0009 \u0009", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2806_password_common_password", "Password", "password", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2807_password_common_12345678", "Password", "12345678", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2808_password_common_123456789", "Password", "123456789", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2809_password_common_1234567890", "Password", "1234567890", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2810_password_common_qwertyui", "Password", "qwertyui", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2811_password_common_qwertyuiop", "Password", "qwertyuiop", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2812_password_common_abcdefgh", "Password", "abcdefgh", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2813_password_common_11111111", "Password", "11111111", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2814_password_common_00000000", "Password", "00000000", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2815_password_common_aaaaaaaa", "Password", "aaaaaaaa", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2816_password_common_password1", "Password", "password1", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2817_password_common_password1", "Password", "Password1", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2818_password_common_iloveyou", "Password", "iloveyou", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2819_password_common_admin123", "Password", "admin123", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2820_password_common_letmein1", "Password", "letmein1", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2821_password_common_welcome1", "Password", "welcome1", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2822_password_common_abc12345", "Password", "abc12345", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2823_password_common_12341234", "Password", "12341234", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2824_password_common_987654321", "Password", "987654321", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2825_password_common_password123", "Password", "password123", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2826_password_common_passw0rd", "Password", "Passw0rd", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2827_password_common_patssw0rd", "Password", "P@ssw0rd", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2828_password_equals_email", "PasswordEqualsEmail", (string?)null, RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2829_password_equals_phone", "PasswordEqualsPhone", (string?)null, RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2830_password_equals_cccd", "PasswordEqualsCccd", (string?)null, RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2831_password_equals_name", "PasswordEqualsName", "KhoaNguyenVan", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2832_password_equals_email_upper", "PasswordEqualsEmailUpper", (string?)null, RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2833_password_control_nul_inside", "Password", "pass\u0000word1", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2834_password_control_all_nul", "Password", "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2835_password_control_nul_trailing", "Password", "password\u0000", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2836_password_control_bel_inside", "Password", "pass\u0007word", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2837_password_control_esc_inside", "Password", "pass\u001Bword", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2838_password_control_del_trailing", "Password", "ValidPass\u007F", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2839_password_ok_min_8", "Password", "Aa1!Aa1!", RegistrationProbe.Accept, RegistrationProbe.AnyStored);
        yield return Row("TC_2840_password_ok_max_64", "Password", string.Concat(Enumerable.Repeat("Aa1!", 16)), RegistrationProbe.Accept, RegistrationProbe.AnyStored);
        yield return Row("TC_2841_password_ok_utf8_72_bytes", "Password", string.Concat(Enumerable.Repeat("\u1EBF\u1EC1\u1EC3\u1EC5\u1EC7\u1EA5", 4)), RegistrationProbe.Accept, RegistrationProbe.AnyStored);
        yield return Row("TC_2842_password_ok_default", "Password", "ValidPassword!", RegistrationProbe.Accept, RegistrationProbe.AnyStored);
        yield return Row("TC_2843_password_ok_padded_not_trimmed", "Password", " ValidPassword! ", RegistrationProbe.Accept, RegistrationProbe.AnyStored);
        yield return Row("TC_2844_password_ok_vietnamese", "Password", "M\u1EADt kh\u1EA9u m\u1EA1nh 123!", RegistrationProbe.Accept, RegistrationProbe.AnyStored);
        yield return Row("TC_2845_password_ok_passphrase", "Password", "correct horse battery staple", RegistrationProbe.Accept, RegistrationProbe.AnyStored);
        yield return Row("TC_2846_password_ok_mixed", "Password", "Tr0ub4dor&3xyz", RegistrationProbe.Accept, RegistrationProbe.AnyStored);
        yield return Row("TC_2847_password_len_7", "Password", "Aa1!Aa1", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2848_password_len_65", "Password", string.Concat(Enumerable.Repeat("Aa1!", 16)) + "x", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2849_password_utf8_73_bytes_a", "Password", string.Concat(Enumerable.Repeat("\u1EBF\u1EC1\u1EC3\u1EC5\u1EC7\u1EA5", 4)) + "a", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2850_password_utf8_73_bytes_b", "Password", string.Concat(Enumerable.Repeat("\u1EBF\u1EC1\u1EC3\u1EC5\u1EC7\u1EA5", 4)).Substring(0, 23) + "aaaa", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2851_password_ok_emoji_72_bytes", "Password", string.Concat(Enumerable.Repeat("\uD83D\uDE00", 18)), RegistrationProbe.Accept, RegistrationProbe.AnyStored);
        yield return Row("TC_2852_password_emoji_76_bytes", "Password", string.Concat(Enumerable.Repeat("\uD83D\uDE00", 19)), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2853_password_oneclass_abcdefghijkl", "Password", "abcdefghijkl", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2854_password_oneclass_abcdefghijkl", "Password", "ABCDEFGHIJKL", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2855_password_oneclass_123456789012", "Password", "123456789012", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2856_password_oneclass_symbols", "Password", "!@#$%^&*()_+", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2857_password_oneclass_aaaaaaaaaaaa", "Password", "aaaaaaaaaaaa", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2858_password_oneclass_abababababab", "Password", "abababababab", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2859_password_oneclass_zzzzzzzzzzzz", "Password", "zzzzzzzzzzzz", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2860_password_oneclass_kkkkkkkkkkkk", "Password", "kkkkkkkkkkkk", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2861_password_pattern_abcd1234", "Password", "abcd1234", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2862_password_pattern_qwerty123", "Password", "qwerty123", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2863_password_pattern_asdfghjkl", "Password", "asdfghjkl", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2864_password_pattern_zxcvbnm1", "Password", "zxcvbnm1", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2865_password_pattern_1q2w3e4r", "Password", "1q2w3e4r", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2866_password_pattern_qazwsxedc", "Password", "qazwsxedc", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2867_password_pattern_12345678a", "Password", "12345678a", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2868_password_pattern_a1234567", "Password", "a1234567", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2869_password_year_summer2024", "Password", "Summer2024", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2870_password_year_winter2025", "Password", "Winter2025", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2871_password_year_spring2026", "Password", "Spring2026", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2872_password_year_autumn2026", "Password", "Autumn2026", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2873_password_year_password2026", "Password", "Password2026", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2874_password_year_welcome2026", "Password", "Welcome2026", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2875_password_year_admin2026", "Password", "Admin2026", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2876_password_year_qwerty2026", "Password", "Qwerty2026", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2877_password_word_mypassword", "Password", "MyPassword!9", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2878_password_word_admin_wrapped", "Password", "xxAdminxx99", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2879_password_word_leet_password", "Password", "Passw0rd!!!!", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2880_password_word_letmein", "Password", "letmein!!!!", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2881_password_word_digits_tail", "Password", "123456!!!!!!", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2882_password_word_admin_digits", "Password", "Admin@123456", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2883_password_vn_matkhau123", "Password", "matkhau123", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2884_password_vn_matkhau1234", "Password", "matkhau1234", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2885_password_vn_123456abc", "Password", "123456abc", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2886_password_vn_vietnam123", "Password", "vietnam123", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2887_password_vn_hanoi12345", "Password", "hanoi12345", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2888_password_vn_saigon123", "Password", "saigon123", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2889_password_vn_toiyeuvietnam", "Password", "toiyeuvietnam", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2890_password_repeat_abcabcabc1", "Password", "abcabcabc1", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2891_password_repeat_xyzxyzxyz1", "Password", "xyzxyzxyz1", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2892_password_repeat_1212121212", "Password", "1212121212", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2893_password_repeat_9898989898", "Password", "9898989898", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2894_password_ok_strong_1", "Password", "Zx9#mK2$pL", RegistrationProbe.Accept, RegistrationProbe.AnyStored);
        yield return Row("TC_2895_password_ok_strong_2", "Password", "4Qw!8nR@2v", RegistrationProbe.Accept, RegistrationProbe.AnyStored);
        yield return Row("TC_2896_password_ok_strong_3", "Password", "gT7&hB3*yU1", RegistrationProbe.Accept, RegistrationProbe.AnyStored);
        yield return Row("TC_2897_password_ok_strong_4", "Password", "Mx5^qP9%wE", RegistrationProbe.Accept, RegistrationProbe.AnyStored);
        yield return Row("TC_2898_password_ok_strong_5", "Password", "vN8!cD4#sA", RegistrationProbe.Accept, RegistrationProbe.AnyStored);
        yield return Row("TC_2899_password_ok_strong_6", "Password", "Lp3$kJ6&bH", RegistrationProbe.Accept, RegistrationProbe.AnyStored);
        yield return Row("TC_2900_password_ok_strong_7", "Password", "Rt2@fG7!nY", RegistrationProbe.Accept, RegistrationProbe.AnyStored);
    }

    [TestCaseSource(nameof(Cases))]
    public Task Register_PasswordProbe(string field, string? value, string expectation, string expectedStored)
        => RegistrationProbe.RunFieldProbe(field, value, expectation, expectedStored);

    private static TestCaseData Row(string name, string field, string? value, string expectation, string expectedStored)
        => new TestCaseData(field, value, expectation, expectedStored).SetName(name);
}
