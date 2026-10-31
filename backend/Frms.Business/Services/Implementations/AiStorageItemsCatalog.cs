using System.Text.Json;
using System.Text.RegularExpressions;

namespace Frms.Business.Services.Implementations;

public sealed record AiStoragePolicyItem(string Id, string Title, IReadOnlyList<string> Examples);
public sealed record AiStorageLegalReference(string DocumentNumber, string Title, string OfficialUrl);
public sealed record AiStoragePolicyView(
    string PolicyKey, string ApprovalStatus, string Version,
    string Title, string Summary, string ProhibitedWarning,
    IReadOnlyList<AiStoragePolicyItem> Prohibited,
    IReadOnlyList<AiStoragePolicyItem> Allowed,
    IReadOnlyList<AiStorageLegalReference> LegalReferences);

public enum AiStorageDecision { Prohibited, Allowed }

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
            Text(notice, "prohibitedWarning"),
            prohibited, allowed, refs.Select(x => x.Value).ToArray()));
    }

    public AiStorageDecision Assess(string description)
    {
        var input = Normalize(description);
        // Binary intake decision: a prohibited keyword anywhere blocks the whole request.
        // Unknown items, negations and replicas are not silently accepted as harmless.
        static bool Matches(string text, string example) => Regex.IsMatch(text,
            $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(Normalize(example)).Replace("\\ ", @"\s+")}(?![\p{{L}}\p{{N}}])",
            RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

        if (PublicView.Prohibited.Any(rule => rule.Examples.Any(example => Matches(input, example))))
            return AiStorageDecision.Prohibited;

        // Every comma/semicolon/conjunction-separated item must match ALLOWED.
        // A known allowed item cannot whitelist an unknown item in the same request.
        var portions = Regex.Split(input, @"[,;+\n]|\s+(?:và|and)\s+")
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        if (portions.Length == 0) return AiStorageDecision.Prohibited;
        return portions.All(portion => PublicView.Allowed.Any(item =>
            item.Examples.Any(example => Matches(portion, example))))
            ? AiStorageDecision.Allowed : AiStorageDecision.Prohibited;
    }

    private static string Normalize(string value) =>
        Regex.Replace(value.Normalize(System.Text.NormalizationForm.FormC)
            .ToLowerInvariant().Trim(), @"\s+", " ");

    private static string Text(JsonElement element, string name) =>
        element.GetProperty(name).GetString() is { } value && !string.IsNullOrWhiteSpace(value)
            ? value : throw new InvalidDataException($"Invalid item policy field: {name}.");
}
