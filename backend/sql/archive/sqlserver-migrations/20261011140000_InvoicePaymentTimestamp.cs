using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Frms.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InvoicePaymentTimestamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Invoice",
                type: "datetime2(3)",
                nullable: true);

            // No historical timestamp backfill: missing provider evidence remains null.
            migrationBuilder.Sql(ApplyPaymentResultSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Downgrade must not silently discard verified financial timestamp evidence.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM dbo.Invoice WITH (TABLOCKX, HOLDLOCK) WHERE PaidAt IS NOT NULL)
                    THROW 51031, 'Cannot downgrade Invoice payment timestamps while verified timestamp evidence exists.', 1;
                """);
            migrationBuilder.Sql(ApplyPaymentResultSql.Replace(
                "UPDATE dbo.Invoice SET Status = 'PAID', PaidAt = @VerifiedPaidAtUtc WHERE InvoiceId = @invoiceId;",
                "UPDATE dbo.Invoice SET Status = 'PAID' WHERE InvoiceId = @invoiceId;",
                StringComparison.Ordinal));
            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Invoice");
        }
    }
}
