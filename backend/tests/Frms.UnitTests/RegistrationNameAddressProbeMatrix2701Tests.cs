namespace Frms.UnitTests;

[TestFixture, Category("RegistrationProbe")]
public sealed class RegistrationNameAddressProbeMatrix2701Tests
{
    // Full name and address content: 100 generated probe cases (see TEST_CASES_3000.md).
    public static IEnumerable<TestCaseData> Cases()
    {
        yield return Row("TC_2701_name_invisible_zwsp", "FullName", "\u200B", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2702_name_invisible_three_zwsp", "FullName", "\u200B\u200B\u200B", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2703_name_invisible_zwnj", "FullName", "\u200C", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2704_name_invisible_zwj", "FullName", "\u200D", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2705_name_invisible_word_joiner", "FullName", "\u2060", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2706_name_invisible_bom", "FullName", "\uFEFF", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2707_name_invisible_mongolian_separator", "FullName", "\u180E", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2708_name_invisible_nbsp_then_zwsp", "FullName", "\u00A0\u200B", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2709_name_control_nul", "FullName", "Khoa\u0000", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2710_name_control_bel", "FullName", "Kh\u0007oa", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2711_name_control_esc", "FullName", "\u001BKhoa", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2712_name_control_del", "FullName", "Khoa\u007F", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2713_name_control_inner_tab", "FullName", "Kh\u0009oa", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2714_name_control_inner_newline", "FullName", "Kh\u000Aoa", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2715_name_control_inner_cr", "FullName", "Kh\u000Doa", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2716_name_nonletters_dots", "FullName", "...", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2717_name_nonletters_digits", "FullName", "12345", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2718_name_nonletters_bangs", "FullName", "!!!!", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2719_name_nonletters_symbols", "FullName", "@#$%", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2720_name_nonletters_hyphen", "FullName", "-", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2721_name_nonletters_underscore", "FullName", "_", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2722_name_payload_script_tag", "FullName", "<script>alert(1)</script>", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2723_name_payload_bold_tag", "FullName", "<b>Khoa</b>", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2724_name_payload_sql_drop", "FullName", "Khoa'; DROP TABLE Customers;--", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2725_name_payload_template", "FullName", "{{7*7}}", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2726_name_payload_jndi", "FullName", "${jndi:ldap://x}", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2727_name_payload_csv_equals", "FullName", "=cmd|' /C calc'!A0", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2728_name_payload_csv_plus", "FullName", "+1+1", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2729_name_payload_csv_at", "FullName", "@SUM(1+1)", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2730_name_payload_csv_minus", "FullName", "-2+3", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2731_name_nfd_nfd_nguyen", "FullName", "Nguye\u0302\u0303n Va\u0306n A\u0301", RegistrationProbe.Accept, "Nguy\u1EC5n V\u0103n \u00C1");
        yield return Row("TC_2732_name_nfd_nfd_tran", "FullName", "Tra\u0302\u0300n Thi\u0323 Be\u0301", RegistrationProbe.Accept, "Tr\u1EA7n Th\u1ECB B\u00E9");
        yield return Row("TC_2733_name_nfd_nfd_le", "FullName", "Le\u0302 Thi\u0323 Ngo\u0323c A\u0301nh", RegistrationProbe.Accept, "L\u00EA Th\u1ECB Ng\u1ECDc \u00C1nh");
        yield return Row("TC_2734_name_nfd_nfd_pham", "FullName", "Pha\u0323m Quy\u0300nh Anh", RegistrationProbe.Accept, "Ph\u1EA1m Qu\u1EF3nh Anh");
        yield return Row("TC_2735_name_nfd_nfd_do", "FullName", "\u0110o\u0302\u0303 Thi\u0323 Hu\u031Bo\u031Bng", RegistrationProbe.Accept, "\u0110\u1ED7 Th\u1ECB H\u01B0\u01A1ng");
        yield return Row("TC_2736_name_nfd_nfd_ho", "FullName", "Ho\u0302\u0300 Thi\u0323 My\u0303", RegistrationProbe.Accept, "H\u1ED3 Th\u1ECB M\u1EF9");
        yield return Row("TC_2737_name_double_space", "FullName", "Khoa  Nguyen", RegistrationProbe.Accept, "Khoa Nguyen");
        yield return Row("TC_2738_name_four_spaces", "FullName", "Khoa    Nguyen", RegistrationProbe.Accept, "Khoa Nguyen");
        yield return Row("TC_2739_name_inner_nbsp", "FullName", "Khoa\u00A0Nguyen", RegistrationProbe.Accept, "Khoa Nguyen");
        yield return Row("TC_2740_name_ok_viet_nguyen", "FullName", "Nguy\u1EC5n V\u0103n A", RegistrationProbe.Accept, "Nguy\u1EC5n V\u0103n A");
        yield return Row("TC_2741_name_ok_viet_tran", "FullName", "Tr\u1EA7n Th\u1ECB B", RegistrationProbe.Accept, "Tr\u1EA7n Th\u1ECB B");
        yield return Row("TC_2742_name_ok_apostrophe", "FullName", "O'Brien", RegistrationProbe.Accept, "O'Brien");
        yield return Row("TC_2743_name_ok_hyphen", "FullName", "Jean-Luc Picard", RegistrationProbe.Accept, "Jean-Luc Picard");
        yield return Row("TC_2744_name_ok_viet_long", "FullName", "\u0110\u1EB7ng Th\u1ECB H\u1ED3ng Nhung", RegistrationProbe.Accept, "\u0110\u1EB7ng Th\u1ECB H\u1ED3ng Nhung");
        yield return Row("TC_2745_name_ok_cjk", "FullName", "\u674E\u5C0F\u9F8D", RegistrationProbe.Accept, "\u674E\u5C0F\u9F8D");
        yield return Row("TC_2746_name_ok_spanish", "FullName", "Jos\u00E9 \u00D1and\u00FA", RegistrationProbe.Accept, "Jos\u00E9 \u00D1and\u00FA");
        yield return Row("TC_2747_name_ok_single_char", "FullName", "A", RegistrationProbe.Accept, "A");
        yield return Row("TC_2748_name_ok_len_150", "FullName", new string('N', 150), RegistrationProbe.Accept, new string('N', 150));
        yield return Row("TC_2749_name_ok_padded", "FullName", " Khoa ", RegistrationProbe.Accept, "Khoa");
        yield return Row("TC_2750_name_ok_two_surnames", "FullName", "Mary Jane Watson-Parker", RegistrationProbe.Accept, "Mary Jane Watson-Parker");
        yield return Row("TC_2751_name_ok_title", "FullName", "Dr. Khoa", RegistrationProbe.Accept, "Dr. Khoa");
        yield return Row("TC_2752_name_ok_len_150_multibyte", "FullName", new string('\u1EBF', 150), RegistrationProbe.Accept, new string('\u1EBF', 150));
        yield return Row("TC_2753_name_len_151_multibyte", "FullName", new string('\u1EBF', 151), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2754_name_emoji_grinning", "FullName", "Khoa \uD83D\uDE00", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2755_name_emoji_smile_only", "FullName", "\uD83D\uDE42", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2756_name_emoji_family_zwj", "FullName", "\uD83D\uDC68\u200D\uD83D\uDC69\u200D\uD83D\uDC67", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2757_name_dots_150", "FullName", new string('.', 150), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2758_name_ok_padded_150_spaces", "FullName", new string(' ', 150) + "A", RegistrationProbe.Accept, "A");
        yield return Row("TC_2759_name_len_151_with_space", "FullName", new string('A', 149) + " B", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2760_name_bidi_leading", "FullName", "\u202EKhoa", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2761_name_bidi_inner", "FullName", "Kho\u202Ea", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2762_name_replacement_char", "FullName", "Kho\uFFFDa", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2763_address_bidi_leading", "Address", "\u202EHCM", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2764_address_bidi_inner", "Address", "HC\u202EM", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2765_address_control_nul", "Address", "HCM\u0000", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2766_address_control_bel", "Address", "HCM\u0007", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2767_address_control_esc", "Address", "12 Le Loi\u001B", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2768_address_control_del", "Address", "HCM\u007F", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2769_address_payload_script_tag", "Address", "<script>alert(1)</script>", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2770_address_payload_img_onerror", "Address", "<img src=x onerror=alert(1)>", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2771_address_payload_sql_drop", "Address", "'; DROP TABLE Addresses;--", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2772_address_payload_template", "Address", "{{7*7}}", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2773_address_payload_csv_hyperlink", "Address", "=HYPERLINK(\"http://x\")", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2774_address_invisible_zwsp", "Address", "\u200B", RegistrationProbe.Accept, RegistrationProbe.NullStored);
        yield return Row("TC_2775_address_invisible_two_zwsp", "Address", "\u200B\u200B", RegistrationProbe.Accept, RegistrationProbe.NullStored);
        yield return Row("TC_2776_address_invisible_bom", "Address", "\uFEFF", RegistrationProbe.Accept, RegistrationProbe.NullStored);
        yield return Row("TC_2777_address_blank_empty", "Address", "", RegistrationProbe.Accept, RegistrationProbe.NullStored);
        yield return Row("TC_2778_address_blank_space", "Address", " ", RegistrationProbe.Accept, RegistrationProbe.NullStored);
        yield return Row("TC_2779_address_blank_tab", "Address", "\u0009", RegistrationProbe.Accept, RegistrationProbe.NullStored);
        yield return Row("TC_2780_address_blank_newline", "Address", "\u000A", RegistrationProbe.Accept, RegistrationProbe.NullStored);
        yield return Row("TC_2781_address_blank_mixed", "Address", "   \u0009  ", RegistrationProbe.Accept, RegistrationProbe.NullStored);
        yield return Row("TC_2782_address_blank_nbsp", "Address", "\u00A0", RegistrationProbe.Accept, RegistrationProbe.NullStored);
        yield return Row("TC_2783_address_ok_len_1000", "Address", new string('A', 1000), RegistrationProbe.Accept, new string('A', 1000));
        yield return Row("TC_2784_address_ok_len_999", "Address", new string('A', 999), RegistrationProbe.Accept, new string('A', 999));
        yield return Row("TC_2785_address_ok_len_1000_padded", "Address", " " + new string('A', 1000) + " ", RegistrationProbe.Accept, new string('A', 1000));
        yield return Row("TC_2786_address_len_1001", "Address", new string('A', 1001), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2787_address_len_1001_trailing_space", "Address", new string('A', 1001) + " ", RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2788_address_len_1002", "Address", new string('A', 1002), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2789_address_len_2000", "Address", new string('A', 2000), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2790_address_len_10000", "Address", new string('A', 10000), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2791_address_len_100000", "Address", new string('A', 100000), RegistrationProbe.Reject, RegistrationProbe.AnyStored);
        yield return Row("TC_2792_address_ok_hcm_full", "Address", "12 L\u00EA L\u1EE3i, Qu\u1EADn 1, TP.HCM", RegistrationProbe.Accept, "12 L\u00EA L\u1EE3i, Qu\u1EADn 1, TP.HCM");
        yield return Row("TC_2793_address_ok_short", "Address", "HCM", RegistrationProbe.Accept, "HCM");
        yield return Row("TC_2794_address_ok_slash", "Address", "123/4A Nguy\u1EC5n Hu\u1EC7", RegistrationProbe.Accept, "123/4A Nguy\u1EC5n Hu\u1EC7");
        yield return Row("TC_2795_address_ok_ward", "Address", "S\u1ED1 1, \u0110\u01B0\u1EDDng 2, P.3", RegistrationProbe.Accept, "S\u1ED1 1, \u0110\u01B0\u1EDDng 2, P.3");
        yield return Row("TC_2796_address_ok_hash", "Address", "Apt #5, 10 Main St.", RegistrationProbe.Accept, "Apt #5, 10 Main St.");
        yield return Row("TC_2797_address_ok_padded", "Address", "  HCM  ", RegistrationProbe.Accept, "HCM");
        yield return Row("TC_2798_address_nfd_le_loi", "Address", "Le\u0302 Lo\u031B\u0323i", RegistrationProbe.Accept, "L\u00EA L\u1EE3i");
        yield return Row("TC_2799_address_nfd_nguyen_hue", "Address", "Nguye\u0302\u0303n Hue\u0323\u0302", RegistrationProbe.Accept, "Nguy\u1EC5n Hu\u1EC7");
        yield return Row("TC_2800_address_nfd_dien_bien_phu", "Address", "\u0110ie\u0323\u0302n Bie\u0302n Phu\u0309", RegistrationProbe.Accept, "\u0110i\u1EC7n Bi\u00EAn Ph\u1EE7");
    }

    [TestCaseSource(nameof(Cases))]
    public Task Register_NameAndAddressProbe(string field, string? value, string expectation, string expectedStored)
        => RegistrationProbe.RunFieldProbe(field, value, expectation, expectedStored);

    private static TestCaseData Row(string name, string field, string? value, string expectation, string expectedStored)
        => new TestCaseData(field, value, expectation, expectedStored).SetName(name);
}
