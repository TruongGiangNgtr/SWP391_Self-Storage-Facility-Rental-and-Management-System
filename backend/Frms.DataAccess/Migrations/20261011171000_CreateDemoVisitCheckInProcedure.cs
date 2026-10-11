
using Frms.DataAccess.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.DataAccess.Migrations;

// DEMO ONLY. Creates an additional procedure without changing usp_CheckInVisit.
// Deploy this migration ONLY to an isolated demo database, never production.
[DbContext(typeof(FrmsDbContext))]
[Migration("20261011171000_CreateDemoVisitCheckInProcedure")]
public sealed class CreateDemoVisitCheckInProcedure : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        // SQL Server requires CREATE OR ALTER PROCEDURE in its own batch.
        // EXEC(N'...') works inside an EF Core migration transaction; do not use GO.
        var definition = """
            CREATE OR ALTER PROCEDURE dbo.usp_CheckInVisitDemo
                @VisitId uniqueidentifier,
                @EmployeeId uniqueidentifier,
                @NowUtc datetime2(3) = NULL
            AS
            BEGIN
                SET NOCOUNT ON;
                SET XACT_ABORT ON;
                BEGIN TRY
                    -- Preserve the production lock order and entity validation.
                    DECLARE @EntityId uniqueidentifier, @Type varchar(50);
                    SELECT @EntityId = EntityId, @Type = VisitType
                    FROM dbo.Visit WHERE VisitId = @VisitId;
                    IF @EntityId IS NULL OR @Type NOT IN ('RESERVATION', 'ACCESS', 'RETURN')
                        THROW 51301, 'VISIT_ENTITY_MISMATCH', 1;

                    BEGIN TRANSACTION;

                    DECLARE @FacilityId uniqueidentifier, @EntityStatus varchar(50);
                    IF @Type = 'RESERVATION'
                        SELECT @FacilityId = FacilityId, @EntityStatus = Status
                        FROM dbo.Reservation WITH (UPDLOCK, HOLDLOCK)
                        WHERE ReservationId = @EntityId;
                    ELSE
                        SELECT @FacilityId = FacilityId, @EntityStatus = Status
                        FROM dbo.Contract WITH (UPDLOCK, HOLDLOCK)
                        WHERE ContractId = @EntityId;
                    IF @FacilityId IS NULL
                        THROW 51301, 'VISIT_ENTITY_MISMATCH', 1;

                    DECLARE @LockedEntityId uniqueidentifier, @LockedType varchar(50),
                        @VisitStatus varchar(50), @VisitDate date;
                    SELECT @LockedEntityId = EntityId,
                           @LockedType = VisitType,
                           @VisitStatus = Status,
                           @VisitDate = VisitDate
                    FROM dbo.Visit WITH (UPDLOCK, HOLDLOCK)
                    WHERE VisitId = @VisitId;
                    IF @LockedEntityId IS NULL OR @LockedEntityId <> @EntityId OR @LockedType <> @Type
                        THROW 51301, 'VISIT_ENTITY_MISMATCH', 1;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM dbo.Employee e WITH (HOLDLOCK)
                        JOIN dbo.UserAccount a WITH (HOLDLOCK)
                            ON a.UserAccountId = e.UserAccountId
                        JOIN dbo.UserRole r WITH (HOLDLOCK)
                            ON r.RoleId = a.RoleId
                        WHERE e.EmployeeId = @EmployeeId
                          AND e.FacilityId = @FacilityId
                          AND a.Status = 'ACTIVE'
                          AND r.RoleName = 'FACILITY_STAFF'
                    )
                        THROW 51302, 'FORBIDDEN', 1;

                    IF @VisitStatus <> 'SCHEDULED'
                       OR (@Type = 'RESERVATION' AND @EntityStatus <> 'CONFIRMED')
                       OR (@Type IN ('ACCESS', 'RETURN') AND @EntityStatus <> 'ACTIVE')
                        THROW 51303, 'VISIT_INVALID_STATUS', 1;

                    -- DEMO DIFFERENCE: No VisitDate versus current GMT+7 check.
                    -- VisitDate remains unchanged, even for early check-in.
                    UPDATE dbo.Visit
                    SET Status = 'CHECKED_IN', EmployeeId = @EmployeeId
                    WHERE VisitId = @VisitId;

                    COMMIT TRANSACTION;
                END TRY
                BEGIN CATCH
                    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;
            END;
            """;

        var escaped = definition.Replace("'", "''", StringComparison.Ordinal);
        migrationBuilder.Sql($"EXEC(N'{escaped}');");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        // Rollback removes only the demo procedure, never production procedure or Visit data.
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_CheckInVisitDemo;");
    }
}
