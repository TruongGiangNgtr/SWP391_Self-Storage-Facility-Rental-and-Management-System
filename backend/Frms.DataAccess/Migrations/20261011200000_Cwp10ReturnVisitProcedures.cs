using Frms.DataAccess.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.DataAccess.Migrations;

// VIS-002/005: procedure-only upgrade. Legacy create callers keep their three parameters.
// Contract lock is shared with usp_CreateAccessVisit/usp_RenewContract. No financial writes.
[DbContext(typeof(FrmsDbContext))]
[Migration("20261011200000_Cwp10ReturnVisitProcedures")]
public sealed class Cwp10ReturnVisitProcedures : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        Replace(migrationBuilder, CreateSql);
        Replace(migrationBuilder, RescheduleSql);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_RescheduleReturnVisit;");
        // Restore exactly the legacy definition shipped by RestoreLegacySqlObjects.
        Replace(migrationBuilder, LegacyCreateSql);
    }
    private static void Replace(MigrationBuilder builder, string sql) =>
        builder.Sql("EXEC(N'" + sql.Replace("'", "''", StringComparison.Ordinal) + "');");

    private const string CreateSql = """
        CREATE OR ALTER PROCEDURE dbo.usp_CreateReturnVisit
            @ContractId UNIQUEIDENTIFIER, @CustomerId UNIQUEIDENTIFIER, @VisitDate DATE,
            @UserAccountId UNIQUEIDENTIFIER = NULL, @NowUtc DATETIME2(3) = NULL
        AS
        BEGIN
            SET NOCOUNT ON; SET XACT_ABORT ON;
            IF @NowUtc IS NULL SET @NowUtc = SYSUTCDATETIME();
            BEGIN TRY
                BEGIN TRANSACTION;
                DECLARE @Status VARCHAR(50), @Owner UNIQUEIDENTIFIER;
                SELECT @Status = Status, @Owner = CustomerId FROM dbo.Contract WITH (UPDLOCK, HOLDLOCK)
                WHERE ContractId = @ContractId;
                IF @Status IS NULL OR @Owner <> @CustomerId OR @CustomerId IS NULL
                    THROW 51301, 'RESOURCE_NOT_FOUND', 1;
                DECLARE @AccountStatus VARCHAR(50), @Role VARCHAR(50);
                SELECT @AccountStatus = a.Status, @Role = r.RoleName
                FROM dbo.Customer c WITH (HOLDLOCK)
                JOIN dbo.UserAccount a WITH (HOLDLOCK) ON a.UserAccountId = c.UserAccountId
                JOIN dbo.UserRole r WITH (HOLDLOCK) ON r.RoleId = a.RoleId
                WHERE c.CustomerId = @CustomerId AND (@UserAccountId IS NULL OR a.UserAccountId = @UserAccountId);
                IF @AccountStatus IS NULL THROW 51301, 'RESOURCE_NOT_FOUND', 1;
                IF @AccountStatus <> 'ACTIVE' THROW 51302, 'ACCOUNT_INACTIVE', 1;
                IF @Role <> 'CUSTOMER' THROW 51303, 'FORBIDDEN', 1;
                IF @Status <> 'ACTIVE' THROW 51304, 'CONTRACT_NOT_ACTIVE', 1;
                IF @VisitDate IS NULL THROW 51308, 'VALIDATION_ERROR', 1;
                IF @VisitDate < CAST(DATEADD(HOUR, 7, @NowUtc) AS DATE)
                    THROW 51306, 'VISIT_DATE_IN_PAST', 1;
                IF EXISTS (SELECT 1 FROM dbo.Visit WITH (UPDLOCK, HOLDLOCK)
                    WHERE EntityId = @ContractId AND VisitType = 'RETURN' AND Status IN ('SCHEDULED','CHECKED_IN'))
                    THROW 51305, 'RETURN_VISIT_PENDING', 1;
                DECLARE @VisitId UNIQUEIDENTIFIER = NEWID();
                INSERT dbo.Visit(VisitId, EntityId, EmployeeId, VisitType, VisitDate, ActualReturnDate, Status)
                VALUES(@VisitId, @ContractId, NULL, 'RETURN', @VisitDate, NULL, 'SCHEDULED');
                COMMIT TRANSACTION;
                SELECT VisitId, EntityId, EmployeeId, VisitType, VisitDate, ActualReturnDate, Status
                FROM dbo.Visit WHERE VisitId = @VisitId;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """;

    private const string RescheduleSql = """
        CREATE OR ALTER PROCEDURE dbo.usp_RescheduleReturnVisit
            @VisitId UNIQUEIDENTIFIER, @CustomerId UNIQUEIDENTIFIER, @VisitDate DATE,
            @UserAccountId UNIQUEIDENTIFIER, @NowUtc DATETIME2(3) = NULL
        AS
        BEGIN
            SET NOCOUNT ON; SET XACT_ABORT ON;
            IF @NowUtc IS NULL SET @NowUtc = SYSUTCDATETIME();
            BEGIN TRY
                BEGIN TRANSACTION;
                -- Discover immutable EntityId, then take locks in Contract -> Visit order.
                DECLARE @ContractId UNIQUEIDENTIFIER;
                SELECT @ContractId = EntityId FROM dbo.Visit WHERE VisitId = @VisitId AND VisitType = 'RETURN';
                DECLARE @ContractStatus VARCHAR(50), @Owner UNIQUEIDENTIFIER;
                SELECT @ContractStatus = Status, @Owner = CustomerId FROM dbo.Contract WITH (UPDLOCK, HOLDLOCK)
                WHERE ContractId = @ContractId;
                IF @ContractStatus IS NULL OR @Owner <> @CustomerId OR @CustomerId IS NULL
                    THROW 51301, 'RESOURCE_NOT_FOUND', 1;
                DECLARE @Status VARCHAR(50), @ActualReturnDate DATETIME2(3);
                SELECT @Status = Status, @ActualReturnDate = ActualReturnDate FROM dbo.Visit WITH (UPDLOCK, HOLDLOCK)
                WHERE VisitId = @VisitId AND EntityId = @ContractId AND VisitType = 'RETURN';
                IF @Status IS NULL THROW 51301, 'RESOURCE_NOT_FOUND', 1;
                DECLARE @AccountStatus VARCHAR(50), @Role VARCHAR(50);
                SELECT @AccountStatus = a.Status, @Role = r.RoleName
                FROM dbo.Customer c WITH (HOLDLOCK)
                JOIN dbo.UserAccount a WITH (HOLDLOCK) ON a.UserAccountId = c.UserAccountId
                JOIN dbo.UserRole r WITH (HOLDLOCK) ON r.RoleId = a.RoleId
                WHERE c.CustomerId = @CustomerId AND a.UserAccountId = @UserAccountId;
                IF @AccountStatus IS NULL THROW 51301, 'RESOURCE_NOT_FOUND', 1;
                IF @AccountStatus <> 'ACTIVE' THROW 51302, 'ACCOUNT_INACTIVE', 1;
                IF @Role <> 'CUSTOMER' THROW 51303, 'FORBIDDEN', 1;
                IF @Status <> 'SCHEDULED' OR @ActualReturnDate IS NOT NULL THROW 51307, 'VISIT_INVALID_STATUS', 1;
                IF @ContractStatus <> 'ACTIVE' THROW 51304, 'CONTRACT_NOT_ACTIVE', 1;
                IF @VisitDate IS NULL THROW 51308, 'VALIDATION_ERROR', 1;
                IF @VisitDate < CAST(DATEADD(HOUR, 7, @NowUtc) AS DATE) THROW 51306, 'VISIT_DATE_IN_PAST', 1;
                UPDATE dbo.Visit SET VisitDate = @VisitDate WHERE VisitId = @VisitId;
                COMMIT TRANSACTION;
                SELECT VisitId, EntityId, EmployeeId, VisitType, VisitDate, ActualReturnDate, Status
                FROM dbo.Visit WHERE VisitId = @VisitId;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """;

    private const string LegacyCreateSql = """
        CREATE OR ALTER PROCEDURE dbo.usp_CreateReturnVisit
            @ContractId UNIQUEIDENTIFIER, @CustomerId UNIQUEIDENTIFIER, @VisitDate DATE
        AS
        BEGIN
            SET NOCOUNT ON; SET XACT_ABORT ON;
            BEGIN TRY
                BEGIN TRANSACTION;
                IF NOT EXISTS (SELECT 1 FROM dbo.Contract WITH (UPDLOCK, HOLDLOCK)
                    WHERE ContractId = @ContractId AND CustomerId = @CustomerId AND Status = 'ACTIVE')
                    THROW 51120, 'CONTRACT_NOT_ACTIVE_OR_NOT_OWNED', 1;
                IF EXISTS (SELECT 1 FROM dbo.Visit WHERE EntityId = @ContractId AND VisitType = 'RETURN'
                    AND Status IN ('SCHEDULED','CHECKED_IN')) THROW 51122, 'RETURN_VISIT_PENDING', 1;
                IF @VisitDate < CAST(SYSUTCDATETIME() AS DATE) THROW 51123, 'VISIT_DATE_IN_PAST', 1;
                DECLARE @VisitId UNIQUEIDENTIFIER = NEWID();
                INSERT dbo.Visit(VisitId, EntityId, VisitType, VisitDate, Status)
                VALUES(@VisitId, @ContractId, 'RETURN', @VisitDate, 'SCHEDULED');
                COMMIT TRANSACTION;
                SELECT @VisitId AS VisitId, 'SCHEDULED' AS Status;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """;
}
