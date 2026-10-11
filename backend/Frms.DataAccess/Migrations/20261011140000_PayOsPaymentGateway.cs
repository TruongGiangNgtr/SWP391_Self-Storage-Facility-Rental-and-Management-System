using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Frms.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class PayOsPaymentGateway : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Payment_Method",
                table: "Payment");

            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateSequence(
                name: "ProviderOrderCodeSequence",
                schema: "dbo",
                startValue: 1000L);

            migrationBuilder.AddColumn<long>(
                name: "ProviderOrderCode",
                table: "Payment",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payment_ProviderOrderCode",
                table: "Payment",
                column: "ProviderOrderCode",
                unique: true,
                filter: "[ProviderOrderCode] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payment_Method",
                table: "Payment",
                sql: "[PaymentMethod] IN ('MOMO','VNPAY','PAYOS')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM dbo.Payment WITH (TABLOCKX, HOLDLOCK)
                    WHERE PaymentMethod = 'PAYOS' OR ProviderOrderCode IS NOT NULL)
                    THROW 51032, 'Cannot downgrade payOS support while PAYOS rows or provider order-code evidence exist.', 1;
                """);
            migrationBuilder.DropIndex(
                name: "IX_Payment_ProviderOrderCode",
                table: "Payment");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payment_Method",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "ProviderOrderCode",
                table: "Payment");

            migrationBuilder.DropSequence(
                name: "ProviderOrderCodeSequence",
                schema: "dbo");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payment_Method",
                table: "Payment",
                sql: "[PaymentMethod] IN ('MOMO','VNPAY')");
        }
    }
}
