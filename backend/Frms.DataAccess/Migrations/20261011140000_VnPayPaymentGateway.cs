using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Frms.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class VnPayPaymentGateway : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Payment_Method",
                table: "Payment");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payment_Method",
                table: "Payment",
                sql: "[PaymentMethod] IN ('MOMO','VNPAY')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM dbo.Payment WITH (TABLOCKX, HOLDLOCK) WHERE PaymentMethod = 'VNPAY')
                    THROW 51030, 'Cannot downgrade VNPay Payment method support while VNPAY rows exist.', 1;
                """);
            migrationBuilder.DropCheckConstraint(
                name: "CK_Payment_Method",
                table: "Payment");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payment_Method",
                table: "Payment",
                sql: "[PaymentMethod]='MOMO'");
        }
    }
}
