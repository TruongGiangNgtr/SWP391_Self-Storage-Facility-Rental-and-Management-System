using System.Text.Json;
using System.Text.RegularExpressions;

namespace Frms.Business.Services.Implementations;

public sealed record AiStoragePolicyItem(string Id, string Title, IReadOnlyList<string> Examples);
public sealed record AiStorageLegalReference(string DocumentNumber, string Title, string OfficialUrl);
public sealed record AiStoragePolicyView(
    string PolicyKey, string ApprovalStatus, string Version,
    string Title, string Summary, string ProhibitedWarning, string NeedsReviewWarning,
    IReadOnlyList<AiStoragePolicyItem> Prohibited,
    IReadOnlyList<AiStoragePolicyItem> Allowed,
    IReadOnlyList<AiStorageLegalReference> LegalReferences);

public enum AiStorageDecision { Prohibited, Allowed, NeedsReview }

/// <summary>
/// Version-controlled, preliminary FRMS item acceptance rules. This is an
/// operational input screen, not a determination of legality or ownership.
/// </summary>
public sealed class AiStorageItemsCatalog
{
    public AiStoragePolicyView PublicView { get; }

    private AiStorageItemsCatalog(AiStoragePolicyView view) => PublicView = view;

    public static AiStorageItemsCatalog Load(string filePath)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(filePath));
        var root = doc.RootElement;
        if (root.GetProperty("schemaVersion").GetInt32() != 1 ||
            Text(root, "policyKey") != "facility-rules-prohibited-items")
            throw new InvalidDataException("Invalid AI storage item rule version or policy key.");

        var approval = Text(root, "approvalStatus");
        if (approval is not ("APPROVED" or "DRAFT_REQUIRES_LEGAL_AND_BUSINESS_APPROVAL"))
            throw new InvalidDataException("Unknown AI storage rule approval status.");

        var notice = root.GetProperty("publicNotice");
        var refs = root.GetProperty("legalReferences").EnumerateArray()
            .Select(x => new { Id = Text(x, "id"), Value = new AiStorageLegalReference(
                Text(x, "documentNumber"), Text(x, "title"), Text(x, "officialUrl")) })
            .ToArray();
        var refIds = refs.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        if (refs.Length == 0 || refIds.Count != refs.Length || refs.Any(x =>
                !Uri.TryCreate(x.Value.OfficialUrl, UriKind.Absolute, out var uri) ||
                uri.Scheme != "https"))
            throw new InvalidDataException("Invalid legal reference list.");

        var ids = new HashSet<string>(StringComparer.Ordinal);
        AiStoragePolicyItem[] ReadGroup(string name)
        {
            return root.GetProperty(name).EnumerateArray().Select(x =>
            {
                var id = Text(x, "id");
                if (!ids.Add(id)) throw new InvalidDataException("Duplicate item policy rule ID.");
                var examples = x.GetProperty("examples").EnumerateArray()
                    .Select(v => v.GetString()?.Trim() ?? "").ToArray();
                if (examples.Length == 0 || examples.Any(string.IsNullOrWhiteSpace))
                    throw new InvalidDataException("Invalid item policy examples.");
                if (name == "PROHIBITED" && x.GetProperty("legalReferenceIds").EnumerateArray()
                        .Any(v => !refIds.Contains(v.GetString() ?? "")))
                    throw new InvalidDataException("Unresolved policy legal reference ID.");
                return new AiStoragePolicyItem(id, Text(x, "title"), examples);
            }).ToArray();
        }
        var prohibited = ReadGroup("PROHIBITED");
        var allowed = ReadGroup("ALLOWED");
        if (prohibited.Length == 0 || allowed.Length == 0)
            throw new InvalidDataException("Both PROHIBITED and ALLOWED rules are required.");

        return new AiStorageItemsCatalog(new AiStoragePolicyView(
            Text(root, "policyKey"), approval, Text(root, "version"),
            Text(notice, "title"), Text(notice, "summary"),
            Text(notice, "prohibitedWarning"), Text(notice, "needsReviewWarning"),
            prohibited, allowed, refs.Select(x => x.Value).ToArray()));
    }

    public AiStorageDecision Assess(string description)
    {
        var input = Normalize(description);
        var positiveProhibited = false;
        var ambiguous = false;
        foreach (var rule in PublicView.Prohibited)
        {
            foreach (var example in rule.Examples)
            {
                var term = Normalize(example);
                foreach (Match match in Regex.Matches(input,
                    $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(term).Replace("\\ ", @"\s+")}(?![\p{{L}}\p{{N}}])",
                    RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)))
                {
                    var before = input[..match.Index];
                    var recent = before.Length > 55 ? before[^55..] : before;
                    var after = input[(match.Index + match.Length)..];
                    var following = after.Length > 32 ? after[..32] : after;
                    // Treat an immediately adjacent toy/replica qualifier as ambiguous,
                    // but NOT "đồ chơi và thuốc nổ" or "đồ chơi có thuốc nổ".
                    // Otherwise harmless ALLOWED items could hide a prohibited item.
                    if (Regex.IsMatch(recent,
                            @"(?:không|ko|chẳng|chua|chưa|not|without|khong|ví dụ|vi du)\s+(?:\S+\s+){0,3}$",
                            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) ||
                        Regex.IsMatch(recent,
                            @"(?:mô hình|đồ chơi|mo hinh|do choi|toy|water|plastic)\s+$",
                            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) ||
                        Regex.IsMatch(following,
                            @"^\s*(?:giả|đồ chơi|mô hình|rỗng|nước|nhựa|không có|không chứa|toy|replica|empty)\b",
                            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                        ambiguous = true;
                    else
                        positiveProhibited = true;
                }
            }
        }
        // Any unambiguous prohibited match blocks, even if ordinary allowed
        // items are mentioned in the same description.
        if (positiveProhibited) return AiStorageDecision.Prohibited;
        if (ambiguous) return AiStorageDecision.NeedsReview;
        // Each separately declared item group must match an ALLOWED example;
        // a single harmless item cannot whitelist other unknown declared items.
        var portions = Regex.Split(input, @"[,;+\n]|\s+(?:và|and)\s+")
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        if (portions.Length == 0) return AiStorageDecision.NeedsReview;
        var allAllowed = portions.All(portion => PublicView.Allowed.Any(item =>
            item.Examples.Any(example => Regex.IsMatch(portion,
                $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(Normalize(example)).Replace("\\ ", @"\s+")}(?![\p{{L}}\p{{N}}])",
                RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)))));
        return allAllowed ? AiStorageDecision.Allowed : AiStorageDecision.NeedsReview;
    }

    private static string Normalize(string value) =>
        Regex.Replace(value.Normalize(System.Text.NormalizationForm.FormC)
            .ToLowerInvariant().Trim(), @"\s+", " ");

    private static string Text(JsonElement element, string name) =>
        element.GetProperty(name).GetString() is { } value && !string.IsNullOrWhiteSpace(value)
            ? value : throw new InvalidDataException($"Invalid item policy field: {name}.");
}
