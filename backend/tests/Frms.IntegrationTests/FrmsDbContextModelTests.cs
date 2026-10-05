using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
namespace Frms.IntegrationTests;

[TestFixture]
public sealed class FrmsDbContextModelTests
{
    private static FrmsDbContext CreateContext() => new(new DbContextOptionsBuilder<FrmsDbContext>().UseSqlServer("Server=localhost;Database=Frms_ModelOnly;Trusted_Connection=True;TrustServerCertificate=True").Options);
    [Test]
    public void Model_ReleaseOneBaseline_ContainsExactlyTwentySevenPersistedEntities() { using var context = CreateContext(); Assert.That(context.Model.GetEntityTypes().Select(type => type.ClrType).Distinct().ToArray(), Has.Length.EqualTo(27)); }
    [Test]
    public void UserAccount_EmailAndPhoneNumber_AreBothUniqueIndexes() { using var context = CreateContext(); var entity = context.Model.FindEntityType(typeof(UserAccount))!; Assert.Multiple(() => { Assert.That(IsUnique(entity, nameof(UserAccount.Email)), Is.True); Assert.That(IsUnique(entity, nameof(UserAccount.PhoneNumber)), Is.True); }); }
    [Test]
    public void AuthoritativeSeed_RolesPolicyAndDamageTypes_ArePresentWithoutInventedExtraFeeAmounts()
    {
        using var context = CreateContext(); var model = context.GetService<IDesignTimeModel>().Model; var roleSeed = model.FindEntityType(typeof(UserRole))!.GetSeedData().ToArray(); var policySeed = model.FindEntityType(typeof(Policy))!.GetSeedData().ToArray(); var damageSeed = model.FindEntityType(typeof(DamageType))!.GetSeedData().ToArray(); var extraFeeSeed = model.FindEntityType(typeof(ExtraFeeType))!.GetSeedData().ToArray();
        Assert.Multiple(() => { Assert.That(roleSeed, Has.Length.EqualTo(5)); Assert.That(policySeed, Has.Length.EqualTo(1)); Assert.That(damageSeed, Has.Length.EqualTo(6)); Assert.That(extraFeeSeed, Is.Empty); });
    }
    private static bool IsUnique(IReadOnlyEntityType entity, string property) => entity.GetIndexes().Any(index => index.IsUnique && index.Properties.Select(p => p.Name).SequenceEqual(new[] { property }));
}
