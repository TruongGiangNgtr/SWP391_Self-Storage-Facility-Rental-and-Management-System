using System;
using Frms.DataAccess.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.DataAccess.Migrations;

[DbContext(typeof(FrmsDbContext))]
[Migration("20261011130000_AlignFinalizeReturnV101")]
public sealed class AlignFinalizeReturnV101 : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        const string definition = """
        CREATE OR ALTER PROCEDURE dbo.usp_FinalizeReturn
            @ContractId UNIQUEIDENTIFIER,
            @StorageUnitStatus VARCHAR(20),
            @EmployeeId UNIQUEIDENTIFIER
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;
            SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

            IF @ContractId IS NULL OR @EmployeeId IS NULL
               OR @StorageUnitStatus IS NULL
               OR @StorageUnitStatus NOT IN ('AVAILABLE', 'MAINTENANCE')
                THROW 51280, 'VALIDATION_ERROR', 1;

            BEGIN TRY
                BEGIN TRANSACTION;

                DECLARE @FacilityId UNIQUEIDENTIFIER,
                        @StorageUnitId UNIQUEIDENTIFIER,
                        @ReservationId UNIQUEIDENTIFIER,
                        @ContractEndMonth DATE,
                        @ContractStatus VARCHAR(50),
                        @CurrentUnitStatus VARCHAR(50),
                        @InspectionId UNIQUEIDENTIFIER,
                        @ReturnVisitId UNIQUEIDENTIFIER,
                        @ActualReturnDate DATETIME2(3),
                        @ActorAccountId UNIQUEIDENTIFIER;

                SELECT @FacilityId = c.FacilityId,
                       @StorageUnitId = c.StorageUnitId,
                       @ReservationId = c.ReservationId,
                       @ContractEndMonth = c.EndMonth,
                       @ContractStatus = c.Status,
                       @CurrentUnitStatus = su.Status
                FROM dbo.Contract AS c WITH (UPDLOCK, HOLDLOCK)
                JOIN dbo.StorageUnit AS su WITH (UPDLOCK, HOLDLOCK)
                  ON su.StorageUnitId = c.StorageUnitId
                WHERE c.ContractId = @ContractId;

                IF @ContractStatus IS NULL
                    THROW 51281, 'RESOURCE_NOT_FOUND', 1;

                IF @ContractStatus <> 'ACTIVE'
                   OR EXISTS (
                       SELECT 1
                       FROM dbo.DepositSettlement WITH (UPDLOCK, HOLDLOCK)
                       WHERE ContractId = @ContractId
                   )
                    THROW 51282, 'RETURN_ALREADY_FINALIZED', 1;

                SELECT @ActorAccountId = e.UserAccountId
                FROM dbo.Employee AS e
                JOIN dbo.UserAccount AS ua WITH (UPDLOCK, HOLDLOCK)
                  ON ua.UserAccountId = e.UserAccountId
                JOIN dbo.UserRole AS ur
                  ON ur.RoleId = ua.RoleId
                WHERE e.EmployeeId = @EmployeeId
                  AND e.FacilityId = @FacilityId
                  AND ua.Status = 'ACTIVE'
                  AND ur.RoleName = 'FACILITY_STAFF';

                IF @ActorAccountId IS NULL
                    THROW 51283, 'FORBIDDEN', 1;

                IF @CurrentUnitStatus <> 'INSPECTION'
                    THROW 51284, 'UNIT_INVALID_STATUS', 1;

                IF EXISTS (
                    SELECT 1
                    FROM dbo.Inspection WITH (UPDLOCK, HOLDLOCK)
                    WHERE ContractId = @ContractId
                      AND StorageUnitId = @StorageUnitId
                      AND Status <> 'COMPLETED'
                )
                    THROW 51285, 'INSPECTION_INVALID_STATUS', 1;

                SELECT TOP (1)
                       @InspectionId = i.InspectionId,
                       @ReturnVisitId = i.VisitId
                FROM dbo.Inspection AS i WITH (UPDLOCK, HOLDLOCK)
                WHERE i.ContractId = @ContractId
                  AND i.StorageUnitId = @StorageUnitId
                  AND i.Status = 'COMPLETED'
                ORDER BY i.CompletedAt DESC, i.InspectionId DESC;

                IF @InspectionId IS NULL
                    THROW 51285, 'INSPECTION_INVALID_STATUS', 1;

                IF @ReturnVisitId IS NOT NULL
                BEGIN
                    SELECT @ActualReturnDate = v.ActualReturnDate
                    FROM dbo.Visit AS v WITH (UPDLOCK, HOLDLOCK)
                    WHERE v.VisitId = @ReturnVisitId
                      AND v.EntityId = @ContractId
                      AND v.VisitType = 'RETURN';

                    IF @ActualReturnDate IS NULL
                        THROW 51286, 'VISIT_ENTITY_MISMATCH', 1;
                END;

                IF EXISTS (
                    SELECT 1
                    FROM dbo.DamageRecord AS d WITH (UPDLOCK, HOLDLOCK)
                    WHERE d.InspectionId = @InspectionId
                      AND d.Status = 'PENDING'
                )
                    THROW 51287, 'DAMAGE_DECISION_PENDING', 1;

                DECLARE @DepositPaid DECIMAL(18,2),
                        @LateAmount DECIMAL(18,2),
                        @ExtraAmount DECIMAL(18,2),
                        @DamageAmount DECIMAL(18,2),
                        @TotalDeduction DECIMAL(18,2),
                        @RefundAmount DECIMAL(18,2),
                        @AdditionalAmountDue DECIMAL(18,2),
                        @TerminalStatus VARCHAR(50);

                SELECT @DepositPaid = SUM(i.AmountDue)
                FROM dbo.Invoice AS i WITH (UPDLOCK, HOLDLOCK)
                WHERE i.EntityId = @ReservationId
                  AND i.InvoiceType = 'DEPOSIT'
                  AND i.Status = 'PAID';

                IF @DepositPaid IS NULL
                    THROW 51288, 'VALIDATION_ERROR', 1;

                SELECT @LateAmount = COALESCE(SUM(lf.Amount), 0)
                FROM dbo.LateFee AS lf WITH (UPDLOCK, HOLDLOCK)
                JOIN dbo.Invoice AS i
                  ON i.InvoiceId = lf.InvoiceId
                WHERE i.EntityId = @ContractId
                  AND i.InvoiceType = 'RENTAL_FEE'
                  AND i.Status <> 'CANCELLED';

                SELECT @ExtraAmount = COALESCE(SUM(ef.Amount), 0)
                FROM dbo.ExtraFee AS ef WITH (UPDLOCK, HOLDLOCK)
                WHERE ef.InspectionId = @InspectionId;

                SELECT @DamageAmount = COALESCE(SUM(dr.DamageAmount), 0)
                FROM dbo.DamageRecord AS dr WITH (UPDLOCK, HOLDLOCK)
                WHERE dr.InspectionId = @InspectionId
                  AND dr.Status = 'APPROVED';

                SET @TotalDeduction =
                    @LateAmount + @ExtraAmount + @DamageAmount;

                SET @AdditionalAmountDue = CASE
                    WHEN @TotalDeduction > @DepositPaid
                        THEN @TotalDeduction - @DepositPaid
                    ELSE 0
                END;

                IF @ActualReturnDate IS NOT NULL
                   AND CAST(@ActualReturnDate AS DATE)
                       < EOMONTH(@ContractEndMonth)
                BEGIN
                    SET @TerminalStatus = 'TERMINATED';
                    SET @RefundAmount = 0;
                END
                ELSE
                BEGIN
                    SET @TerminalStatus = 'COMPLETED';

                    SET @RefundAmount = CASE
                        WHEN @DepositPaid > @TotalDeduction
                            THEN @DepositPaid - @TotalDeduction
                        ELSE 0
                    END;
                END;

                INSERT INTO dbo.DepositSettlement (
                    DepositSettlementId,
                    ContractId,
                    TotalDeduction,
                    RefundAmount,
                    AdditionalAmountDue,
                    Status,
                    CalculatedAt
                )
                VALUES (
                    NEWID(),
                    @ContractId,
                    @TotalDeduction,
                    @RefundAmount,
                    @AdditionalAmountDue,
                    'FINALIZED',
                    SYSUTCDATETIME()
                );

                UPDATE dbo.Contract
                SET Status = @TerminalStatus
                WHERE ContractId = @ContractId
                  AND Status = 'ACTIVE';

                IF @@ROWCOUNT <> 1
                    THROW 51282, 'RETURN_ALREADY_FINALIZED', 1;

                UPDATE dbo.StorageUnit
                SET Status = @StorageUnitStatus
                WHERE StorageUnitId = @StorageUnitId
                  AND Status = 'INSPECTION';

                IF @@ROWCOUNT <> 1
                    THROW 51284, 'UNIT_INVALID_STATUS', 1;

                INSERT INTO dbo.AuditLog (
                    UserAccountId,
                    Action,
                    EntityType,
                    EntityId,
                    CreatedAt
                )
                VALUES (
                    @ActorAccountId,
                    'FINALIZE_RETURN',
                    'CONTRACT',
                    @ContractId,
                    SYSUTCDATETIME()
                );

                COMMIT TRANSACTION;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0
                    ROLLBACK TRANSACTION;

                THROW;
            END CATCH
        END;
        """;

        var escapedDefinition =
            definition.Replace("'", "''", StringComparison.Ordinal);

        migrationBuilder.Sql($"EXEC(N'{escapedDefinition}');");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        throw new NotSupportedException(
            "FinalizeReturn SQL downgrade requires approved manual review; previous procedure definition is not preserved.");
    }
}
