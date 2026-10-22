namespace Frms.UnitTests;

[TestFixture, Category("RegistrationProbe")]
public sealed class RegistrationEmailProbeMatrix2601Tests
{
    // Email format: 100 generated probe cases (see TEST_CASES_3000.md).
    public static IEnumerable<TestCaseData> Cases()
    {
        yield return Row("TC_2601_email_double_dot_local", "Email", "a..b@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2602_email_leading_dot_local", "Email", ".user@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2603_email_trailing_dot_local", "Email", "user.@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2604_email_leading_dot_domain", "Email", "user@.example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2605_email_double_dot_domain", "Email", "user@example..test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2606_email_trailing_dot_domain", "Email", "user@example.test.", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2607_email_hyphen_leading_label", "Email", "user@-example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2608_email_hyphen_trailing_label", "Email", "user@example-.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2609_email_hyphen_leading_tld", "Email", "user@example.-test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2610_email_underscore_domain", "Email", "user@exam_ple.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2611_email_only_dots_local", "Email", "..@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2612_email_double_dot_start_domain", "Email", "user@..test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2613_email_local_lparen", "Email", "us(er@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2614_email_local_rparen", "Email", "us)er@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2615_email_local_comma", "Email", "us,er@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2616_email_local_colon", "Email", "us:er@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2617_email_local_semicolon", "Email", "us;er@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2618_email_local_lt", "Email", "us<er@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2619_email_local_gt", "Email", "us>er@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2620_email_local_lbracket", "Email", "us[er@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2621_email_local_rbracket", "Email", "us]er@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2622_email_local_backslash", "Email", "us\\er@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2623_email_local_dquote", "Email", "us\"er@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2624_email_local_del", "Email", "us\u007Fer@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2625_email_local_soh", "Email", "us\u0001er@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2626_email_angle_only", "Email", "<user@example.test>", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2627_email_display_name", "Email", "User <user@example.test>", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2628_email_mailto", "Email", "mailto:user@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2629_email_trailing_angle", "Email", "user@example.test>", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2630_email_quoted_display_name", "Email", "\"User\" <user@example.test>", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2631_email_trailing_comment", "Email", "user@example.test (comment)", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2632_email_domain_localhost", "Email", "user@localhost", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2633_email_domain_no_tld", "Email", "user@example", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2634_email_domain_numeric_labels", "Email", "user@1.2.3", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2635_email_domain_ip_literal", "Email", "user@[127.0.0.1]", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2636_email_domain_one_char_tld", "Email", "user@example.t", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2637_email_domain_numeric_tld", "Email", "user@example.123", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2638_email_domain_query_string", "Email", "user@example.test?x=1", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2639_email_domain_path_suffix", "Email", "user@example.test/path", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2640_email_domain_fragment", "Email", "user@example.test#frag", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2641_email_domain_port_suffix", "Email", "user@example.test:8080", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2642_email_domain_trailing_slash", "Email", "user@example.test/", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2643_email_domain_bare_ip", "Email", "user@127.0.0.1", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2644_email_local_len_65", "Email", new string('a', 65) + "@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2645_email_local_len_80", "Email", new string('a', 80) + "@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2646_email_local_len_120", "Email", new string('a', 120) + "@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2647_email_local_len_200", "Email", new string('a', 200) + "@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2648_email_label_len_64", "Email", "user@" + new string('b', 64) + ".test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2649_email_label_len_70", "Email", "user@" + new string('b', 70) + ".test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2650_email_u_umlaut", "Email", "\u00FCser@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2651_email_cjk_local", "Email", "\u7528\u6237@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2652_email_umlaut_domain", "Email", "user@ex\u00E4mple.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2653_email_cjk_domain", "Email", "user@\u4F8B\u3048.\u30C6\u30B9\u30C8", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2654_email_e_acute", "Email", "us\u00E9r@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2655_email_fullwidth_at", "Email", "user\uFF20example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2656_email_zwsp_in_local", "Email", "user\u200B@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2657_email_trailing_zwsp", "Email", "user@example.test\u200B", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2658_email_nul_in_local", "Email", "us\u0000er@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2659_email_bel_in_domain", "Email", "user@exam\u0007ple.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2660_email_nbsp_before_at", "Email", "user\u00A0@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2661_email_ok_plain", "Email", "user@example.test", RegistrationProbe.Accept, "user@example.test");
        yield return Row("TC_2662_email_ok_upper", "Email", "USER@EXAMPLE.TEST", RegistrationProbe.Accept, "user@example.test");
        yield return Row("TC_2663_email_ok_mixed_case", "Email", "User.Name@Example.Test", RegistrationProbe.Accept, "user.name@example.test");
        yield return Row("TC_2664_email_ok_plus_tag", "Email", "user+tag@example.test", RegistrationProbe.Accept, "user+tag@example.test");
        yield return Row("TC_2665_email_ok_underscore_local", "Email", "user_name@example.test", RegistrationProbe.Accept, "user_name@example.test");
        yield return Row("TC_2666_email_ok_hyphen_local", "Email", "user-name@example.test", RegistrationProbe.Accept, "user-name@example.test");
        yield return Row("TC_2667_email_ok_subdomain", "Email", "user.name@sub.example.test", RegistrationProbe.Accept, "user.name@sub.example.test");
        yield return Row("TC_2668_email_ok_short", "Email", "u@e.co", RegistrationProbe.Accept, "u@e.co");
        yield return Row("TC_2669_email_ok_digits", "Email", "user123@example123.test", RegistrationProbe.Accept, "user123@example123.test");
        yield return Row("TC_2670_email_ok_country_tld", "Email", "first.last@example.co.uk", RegistrationProbe.Accept, "first.last@example.co.uk");
        yield return Row("TC_2671_email_ok_padded", "Email", "  user@example.test  ", RegistrationProbe.Accept, "user@example.test");
        yield return Row("TC_2672_email_ok_local_64", "Email", new string('a', 64) + "@example.test", RegistrationProbe.Accept, new string('a', 64) + "@example.test");
        yield return Row("TC_2673_email_ok_label_63", "Email", "user@" + new string('b', 63) + ".test", RegistrationProbe.Accept, "user@" + new string('b', 63) + ".test");
        yield return Row("TC_2674_email_ok_len_254", "Email", new string('a', 64) + "@" + new string('b', 63) + "." + new string('b', 63) + "." + new string('c', 56) + ".test", RegistrationProbe.Accept, new string('a', 64) + "@" + new string('b', 63) + "." + new string('b', 63) + "." + new string('c', 56) + ".test");
        yield return Row("TC_2675_email_ok_apostrophe", "Email", "user'quote@example.test", RegistrationProbe.Accept, "user'quote@example.test");
        yield return Row("TC_2676_email_len_255", "Email", new string('a', 64) + "@" + new string('b', 63) + "." + new string('b', 63) + "." + new string('c', 57) + ".test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2677_email_len_300", "Email", new string('a', 287) + "@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2678_email_len_1000", "Email", new string('a', 987) + "@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2679_email_len_100000", "Email", new string('a', 99987) + "@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2680_email_domain_char_bang", "Email", "user@exa!mple.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2681_email_domain_char_hash", "Email", "user@exa#mple.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2682_email_domain_char_dollar", "Email", "user@exa$mple.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2683_email_domain_char_percent", "Email", "user@exa%mple.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2684_email_domain_char_caret", "Email", "user@exa^mple.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2685_email_domain_char_amp", "Email", "user@exa&mple.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2686_email_domain_char_star", "Email", "user@exa*mple.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2687_email_domain_char_lparen", "Email", "user@exa(mple.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2688_email_domain_char_rparen", "Email", "user@exa)mple.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2689_email_domain_char_eq", "Email", "user@exa=mple.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2690_email_domain_char_plus", "Email", "user@exa+mple.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2691_email_domain_char_lbrace", "Email", "user@exa{mple.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2692_email_domain_char_rbrace", "Email", "user@exa}mple.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2693_email_domain_char_pipe", "Email", "user@exa|mple.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2694_email_multi_comma", "Email", "a@example.test,b@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2695_email_multi_space", "Email", "a@example.test b@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2696_email_multi_semicolon", "Email", "a@example.test;b@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2697_email_multi_newline", "Email", "a@example.test\u000Ab@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2698_email_multi_comma_space", "Email", "a@example.test, b@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2699_email_multi_pipe", "Email", "a@example.test|b@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2700_email_multi_slash", "Email", "a@example.test/b@example.test", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
    }

    [TestCaseSource(nameof(Cases))]
    public Task Register_EmailProbe(string field, string? value, string expectation, string expectedStored)
        => RegistrationProbe.RunFieldProbe(field, value, expectation, expectedStored);

    private static TestCaseData Row(string name, string field, string? value, string expectation, string expectedStored)
        => new TestCaseData(field, value, expectation, expectedStored).SetName(name);
}
