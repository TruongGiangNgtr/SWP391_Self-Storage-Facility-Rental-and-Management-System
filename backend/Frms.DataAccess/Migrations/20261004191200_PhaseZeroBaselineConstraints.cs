using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Frms.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class PhaseZeroBaselineConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Policy_Days",
                table: "Policy");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Visit",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "SCHEDULED",
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "UserAccount",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "ACTIVE",
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "SupportTicket",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "OPEN",
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "StorageUnit",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "AVAILABLE",
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Reservation",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "PENDING_DEPOSIT",
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Policy",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "ACTIVE",
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Policy",
                type: "datetime2(3)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Payment",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "PENDING",
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "NotificationLog",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "PENDING",
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Invoice",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "UNPAID",
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Inspection",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "PENDING",
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Facility",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "INACTIVE",
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ExtraFeeType",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "ACTIVE",
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Discount",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "ACTIVE",
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DepositSettlement",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "PENDING",
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DamageType",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "ACTIVE",
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DamageRecord",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "PENDING",
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Contract",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "ACTIVE",
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Policy_Days",
                table: "Policy",
                sql: "[DepositTimeoutHours]>0 AND [ReservationVisitStartDay] BETWEEN 1 AND 31 AND [ReservationVisitEndDay] BETWEEN 1 AND 31 AND [ReservationVisitStartDay] <= [ReservationVisitEndDay] AND [MonthlyPaymentDueDay] BETWEEN 1 AND 31 AND [OverdueStartDay] BETWEEN 1 AND 31 AND [LateFeeDivisorDays]>0 AND [EarlyReturnWaiveFeeUntilDay] BETWEEN 1 AND 31");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NotificationLog_SentAt",
                table: "NotificationLog",
                sql: "([Status]='PENDING' AND [SentAt] IS NULL) OR ([Status]='SENT' AND [SentAt] IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Policy_Days",
                table: "Policy");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NotificationLog_SentAt",
                table: "NotificationLog");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Visit",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldDefaultValue: "SCHEDULED");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "UserAccount",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldDefaultValue: "ACTIVE");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "SupportTicket",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldDefaultValue: "OPEN");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "StorageUnit",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldDefaultValue: "AVAILABLE");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Reservation",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldDefaultValue: "PENDING_DEPOSIT");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Policy",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldDefaultValue: "ACTIVE");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Policy",
                type: "datetime2(3)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Payment",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldDefaultValue: "PENDING");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "NotificationLog",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldDefaultValue: "PENDING");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Invoice",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldDefaultValue: "UNPAID");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Inspection",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldDefaultValue: "PENDING");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Facility",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldDefaultValue: "INACTIVE");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ExtraFeeType",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldDefaultValue: "ACTIVE");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Discount",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldDefaultValue: "ACTIVE");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DepositSettlement",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldDefaultValue: "PENDING");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DamageType",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldDefaultValue: "ACTIVE");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DamageRecord",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldDefaultValue: "PENDING");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Contract",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldDefaultValue: "ACTIVE");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Policy_Days",
                table: "Policy",
                sql: "[DepositTimeoutHours]>0 AND [ReservationVisitStartDay] BETWEEN 1 AND 31 AND [ReservationVisitEndDay] BETWEEN 1 AND 31 AND [MonthlyPaymentDueDay] BETWEEN 1 AND 31 AND [OverdueStartDay] BETWEEN 1 AND 31 AND [LateFeeDivisorDays]>0 AND [EarlyReturnWaiveFeeUntilDay] BETWEEN 1 AND 31");
        }
    }
}
