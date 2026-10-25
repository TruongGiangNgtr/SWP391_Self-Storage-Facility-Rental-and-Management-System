using Frms.Business.Services.Implementations;
using NUnit.Framework;

namespace Frms.UnitTests;

[TestFixture]
public sealed class AiStorageItemsCatalogTests
{
    private static AiStorageItemsCatalog Load()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var file = Path.Combine(directory.FullName, "backend", "Frms.Api",
                "Content", "AiStorageRules", "storage-items.vi-VN.json");
            if (File.Exists(file)) return AiStorageItemsCatalog.Load(file);
            directory = directory.Parent;
        }
        throw new FileNotFoundException("AI storage rules JSON was not found in the checkout.");
    }

    [Test]
    public void Catalog_ProvidesBothPublicGroupsAndDraftStatus()
    {
        var catalog = Load();
        Assert.That(catalog.PublicView.Prohibited, Is.Not.Empty);
        Assert.That(catalog.PublicView.Allowed, Is.Not.Empty);
        Assert.That(catalog.PublicView.ApprovalStatus, Is.EqualTo("DRAFT_REQUIRES_LEGAL_AND_BUSINESS_APPROVAL"));
    }

    [TestCase("thuốc nổ", AiStorageDecision.Prohibited)]
    [TestCase("Tôi muốn để sách và thuốc nổ", AiStorageDecision.Prohibited)]
    [TestCase("Tôi không có thuốc nổ", AiStorageDecision.NeedsReview)]
    [TestCase("sách", AiStorageDecision.Allowed)]
    [TestCase("sách và quần áo", AiStorageDecision.Allowed)]
    [TestCase("đồ chơi", AiStorageDecision.Allowed)]
    [TestCase("đồ chơi xếp hình", AiStorageDecision.Allowed)]
    [TestCase("đồ chơi và sách", AiStorageDecision.Allowed)]
    [TestCase("đồ chơi và thuốc nổ", AiStorageDecision.Prohibited)]
    [TestCase("đồ chơi có chứa thuốc nổ", AiStorageDecision.Prohibited)]
    [TestCase("súng đồ chơi", AiStorageDecision.NeedsReview)]
    [TestCase("đồ chơi súng", AiStorageDecision.NeedsReview)]
    [TestCase("sách và đồ chưa xác định", AiStorageDecision.NeedsReview)]
    [TestCase("món đồ chưa xác định", AiStorageDecision.NeedsReview)]
    public void Assess_UsesConservativeRuleScreening(string text, AiStorageDecision expected) =>
        Assert.That(Load().Assess(text), Is.EqualTo(expected));
}
