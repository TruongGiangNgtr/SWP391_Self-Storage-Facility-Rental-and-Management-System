using System.Text.Json;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;

namespace Frms.Business.Services.Implementations;

/// <summary>
/// Loads only reviewed, explicitly published articles from version-controlled JSON.
/// An empty catalogue is intentionally safe until approved copy is provided.
/// </summary>
public sealed class JsonPublicPolicyContentProvider : IPublicPolicyContentProvider
{
    private static readonly string[] AllowedKeys =
    [
        "reservation-cancellation", "deposit-payment-refund",
        "storage-access-handover-contract", "facility-rules-prohibited-items"
    ];

    public IReadOnlyList<PublishedPolicyArticle> PublishedArticles { get; }

    private JsonPublicPolicyContentProvider(IReadOnlyList<PublishedPolicyArticle> articles) =>
        PublishedArticles = articles;

    public static JsonPublicPolicyContentProvider Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new InvalidOperationException("Approved public policy article catalogue is missing.");
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(filePath));
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("articles", out var articles) ||
                articles.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException();

            var result = new List<PublishedPolicyArticle>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var article in articles.EnumerateArray())
            {
                var key = RequiredText(article, "policyKey");
                var title = RequiredText(article, "title");
                var summary = RequiredText(article, "summary");
                if (!AllowedKeys.Contains(key, StringComparer.Ordinal) || !seen.Add(key) ||
                    !article.TryGetProperty("sections", out var sections) ||
                    sections.ValueKind != JsonValueKind.Array || sections.GetArrayLength() == 0)
                    throw new InvalidDataException();
                var blocks = sections.EnumerateArray().Select(s =>
                    new PublicPolicySection(RequiredText(s, "heading"), RequiredText(s, "body"))).ToArray();
                result.Add(new PublishedPolicyArticle(key, title, summary, blocks));
            }
            return new JsonPublicPolicyContentProvider(result);
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or InvalidDataException)
        {
            // Never include unreviewed copy or deployment paths in API/log output.
            throw new InvalidOperationException("Approved public policy catalogue is invalid.");
        }
    }

    private static string RequiredText(JsonElement item, string key)
    {
        if (item.ValueKind != JsonValueKind.Object ||
            !item.TryGetProperty(key, out var value) || value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
            throw new InvalidDataException();
        var text = value.GetString()!;
        // Public bodies are plain text, never executable HTML/script markup.
        if (System.Text.RegularExpressions.Regex.IsMatch(text, @"<[A-Za-z/!][^>]*>"))
            throw new InvalidDataException();
        return text;
    }
}
