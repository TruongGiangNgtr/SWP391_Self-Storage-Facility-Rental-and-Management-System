using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Frms.Migrations.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class DiagnoseModelDrift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "public",
                table: "HandoverUnitAssignment",
                type: "varchar(12)",
                nullable: false,
                defaultValue: "ACTIVE",
                collation: "frms_ci_as",
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldDefaultValue: "ACTIVE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "public",
                table: "HandoverUnitAssignment",
                type: "varchar(12)",
                nullable: false,
                defaultValue: "ACTIVE",
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldDefaultValue: "ACTIVE",
                oldCollation: "frms_ci_as");
        }
    }
}
