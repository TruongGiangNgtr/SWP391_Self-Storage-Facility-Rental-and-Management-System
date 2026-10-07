using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Frms.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class PaymentProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No data backfill is authorized. Locks survive until the migration transaction commits.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM dbo.Payment WITH (TABLOCKX, HOLDLOCK) WHERE InvoiceId IS NULL)
                    THROW 51001, 'Payment migration blocked: unexpected NULL InvoiceId. No links were fabricated; review existing data.', 1;
                IF EXISTS (SELECT 1 FROM dbo.Payment WITH (TABLOCKX, HOLDLOCK))
                    THROW 51002, 'Payment migration blocked: existing rows have no approved IdempotencyKey. No keys were manufactured; review existing data.', 1;
                """);
            migrationBuilder.DropIndex(
                name: "IX_Payment_InvoiceId",
                table: "Payment");

            migrationBuilder.AlterColumn<Guid>(
                name: "InvoiceId",
                table: "Payment",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IdempotencyKey",
                table: "Payment",
                type: "uniqueidentifier",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentUrl",
                table: "Payment",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentUrlExpiresAt",
                table: "Payment",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payment_IdempotencyKey",
                table: "Payment",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payment_InvoiceId_Status_CreatedAt",
                table: "Payment",
                columns: new[] { "InvoiceId", "Status", "CreatedAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payment_UrlExpiry",
                table: "Payment",
                sql: "[PaymentUrlExpiresAt] IS NULL OR [PaymentUrl] IS NOT NULL");

            migrationBuilder.Sql(ApplyPaymentResultSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // A populated downgrade would destroy lifetime idempotency/session evidence.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM dbo.Payment WITH (TABLOCKX, HOLDLOCK))
                    THROW 51003, 'Payment downgrade blocked: preserve existing idempotency and session data. Deployment review required.', 1;
                DROP PROCEDURE IF EXISTS dbo.usp_ApplyPaymentResult;
                """);
            migrationBuilder.DropIndex(
                name: "IX_Payment_IdempotencyKey",
                table: "Payment");

            migrationBuilder.DropIndex(
                name: "IX_Payment_InvoiceId_Status_CreatedAt",
                table: "Payment");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payment_UrlExpiry",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "PaymentUrl",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "PaymentUrlExpiresAt",
                table: "Payment");

            migrationBuilder.AlterColumn<Guid>(
                name: "InvoiceId",
                table: "Payment",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_InvoiceId",
                table: "Payment",
                column: "InvoiceId");
        }
    }
}
