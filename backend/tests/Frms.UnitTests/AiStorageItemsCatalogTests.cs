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
        Assert.That(Enum.GetNames<AiStorageDecision>(),
            Is.EquivalentTo(new[] { nameof(AiStorageDecision.Allowed), nameof(AiStorageDecision.Prohibited) }));
    }

    [TestCase("thuốc nổ", AiStorageDecision.Prohibited)]
    [TestCase("Tôi muốn để sách và thuốc nổ", AiStorageDecision.Prohibited)]
    [TestCase("Tôi không có thuốc nổ", AiStorageDecision.Prohibited)]
    [TestCase("sách", AiStorageDecision.Allowed)]
    [TestCase("sách và quần áo", AiStorageDecision.Allowed)]
    [TestCase("đồ chơi", AiStorageDecision.Allowed)]
    [TestCase("ĐỒ CHƠI", AiStorageDecision.Allowed)]
    [TestCase("đồ chơi.", AiStorageDecision.Allowed)]
    [TestCase("toy", AiStorageDecision.Allowed)]
    [TestCase("toys", AiStorageDecision.Allowed)]
    [TestCase("đồ chơi xếp hình", AiStorageDecision.Allowed)]
    [TestCase("table", AiStorageDecision.Allowed)]
    [TestCase("dining table", AiStorageDecision.Allowed)]
    [TestCase("cái bàn", AiStorageDecision.Allowed)]
    [TestCase("bàn ăn", AiStorageDecision.Allowed)]
    [TestCase("table và đồ chơi", AiStorageDecision.Allowed)]
    [TestCase("đồ chơi và sách", AiStorageDecision.Allowed)]
    [TestCase("đồ chơi và thuốc nổ", AiStorageDecision.Prohibited)]
    [TestCase("table và thuốc nổ", AiStorageDecision.Prohibited)]
    [TestCase("đồ chơi có chứa thuốc nổ", AiStorageDecision.Prohibited)]
    [TestCase("súng đồ chơi", AiStorageDecision.Prohibited)]
    [TestCase("đồ chơi súng", AiStorageDecision.Prohibited)]
    [TestCase("toy gun", AiStorageDecision.Prohibited)]
    [TestCase("sách và đồ chưa xác định", AiStorageDecision.Prohibited)]
    [TestCase("món đồ chưa xác định", AiStorageDecision.Prohibited)]
    [TestCase("table và món đồ chưa xác định", AiStorageDecision.Prohibited)]
    public void Assess_UsesConservativeRuleScreening(string text, AiStorageDecision expected) =>
        Assert.That(Load().Assess(text), Is.EqualTo(expected));
}
