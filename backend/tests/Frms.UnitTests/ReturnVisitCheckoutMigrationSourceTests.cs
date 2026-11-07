using NUnit.Framework;

namespace Frms.UnitTests;

// Source-contract regression only. Real PostgreSQL atomicity/E2E is a separate release gate.
[TestFixture]
public sealed class ReturnVisitCheckoutMigrationSourceTests
{
    [Test]
    public void Pg020ClosesOnlyTheLinkedConfirmedReturnVisit()
    {
        var sql = ReadSource("backend", "sql", "postgresql", "ReturnVisitCheckout.sql");
        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("CREATE OR REPLACE FUNCTION public.\"usp_FinalizeReturn\""));
            Assert.That(sql, Does.Contain("IF v_inspection.\"VisitId\" IS NOT NULL THEN"));
            Assert.That(sql, Does.Contain("SET \"Status\"='CHECKED_OUT'"));
            Assert.That(sql, Does.Contain("target.\"EntityId\"=p_contract_id"));
            Assert.That(sql, Does.Contain("target.\"VisitType\"='RETURN'"));
            Assert.That(sql, Does.Contain("target.\"Status\"='CHECKED_IN'"));
            Assert.That(sql, Does.Contain("target.\"ActualReturnDate\" IS NOT NULL"));
            Assert.That(sql, Does.Contain("MESSAGE='VISIT_INVALID_STATUS'"));
            Assert.That(sql, Does.Contain("MESSAGE='DAMAGE_DECISION_PENDING'"));
            Assert.That(sql, Does.Contain("set_config('frms.user_account_id',v_actor::text,true)"));
            Assert.That(sql, Does.Contain("set_config('frms.user_account_id',v_prior_actor,true)"));
            Assert.That(sql, Does.Contain("SET \"Status\"=p_storage_unit_status"));
        });
    }

    [Test]
    public void Pg020UsesForwardOnlyEmbeddedRoutine()
    {
        var migration = ReadSource("backend", "Frms.Migrations.PostgreSql", "Migrations", "20261107100000_Pg020_ReturnVisitCheckout.cs");
        Assert.Multiple(() =>
        {
            Assert.That(migration, Does.Contain("20261107100000_Pg020_ReturnVisitCheckout"));
            Assert.That(migration, Does.Contain("Frms.DataAccess.PostgreSql.ReturnVisitCheckout.sql"));
            Assert.That(migration, Does.Contain("NotSupportedException"));
        });
    }

    private static string ReadSource(params string[] segments)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var path = Path.Combine(new[] { dir.FullName }.Concat(segments).ToArray());
            if (File.Exists(path)) return File.ReadAllText(path);
            dir = dir.Parent;
        }
        throw new FileNotFoundException($"Run from source checkout containing {string.Join('/', segments)}.");
    }
}
