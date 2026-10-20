using Frms.Business.Services.Implementations;

namespace Frms.UnitTests;

public sealed class PublicPolicyContentProviderTests
{
    [Test]
    public void EmptyApprovedCataloguePublishesNoArticles()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(new { articles = Array.Empty<object>() }));
            var provider = JsonPublicPolicyContentProvider.Load(path);
            Assert.That(provider.PublishedArticles, Is.Empty);
        }
        finally { File.Delete(path); }
    }

    [Test]
    public void RejectsHtmlAndUnapprovedTopics()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(new
            {
                articles = new[] { new
                {
                    policyKey = "unapproved-topic",
                    title = "Title", summary = "Summary",
                    sections = new[] { new { heading = "Heading", body = "<script>alert(1)</script>" } }
                } }
            }));
            Assert.That(() => JsonPublicPolicyContentProvider.Load(path),
                Throws.TypeOf<InvalidOperationException>());
        }
        finally { File.Delete(path); }
    }
}
