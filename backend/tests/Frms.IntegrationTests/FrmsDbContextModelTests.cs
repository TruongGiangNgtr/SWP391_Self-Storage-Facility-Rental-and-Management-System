using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Frms.IntegrationTests;

[TestFixture]
public sealed class FrmsDbContextModelTests
{
    private static FrmsDbContext CreateContext() => new(new DbContextOptionsBuilder<FrmsDbContext>()
        .UseNpgsql("Host=127.0.0.1;Database=frms_model_tests;Username=frms_test;Password=test-password;Pooling=false")
        .Options);

    [Test]
    public void Model_ContainsCurrentEntitySet()
    {
        using var context = CreateContext();
        var entities = context.Model.GetEntityTypes().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(entities, Has.Length.EqualTo(27));
            Assert.That(entities.Select(entity => entity.ClrType), Does.Contain(typeof(UserAccount)));
            Assert.That(entities.Select(entity => entity.ClrType), Does.Contain(typeof(Customer)));
            Assert.That(entities.Select(entity => entity.ClrType), Does.Contain(typeof(Employee)));
            Assert.That(entities.Select(entity => entity.ClrType), Does.Contain(typeof(LoginHistory)));
            Assert.That(entities.Select(entity => entity.ClrType), Does.Contain(typeof(Payment)));
        });
    }

    [Test]
    public void UserAccount_EmailAndPhoneNumber_HaveUniqueIndexes()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(UserAccount))!;

        Assert.Multiple(() =>
        {
            Assert.That(IsUnique(entity, nameof(UserAccount.Email)), Is.True);
            Assert.That(IsUnique(entity, nameof(UserAccount.PhoneNumber)), Is.True);
        });
    }

    [Test]
    public void UserAccount_Status_HasActiveDefaultAndAllowedValuesConstraint()
    {
        using var context = CreateContext();
        // Check-constraint metadata is design-time metadata and is not exposed by EF Core's
        // read-optimized runtime model. Use IDesignTimeModel for this metadata assertion.
        var model = context.GetService<IDesignTimeModel>().Model;
        var entity = model.FindEntityType(typeof(UserAccount))!;
        var status = entity.FindProperty(nameof(UserAccount.Status))!;
        var table = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
        var constraints = entity.GetCheckConstraints().Select(constraint => constraint.Name).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(status.GetDefaultValue(), Is.EqualTo("ACTIVE"));
            Assert.That(constraints, Does.Contain("CK_UserAccount_Status"));
            Assert.That(status.GetColumnType(table), Is.EqualTo("varchar(50)"));
        });
    }

    private static bool IsUnique(IReadOnlyEntityType entity, string property) =>
        entity.GetIndexes().Any(index => index.IsUnique && index.Properties.Select(item => item.Name).SequenceEqual(new[] { property }));
}
