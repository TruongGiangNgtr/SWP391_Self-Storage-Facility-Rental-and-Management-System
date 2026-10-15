
using Frms.DataAccess.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.DataAccess.Migrations;

[DbContext(typeof(FrmsDbContext))]
[Migration("20261011140000_AlignVisitCheckInBusinessDate")]
public sealed class AlignVisitCheckInBusinessDate : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.usp_CheckInVisit', N'P') IS NULL
                THROW 51272,
                    'dbo.usp_CheckInVisit is missing; apply the baseline migration first.',
                    1;
            """);

        var definition = """
            CREATE OR ALTER PROCEDURE dbo.usp_CheckInVisit
                @VisitId UNIQUEIDENTIFIER,
                @EmployeeId UNIQUEIDENTIFIER,
                @NowUtc DATETIME2(3) = NULL,
                @BusinessDate DATE = NULL
            AS
            BEGIN
                SET NOCOUNT ON;
                SET XACT_ABORT ON;

                -- Compatibility with old backend callers.
                -- Current SRS business timezone is GMT+7.
                -- Updated backend supplies @BusinessDate from IClock.
                IF @BusinessDate IS NULL
                BEGIN
                    SET @NowUtc = COALESCE(@NowUtc, SYSUTCDATETIME());
                    SET @BusinessDate =
                        CAST(DATEADD(HOUR, 7, @NowUtc) AS DATE);
                END;

                BEGIN TRY
                    BEGIN TRANSACTION;

                    DECLARE
                        @VisitType VARCHAR(50),
                        @EntityId UNIQUEIDENTIFIER,
                        @VisitDate DATE,
                        @VisitStatus VARCHAR(50),
                        @FacilityId UNIQUEIDENTIFIER;

                    SELECT
                        @VisitType = VisitType,
                        @EntityId = EntityId,
                        @VisitDate = VisitDate,
                        @VisitStatus = Status
                    FROM dbo.Visit WITH (UPDLOCK, HOLDLOCK)
                    WHERE VisitId = @VisitId;

                    IF @VisitStatus <> 'SCHEDULED'
                        THROW 51112, 'VISIT_INVALID_STATUS', 1;

                    -- Compare business calendar dates, not UTC dates.
                    IF @VisitDate <> @BusinessDate
                        THROW 51113, 'VISIT_DATE_MISMATCH', 1;

                    IF @VisitType = 'RESERVATION'
                    BEGIN
                        SELECT @FacilityId = FacilityId
                        FROM dbo.Reservation
                        WHERE ReservationId = @EntityId;
                    END
                    ELSE
                    BEGIN
                        SELECT @FacilityId = FacilityId
                        FROM dbo.Contract
                        WHERE ContractId = @EntityId;
                    END

                    IF NOT EXISTS (
                        SELECT 1
                        FROM dbo.Employee e
                        JOIN dbo.UserAccount ua
                            ON ua.UserAccountId = e.UserAccountId
                        JOIN dbo.UserRole ur
                            ON ur.RoleId = ua.RoleId
                        WHERE e.EmployeeId = @EmployeeId
                          AND e.FacilityId = @FacilityId
                          AND ua.Status = 'ACTIVE'
                          AND ur.RoleName = 'FACILITY_STAFF'
                    )
                        THROW 51114,
                            'STAFF_FACILITY_OR_ROLE_INVALID', 1;

                    UPDATE dbo.Visit
                    SET
                        Status = 'CHECKED_IN',
                        EmployeeId = @EmployeeId
                    WHERE VisitId = @VisitId
                      AND Status = 'SCHEDULED';

                    IF @@ROWCOUNT <> 1
                        THROW 51112, 'VISIT_INVALID_STATUS', 1;

                    COMMIT TRANSACTION;

                    SELECT
                        @VisitId AS VisitId,
                        'CHECKED_IN' AS Status,
                        @EmployeeId AS EmployeeId;
                END TRY
                BEGIN CATCH
                    IF XACT_STATE() <> 0
                        ROLLBACK TRANSACTION;

                    THROW;
                END CATCH
            END;
            """;

        // CREATE OR ALTER PROCEDURE must execute in its own SQL batch.
        var escapedDefinition = definition.Replace(
            "'",
            "''",
            StringComparison.Ordinal);

        migrationBuilder.Sql(
            $"EXEC(N'{escapedDefinition}');");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        // Do not restore the old UTC-date comparison automatically.
        throw new NotSupportedException(
            "Automatic rollback would restore incorrect UTC date semantics. " +
            "Restore a reviewed prior procedure manually if required.");
    }
}
