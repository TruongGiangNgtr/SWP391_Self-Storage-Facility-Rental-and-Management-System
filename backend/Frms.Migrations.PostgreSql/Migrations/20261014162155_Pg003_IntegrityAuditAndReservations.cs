using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Frms.Migrations.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class Pg003_IntegrityAuditAndReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservation_FacilityId",
                schema: "public",
                table: "Reservation");

            migrationBuilder.CreateIndex(
                name: "UX_Visit_Reservation_OneVisit",
                schema: "public",
                table: "Visit",
                column: "EntityId",
                unique: true,
                filter: "\"VisitType\" = 'RESERVATION'");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_Capacity",
                schema: "public",
                table: "Reservation",
                columns: new[] { "FacilityId", "UnitTypeId", "StartMonth", "EndMonth", "Status" });
            migrationBuilder.Sql(Read("Integrity.sql"));
            migrationBuilder.Sql(Read("Audit.sql"));
            migrationBuilder.Sql(Read("Reservations.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverting would disable active invariant/audit protections. Redeploy
            // a reviewed compatible definition; do not silently drop protections.
            throw new NotSupportedException("PostgreSQL invariant downgrade requires a reviewed compatible redeploy.");
        }

        private static string Read(string name)
        {
            using var stream = typeof(Pg003_IntegrityAuditAndReservations).Assembly
                .GetManifestResourceStream("Frms.DataAccess.PostgreSql." + name)
                ?? throw new InvalidOperationException("Missing PostgreSQL migration resource.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
