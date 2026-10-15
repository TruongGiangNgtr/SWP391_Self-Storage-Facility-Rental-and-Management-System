using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.Migrations.PostgreSql.Migrations;
public partial class Pg007_DeploymentSeedBillingAndJobs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Pg001 now seeds timestamps from PostgreSQL defaults. Never rewrite
        // Policy history to reconcile former static EF HasData timestamp values.
        foreach (var name in new[] { "Billing.sql", "Jobs.sql", "Notifications.sql" })
        {
            using var stream = typeof(Pg007_DeploymentSeedBillingAndJobs).Assembly
                .GetManifestResourceStream("Frms.DataAccess.PostgreSql." + name)
                ?? throw new InvalidOperationException("Missing PostgreSQL migration resource.");
            using var reader = new StreamReader(stream);
            migrationBuilder.Sql(reader.ReadToEnd());
        }
    }
    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Billing/job downgrade requires a reviewed compatible redeploy.");
}
