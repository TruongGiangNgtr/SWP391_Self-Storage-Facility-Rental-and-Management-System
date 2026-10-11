// Generated from FRMS_Database_V2_Final_InspectionSafe.sql (uploaded legacy DB source).
// RESERVATION-FOCUSED RESTORE: legacy SQL is installed only if missing, except current V10.1 Handover/billing procedures which are upgraded.
// V2 stored procedure semantics are NOT automatically aligned with SRS V10.1.
// CompleteHandover and first-month billing exclusions use V10.1 semantics; Return/Inspection remain out of scope.
// No SQL Server data, tables, system configuration, or migration history is modified manually.
using System;
using Frms.DataAccess.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Frms.DataAccess.Migrations;

[DbContext(typeof(FrmsDbContext))]
[Migration("20261011120000_RestoreLegacySqlObjects")]
public sealed class RestoreLegacySqlObjects : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Executes each CREATE OR ALTER definition as its own SQL batch via sp_executesql.
        // SQL Server requires CREATE OR ALTER PROCEDURE/TRIGGER to be first in its batch.
        // Payment procedures from newer migrations are preserved; the owner-approved Handover and first-month billing definitions are upgraded.
        // Reservation dependencies absent from InitialPhaseZero EF migration.
        // Guard DDL so this repair is non-destructive on an existing database.
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.Reservation', N'U') IS NULL
                THROW 51250, 'Cannot restore Reservation procedures: dbo.Reservation is missing.', 1;
            IF OBJECT_ID(N'dbo.Visit', N'U') IS NULL
                THROW 51250, 'Cannot restore Reservation procedures: dbo.Visit is missing.', 1;

            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE object_id = OBJECT_ID(N'dbo.Reservation')
                  AND name = N'IX_Reservation_Capacity')
                CREATE INDEX IX_Reservation_Capacity
                    ON dbo.Reservation(FacilityId, UnitTypeId, StartMonth, EndMonth, Status);

            -- At most one RESERVATION Visit for each Reservation, also under concurrency.
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE object_id = OBJECT_ID(N'dbo.Visit')
                  AND name = N'UX_Visit_Reservation_OneVisit')
                CREATE UNIQUE INDEX UX_Visit_Reservation_OneVisit
                    ON dbo.Visit(EntityId)
                    WHERE VisitType = 'RESERVATION';
            """);

        // TRIGGER: dbo.trg_Visit_ValidateEntity — last definition from legacy source batch 70.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Visit_ValidateEntity", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Visit_ValidateEntity
        ON dbo.Visit
        AFTER INSERT, UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                WHERE i.VisitType = 'RESERVATION'
                  AND NOT EXISTS (
                      SELECT 1 FROM dbo.Reservation r WHERE r.ReservationId = i.EntityId
                  )
            )
                THROW 51001, 'VISIT_ENTITY_MISMATCH', 1;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                WHERE i.VisitType IN ('ACCESS','RETURN')
                  AND NOT EXISTS (
                      SELECT 1 FROM dbo.Contract c WHERE c.ContractId = i.EntityId
                  )
            )
                THROW 51001, 'VISIT_ENTITY_MISMATCH', 1;

            IF EXISTS (
                SELECT 1 FROM inserted
                WHERE VisitType <> 'RETURN' AND ActualReturnDate IS NOT NULL
            )
                THROW 51002, 'ActualReturnDate is allowed only for RETURN visits.', 1;
        END;
        """);

        // TRIGGER: dbo.trg_Invoice_ValidateEntity — last definition from legacy source batch 71.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Invoice_ValidateEntity", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Invoice_ValidateEntity
        ON dbo.Invoice
        AFTER INSERT, UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                WHERE i.InvoiceType = 'DEPOSIT'
                  AND NOT EXISTS (
                      SELECT 1 FROM dbo.Reservation r WHERE r.ReservationId = i.EntityId
                  )
            )
                THROW 51003, 'INVOICE_ENTITY_MISMATCH', 1;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                WHERE i.InvoiceType = 'RENTAL_FEE'
                  AND NOT EXISTS (
                      SELECT 1 FROM dbo.Contract c WHERE c.ContractId = i.EntityId
                  )
            )
                THROW 51003, 'INVOICE_ENTITY_MISMATCH', 1;

            IF EXISTS (
                SELECT 1 FROM inserted
                WHERE InvoiceType = 'DEPOSIT'
                  AND (DiscountId IS NOT NULL OR DiscountAmount <> 0)
            )
                THROW 51004, 'Deposit invoice cannot use a discount.', 1;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN dbo.Contract c ON c.ContractId = i.EntityId
                WHERE i.InvoiceType = 'RENTAL_FEE'
                  AND i.DiscountId IS NOT NULL
                  AND (c.DiscountId IS NULL OR c.DiscountId <> i.DiscountId)
            )
                THROW 51004, 'Rental invoice DiscountId must match the Contract-selected Discount when applied.', 1;
        END;
        """);

        // TRIGGER: dbo.trg_Invoice_ProtectSnapshot — last definition from legacy source batch 72.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Invoice_ProtectSnapshot", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Invoice_ProtectSnapshot
        ON dbo.Invoice
        AFTER UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;

            /* Primary identity and all issued snapshot fields are immutable. */
            IF UPDATE(InvoiceId)
                THROW 51215, 'INVOICE_SNAPSHOT_IMMUTABLE', 1;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN deleted d ON d.InvoiceId = i.InvoiceId
                WHERE i.EntityId <> d.EntityId
                   OR i.InvoiceType <> d.InvoiceType
                   OR (i.BillingMonth <> d.BillingMonth
                       OR (i.BillingMonth IS NULL AND d.BillingMonth IS NOT NULL)
                       OR (i.BillingMonth IS NOT NULL AND d.BillingMonth IS NULL))
                   OR i.BaseAmount <> d.BaseAmount
                   OR (i.DiscountId <> d.DiscountId
                       OR (i.DiscountId IS NULL AND d.DiscountId IS NOT NULL)
                       OR (i.DiscountId IS NOT NULL AND d.DiscountId IS NULL))
                   OR i.DiscountAmount <> d.DiscountAmount
                   OR i.AmountDue <> d.AmountDue
                   OR i.DueDate <> d.DueDate
                   OR i.CreatedAt <> d.CreatedAt
            )
                THROW 51215, 'INVOICE_SNAPSHOT_IMMUTABLE', 1;
        END;
        """);

        // TRIGGER: dbo.trg_Invoice_StatusTransition — last definition from legacy source batch 73.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Invoice_StatusTransition", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Invoice_StatusTransition
        ON dbo.Invoice
        AFTER UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN deleted d ON d.InvoiceId = i.InvoiceId
                WHERE i.Status <> d.Status
                  AND NOT (
                        (d.Status = 'UNPAID' AND i.Status IN ('PAID','CANCELLED'))
                     OR (d.Status = 'UNPAID' AND i.Status = 'OVERDUE' AND d.InvoiceType = 'RENTAL_FEE')
                     OR (d.Status = 'OVERDUE' AND i.Status = 'PAID')
                  )
            )
                THROW 51216, 'INVOICE_INVALID_STATUS', 1;
        END;
        """);

        // TRIGGER: dbo.trg_Reservation_StatusTransition — last definition from legacy source batch 74.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Reservation_StatusTransition", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Reservation_StatusTransition
        ON dbo.Reservation
        AFTER UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN deleted d ON d.ReservationId = i.ReservationId
                WHERE i.Status <> d.Status
                  AND NOT (
                      (d.Status = 'PENDING_DEPOSIT' AND i.Status IN ('CONFIRMED','CANCELLED'))
                      OR (d.Status = 'CONFIRMED' AND i.Status IN ('COMPLETED','CANCELLED'))
                  )
            )
                THROW 51005, 'RESERVATION_INVALID_STATUS', 1;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN deleted d ON d.ReservationId = i.ReservationId
                WHERE i.CustomerId <> d.CustomerId
                   OR i.FacilityId <> d.FacilityId
                   OR i.UnitTypeId <> d.UnitTypeId
                   OR i.PolicyId <> d.PolicyId
                   OR i.StartMonth <> d.StartMonth
                   OR i.EndMonth <> d.EndMonth
                   OR i.LockedRentalPrice <> d.LockedRentalPrice
                   OR i.DepositAmount <> d.DepositAmount
            )
                THROW 51006, 'Reservation business snapshot fields are immutable.', 1;
        END;
        """);

        // TRIGGER: dbo.trg_Visit_StatusTransition — last definition from legacy source batch 75.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Visit_StatusTransition", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Visit_StatusTransition
        ON dbo.Visit
        AFTER UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN deleted d ON d.VisitId = i.VisitId
                WHERE i.Status <> d.Status
                  AND NOT (
                      (d.Status = 'SCHEDULED' AND i.Status IN ('CHECKED_IN','CANCELLED'))
                      OR (d.Status = 'CHECKED_IN' AND i.Status = 'CHECKED_OUT')
                  )
            )
                THROW 51007, 'VISIT_INVALID_STATUS', 1;
        END;
        """);

        // TRIGGER: dbo.trg_Contract_StatusTransition — last definition from legacy source batch 76.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Contract_StatusTransition", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Contract_StatusTransition
        ON dbo.Contract
        AFTER UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN deleted d ON d.ContractId = i.ContractId
                WHERE i.Status <> d.Status
                  AND NOT (d.Status = 'ACTIVE' AND i.Status IN ('COMPLETED','TERMINATED'))
            )
                THROW 51008, 'CONTRACT_INVALID_STATUS', 1;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN deleted d ON d.ContractId = i.ContractId
                WHERE ISNULL(CONVERT(VARCHAR(36),i.DiscountId),'') <> ISNULL(CONVERT(VARCHAR(36),d.DiscountId),'')
            )
                THROW 51009, 'Contract-selected Discount is immutable in V1.', 1;
        END;
        """);

        // TRIGGER: dbo.trg_Inspection_StatusTransition — last definition from legacy source batch 77.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Inspection_StatusTransition", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Inspection_StatusTransition
        ON dbo.Inspection
        AFTER UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN deleted d ON d.InspectionId = i.InspectionId
                WHERE i.Status <> d.Status
                  AND NOT (
                      (d.Status = 'PENDING' AND i.Status = 'IN_PROGRESS')
                      OR (d.Status = 'IN_PROGRESS' AND i.Status = 'COMPLETED')
                  )
            )
                THROW 51010, 'INSPECTION_INVALID_STATUS', 1;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN deleted d ON d.InspectionId = i.InspectionId
                WHERE d.Status = 'IN_PROGRESS'
                  AND i.EmployeeId <> d.EmployeeId
            )
                THROW 51011, 'Inspection claim actor cannot be overwritten.', 1;
        END;
        """);

        // TRIGGER: dbo.trg_Inspection_ValidateVisit — last definition from legacy source batch 78.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Inspection_ValidateVisit", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Inspection_ValidateVisit
        ON dbo.Inspection
        AFTER INSERT, UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                LEFT JOIN dbo.Visit v ON v.VisitId = i.VisitId
                WHERE i.VisitId IS NOT NULL
                  AND (
                      v.VisitId IS NULL
                      OR v.VisitType <> 'RETURN'
                      OR v.EntityId <> i.ContractId
                  )
            )
                THROW 51012, 'Inspection VisitId must reference a RETURN Visit for the same Contract.', 1;
        END;
        """);

        // TRIGGER: dbo.trg_SupportTicket_StatusTransition — last definition from legacy source batch 80.
        RestoreIfMissing(migrationBuilder, "dbo.trg_SupportTicket_StatusTransition", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_SupportTicket_StatusTransition
        ON dbo.SupportTicket
        AFTER UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN deleted d ON d.SupportTicketId = i.SupportTicketId
                WHERE i.Status <> d.Status
                  AND NOT (
                      (d.Status = 'OPEN' AND i.Status IN ('IN_PROGRESS','CANCELLED'))
                      OR (d.Status = 'IN_PROGRESS' AND i.Status IN ('COMPLETED','CANCELLED'))
                  )
            )
                THROW 51014, 'SUPPORT_TICKET_INVALID_STATUS', 1;
        END;
        """);

        // TRIGGER: dbo.trg_Policy_ProtectHistoricalVersion — last definition from legacy source batch 81.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Policy_ProtectHistoricalVersion", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Policy_ProtectHistoricalVersion
        ON dbo.Policy
        INSTEAD OF DELETE
        AS
        BEGIN
            SET NOCOUNT ON;
            THROW 51015, 'Historical Policy rows cannot be deleted.', 1;
        END;
        """);

        // TRIGGER: dbo.trg_Policy_ProtectBusinessValues — last definition from legacy source batch 82.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Policy_ProtectBusinessValues", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Policy_ProtectBusinessValues
        ON dbo.Policy
        AFTER UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN deleted d ON d.PolicyId = i.PolicyId
                WHERE i.Version <> d.Version
                   OR i.EffectiveFrom <> d.EffectiveFrom
                   OR i.DepositTimeoutHours <> d.DepositTimeoutHours
                   OR i.ReservationVisitStartDay <> d.ReservationVisitStartDay
                   OR i.ReservationVisitEndDay <> d.ReservationVisitEndDay
                   OR i.MonthlyPaymentDueDay <> d.MonthlyPaymentDueDay
                   OR i.OverdueStartDay <> d.OverdueStartDay
                   OR i.LateFeeDivisorDays <> d.LateFeeDivisorDays
                   OR i.EarlyReturnWaiveFeeUntilDay <> d.EarlyReturnWaiveFeeUntilDay
                   OR i.CreatedAt <> d.CreatedAt
            )
                THROW 51016, 'Historical Policy business values are immutable.', 1;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN deleted d ON d.PolicyId = i.PolicyId
                WHERE (
                        (i.EffectiveTo <> d.EffectiveTo)
                        OR (i.EffectiveTo IS NULL AND d.EffectiveTo IS NOT NULL)
                        OR (i.EffectiveTo IS NOT NULL AND d.EffectiveTo IS NULL)
                      )
                  AND NOT (
                        d.Status = 'ACTIVE'
                        AND i.Status = 'INACTIVE'
                        AND d.EffectiveTo IS NULL
                        AND i.EffectiveTo IS NOT NULL
                      )
            )
                THROW 51016, 'Historical Policy EffectiveTo is immutable after inactivation.', 1;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN deleted d ON d.PolicyId = i.PolicyId
                WHERE i.Status <> d.Status
                  AND NOT (d.Status = 'ACTIVE' AND i.Status = 'INACTIVE')
            )
                THROW 51017, 'Invalid Policy status transition.', 1;
        END;
        """);

        // TRIGGER: dbo.trg_ContractExtension_AppendOnly — last definition from legacy source batch 83.
        RestoreIfMissing(migrationBuilder, "dbo.trg_ContractExtension_AppendOnly", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_ContractExtension_AppendOnly
        ON dbo.ContractExtension
        INSTEAD OF UPDATE, DELETE
        AS
        BEGIN
            SET NOCOUNT ON;
            THROW 51018, 'ContractExtension is append-only.', 1;
        END;
        """);

        // TRIGGER: dbo.trg_LoginHistory_AppendOnly — last definition from legacy source batch 84.
        RestoreIfMissing(migrationBuilder, "dbo.trg_LoginHistory_AppendOnly", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_LoginHistory_AppendOnly
        ON dbo.LoginHistory
        INSTEAD OF UPDATE, DELETE
        AS
        BEGIN
            SET NOCOUNT ON;
            THROW 51019, 'LoginHistory is append-only.', 1;
        END;
        """);

        // TRIGGER: dbo.trg_AuditLog_AppendOnly — last definition from legacy source batch 85.
        RestoreIfMissing(migrationBuilder, "dbo.trg_AuditLog_AppendOnly", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_AuditLog_AppendOnly
        ON dbo.AuditLog
        INSTEAD OF UPDATE, DELETE
        AS
        BEGIN
            SET NOCOUNT ON;
            THROW 51020, 'AuditLog is append-only.', 1;
        END;
        """);

        // TRIGGER: dbo.trg_Contract_ValidateDiscountOwnership — last definition from legacy source batch 86.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Contract_ValidateDiscountOwnership", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Contract_ValidateDiscountOwnership
        ON dbo.Contract
        AFTER INSERT, UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                LEFT JOIN deleted d ON d.ContractId = i.ContractId
                LEFT JOIN dbo.Discount x ON x.DiscountId = i.DiscountId
                WHERE i.DiscountId IS NOT NULL
                  AND (d.ContractId IS NULL OR d.DiscountId IS NULL OR i.DiscountId <> d.DiscountId)
                  AND (
                      x.DiscountId IS NULL
                      OR x.CustomerId <> i.CustomerId
                      OR x.Status <> 'ACTIVE'
                      OR x.EffectiveFrom > SYSUTCDATETIME()
                      OR (x.EffectiveTo IS NOT NULL AND x.EffectiveTo < SYSUTCDATETIME())
                  )
            )
                THROW 51021, 'DISCOUNT_NOT_VALID_OR_NOT_OWNED_BY_CUSTOMER', 1;
        END;
        """);

        // TRIGGER: dbo.trg_Employee_ValidateFacilityByRole — last definition from legacy source batch 87.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Employee_ValidateFacilityByRole", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Employee_ValidateFacilityByRole
        ON dbo.Employee
        AFTER INSERT, UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;
            IF TRY_CONVERT(INT, CONVERT(NVARCHAR(20), SESSION_CONTEXT(N'FRMS_ROLE_FACILITY_UPDATE'))) = 1 RETURN;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN dbo.UserAccount ua ON ua.UserAccountId = i.UserAccountId
                JOIN dbo.UserRole ur ON ur.RoleId = ua.RoleId
                WHERE (ur.RoleName IN ('FACILITY_STAFF','FACILITY_MANAGER') AND i.FacilityId IS NULL)
                   OR (ur.RoleName IN ('BUSINESS_OPERATIONS_MANAGER','SYSTEM_ADMINISTRATOR') AND i.FacilityId IS NOT NULL)
                   OR ur.RoleName = 'CUSTOMER'
            )
                THROW 51022, 'EMPLOYEE_ROLE_FACILITY_INVALID', 1;
        END;
        """);

        // TRIGGER: dbo.trg_UserAccount_ValidateRoleProfile — last definition from legacy source batch 88.
        RestoreIfMissing(migrationBuilder, "dbo.trg_UserAccount_ValidateRoleProfile", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_UserAccount_ValidateRoleProfile
        ON dbo.UserAccount
        AFTER UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;
            IF TRY_CONVERT(INT, CONVERT(NVARCHAR(20), SESSION_CONTEXT(N'FRMS_ROLE_FACILITY_UPDATE'))) = 1 RETURN;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN dbo.UserRole ur ON ur.RoleId = i.RoleId
                LEFT JOIN dbo.Customer c ON c.UserAccountId = i.UserAccountId
                LEFT JOIN dbo.Employee e ON e.UserAccountId = i.UserAccountId
                WHERE (ur.RoleName = 'CUSTOMER' AND e.EmployeeId IS NOT NULL)
                   OR (ur.RoleName <> 'CUSTOMER' AND c.CustomerId IS NOT NULL)
                   OR (
                        e.EmployeeId IS NOT NULL
                        AND (
                            (ur.RoleName IN ('FACILITY_STAFF','FACILITY_MANAGER') AND e.FacilityId IS NULL)
                            OR
                            (ur.RoleName IN ('BUSINESS_OPERATIONS_MANAGER','SYSTEM_ADMINISTRATOR') AND e.FacilityId IS NOT NULL)
                        )
                   )
            )
                THROW 51023, 'ACCOUNT_ROLE_PROFILE_INVALID', 1;
        END;
        """);

        // TRIGGER: dbo.trg_LateFee_ValidateInvoice — last definition from legacy source batch 89.
        RestoreIfMissing(migrationBuilder, "dbo.trg_LateFee_ValidateInvoice", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_LateFee_ValidateInvoice
        ON dbo.LateFee
        AFTER INSERT, UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN dbo.Invoice inv ON inv.InvoiceId = i.InvoiceId
                LEFT JOIN dbo.Contract c
                  ON c.ContractId = inv.EntityId AND inv.InvoiceType = 'RENTAL_FEE'
                LEFT JOIN dbo.Policy p
                  ON p.PolicyId = c.PolicyId
                WHERE inv.InvoiceType <> 'RENTAL_FEE'
                   OR inv.Status NOT IN ('OVERDUE','PAID')
                   OR (
                        inv.Status = 'PAID'
                        AND NOT EXISTS (
                            SELECT 1
                            FROM dbo.Payment pay
                            WHERE pay.InvoiceId = inv.InvoiceId
                              AND pay.Status = 'SUCCESS'
                              AND pay.PaidAt >= CAST(
                                    DATEFROMPARTS(
                                        YEAR(inv.BillingMonth),
                                        MONTH(inv.BillingMonth),
                                        CASE
                                            WHEN p.OverdueStartDay > DAY(EOMONTH(inv.BillingMonth))
                                                THEN DAY(EOMONTH(inv.BillingMonth))
                                            ELSE p.OverdueStartDay
                                        END
                                    ) AS DATETIME2(3)
                                )
                        )
                      )
            )
                THROW 51024, 'LateFee is valid only for an overdue Rental Fee invoice.', 1;
        END;
        """);

        // TRIGGER: dbo.trg_Audit_Reservation — last definition from legacy source batch 90.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Audit_Reservation", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Audit_Reservation
        ON dbo.Reservation
        AFTER INSERT, UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;
            DECLARE @Actor UNIQUEIDENTIFIER=TRY_CONVERT(UNIQUEIDENTIFIER,CONVERT(NVARCHAR(36),SESSION_CONTEXT(N'UserAccountId')));

            INSERT INTO dbo.AuditLog(UserAccountId,Action,EntityType,EntityId)
            SELECT @Actor,
                   CASE
                     WHEN d.ReservationId IS NULL THEN 'CREATE_RESERVATION'
                     WHEN d.Status<>i.Status AND i.Status='CONFIRMED' THEN 'CONFIRM_RESERVATION'
                     WHEN d.Status<>i.Status AND i.Status='CANCELLED' THEN 'CANCEL_RESERVATION'
                     ELSE 'UPDATE_RESERVATION'
                   END,
                   'RESERVATION',i.ReservationId
            FROM inserted i
            LEFT JOIN deleted d ON d.ReservationId=i.ReservationId;
        END;
        """);

        // TRIGGER: dbo.trg_Audit_Visit — last definition from legacy source batch 91.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Audit_Visit", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Audit_Visit
        ON dbo.Visit
        AFTER INSERT, UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;
            DECLARE @Actor UNIQUEIDENTIFIER=TRY_CONVERT(UNIQUEIDENTIFIER,CONVERT(NVARCHAR(36),SESSION_CONTEXT(N'UserAccountId')));

            INSERT INTO dbo.AuditLog(UserAccountId,Action,EntityType,EntityId)
            SELECT @Actor,
                   CASE
                     WHEN d.VisitId IS NULL THEN 'CREATE_VISIT'
                     WHEN d.Status<>i.Status AND i.Status='CHECKED_IN' THEN 'CHECK_IN_VISIT'
                     WHEN d.Status<>i.Status AND i.Status='CHECKED_OUT' THEN 'CHECK_OUT_VISIT'
                     WHEN d.Status<>i.Status AND i.Status='CANCELLED' THEN 'CANCEL_VISIT'
                     ELSE 'UPDATE_VISIT'
                   END,
                   'VISIT',i.VisitId
            FROM inserted i
            LEFT JOIN deleted d ON d.VisitId=i.VisitId;
        END;
        """);

        // TRIGGER: dbo.trg_Audit_Contract — last definition from legacy source batch 92.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Audit_Contract", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Audit_Contract
        ON dbo.Contract
        AFTER INSERT, UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;
            DECLARE @Actor UNIQUEIDENTIFIER=TRY_CONVERT(UNIQUEIDENTIFIER,CONVERT(NVARCHAR(36),SESSION_CONTEXT(N'UserAccountId')));

            INSERT INTO dbo.AuditLog(UserAccountId,Action,EntityType,EntityId)
            SELECT @Actor,
                   CASE
                     WHEN d.ContractId IS NULL THEN 'COMPLETE_HANDOVER'
                     WHEN d.Status<>i.Status AND i.Status IN('COMPLETED','TERMINATED') THEN 'FINALIZE_RETURN'
                     ELSE 'UPDATE_CONTRACT'
                   END,
                   'CONTRACT',i.ContractId
            FROM inserted i
            LEFT JOIN deleted d ON d.ContractId=i.ContractId;
        END;
        """);

        // TRIGGER: dbo.trg_Audit_Invoice — last definition from legacy source batch 93.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Audit_Invoice", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Audit_Invoice
        ON dbo.Invoice
        AFTER INSERT, UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;
            DECLARE @Actor UNIQUEIDENTIFIER=TRY_CONVERT(UNIQUEIDENTIFIER,CONVERT(NVARCHAR(36),SESSION_CONTEXT(N'UserAccountId')));

            INSERT INTO dbo.AuditLog(UserAccountId,Action,EntityType,EntityId)
            SELECT @Actor,
                   CASE
                     WHEN d.InvoiceId IS NULL AND i.InvoiceType='RENTAL_FEE' THEN 'CREATE_RENTAL_INVOICE'
                     WHEN d.InvoiceId IS NULL THEN 'CREATE_DEPOSIT_INVOICE'
                     WHEN d.Status<>i.Status AND i.Status='OVERDUE' THEN 'MARK_INVOICE_OVERDUE'
                     ELSE 'UPDATE_INVOICE'
                   END,
                   'INVOICE',i.InvoiceId
            FROM inserted i
            LEFT JOIN deleted d ON d.InvoiceId=i.InvoiceId;
        END;
        """);

        // TRIGGER: dbo.trg_Audit_Payment — last definition from legacy source batch 94.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Audit_Payment", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Audit_Payment
        ON dbo.Payment
        AFTER INSERT, UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;
            DECLARE @Actor UNIQUEIDENTIFIER=TRY_CONVERT(UNIQUEIDENTIFIER,CONVERT(NVARCHAR(36),SESSION_CONTEXT(N'UserAccountId')));

            INSERT INTO dbo.AuditLog(UserAccountId,Action,EntityType,EntityId)
            SELECT @Actor,
                   CASE WHEN d.PaymentId IS NOT NULL AND d.Status='PENDING' AND i.Status IN('SUCCESS','FAILED')
                        THEN 'PAYMENT_RESULT' ELSE 'PAYMENT_CHANGE' END,
                   'PAYMENT',i.PaymentId
            FROM inserted i
            LEFT JOIN deleted d ON d.PaymentId=i.PaymentId;
        END;
        """);

        // TRIGGER: dbo.trg_Audit_ContractExtension — last definition from legacy source batch 95.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Audit_ContractExtension", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Audit_ContractExtension
        ON dbo.ContractExtension
        AFTER INSERT
        AS
        BEGIN
            SET NOCOUNT ON;
            DECLARE @Actor UNIQUEIDENTIFIER=TRY_CONVERT(UNIQUEIDENTIFIER,CONVERT(NVARCHAR(36),SESSION_CONTEXT(N'UserAccountId')));
            INSERT INTO dbo.AuditLog(UserAccountId,Action,EntityType,EntityId)
            SELECT @Actor,'RENEW_CONTRACT','CONTRACT_EXTENSION',ContractExtensionId FROM inserted;
        END;
        """);

        // TRIGGER: dbo.trg_Audit_Inspection — last definition from legacy source batch 96.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Audit_Inspection", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Audit_Inspection
        ON dbo.Inspection
        AFTER INSERT, UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;
            DECLARE @Actor UNIQUEIDENTIFIER=TRY_CONVERT(UNIQUEIDENTIFIER,CONVERT(NVARCHAR(36),SESSION_CONTEXT(N'UserAccountId')));
            INSERT INTO dbo.AuditLog(UserAccountId,Action,EntityType,EntityId)
            SELECT @Actor,
                   CASE
                     WHEN d.InspectionId IS NULL THEN 'CREATE_INSPECTION'
                     WHEN d.Status<>i.Status AND i.Status='IN_PROGRESS' THEN 'CLAIM_INSPECTION'
                     WHEN d.Status<>i.Status AND i.Status='COMPLETED' THEN 'COMPLETE_INSPECTION'
                     ELSE 'UPDATE_INSPECTION'
                   END,
                   'INSPECTION',i.InspectionId
            FROM inserted i LEFT JOIN deleted d ON d.InspectionId=i.InspectionId;
        END;
        """);

        // TRIGGER: dbo.trg_Audit_ExtraFee — last definition from legacy source batch 98.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Audit_ExtraFee", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Audit_ExtraFee
        ON dbo.ExtraFee
        AFTER INSERT
        AS
        BEGIN
            SET NOCOUNT ON;
            DECLARE @Actor UNIQUEIDENTIFIER=TRY_CONVERT(UNIQUEIDENTIFIER,CONVERT(NVARCHAR(36),SESSION_CONTEXT(N'UserAccountId')));
            INSERT INTO dbo.AuditLog(UserAccountId,Action,EntityType,EntityId)
            SELECT @Actor,'RECORD_EXTRA_FEE','EXTRA_FEE',ExtraFeeId FROM inserted;
        END;
        """);

        // TRIGGER: dbo.trg_Audit_DepositSettlement — last definition from legacy source batch 99.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Audit_DepositSettlement", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Audit_DepositSettlement
        ON dbo.DepositSettlement
        AFTER INSERT
        AS
        BEGIN
            SET NOCOUNT ON;
            DECLARE @Actor UNIQUEIDENTIFIER=TRY_CONVERT(UNIQUEIDENTIFIER,CONVERT(NVARCHAR(36),SESSION_CONTEXT(N'UserAccountId')));
            INSERT INTO dbo.AuditLog(UserAccountId,Action,EntityType,EntityId)
            SELECT @Actor,'FINALIZE_RETURN','DEPOSIT_SETTLEMENT',DepositSettlementId FROM inserted;
        END;
        """);

        // TRIGGER: dbo.trg_Audit_Policy — last definition from legacy source batch 100.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Audit_Policy", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Audit_Policy
        ON dbo.Policy
        AFTER INSERT
        AS
        BEGIN
            SET NOCOUNT ON;
            DECLARE @Actor UNIQUEIDENTIFIER=TRY_CONVERT(UNIQUEIDENTIFIER,CONVERT(NVARCHAR(36),SESSION_CONTEXT(N'UserAccountId')));
            INSERT INTO dbo.AuditLog(UserAccountId,Action,EntityType,EntityId)
            SELECT @Actor,'CREATE_POLICY_VERSION','POLICY',PolicyId FROM inserted;
        END;
        """);

        // TRIGGER: dbo.trg_Audit_UnitType — last definition from legacy source batch 101.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Audit_UnitType", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Audit_UnitType
        ON dbo.UnitType
        AFTER UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;
            DECLARE @Actor UNIQUEIDENTIFIER=TRY_CONVERT(UNIQUEIDENTIFIER,CONVERT(NVARCHAR(36),SESSION_CONTEXT(N'UserAccountId')));
            INSERT INTO dbo.AuditLog(UserAccountId,Action,EntityType,EntityId)
            SELECT @Actor,'UPDATE_UNIT_PRICE','UNIT_TYPE',i.UnitTypeId
            FROM inserted i JOIN deleted d ON d.UnitTypeId=i.UnitTypeId
            WHERE i.RentalPrice<>d.RentalPrice;
        END;
        """);

        // TRIGGER: dbo.trg_Audit_Discount — last definition from legacy source batch 102.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Audit_Discount", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Audit_Discount
        ON dbo.Discount
        AFTER INSERT, UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;
            DECLARE @Actor UNIQUEIDENTIFIER=TRY_CONVERT(UNIQUEIDENTIFIER,CONVERT(NVARCHAR(36),SESSION_CONTEXT(N'UserAccountId')));
            INSERT INTO dbo.AuditLog(UserAccountId,Action,EntityType,EntityId)
            SELECT @Actor,'CREATE_OR_UPDATE_DISCOUNT','DISCOUNT',DiscountId FROM inserted;
        END;
        """);

        // TRIGGER: dbo.trg_Audit_SupportTicket — last definition from legacy source batch 103.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Audit_SupportTicket", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Audit_SupportTicket
        ON dbo.SupportTicket
        AFTER INSERT, UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;
            DECLARE @Actor UNIQUEIDENTIFIER=TRY_CONVERT(UNIQUEIDENTIFIER,CONVERT(NVARCHAR(36),SESSION_CONTEXT(N'UserAccountId')));
            INSERT INTO dbo.AuditLog(UserAccountId,Action,EntityType,EntityId)
            SELECT @Actor,
                   CASE
                     WHEN d.SupportTicketId IS NULL THEN 'CREATE_SUPPORT_TICKET'
                     WHEN d.AssignedEmployeeId IS NULL AND i.AssignedEmployeeId IS NOT NULL THEN 'ASSIGN_SUPPORT_TICKET'
                     WHEN d.Status<>i.Status AND i.Status='COMPLETED' THEN 'COMPLETE_SUPPORT_TICKET'
                     WHEN d.Status<>i.Status AND i.Status='CANCELLED' THEN 'CANCEL_SUPPORT_TICKET'
                     ELSE 'UPDATE_SUPPORT_TICKET'
                   END,
                   'SUPPORT_TICKET',i.SupportTicketId
            FROM inserted i LEFT JOIN deleted d ON d.SupportTicketId=i.SupportTicketId;
        END;
        """);

        // TRIGGER: dbo.trg_Audit_UserAccount — last definition from legacy source batch 104.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Audit_UserAccount", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Audit_UserAccount
        ON dbo.UserAccount
        AFTER UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;
            DECLARE @Actor UNIQUEIDENTIFIER=TRY_CONVERT(UNIQUEIDENTIFIER,CONVERT(NVARCHAR(36),SESSION_CONTEXT(N'UserAccountId')));
            INSERT INTO dbo.AuditLog(UserAccountId,Action,EntityType,EntityId)
            SELECT @Actor,
                   CASE WHEN i.Status<>d.Status THEN 'CHANGE_ACCOUNT_STATUS' ELSE 'CHANGE_ACCOUNT' END,
                   'USER_ACCOUNT',i.UserAccountId
            FROM inserted i JOIN deleted d ON d.UserAccountId=i.UserAccountId
            WHERE i.Status<>d.Status OR i.RoleId<>d.RoleId;
        END;
        """);

        // TRIGGER: dbo.trg_Audit_Employee — last definition from legacy source batch 105.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Audit_Employee", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Audit_Employee
        ON dbo.Employee
        AFTER UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;
            DECLARE @Actor UNIQUEIDENTIFIER=TRY_CONVERT(UNIQUEIDENTIFIER,CONVERT(NVARCHAR(36),SESSION_CONTEXT(N'UserAccountId')));
            INSERT INTO dbo.AuditLog(UserAccountId,Action,EntityType,EntityId)
            SELECT @Actor,'CHANGE_EMPLOYEE_ROLE_FACILITY','EMPLOYEE',i.EmployeeId
            FROM inserted i JOIN deleted d ON d.EmployeeId=i.EmployeeId
            WHERE ISNULL(CONVERT(VARCHAR(36),i.FacilityId),'')<>ISNULL(CONVERT(VARCHAR(36),d.FacilityId),'');
        END;
        """);

        // PROCEDURE: dbo.usp_CreateReservation — last definition from legacy source batch 107.
        RestoreIfMissing(migrationBuilder, "dbo.usp_CreateReservation", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_CreateReservation
            @CustomerId UNIQUEIDENTIFIER,
            @FacilityId UNIQUEIDENTIFIER,
            @UnitTypeId UNIQUEIDENTIFIER,
            @StartMonth DATE,
            @EndMonth DATE,
            @NowUtc DATETIME2(3) = NULL
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;
            IF @NowUtc IS NULL SET @NowUtc = SYSUTCDATETIME();

            IF DAY(@StartMonth) <> 1 OR DAY(@EndMonth) <> 1 OR @EndMonth < @StartMonth
                THROW 51101, 'INVALID_MONTH_RANGE', 1;

            DECLARE @BusinessNow DATE = CAST(DATEADD(HOUR, 7, @NowUtc) AS DATE);
            DECLARE @CurrentMonth DATE = DATEFROMPARTS(YEAR(@BusinessNow), MONTH(@BusinessNow), 1);
            IF @StartMonth <= @CurrentMonth
                THROW 51101, 'INVALID_MONTH_RANGE', 1;

            SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

            BEGIN TRY
                BEGIN TRANSACTION;

                IF NOT EXISTS (
                    SELECT 1
                    FROM dbo.Customer c
                    JOIN dbo.UserAccount ua ON ua.UserAccountId = c.UserAccountId
                    WHERE c.CustomerId = @CustomerId AND ua.Status = 'ACTIVE'
                )
                    THROW 51102, 'ACCOUNT_INACTIVE_OR_CUSTOMER_NOT_FOUND', 1;

                IF NOT EXISTS (
                    SELECT 1 FROM dbo.Facility WITH (UPDLOCK, HOLDLOCK)
                    WHERE FacilityId = @FacilityId AND Status = 'ACTIVE'
                )
                    THROW 51103, 'FACILITY_INACTIVE', 1;

                DECLARE @RentalPrice DECIMAL(18,2);
                SELECT @RentalPrice = RentalPrice
                FROM dbo.UnitType WITH (UPDLOCK, HOLDLOCK)
                WHERE UnitTypeId = @UnitTypeId;

                IF @RentalPrice IS NULL
                    THROW 51104, 'UNIT_TYPE_NOT_FOUND', 1;

                DECLARE @PolicyId UNIQUEIDENTIFIER, @DepositTimeoutHours INT;
                SELECT TOP (1)
                    @PolicyId = PolicyId,
                    @DepositTimeoutHours = DepositTimeoutHours
                FROM dbo.Policy WITH (UPDLOCK, HOLDLOCK)
                WHERE Status = 'ACTIVE'
                ORDER BY Version DESC;

                IF @PolicyId IS NULL
                    THROW 51105, 'ACTIVE_POLICY_NOT_FOUND', 1;

                DECLARE @Month DATE = @StartMonth;
                WHILE @Month <= @EndMonth
                BEGIN
                    DECLARE @Capacity INT, @ReservationHolds INT, @ContractOccupancy INT;

                    SELECT @Capacity = COUNT_BIG(*)
                    FROM dbo.StorageUnit WITH (UPDLOCK, HOLDLOCK)
                    WHERE FacilityId = @FacilityId
                      AND UnitTypeId = @UnitTypeId
                      AND Status IN ('AVAILABLE','IN_USE','INSPECTION');

                    SELECT @ReservationHolds = COUNT_BIG(*)
                    FROM dbo.Reservation WITH (UPDLOCK, HOLDLOCK, INDEX(IX_Reservation_Capacity))
                    WHERE FacilityId = @FacilityId
                      AND UnitTypeId = @UnitTypeId
                      AND Status IN ('PENDING_DEPOSIT','CONFIRMED')
                      AND StartMonth <= @Month
                      AND EndMonth >= @Month;

                    SELECT @ContractOccupancy = COUNT_BIG(*)
                    FROM dbo.Contract c WITH (UPDLOCK, HOLDLOCK)
                    JOIN dbo.StorageUnit su ON su.StorageUnitId = c.StorageUnitId
                    WHERE c.FacilityId = @FacilityId
                      AND su.UnitTypeId = @UnitTypeId
                      AND c.Status = 'ACTIVE'
                      AND c.StartMonth <= @Month
                      AND c.EndMonth >= @Month;

                    IF ISNULL(@Capacity,0) <= ISNULL(@ReservationHolds,0) + ISNULL(@ContractOccupancy,0)
                        THROW 51106, 'CAPACITY_NOT_AVAILABLE', 1;

                    SET @Month = DATEADD(MONTH, 1, @Month);
                END;

                DECLARE @ReservationId UNIQUEIDENTIFIER = NEWID();
                INSERT INTO dbo.Reservation(
                    ReservationId, CustomerId, FacilityId, UnitTypeId, PolicyId,
                    StartMonth, EndMonth, LockedRentalPrice, DepositAmount, Status, CreatedAt
                )
                VALUES (
                    @ReservationId, @CustomerId, @FacilityId, @UnitTypeId, @PolicyId,
                    @StartMonth, @EndMonth, @RentalPrice, @RentalPrice, 'PENDING_DEPOSIT', @NowUtc
                );

                DECLARE @DepositInvoiceId UNIQUEIDENTIFIER = NEWID();
                INSERT INTO dbo.Invoice(
                    InvoiceId, EntityId, InvoiceType, BillingMonth, BaseAmount,
                    DiscountId, DiscountAmount, AmountDue, DueDate, Status, CreatedAt
                )
                VALUES (
                    @DepositInvoiceId, @ReservationId, 'DEPOSIT', NULL, @RentalPrice,
                    NULL, 0, @RentalPrice, DATEADD(HOUR, @DepositTimeoutHours, @NowUtc), 'UNPAID', @NowUtc
                );

                COMMIT TRANSACTION;

                SELECT @ReservationId AS ReservationId,
                       @DepositInvoiceId AS DepositInvoiceId,
                       @RentalPrice AS DepositAmount,
                       DATEADD(HOUR, @DepositTimeoutHours, @NowUtc) AS DepositDueAt;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

        // PROCEDURE: dbo.usp_ConfirmReservation — last definition from legacy source batch 108.
        RestoreIfMissing(migrationBuilder, "dbo.usp_ConfirmReservation", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_ConfirmReservation
            @ReservationId UNIQUEIDENTIFIER,
            @ReservationVisitDate DATE,
            @NowUtc DATETIME2(3) = NULL
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;
            IF @NowUtc IS NULL SET @NowUtc = SYSUTCDATETIME();

            BEGIN TRY
                BEGIN TRANSACTION;

                DECLARE @StartMonth DATE, @PolicyId UNIQUEIDENTIFIER, @Status VARCHAR(50);
                SELECT @StartMonth = StartMonth, @PolicyId = PolicyId, @Status = Status
                FROM dbo.Reservation WITH (UPDLOCK, HOLDLOCK)
                WHERE ReservationId = @ReservationId;

                IF @Status IS NULL OR @Status <> 'PENDING_DEPOSIT'
                    THROW 51107, 'RESERVATION_INVALID_STATUS', 1;

                IF NOT EXISTS (
                    SELECT 1 FROM dbo.Invoice i
                    WHERE i.EntityId = @ReservationId
                      AND i.InvoiceType = 'DEPOSIT'
                      AND i.Status = 'PAID'
                )
                    THROW 51108, 'DEPOSIT_NOT_PAID', 1;

                DECLARE @StartDay INT, @EndDay INT;
                SELECT @StartDay = ReservationVisitStartDay,
                       @EndDay = ReservationVisitEndDay
                FROM dbo.Policy
                WHERE PolicyId = @PolicyId;

                IF DATEFROMPARTS(YEAR(@ReservationVisitDate), MONTH(@ReservationVisitDate), 1) <> @StartMonth
                   OR DAY(@ReservationVisitDate) NOT BETWEEN @StartDay AND @EndDay
                    THROW 51109, 'VISIT_DATE_OUT_OF_POLICY', 1;

                IF EXISTS (
                    SELECT 1 FROM dbo.Visit
                    WHERE EntityId = @ReservationId AND VisitType = 'RESERVATION'
                )
                    THROW 51110, 'RESERVATION_VISIT_ALREADY_EXISTS', 1;

                DECLARE @VisitId UNIQUEIDENTIFIER = NEWID();
                INSERT INTO dbo.Visit(VisitId, EntityId, EmployeeId, VisitType, VisitDate, ActualReturnDate, Status)
                VALUES (@VisitId, @ReservationId, NULL, 'RESERVATION', @ReservationVisitDate, NULL, 'SCHEDULED');

                UPDATE dbo.Reservation
                SET Status = 'CONFIRMED'
                WHERE ReservationId = @ReservationId;

                COMMIT TRANSACTION;
                SELECT @ReservationId AS ReservationId, @VisitId AS VisitId, 'CONFIRMED' AS Status;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

        // PROCEDURE: dbo.usp_CancelReservation — last definition from legacy source batch 109.
        RestoreIfMissing(migrationBuilder, "dbo.usp_CancelReservation", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_CancelReservation
            @ReservationId UNIQUEIDENTIFIER
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;

            BEGIN TRY
                BEGIN TRANSACTION;

                DECLARE @Status VARCHAR(50);
                SELECT @Status = Status
                FROM dbo.Reservation WITH (UPDLOCK, HOLDLOCK)
                WHERE ReservationId = @ReservationId;

                IF @Status NOT IN ('PENDING_DEPOSIT','CONFIRMED')
                    THROW 51107, 'RESERVATION_INVALID_STATUS', 1;

                IF EXISTS (
                    SELECT 1 FROM dbo.Visit
                    WHERE EntityId = @ReservationId
                      AND VisitType = 'RESERVATION'
                      AND Status = 'CHECKED_IN'
                )
                    THROW 51111, 'Cannot cancel Reservation after Visit CHECKED_IN.', 1;

                UPDATE dbo.Visit
                SET Status = 'CANCELLED'
                WHERE EntityId = @ReservationId
                  AND VisitType = 'RESERVATION'
                  AND Status = 'SCHEDULED';

                UPDATE dbo.Invoice
                SET Status = 'CANCELLED'
                WHERE EntityId = @ReservationId
                  AND InvoiceType = 'DEPOSIT'
                  AND Status = 'UNPAID';

                UPDATE dbo.Reservation
                SET Status = 'CANCELLED'
                WHERE ReservationId = @ReservationId;

                COMMIT TRANSACTION;
                SELECT @ReservationId AS ReservationId, 'CANCELLED' AS Status;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

        // PROCEDURE: dbo.usp_CheckInVisit — last definition from legacy source batch 110.
        RestoreIfMissing(migrationBuilder, "dbo.usp_CheckInVisit", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_CheckInVisit
            @VisitId UNIQUEIDENTIFIER,
            @EmployeeId UNIQUEIDENTIFIER,
            @NowUtc DATETIME2(3) = NULL
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;
            IF @NowUtc IS NULL SET @NowUtc = SYSUTCDATETIME();

            BEGIN TRY
                BEGIN TRANSACTION;

                DECLARE @VisitType VARCHAR(50), @EntityId UNIQUEIDENTIFIER, @VisitDate DATE, @VisitStatus VARCHAR(50);
                SELECT @VisitType = VisitType, @EntityId = EntityId, @VisitDate = VisitDate, @VisitStatus = Status
                FROM dbo.Visit WITH (UPDLOCK, HOLDLOCK)
                WHERE VisitId = @VisitId;

                IF @VisitStatus <> 'SCHEDULED'
                    THROW 51112, 'VISIT_INVALID_STATUS', 1;

                IF @VisitDate <> CAST(@NowUtc AS DATE)
                    THROW 51113, 'VISIT_DATE_MISMATCH', 1;

                DECLARE @FacilityId UNIQUEIDENTIFIER;
                IF @VisitType = 'RESERVATION'
                    SELECT @FacilityId = FacilityId FROM dbo.Reservation WHERE ReservationId = @EntityId;
                ELSE
                    SELECT @FacilityId = FacilityId FROM dbo.Contract WHERE ContractId = @EntityId;

                IF NOT EXISTS (
                    SELECT 1
                    FROM dbo.Employee e
                    JOIN dbo.UserAccount ua ON ua.UserAccountId = e.UserAccountId
                    JOIN dbo.UserRole ur ON ur.RoleId = ua.RoleId
                    WHERE e.EmployeeId = @EmployeeId
                      AND e.FacilityId = @FacilityId
                      AND ua.Status = 'ACTIVE'
                      AND ur.RoleName = 'FACILITY_STAFF'
                )
                    THROW 51114, 'STAFF_FACILITY_OR_ROLE_INVALID', 1;

                UPDATE dbo.Visit
                SET Status = 'CHECKED_IN', EmployeeId = @EmployeeId
                WHERE VisitId = @VisitId AND Status = 'SCHEDULED';

                IF @@ROWCOUNT <> 1
                    THROW 51112, 'VISIT_INVALID_STATUS', 1;

                COMMIT TRANSACTION;
                SELECT @VisitId AS VisitId, 'CHECKED_IN' AS Status, @EmployeeId AS EmployeeId;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

        // PROCEDURE: dbo.usp_CancelVisit — last definition from legacy source batch 111.
        RestoreIfMissing(migrationBuilder, "dbo.usp_CancelVisit", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_CancelVisit
            @VisitId UNIQUEIDENTIFIER
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;

            BEGIN TRY
                BEGIN TRANSACTION;

                DECLARE @VisitType VARCHAR(50), @Status VARCHAR(50);
                SELECT @VisitType = VisitType, @Status = Status
                FROM dbo.Visit WITH (UPDLOCK, HOLDLOCK)
                WHERE VisitId = @VisitId;

                IF @Status <> 'SCHEDULED'
                    THROW 51112, 'VISIT_INVALID_STATUS', 1;

                IF @VisitType = 'RESERVATION'
                    THROW 51115, 'Cancel the Reservation instead of cancelling its RESERVATION Visit directly.', 1;

                UPDATE dbo.Visit SET Status = 'CANCELLED' WHERE VisitId = @VisitId;

                COMMIT TRANSACTION;
                SELECT @VisitId AS VisitId, 'CANCELLED' AS Status;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

        // PROCEDURE: dbo.usp_CreateAccessVisit — last definition from legacy source batch 113.
        RestoreIfMissing(migrationBuilder, "dbo.usp_CreateAccessVisit", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_CreateAccessVisit
            @ContractId UNIQUEIDENTIFIER,
            @CustomerId UNIQUEIDENTIFIER,
            @VisitDate DATE
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;

            BEGIN TRY
                BEGIN TRANSACTION;

                DECLARE @StartMonth DATE, @EndMonth DATE, @Status VARCHAR(50), @Owner UNIQUEIDENTIFIER;
                SELECT @StartMonth = StartMonth, @EndMonth = EndMonth, @Status = Status, @Owner = CustomerId
                FROM dbo.Contract WITH (UPDLOCK, HOLDLOCK)
                WHERE ContractId = @ContractId;

                IF @Status <> 'ACTIVE' OR @Owner <> @CustomerId
                    THROW 51120, 'CONTRACT_NOT_ACTIVE_OR_NOT_OWNED', 1;

                IF @VisitDate < @StartMonth OR @VisitDate > EOMONTH(@EndMonth)
                    THROW 51121, 'ACCESS_VISIT_DATE_OUTSIDE_CONTRACT', 1;

                IF EXISTS (
                    SELECT 1 FROM dbo.Visit
                    WHERE EntityId = @ContractId
                      AND VisitType = 'RETURN'
                      AND Status IN ('SCHEDULED','CHECKED_IN')
                )
                    THROW 51122, 'RETURN_VISIT_PENDING', 1;

                IF @VisitDate < CAST(SYSUTCDATETIME() AS DATE)
                    THROW 51123, 'VISIT_DATE_IN_PAST', 1;

                DECLARE @VisitId UNIQUEIDENTIFIER = NEWID();
                INSERT INTO dbo.Visit(VisitId, EntityId, VisitType, VisitDate, Status)
                VALUES (@VisitId, @ContractId, 'ACCESS', @VisitDate, 'SCHEDULED');

                COMMIT TRANSACTION;
                SELECT @VisitId AS VisitId, 'SCHEDULED' AS Status;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

        // PROCEDURE: dbo.usp_CreateReturnVisit — last definition from legacy source batch 114.
        RestoreIfMissing(migrationBuilder, "dbo.usp_CreateReturnVisit", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_CreateReturnVisit
            @ContractId UNIQUEIDENTIFIER,
            @CustomerId UNIQUEIDENTIFIER,
            @VisitDate DATE
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;

            BEGIN TRY
                BEGIN TRANSACTION;

                IF NOT EXISTS (
                    SELECT 1 FROM dbo.Contract WITH (UPDLOCK, HOLDLOCK)
                    WHERE ContractId = @ContractId
                      AND CustomerId = @CustomerId
                      AND Status = 'ACTIVE'
                )
                    THROW 51120, 'CONTRACT_NOT_ACTIVE_OR_NOT_OWNED', 1;

                IF EXISTS (
                    SELECT 1 FROM dbo.Visit
                    WHERE EntityId = @ContractId
                      AND VisitType = 'RETURN'
                      AND Status IN ('SCHEDULED','CHECKED_IN')
                )
                    THROW 51122, 'RETURN_VISIT_PENDING', 1;

                IF @VisitDate < CAST(SYSUTCDATETIME() AS DATE)
                    THROW 51123, 'VISIT_DATE_IN_PAST', 1;

                DECLARE @VisitId UNIQUEIDENTIFIER = NEWID();
                INSERT INTO dbo.Visit(VisitId, EntityId, VisitType, VisitDate, Status)
                VALUES (@VisitId, @ContractId, 'RETURN', @VisitDate, 'SCHEDULED');

                COMMIT TRANSACTION;
                SELECT @VisitId AS VisitId, 'SCHEDULED' AS Status;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

        // PROCEDURE: dbo.usp_ClaimInspection — last definition from legacy source batch 116.
        RestoreIfMissing(migrationBuilder, "dbo.usp_ClaimInspection", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_ClaimInspection
            @InspectionId UNIQUEIDENTIFIER,
            @EmployeeId UNIQUEIDENTIFIER
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;

            BEGIN TRY
                BEGIN TRANSACTION;

                DECLARE @FacilityId UNIQUEIDENTIFIER;
                SELECT @FacilityId = c.FacilityId
                FROM dbo.Inspection i
                JOIN dbo.Contract c ON c.ContractId = i.ContractId
                WHERE i.InspectionId = @InspectionId;

                IF NOT EXISTS (
                    SELECT 1
                    FROM dbo.Employee e
                    JOIN dbo.UserAccount ua ON ua.UserAccountId = e.UserAccountId
                    JOIN dbo.UserRole ur ON ur.RoleId = ua.RoleId
                    WHERE e.EmployeeId = @EmployeeId
                      AND e.FacilityId = @FacilityId
                      AND ua.Status = 'ACTIVE'
                      AND ur.RoleName = 'FACILITY_STAFF'
                )
                    THROW 51114, 'STAFF_FACILITY_OR_ROLE_INVALID', 1;

                UPDATE dbo.Inspection
                SET Status = 'IN_PROGRESS', EmployeeId = @EmployeeId
                WHERE InspectionId = @InspectionId AND Status = 'PENDING';

                IF @@ROWCOUNT <> 1
                    THROW 51125, 'INSPECTION_ALREADY_CLAIMED', 1;

                COMMIT TRANSACTION;
                SELECT @InspectionId AS InspectionId, @EmployeeId AS EmployeeId, 'IN_PROGRESS' AS Status;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

        // PROCEDURE: dbo.usp_RecordDamage — last definition from legacy source batch 117.
        RestoreIfMissing(migrationBuilder, "dbo.usp_RecordDamage", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_RecordDamage
            @InspectionId UNIQUEIDENTIFIER,
            @DamageTypeId UNIQUEIDENTIFIER,
            @DamageAmount DECIMAL(18,2) = NULL,
            @Note NVARCHAR(MAX) = NULL
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;

            IF NOT EXISTS (SELECT 1 FROM dbo.Inspection WHERE InspectionId = @InspectionId AND Status = 'IN_PROGRESS')
                THROW 51126, 'INSPECTION_INVALID_STATUS', 1;

            IF NOT EXISTS (
                SELECT 1
                FROM dbo.DamageType
                WHERE DamageTypeId = @DamageTypeId
                  AND Status = 'ACTIVE'
            )
                THROW 51212, 'DAMAGE_TYPE_NOT_ACTIVE', 1;

            DECLARE @DefaultAmount DECIMAL(18,2);
            SELECT @DefaultAmount = DefaultAmount
            FROM dbo.DamageType
            WHERE DamageTypeId = @DamageTypeId
              AND Status = 'ACTIVE';

            IF @DamageAmount IS NULL SET @DamageAmount = @DefaultAmount;
            IF @DamageAmount IS NULL OR @DamageAmount < 0
                THROW 51127, 'DAMAGE_AMOUNT_REQUIRED', 1;

            DECLARE @DamageRecordId UNIQUEIDENTIFIER = NEWID();
            INSERT INTO dbo.DamageRecord(
                DamageRecordId, InspectionId, DamageTypeId, DamageAmount, Note, Status
            )
            VALUES (@DamageRecordId, @InspectionId, @DamageTypeId, @DamageAmount, @Note, 'PENDING');

            SELECT @DamageRecordId AS DamageRecordId;
        END;
        """);

        // PROCEDURE: dbo.usp_RecordExtraFee — last definition from legacy source batch 118.
        RestoreIfMissing(migrationBuilder, "dbo.usp_RecordExtraFee", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_RecordExtraFee
            @InspectionId UNIQUEIDENTIFIER,
            @ExtraFeeTypeId UNIQUEIDENTIFIER,
            @Amount DECIMAL(18,2) = NULL,
            @Reason NVARCHAR(MAX)
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;

            IF NOT EXISTS (SELECT 1 FROM dbo.Inspection WHERE InspectionId = @InspectionId AND Status = 'IN_PROGRESS')
                THROW 51126, 'INSPECTION_INVALID_STATUS', 1;

            DECLARE @DefaultAmount DECIMAL(18,2);
            SELECT @DefaultAmount = DefaultAmount
            FROM dbo.ExtraFeeType
            WHERE ExtraFeeTypeId = @ExtraFeeTypeId AND Status = 'ACTIVE';

            IF @DefaultAmount IS NULL
                THROW 51128, 'EXTRA_FEE_TYPE_NOT_ACTIVE', 1;

            IF @Amount IS NULL SET @Amount = @DefaultAmount;
            IF @Amount < 0 THROW 51129, 'EXTRA_FEE_AMOUNT_INVALID', 1;

            DECLARE @ExtraFeeId UNIQUEIDENTIFIER = NEWID();
            INSERT INTO dbo.ExtraFee(ExtraFeeId, InspectionId, ExtraFeeTypeId, Amount, Reason)
            VALUES (@ExtraFeeId, @InspectionId, @ExtraFeeTypeId, @Amount, @Reason);

            SELECT @ExtraFeeId AS ExtraFeeId;
        END;
        """);

        // PROCEDURE: dbo.usp_AddInspectionEvidence — last definition from legacy source batch 119.
        RestoreIfMissing(migrationBuilder, "dbo.usp_AddInspectionEvidence", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_AddInspectionEvidence
            @InspectionId UNIQUEIDENTIFIER,
            @FileData VARBINARY(MAX),
            @EvidenceType VARCHAR(50)
        AS
        BEGIN
            SET NOCOUNT ON;

            IF NOT EXISTS (SELECT 1 FROM dbo.Inspection WHERE InspectionId = @InspectionId AND Status IN ('PENDING','IN_PROGRESS'))
                THROW 51126, 'INSPECTION_INVALID_STATUS', 1;

            IF @FileData IS NULL OR DATALENGTH(@FileData) = 0
                THROW 51130, 'EVIDENCE_FILE_REQUIRED', 1;

            DECLARE @EvidenceId UNIQUEIDENTIFIER = NEWID();
            INSERT INTO dbo.InspectionEvidence(InspectionEvidenceId, InspectionId, FileData, EvidenceType)
            VALUES (@EvidenceId, @InspectionId, @FileData, @EvidenceType);

            SELECT @EvidenceId AS InspectionEvidenceId;
        END;
        """);

        // SCOPE-LOCK: dbo.usp_CompleteInspection intentionally excluded from this Reservation-only repair.
        // Reason: Legacy release of StorageUnit at Inspection completion violates SRS V10.1.
        // Requires explicit CHANGE SCOPE to review and implement the latest authoritative SQL.


        // PROCEDURE: dbo.usp_RenewContract — last definition from legacy source batch 122.
        RestoreIfMissing(migrationBuilder, "dbo.usp_RenewContract", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_RenewContract
            @ContractId UNIQUEIDENTIFIER,
            @NewEndMonth DATE,
            @NowUtc DATETIME2(3) = NULL
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;
            IF @NowUtc IS NULL SET @NowUtc = SYSUTCDATETIME();

            IF DAY(@NewEndMonth) <> 1
                THROW 51135, 'RENEWAL_NOT_CONTIGUOUS', 1;

            SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

            BEGIN TRY
                BEGIN TRANSACTION;

                DECLARE @OldEndMonth DATE, @Status VARCHAR(50), @FacilityId UNIQUEIDENTIFIER,
                        @StorageUnitId UNIQUEIDENTIFIER, @UnitTypeId UNIQUEIDENTIFIER;

                SELECT @OldEndMonth = c.EndMonth,
                       @Status = c.Status,
                       @FacilityId = c.FacilityId,
                       @StorageUnitId = c.StorageUnitId,
                       @UnitTypeId = su.UnitTypeId
                FROM dbo.Contract c WITH (UPDLOCK, HOLDLOCK)
                JOIN dbo.StorageUnit su ON su.StorageUnitId = c.StorageUnitId
                WHERE c.ContractId = @ContractId;

                IF @Status <> 'ACTIVE'
                    THROW 51120, 'CONTRACT_NOT_ACTIVE', 1;

                IF @NewEndMonth <= @OldEndMonth
                    THROW 51135, 'RENEWAL_NOT_CONTIGUOUS', 1;

                IF EXISTS (
                    SELECT 1 FROM dbo.Visit
                    WHERE EntityId = @ContractId
                      AND VisitType = 'RETURN'
                      AND Status IN ('SCHEDULED','CHECKED_IN')
                )
                    THROW 51122, 'RETURN_VISIT_PENDING', 1;

                DECLARE @Month DATE = DATEADD(MONTH,1,@OldEndMonth);
                WHILE @Month <= @NewEndMonth
                BEGIN
                    DECLARE @Capacity INT, @ReservationHolds INT, @ContractOccupancy INT;

                    SELECT @Capacity = COUNT_BIG(*)
                    FROM dbo.StorageUnit WITH (UPDLOCK, HOLDLOCK)
                    WHERE FacilityId = @FacilityId
                      AND UnitTypeId = @UnitTypeId
                      AND Status IN ('AVAILABLE','IN_USE','INSPECTION');

                    SELECT @ReservationHolds = COUNT_BIG(*)
                    FROM dbo.Reservation WITH (UPDLOCK, HOLDLOCK, INDEX(IX_Reservation_Capacity))
                    WHERE FacilityId = @FacilityId
                      AND UnitTypeId = @UnitTypeId
                      AND Status IN ('PENDING_DEPOSIT','CONFIRMED')
                      AND StartMonth <= @Month
                      AND EndMonth >= @Month;

                    SELECT @ContractOccupancy = COUNT_BIG(*)
                    FROM dbo.Contract c WITH (UPDLOCK, HOLDLOCK)
                    JOIN dbo.StorageUnit su ON su.StorageUnitId = c.StorageUnitId
                    WHERE c.FacilityId = @FacilityId
                      AND su.UnitTypeId = @UnitTypeId
                      AND c.Status = 'ACTIVE'
                      AND c.StartMonth <= @Month
                      AND c.EndMonth >= @Month;

                    IF ISNULL(@Capacity,0) <= ISNULL(@ReservationHolds,0) + ISNULL(@ContractOccupancy,0)
                        THROW 51136, 'RENEWAL_CAPACITY_NOT_AVAILABLE', 1;

                    SET @Month = DATEADD(MONTH,1,@Month);
                END;

                DECLARE @AppliedPrice DECIMAL(18,2);
                SELECT @AppliedPrice = RentalPrice FROM dbo.UnitType WHERE UnitTypeId = @UnitTypeId;

                DECLARE @ExtensionId UNIQUEIDENTIFIER = NEWID();
                INSERT INTO dbo.ContractExtension(
                    ContractExtensionId, ContractId, OldEndMonth, NewEndMonth,
                    AppliedMonthlyPrice, CreatedAt
                )
                VALUES (
                    @ExtensionId, @ContractId, @OldEndMonth, @NewEndMonth,
                    @AppliedPrice, @NowUtc
                );

                UPDATE dbo.Contract
                SET EndMonth = @NewEndMonth
                WHERE ContractId = @ContractId AND EndMonth = @OldEndMonth;

                COMMIT TRANSACTION;
                SELECT @ExtensionId AS ContractExtensionId,
                       @OldEndMonth AS OldEndMonth,
                       @NewEndMonth AS NewEndMonth,
                       @AppliedPrice AS AppliedMonthlyPrice;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

        // PROCEDURE: dbo.usp_CreatePaymentAttempt — last definition from legacy source batch 124.
        RestoreIfMissing(migrationBuilder, "dbo.usp_CreatePaymentAttempt", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_CreatePaymentAttempt
            @InvoiceId UNIQUEIDENTIFIER = NULL,
            @Amount DECIMAL(18,2),
            @TransactionCode VARCHAR(255) = NULL
        AS
        BEGIN
            SET NOCOUNT ON;

            IF @Amount <= 0 THROW 51139, 'PAYMENT_AMOUNT_INVALID', 1;

            IF @InvoiceId IS NOT NULL
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM dbo.Invoice
                    WHERE InvoiceId = @InvoiceId
                      AND Status IN ('UNPAID','OVERDUE')
                      AND AmountDue = @Amount
                )
                    THROW 51140, 'INVOICE_NOT_PAYABLE_OR_AMOUNT_MISMATCH', 1;
            END;

            DECLARE @PaymentId UNIQUEIDENTIFIER = NEWID();
            INSERT INTO dbo.Payment(
                PaymentId, InvoiceId, Amount, PaymentMethod, TransactionCode, Status, PaidAt
            )
            VALUES (
                @PaymentId, @InvoiceId, @Amount, 'MOMO', @TransactionCode, 'PENDING', NULL
            );

            SELECT @PaymentId AS PaymentId;
        END;
        """);

        // PROCEDURE: dbo.usp_ApplyPaymentResult — last definition from legacy source batch 125.
        RestoreIfMissing(migrationBuilder, "dbo.usp_ApplyPaymentResult", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_ApplyPaymentResult
            @PaymentId UNIQUEIDENTIFIER,
            @TransactionCode VARCHAR(255),
            @ResultStatus VARCHAR(50),
            @PaidAt DATETIME2(3) = NULL
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;

            IF @ResultStatus NOT IN ('SUCCESS','FAILED')
                THROW 51141, 'PAYMENT_RESULT_INVALID', 1;
            IF @ResultStatus = 'SUCCESS' AND @PaidAt IS NULL
                SET @PaidAt = SYSUTCDATETIME();

            BEGIN TRY
                BEGIN TRANSACTION;

                IF @TransactionCode IS NULL OR LTRIM(RTRIM(@TransactionCode)) = ''
                    THROW 51141, 'PAYMENT_RESULT_INVALID', 1;

                DECLARE @CurrentStatus VARCHAR(50), @InvoiceId UNIQUEIDENTIFIER,
                        @ExistingTransactionCode VARCHAR(255);
                SELECT @CurrentStatus = Status,
                       @InvoiceId = InvoiceId,
                       @ExistingTransactionCode = TransactionCode
                FROM dbo.Payment WITH (UPDLOCK, HOLDLOCK)
                WHERE PaymentId = @PaymentId;

                IF @CurrentStatus IS NULL
                    THROW 51142, 'PAYMENT_NOT_FOUND', 1;

                IF @ExistingTransactionCode IS NOT NULL
                   AND @ExistingTransactionCode <> @TransactionCode
                    THROW 51144, 'PAYMENT_CALLBACK_DUPLICATE', 1;

                IF EXISTS (
                    SELECT 1 FROM dbo.Payment
                    WHERE TransactionCode = @TransactionCode
                      AND PaymentId <> @PaymentId
                )
                    THROW 51144, 'PAYMENT_CALLBACK_DUPLICATE', 1;

                IF @CurrentStatus IN ('SUCCESS','FAILED')
                BEGIN
                    IF @CurrentStatus = @ResultStatus
                    BEGIN
                        COMMIT TRANSACTION;
                        SELECT @PaymentId AS PaymentId, @CurrentStatus AS Status, 'IDEMPOTENT' AS Result;
                        RETURN;
                    END;
                    THROW 51143, 'PAYMENT_ALREADY_FINALIZED', 1;
                END;

                IF @ResultStatus = 'SUCCESS' AND @InvoiceId IS NOT NULL
                BEGIN
                    DECLARE @InvoiceStatus VARCHAR(50), @InvoiceType VARCHAR(50),
                            @BillingMonth DATE, @ContractId UNIQUEIDENTIFIER;

                    SELECT @InvoiceStatus = Status, @InvoiceType = InvoiceType,
                           @BillingMonth = BillingMonth, @ContractId = EntityId
                    FROM dbo.Invoice WITH (UPDLOCK, HOLDLOCK)
                    WHERE InvoiceId = @InvoiceId;

                    IF @InvoiceStatus = 'OVERDUE' AND @InvoiceType = 'RENTAL_FEE'
                    BEGIN
                        DECLARE @PolicyId UNIQUEIDENTIFIER, @Divisor INT, @OverdueStartDay INT,
                                @OverdueStartDate DATE, @Days INT, @FeeAmount DECIMAL(18,2),
                                @RentalAmount DECIMAL(18,2);

                        SELECT @PolicyId = PolicyId FROM dbo.Contract WHERE ContractId = @ContractId;
                        SELECT @Divisor = LateFeeDivisorDays, @OverdueStartDay = OverdueStartDay
                        FROM dbo.Policy WHERE PolicyId = @PolicyId;
                        SELECT @RentalAmount = AmountDue FROM dbo.Invoice WHERE InvoiceId = @InvoiceId;

                        SET @OverdueStartDate = DATEFROMPARTS(
                            YEAR(@BillingMonth), MONTH(@BillingMonth),
                            CASE WHEN @OverdueStartDay > DAY(EOMONTH(@BillingMonth))
                                 THEN DAY(EOMONTH(@BillingMonth)) ELSE @OverdueStartDay END
                        );

                        SET @Days = DATEDIFF(DAY, @OverdueStartDate, CAST(@PaidAt AS DATE)) + 1;
                        IF @Days < 1 SET @Days = 1;
                        SET @FeeAmount = ROUND(CAST(@RentalAmount AS DECIMAL(18,4)) / @Divisor * @Days, 2);

                        IF EXISTS (SELECT 1 FROM dbo.LateFee WHERE InvoiceId = @InvoiceId)
                            UPDATE dbo.LateFee
                            SET OverdueDays = @Days, Amount = @FeeAmount, CalculatedAt = @PaidAt
                            WHERE InvoiceId = @InvoiceId;
                        ELSE
                            INSERT INTO dbo.LateFee(InvoiceId, OverdueDays, Amount, CalculatedAt)
                            VALUES (@InvoiceId, @Days, @FeeAmount, @PaidAt);
                    END;

                    UPDATE dbo.Invoice
                    SET Status = 'PAID'
                    WHERE InvoiceId = @InvoiceId
                      AND Status IN ('UNPAID','OVERDUE');
                END;

                UPDATE dbo.Payment
                SET TransactionCode = @TransactionCode,
                    Status = @ResultStatus,
                    PaidAt = CASE WHEN @ResultStatus = 'SUCCESS' THEN @PaidAt ELSE NULL END
                WHERE PaymentId = @PaymentId;

                COMMIT TRANSACTION;
                SELECT @PaymentId AS PaymentId, @ResultStatus AS Status;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

        // PROCEDURE: dbo.usp_CalculateLateFee — last definition from legacy source batch 126.
        RestoreIfMissing(migrationBuilder, "dbo.usp_CalculateLateFee", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_CalculateLateFee
            @InvoiceId UNIQUEIDENTIFIER,
            @AsOfUtc DATETIME2(3) = NULL
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;
            IF @AsOfUtc IS NULL SET @AsOfUtc = SYSUTCDATETIME();

            BEGIN TRY
                BEGIN TRANSACTION;

                DECLARE @Status VARCHAR(50), @Type VARCHAR(50), @BillingMonth DATE,
                        @ContractId UNIQUEIDENTIFIER, @RentalAmount DECIMAL(18,2);

                SELECT @Status = Status, @Type = InvoiceType, @BillingMonth = BillingMonth,
                       @ContractId = EntityId, @RentalAmount = AmountDue
                FROM dbo.Invoice WITH (UPDLOCK, HOLDLOCK)
                WHERE InvoiceId = @InvoiceId;

                IF @Type <> 'RENTAL_FEE' OR @Status NOT IN ('OVERDUE','PAID')
                    THROW 51145, 'LATE_FEE_NOT_APPLICABLE', 1;

                DECLARE @PolicyId UNIQUEIDENTIFIER, @Divisor INT, @OverdueStartDay INT;
                SELECT @PolicyId = PolicyId FROM dbo.Contract WHERE ContractId = @ContractId;
                SELECT @Divisor = LateFeeDivisorDays, @OverdueStartDay = OverdueStartDay
                FROM dbo.Policy WHERE PolicyId = @PolicyId;

                DECLARE @Cutoff DATETIME2(3) = @AsOfUtc;
                IF @Status = 'PAID'
                BEGIN
                    SELECT TOP (1) @Cutoff = PaidAt
                    FROM dbo.Payment
                    WHERE InvoiceId = @InvoiceId AND Status = 'SUCCESS'
                    ORDER BY PaidAt DESC;
                END;

                DECLARE @StartDate DATE = DATEFROMPARTS(
                    YEAR(@BillingMonth), MONTH(@BillingMonth),
                    CASE WHEN @OverdueStartDay > DAY(EOMONTH(@BillingMonth))
                         THEN DAY(EOMONTH(@BillingMonth)) ELSE @OverdueStartDay END
                );

                DECLARE @Days INT = DATEDIFF(DAY, @StartDate, CAST(@Cutoff AS DATE)) + 1;
                IF @Days < 1 THROW 51145, 'LATE_FEE_NOT_APPLICABLE', 1;

                DECLARE @Amount DECIMAL(18,2) =
                    ROUND(CAST(@RentalAmount AS DECIMAL(18,4)) / @Divisor * @Days, 2);

                IF EXISTS (SELECT 1 FROM dbo.LateFee WHERE InvoiceId = @InvoiceId)
                    UPDATE dbo.LateFee
                    SET OverdueDays = @Days, Amount = @Amount, CalculatedAt = @AsOfUtc
                    WHERE InvoiceId = @InvoiceId;
                ELSE
                    INSERT INTO dbo.LateFee(InvoiceId, OverdueDays, Amount, CalculatedAt)
                    VALUES (@InvoiceId, @Days, @Amount, @AsOfUtc);

                COMMIT TRANSACTION;
                SELECT @InvoiceId AS InvoiceId, @Days AS OverdueDays, @Amount AS LateFeeAmount;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

        // PROCEDURE: dbo.usp_MarkOverdueInvoices — last definition from legacy source batch 127.
        RestoreIfMissing(migrationBuilder, "dbo.usp_MarkOverdueInvoices", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_MarkOverdueInvoices
            @NowUtc DATETIME2(3) = NULL
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;
            IF @NowUtc IS NULL SET @NowUtc = SYSUTCDATETIME();

            UPDATE inv
            SET Status = 'OVERDUE'
            FROM dbo.Invoice inv
            JOIN dbo.Contract c ON c.ContractId = inv.EntityId
            JOIN dbo.Policy p ON p.PolicyId = c.PolicyId
            WHERE inv.InvoiceType = 'RENTAL_FEE'
              AND inv.Status = 'UNPAID'
              AND @NowUtc >= CAST(DATEFROMPARTS(
                    YEAR(inv.BillingMonth), MONTH(inv.BillingMonth),
                    CASE WHEN p.OverdueStartDay > DAY(EOMONTH(inv.BillingMonth))
                         THEN DAY(EOMONTH(inv.BillingMonth)) ELSE p.OverdueStartDay END
                  ) AS DATETIME2(3));

            SELECT @@ROWCOUNT AS MarkedOverdueCount;
        END;
        """);

        // PROCEDURE: dbo.usp_CreatePolicyVersion — last definition from legacy source batch 128.
        RestoreIfMissing(migrationBuilder, "dbo.usp_CreatePolicyVersion", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_CreatePolicyVersion
            @DepositTimeoutHours INT,
            @ReservationVisitStartDay INT,
            @ReservationVisitEndDay INT,
            @MonthlyPaymentDueDay INT,
            @OverdueStartDay INT,
            @LateFeeDivisorDays INT,
            @EarlyReturnWaiveFeeUntilDay INT,
            @NowUtc DATETIME2(3) = NULL
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;
            IF @NowUtc IS NULL SET @NowUtc = SYSUTCDATETIME();

            BEGIN TRY
                BEGIN TRANSACTION;

                DECLARE @CurrentPolicyId UNIQUEIDENTIFIER, @NextVersion INT;
                SELECT TOP (1)
                    @CurrentPolicyId = PolicyId,
                    @NextVersion = Version + 1
                FROM dbo.Policy WITH (UPDLOCK, HOLDLOCK)
                WHERE Status = 'ACTIVE'
                ORDER BY Version DESC;

                IF @NextVersion IS NULL
                    SELECT @NextVersion = ISNULL(MAX(Version),0) + 1 FROM dbo.Policy WITH (UPDLOCK, HOLDLOCK);

                IF @CurrentPolicyId IS NOT NULL
                    UPDATE dbo.Policy
                    SET Status = 'INACTIVE', EffectiveTo = @NowUtc
                    WHERE PolicyId = @CurrentPolicyId;

                DECLARE @NewPolicyId UNIQUEIDENTIFIER = NEWID();
                INSERT INTO dbo.Policy(
                    PolicyId, Version, Status, EffectiveFrom, EffectiveTo,
                    DepositTimeoutHours, ReservationVisitStartDay, ReservationVisitEndDay,
                    MonthlyPaymentDueDay, OverdueStartDay, LateFeeDivisorDays,
                    EarlyReturnWaiveFeeUntilDay, CreatedAt
                )
                VALUES (
                    @NewPolicyId, @NextVersion, 'ACTIVE', @NowUtc, NULL,
                    @DepositTimeoutHours, @ReservationVisitStartDay, @ReservationVisitEndDay,
                    @MonthlyPaymentDueDay, @OverdueStartDay, @LateFeeDivisorDays,
                    @EarlyReturnWaiveFeeUntilDay, @NowUtc
                );

                COMMIT TRANSACTION;
                SELECT @NewPolicyId AS PolicyId, @NextVersion AS Version, 'ACTIVE' AS Status;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

        // PROCEDURE: dbo.usp_AssignSupportTicket — last definition from legacy source batch 129.
        RestoreIfMissing(migrationBuilder, "dbo.usp_AssignSupportTicket", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_AssignSupportTicket
            @SupportTicketId UNIQUEIDENTIFIER,
            @ManagerEmployeeId UNIQUEIDENTIFIER,
            @StaffEmployeeId UNIQUEIDENTIFIER
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;

            BEGIN TRY
                BEGIN TRANSACTION;

                DECLARE @FacilityId UNIQUEIDENTIFIER, @TicketStatus VARCHAR(50);
                SELECT @FacilityId = c.FacilityId, @TicketStatus = st.Status
                FROM dbo.SupportTicket st WITH (UPDLOCK, HOLDLOCK)
                JOIN dbo.Contract c ON c.ContractId = st.ContractId
                WHERE st.SupportTicketId = @SupportTicketId;

                IF @TicketStatus <> 'OPEN'
                    THROW 51146, 'SUPPORT_TICKET_INVALID_STATUS', 1;

                IF NOT EXISTS (
                    SELECT 1
                    FROM dbo.Employee e
                    JOIN dbo.UserAccount ua ON ua.UserAccountId=e.UserAccountId
                    JOIN dbo.UserRole ur ON ur.RoleId=ua.RoleId
                    WHERE e.EmployeeId=@ManagerEmployeeId
                      AND e.FacilityId=@FacilityId
                      AND ua.Status='ACTIVE'
                      AND ur.RoleName='FACILITY_MANAGER'
                )
                    THROW 51147, 'FACILITY_MANAGER_SCOPE_INVALID', 1;

                IF NOT EXISTS (
                    SELECT 1
                    FROM dbo.Employee e
                    JOIN dbo.UserAccount ua ON ua.UserAccountId=e.UserAccountId
                    JOIN dbo.UserRole ur ON ur.RoleId=ua.RoleId
                    WHERE e.EmployeeId=@StaffEmployeeId
                      AND e.FacilityId=@FacilityId
                      AND ua.Status='ACTIVE'
                      AND ur.RoleName='FACILITY_STAFF'
                )
                    THROW 51114, 'STAFF_FACILITY_OR_ROLE_INVALID', 1;

                UPDATE dbo.SupportTicket
                SET AssignedEmployeeId=@StaffEmployeeId, Status='IN_PROGRESS'
                WHERE SupportTicketId=@SupportTicketId AND Status='OPEN';

                COMMIT TRANSACTION;
                SELECT @SupportTicketId AS SupportTicketId, @StaffEmployeeId AS AssignedEmployeeId, 'IN_PROGRESS' AS Status;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

        // PROCEDURE: dbo.usp_CompleteSupportTicket — last definition from legacy source batch 130.
        RestoreIfMissing(migrationBuilder, "dbo.usp_CompleteSupportTicket", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_CompleteSupportTicket
            @SupportTicketId UNIQUEIDENTIFIER,
            @StaffEmployeeId UNIQUEIDENTIFIER,
            @ResultNote NVARCHAR(MAX),
            @NowUtc DATETIME2(3) = NULL
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;
            IF @NowUtc IS NULL SET @NowUtc=SYSUTCDATETIME();

            UPDATE dbo.SupportTicket
            SET ResultNote=@ResultNote, CompletedAt=@NowUtc, Status='COMPLETED'
            WHERE SupportTicketId=@SupportTicketId
              AND AssignedEmployeeId=@StaffEmployeeId
              AND Status='IN_PROGRESS';

            IF @@ROWCOUNT<>1
                THROW 51146, 'SUPPORT_TICKET_INVALID_STATUS_OR_ACTOR', 1;

            SELECT @SupportTicketId AS SupportTicketId, 'COMPLETED' AS Status;
        END;
        """);

        // PROCEDURE: dbo.usp_CancelSupportTicket — last definition from legacy source batch 131.
        RestoreIfMissing(migrationBuilder, "dbo.usp_CancelSupportTicket", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_CancelSupportTicket
            @SupportTicketId UNIQUEIDENTIFIER,
            @CustomerId UNIQUEIDENTIFIER
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;

            UPDATE dbo.SupportTicket
            SET Status='CANCELLED'
            WHERE SupportTicketId=@SupportTicketId
              AND CustomerId=@CustomerId
              AND Status IN ('OPEN','IN_PROGRESS');

            IF @@ROWCOUNT<>1
                THROW 51146, 'SUPPORT_TICKET_INVALID_STATUS_OR_OWNER', 1;

            SELECT @SupportTicketId AS SupportTicketId, 'CANCELLED' AS Status;
        END;
        """);

        // PROCEDURE: dbo.usp_SetUserAccountStatus — last definition from legacy source batch 132.
        RestoreIfMissing(migrationBuilder, "dbo.usp_SetUserAccountStatus", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_SetUserAccountStatus
            @UserAccountId UNIQUEIDENTIFIER,
            @NewStatus VARCHAR(50)
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;

            IF @NewStatus NOT IN ('ACTIVE','INACTIVE')
                THROW 51148, 'ACCOUNT_STATUS_INVALID', 1;

            IF @NewStatus='INACTIVE'
               AND EXISTS (
                   SELECT 1
                   FROM dbo.Customer c
                   WHERE c.UserAccountId=@UserAccountId
                     AND (
                         EXISTS (SELECT 1 FROM dbo.Reservation r WHERE r.CustomerId=c.CustomerId AND r.Status='CONFIRMED')
                         OR EXISTS (SELECT 1 FROM dbo.Contract ct WHERE ct.CustomerId=c.CustomerId AND ct.Status='ACTIVE')
                     )
               )
                THROW 51149, 'CUSTOMER_HAS_ACTIVE_LIFECYCLE', 1;

            UPDATE dbo.UserAccount
            SET Status=@NewStatus
            WHERE UserAccountId=@UserAccountId;

            IF @@ROWCOUNT<>1 THROW 51150, 'USER_ACCOUNT_NOT_FOUND', 1;

            SELECT @UserAccountId AS UserAccountId, @NewStatus AS Status;
        END;
        """);

        // PROCEDURE: dbo.usp_QueueNotification — last definition from legacy source batch 133.
        RestoreIfMissing(migrationBuilder, "dbo.usp_QueueNotification", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_QueueNotification
            @UserAccountId UNIQUEIDENTIFIER,
            @NotificationType VARCHAR(50),
            @Content NVARCHAR(MAX)
        AS
        BEGIN
            SET NOCOUNT ON;

            DECLARE @NotificationLogId UNIQUEIDENTIFIER=NEWID();
            INSERT INTO dbo.NotificationLog(
                NotificationLogId, UserAccountId, NotificationType, Content, Status, SentAt
            )
            VALUES (
                @NotificationLogId, @UserAccountId, @NotificationType, @Content, 'PENDING', NULL
            );

            SELECT @NotificationLogId AS NotificationLogId;
        END;
        """);

        // PROCEDURE: dbo.usp_MarkNotificationSent — last definition from legacy source batch 134.
        RestoreIfMissing(migrationBuilder, "dbo.usp_MarkNotificationSent", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_MarkNotificationSent
            @NotificationLogId UNIQUEIDENTIFIER,
            @SentAt DATETIME2(3)=NULL
        AS
        BEGIN
            SET NOCOUNT ON;
            IF @SentAt IS NULL SET @SentAt=SYSUTCDATETIME();

            UPDATE dbo.NotificationLog
            SET Status='SENT', SentAt=@SentAt
            WHERE NotificationLogId=@NotificationLogId AND Status='PENDING';

            IF @@ROWCOUNT<>1 THROW 51151, 'NOTIFICATION_NOT_PENDING', 1;
        END;
        """);

        // PROCEDURE: dbo.usp_RetryPendingNotification — last definition from legacy source batch 135.
        RestoreIfMissing(migrationBuilder, "dbo.usp_RetryPendingNotification", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_RetryPendingNotification
        AS
        BEGIN
            SET NOCOUNT ON;

            SELECT TOP (1)
                NotificationLogId, UserAccountId, NotificationType, Content, CreatedAt
            FROM dbo.NotificationLog WITH (UPDLOCK, READPAST, ROWLOCK)
            WHERE Status='PENDING'
            ORDER BY CreatedAt ASC;
        END;
        """);

        // PROCEDURE: dbo.usp_ExpirePendingReservations — last definition from legacy source batch 136.
        RestoreIfMissing(migrationBuilder, "dbo.usp_ExpirePendingReservations", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_ExpirePendingReservations
            @NowUtc DATETIME2(3)=NULL
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;
            IF @NowUtc IS NULL SET @NowUtc=SYSUTCDATETIME();

            BEGIN TRY
                BEGIN TRANSACTION;

                DECLARE @Expired TABLE(ReservationId UNIQUEIDENTIFIER PRIMARY KEY);

                INSERT INTO @Expired(ReservationId)
                SELECT r.ReservationId
                FROM dbo.Reservation r WITH (UPDLOCK, HOLDLOCK)
                JOIN dbo.Invoice i WITH (UPDLOCK, HOLDLOCK)
                  ON i.EntityId=r.ReservationId AND i.InvoiceType='DEPOSIT'
                WHERE r.Status='PENDING_DEPOSIT'
                  AND i.Status='UNPAID'
                  AND i.DueDate < @NowUtc;

                UPDATE i
                SET Status='CANCELLED'
                FROM dbo.Invoice i
                JOIN @Expired e ON e.ReservationId=i.EntityId
                WHERE i.InvoiceType='DEPOSIT' AND i.Status='UNPAID';

                UPDATE r
                SET Status='CANCELLED'
                FROM dbo.Reservation r
                JOIN @Expired e ON e.ReservationId=r.ReservationId;

                DECLARE @Count INT=(SELECT COUNT(*) FROM @Expired);

                COMMIT TRANSACTION;
                SELECT @Count AS ExpiredReservationCount;
            END TRY
            BEGIN CATCH
                IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

        // PROCEDURE: dbo.usp_ProcessReservationNoShow — last definition from legacy source batch 137.
        RestoreIfMissing(migrationBuilder, "dbo.usp_ProcessReservationNoShow", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_ProcessReservationNoShow
            @NowUtc DATETIME2(3)=NULL
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;
            IF @NowUtc IS NULL SET @NowUtc=SYSUTCDATETIME();

            BEGIN TRY
                BEGIN TRANSACTION;

                DECLARE @Targets TABLE(
                    ReservationId UNIQUEIDENTIFIER PRIMARY KEY,
                    VisitId UNIQUEIDENTIFIER,
                    VisitStatus VARCHAR(50)
                );

                INSERT INTO @Targets(ReservationId,VisitId,VisitStatus)
                SELECT r.ReservationId,v.VisitId,v.Status
                FROM dbo.Reservation r WITH (UPDLOCK,HOLDLOCK)
                JOIN dbo.Policy p ON p.PolicyId=r.PolicyId
                JOIN dbo.Visit v WITH (UPDLOCK,HOLDLOCK)
                  ON v.EntityId=r.ReservationId AND v.VisitType='RESERVATION'
                WHERE r.Status='CONFIRMED'
                  AND (
                      CAST(@NowUtc AS DATE) >
                      DATEFROMPARTS(
                          YEAR(r.StartMonth),MONTH(r.StartMonth),
                          CASE WHEN p.ReservationVisitEndDay > DAY(EOMONTH(r.StartMonth))
                               THEN DAY(EOMONTH(r.StartMonth)) ELSE p.ReservationVisitEndDay END
                      )
                  )
                  AND v.Status IN ('SCHEDULED','CHECKED_IN');

                UPDATE v
                SET Status=CASE WHEN t.VisitStatus='SCHEDULED' THEN 'CANCELLED' ELSE 'CHECKED_OUT' END
                FROM dbo.Visit v
                JOIN @Targets t ON t.VisitId=v.VisitId;

                UPDATE r
                SET Status='CANCELLED'
                FROM dbo.Reservation r
                JOIN @Targets t ON t.ReservationId=r.ReservationId;

                DECLARE @Count INT=(SELECT COUNT(*) FROM @Targets);

                COMMIT TRANSACTION;
                SELECT @Count AS ProcessedNoShowCount;
            END TRY
            BEGIN CATCH
                IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

        // PROCEDURE: dbo.usp_GenerateMonthlyInvoices — V10.1 first-month exclusion.
        CreateOrAlterProcedure(migrationBuilder, "dbo.usp_GenerateMonthlyInvoices", """
        CREATE OR ALTER PROCEDURE dbo.usp_GenerateMonthlyInvoices
            @BillingMonth DATE = NULL,
            @NowUtc DATETIME2(3) = NULL
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;
            IF @NowUtc IS NULL SET @NowUtc = SYSUTCDATETIME();
            IF @BillingMonth IS NULL
            BEGIN
                DECLARE @BusinessNow DATE = CAST(DATEADD(HOUR, 7, @NowUtc) AS DATE);
                SET @BillingMonth = DATEFROMPARTS(YEAR(@BusinessNow), MONTH(@BusinessNow), 1);
            END;

            DECLARE @ContractId UNIQUEIDENTIFIER;
            DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
                SELECT ContractId
                FROM dbo.Contract
                WHERE Status = 'ACTIVE'
                  AND StartMonth < @BillingMonth  -- first month was settled offline at handover
                  AND EndMonth >= @BillingMonth;

            OPEN cur;
            FETCH NEXT FROM cur INTO @ContractId;
            WHILE @@FETCH_STATUS = 0
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM dbo.Invoice
                    WHERE EntityId = @ContractId
                      AND InvoiceType = 'RENTAL_FEE'
                      AND BillingMonth = @BillingMonth
                )
                    EXEC dbo.usp_CreateMonthlyInvoice
                        @ContractId = @ContractId,
                        @BillingMonth = @BillingMonth,
                        @NowUtc = @NowUtc;

                FETCH NEXT FROM cur INTO @ContractId;
            END;
            CLOSE cur;
            DEALLOCATE cur;
        END;
        """);


        // PROCEDURE: dbo.usp_RecalculateOpenLateFees — last definition from legacy source batch 139.
        RestoreIfMissing(migrationBuilder, "dbo.usp_RecalculateOpenLateFees", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_RecalculateOpenLateFees
            @NowUtc DATETIME2(3)=NULL
        AS
        BEGIN
            SET NOCOUNT ON;
            IF @NowUtc IS NULL SET @NowUtc=SYSUTCDATETIME();

            DECLARE @InvoiceId UNIQUEIDENTIFIER;
            DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
                SELECT InvoiceId
                FROM dbo.Invoice
                WHERE InvoiceType='RENTAL_FEE'
                  AND Status='OVERDUE';

            OPEN cur;
            FETCH NEXT FROM cur INTO @InvoiceId;
            WHILE @@FETCH_STATUS=0
            BEGIN
                EXEC dbo.usp_CalculateLateFee @InvoiceId=@InvoiceId,@AsOfUtc=@NowUtc;
                FETCH NEXT FROM cur INTO @InvoiceId;
            END;
            CLOSE cur;
            DEALLOCATE cur;
        END;
        """);

        // PROCEDURE: dbo.usp_RescheduleVisit — last definition from legacy source batch 140.
        RestoreIfMissing(migrationBuilder, "dbo.usp_RescheduleVisit", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_RescheduleVisit
            @VisitId UNIQUEIDENTIFIER,
            @NewVisitDate DATE
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;

            BEGIN TRY
                BEGIN TRANSACTION;

                DECLARE @VisitType VARCHAR(50), @EntityId UNIQUEIDENTIFIER, @Status VARCHAR(50);
                SELECT @VisitType=VisitType,@EntityId=EntityId,@Status=Status
                FROM dbo.Visit WITH (UPDLOCK,HOLDLOCK)
                WHERE VisitId=@VisitId;

                IF @Status<>'SCHEDULED'
                    THROW 51112,'VISIT_INVALID_STATUS',1;

                IF @NewVisitDate < CAST(SYSUTCDATETIME() AS DATE)
                    THROW 51123,'VISIT_DATE_IN_PAST',1;

                IF @VisitType='RESERVATION'
                BEGIN
                    DECLARE @StartMonth DATE,@PolicyId UNIQUEIDENTIFIER,@StartDay INT,@EndDay INT;
                    SELECT @StartMonth=StartMonth,@PolicyId=PolicyId
                    FROM dbo.Reservation WHERE ReservationId=@EntityId;

                    SELECT @StartDay=ReservationVisitStartDay,@EndDay=ReservationVisitEndDay
                    FROM dbo.Policy WHERE PolicyId=@PolicyId;

                    IF DATEFROMPARTS(YEAR(@NewVisitDate),MONTH(@NewVisitDate),1)<>@StartMonth
                       OR DAY(@NewVisitDate) NOT BETWEEN @StartDay AND @EndDay
                        THROW 51109,'VISIT_DATE_OUT_OF_POLICY',1;
                END
                ELSE IF @VisitType='ACCESS'
                BEGIN
                    DECLARE @ContractStart DATE,@ContractEnd DATE,@ContractStatus VARCHAR(50);
                    SELECT @ContractStart=StartMonth,@ContractEnd=EndMonth,@ContractStatus=Status
                    FROM dbo.Contract WHERE ContractId=@EntityId;

                    IF @ContractStatus<>'ACTIVE'
                        THROW 51120,'CONTRACT_NOT_ACTIVE',1;

                    IF @NewVisitDate<@ContractStart OR @NewVisitDate>EOMONTH(@ContractEnd)
                        THROW 51121,'ACCESS_VISIT_DATE_OUTSIDE_CONTRACT',1;
                END
                ELSE IF @VisitType='RETURN'
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM dbo.Contract WHERE ContractId=@EntityId AND Status='ACTIVE')
                        THROW 51120,'CONTRACT_NOT_ACTIVE',1;
                END;

                UPDATE dbo.Visit SET VisitDate=@NewVisitDate WHERE VisitId=@VisitId;

                COMMIT TRANSACTION;
                SELECT @VisitId AS VisitId,@NewVisitDate AS VisitDate,'SCHEDULED' AS Status;
            END TRY
            BEGIN CATCH
                IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

        // PROCEDURE: dbo.usp_CreateSupportTicket — last definition from legacy source batch 141.
        RestoreIfMissing(migrationBuilder, "dbo.usp_CreateSupportTicket", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_CreateSupportTicket
            @ContractId UNIQUEIDENTIFIER,
            @CustomerId UNIQUEIDENTIFIER,
            @Category VARCHAR(50),
            @Description NVARCHAR(MAX)
        AS
        BEGIN
            SET NOCOUNT ON;

            IF NOT EXISTS (
                SELECT 1 FROM dbo.Contract
                WHERE ContractId=@ContractId AND CustomerId=@CustomerId
            )
                THROW 51152,'SUPPORT_CONTRACT_NOT_OWNED',1;

            DECLARE @SupportTicketId UNIQUEIDENTIFIER=NEWID();
            INSERT INTO dbo.SupportTicket(
                SupportTicketId,ContractId,CustomerId,AssignedEmployeeId,
                Category,Description,Status,ResultNote,CompletedAt
            )
            VALUES(
                @SupportTicketId,@ContractId,@CustomerId,NULL,
                @Category,@Description,'OPEN',NULL,NULL
            );

            SELECT @SupportTicketId AS SupportTicketId,'OPEN' AS Status;
        END;
        """);

        // PROCEDURE: dbo.usp_UpdateEmployeeRoleFacility — last definition from legacy source batch 145.
        RestoreIfMissing(migrationBuilder, "dbo.usp_UpdateEmployeeRoleFacility", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_UpdateEmployeeRoleFacility
            @EmployeeId UNIQUEIDENTIFIER,
            @RoleName VARCHAR(50),
            @FacilityId UNIQUEIDENTIFIER=NULL
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;

            IF @RoleName NOT IN(
                'FACILITY_STAFF','FACILITY_MANAGER',
                'BUSINESS_OPERATIONS_MANAGER','SYSTEM_ADMINISTRATOR'
            )
                THROW 51156,'EMPLOYEE_ROLE_INVALID',1;

            IF @RoleName IN('FACILITY_STAFF','FACILITY_MANAGER') AND @FacilityId IS NULL
                THROW 51157,'FACILITY_REQUIRED_FOR_EMPLOYEE_ROLE',1;

            IF @RoleName IN('BUSINESS_OPERATIONS_MANAGER','SYSTEM_ADMINISTRATOR') AND @FacilityId IS NOT NULL
                THROW 51158,'GLOBAL_ROLE_MUST_NOT_HAVE_FACILITY',1;

            BEGIN TRY
                BEGIN TRANSACTION;

                DECLARE @UserAccountId UNIQUEIDENTIFIER;
                SELECT @UserAccountId=UserAccountId
                FROM dbo.Employee WITH(UPDLOCK,HOLDLOCK)
                WHERE EmployeeId=@EmployeeId;

                IF @UserAccountId IS NULL THROW 51159,'EMPLOYEE_NOT_FOUND',1;

                DECLARE @RoleId UNIQUEIDENTIFIER=
                    (SELECT RoleId FROM dbo.UserRole WHERE RoleName=@RoleName);

                EXEC sys.sp_set_session_context @key=N'FRMS_ROLE_FACILITY_UPDATE', @value=1;

                UPDATE dbo.Employee SET FacilityId=@FacilityId WHERE EmployeeId=@EmployeeId;
                UPDATE dbo.UserAccount SET RoleId=@RoleId WHERE UserAccountId=@UserAccountId;

                /* Validate the final combined state before commit. */
                IF EXISTS (
                    SELECT 1
                    FROM dbo.Employee e
                    JOIN dbo.UserAccount ua ON ua.UserAccountId=e.UserAccountId
                    JOIN dbo.UserRole ur ON ur.RoleId=ua.RoleId
                    WHERE e.EmployeeId=@EmployeeId
                      AND (
                          (ur.RoleName IN('FACILITY_STAFF','FACILITY_MANAGER') AND e.FacilityId IS NULL)
                          OR
                          (ur.RoleName IN('BUSINESS_OPERATIONS_MANAGER','SYSTEM_ADMINISTRATOR') AND e.FacilityId IS NOT NULL)
                          OR ur.RoleName='CUSTOMER'
                      )
                )
                    THROW 51160,'EMPLOYEE_ROLE_FACILITY_INVALID',1;

                EXEC sys.sp_set_session_context @key=N'FRMS_ROLE_FACILITY_UPDATE', @value=NULL;
                COMMIT TRANSACTION;
                SELECT @EmployeeId AS EmployeeId,@RoleName AS RoleName,@FacilityId AS FacilityId;
            END TRY
            BEGIN CATCH
                EXEC sys.sp_set_session_context @key=N'FRMS_ROLE_FACILITY_UPDATE', @value=NULL;
                IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

        // TRIGGER: dbo.trg_StorageUnit_StatusTransition — last definition from legacy source batch 164.
        RestoreIfMissing(migrationBuilder, "dbo.trg_StorageUnit_StatusTransition", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_StorageUnit_StatusTransition
        ON dbo.StorageUnit
        AFTER UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN deleted d ON d.StorageUnitId = i.StorageUnitId
                WHERE i.Status <> d.Status
                  AND NOT (
                      (d.Status = 'AVAILABLE' AND i.Status IN ('IN_USE','MAINTENANCE'))
                      OR (d.Status = 'IN_USE' AND i.Status = 'INSPECTION')
                      OR (d.Status = 'INSPECTION' AND i.Status IN ('AVAILABLE','MAINTENANCE'))
                      OR (d.Status = 'MAINTENANCE' AND i.Status = 'AVAILABLE')
                  )
            )
                THROW 51013, 'Invalid StorageUnit status transition.', 1;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN deleted d ON d.StorageUnitId = i.StorageUnitId
                WHERE d.Status = 'AVAILABLE'
                  AND i.Status = 'MAINTENANCE'
                  AND EXISTS (
                      SELECT 1 FROM dbo.Contract c
                      WHERE c.StorageUnitId = i.StorageUnitId
                        AND c.Status = 'ACTIVE'
                  )
            )
                THROW 51202, 'UNIT_ACTIVE_CONTRACT_BLOCKS_MAINTENANCE', 1;
        END;
        """);

        // TRIGGER: dbo.trg_StorageUnit_ProtectUnitTypeReassignment — last definition from legacy source batch 165.
        RestoreIfMissing(migrationBuilder, "dbo.trg_StorageUnit_ProtectUnitTypeReassignment", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_StorageUnit_ProtectUnitTypeReassignment
        ON dbo.StorageUnit
        AFTER UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN deleted d ON d.StorageUnitId = i.StorageUnitId
                WHERE i.UnitTypeId <> d.UnitTypeId
                  AND (
                      d.Status IN ('IN_USE','INSPECTION')
                      OR i.Status IN ('IN_USE','INSPECTION')
                      OR EXISTS (
                          SELECT 1 FROM dbo.Contract c
                          WHERE c.StorageUnitId = i.StorageUnitId
                            AND c.Status = 'ACTIVE'
                      )
                  )
            )
                THROW 51203, 'UNIT_TYPE_REASSIGNMENT_BLOCKED', 1;
        END;
        """);

        // TRIGGER: dbo.trg_Discount_ProtectReferencedSemantics — last definition from legacy source batch 166.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Discount_ProtectReferencedSemantics", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Discount_ProtectReferencedSemantics
        ON dbo.Discount
        AFTER UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN deleted d ON d.DiscountId = i.DiscountId
                WHERE EXISTS (
                    SELECT 1 FROM dbo.Contract c WHERE c.DiscountId = i.DiscountId
                )
                  AND (
                      i.CustomerId <> d.CustomerId
                      OR i.Percentage <> d.Percentage
                      OR i.EffectiveFrom <> d.EffectiveFrom
                      OR (i.EffectiveTo <> d.EffectiveTo)
                      OR (i.EffectiveTo IS NULL AND d.EffectiveTo IS NOT NULL)
                      OR (i.EffectiveTo IS NOT NULL AND d.EffectiveTo IS NULL)
                  )
            )
                THROW 51204, 'REFERENCED_DISCOUNT_SEMANTICS_IMMUTABLE', 1;
        END;
        """);

        // TRIGGER: dbo.trg_DamageRecord_StatusTransition — last definition from legacy source batch 167.
        RestoreIfMissing(migrationBuilder, "dbo.trg_DamageRecord_StatusTransition", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_DamageRecord_StatusTransition
        ON dbo.DamageRecord
        AFTER UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN deleted d ON d.DamageRecordId = i.DamageRecordId
                WHERE i.Status <> d.Status
                  AND NOT (
                      d.Status = 'PENDING'
                      AND i.Status IN ('APPROVED','REJECTED')
                  )
            )
                THROW 51205, 'DAMAGE_INVALID_STATUS', 1;
        END;
        """);

        // TRIGGER: dbo.trg_Audit_DamageRecord — last definition from legacy source batch 168.
        RestoreIfMissing(migrationBuilder, "dbo.trg_Audit_DamageRecord", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_Audit_DamageRecord
        ON dbo.DamageRecord
        AFTER INSERT, UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;
            DECLARE @Actor UNIQUEIDENTIFIER=
                TRY_CONVERT(UNIQUEIDENTIFIER,CONVERT(NVARCHAR(36),SESSION_CONTEXT(N'UserAccountId')));

            INSERT INTO dbo.AuditLog(UserAccountId,Action,EntityType,EntityId)
            SELECT @Actor,
                   CASE WHEN d.DamageRecordId IS NULL THEN 'RECORD_DAMAGE' ELSE 'UPDATE_DAMAGE_RECORD' END,
                   'DAMAGE_RECORD',i.DamageRecordId
            FROM inserted i
            LEFT JOIN deleted d ON d.DamageRecordId=i.DamageRecordId
            WHERE d.DamageRecordId IS NULL
               OR i.Status = d.Status;
        END;
        """);

        // TRIGGER: dbo.trg_UserAccount_ProtectLoginIdentifiers — last definition from legacy source batch 169.
        RestoreIfMissing(migrationBuilder, "dbo.trg_UserAccount_ProtectLoginIdentifiers", "TR", """
        CREATE OR ALTER TRIGGER dbo.trg_UserAccount_ProtectLoginIdentifiers
        ON dbo.UserAccount
        AFTER UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;

            IF EXISTS (
                SELECT 1
                FROM inserted i
                JOIN deleted d ON d.UserAccountId = i.UserAccountId
                WHERE i.Email <> d.Email OR i.PhoneNumber <> d.PhoneNumber
            )
                THROW 51206, 'LOGIN_IDENTIFIER_IMMUTABLE', 1;
        END;
        """);

        // PROCEDURE: dbo.usp_CreateCustomerAccount — last definition from legacy source batch 170.
        RestoreIfMissing(migrationBuilder, "dbo.usp_CreateCustomerAccount", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_CreateCustomerAccount
            @Email VARCHAR(254),
            @PhoneNumber VARCHAR(30),
            @PasswordHash VARCHAR(500),
            @FullName NVARCHAR(150),
            @Address NVARCHAR(1000)=NULL,
            @CCCD VARCHAR(20)=NULL
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;

            BEGIN TRY
                BEGIN TRANSACTION;

                IF EXISTS(SELECT 1 FROM dbo.UserAccount WITH (UPDLOCK,HOLDLOCK) WHERE Email=@Email)
                    THROW 51155,'EMAIL_ALREADY_EXISTS',1;
                IF EXISTS(SELECT 1 FROM dbo.UserAccount WITH (UPDLOCK,HOLDLOCK) WHERE PhoneNumber=@PhoneNumber)
                    THROW 51207,'PHONE_NUMBER_ALREADY_EXISTS',1;

                DECLARE @RoleId UNIQUEIDENTIFIER=
                    (SELECT RoleId FROM dbo.UserRole WHERE RoleName='CUSTOMER');
                DECLARE @UserAccountId UNIQUEIDENTIFIER=NEWID(),@CustomerId UNIQUEIDENTIFIER=NEWID();

                INSERT INTO dbo.UserAccount(
                    UserAccountId,RoleId,Email,PhoneNumber,PasswordHash,Status
                )
                VALUES(
                    @UserAccountId,@RoleId,@Email,@PhoneNumber,@PasswordHash,'ACTIVE'
                );

                INSERT INTO dbo.Customer(CustomerId,UserAccountId,FullName,Address,CCCD)
                VALUES(@CustomerId,@UserAccountId,@FullName,@Address,@CCCD);

                COMMIT TRANSACTION;
                SELECT @UserAccountId AS UserAccountId,@CustomerId AS CustomerId;
            END TRY
            BEGIN CATCH
                IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

        // PROCEDURE: dbo.usp_CreateEmployeeAccount — last definition from legacy source batch 171.
        RestoreIfMissing(migrationBuilder, "dbo.usp_CreateEmployeeAccount", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_CreateEmployeeAccount
            @Email VARCHAR(254),
            @PhoneNumber VARCHAR(30),
            @PasswordHash VARCHAR(500),
            @FullName NVARCHAR(150),
            @RoleName VARCHAR(50),
            @FacilityId UNIQUEIDENTIFIER=NULL
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;

            IF @RoleName NOT IN(
                'FACILITY_STAFF','FACILITY_MANAGER',
                'BUSINESS_OPERATIONS_MANAGER','SYSTEM_ADMINISTRATOR'
            )
                THROW 51156,'EMPLOYEE_ROLE_INVALID',1;

            IF @RoleName IN('FACILITY_STAFF','FACILITY_MANAGER') AND @FacilityId IS NULL
                THROW 51157,'FACILITY_REQUIRED_FOR_EMPLOYEE_ROLE',1;

            IF @RoleName IN('BUSINESS_OPERATIONS_MANAGER','SYSTEM_ADMINISTRATOR') AND @FacilityId IS NOT NULL
                THROW 51158,'GLOBAL_ROLE_MUST_NOT_HAVE_FACILITY',1;

            BEGIN TRY
                BEGIN TRANSACTION;

                IF EXISTS(SELECT 1 FROM dbo.UserAccount WITH (UPDLOCK,HOLDLOCK) WHERE Email=@Email)
                    THROW 51155,'EMAIL_ALREADY_EXISTS',1;
                IF EXISTS(SELECT 1 FROM dbo.UserAccount WITH (UPDLOCK,HOLDLOCK) WHERE PhoneNumber=@PhoneNumber)
                    THROW 51207,'PHONE_NUMBER_ALREADY_EXISTS',1;

                DECLARE @RoleId UNIQUEIDENTIFIER=
                    (SELECT RoleId FROM dbo.UserRole WHERE RoleName=@RoleName);
                DECLARE @UserAccountId UNIQUEIDENTIFIER=NEWID(),@EmployeeId UNIQUEIDENTIFIER=NEWID();

                INSERT INTO dbo.UserAccount(
                    UserAccountId,RoleId,Email,PhoneNumber,PasswordHash,Status
                )
                VALUES(
                    @UserAccountId,@RoleId,@Email,@PhoneNumber,@PasswordHash,'INACTIVE'
                );

                INSERT INTO dbo.Employee(EmployeeId,UserAccountId,FacilityId,FullName)
                VALUES(@EmployeeId,@UserAccountId,@FacilityId,@FullName);

                COMMIT TRANSACTION;
                SELECT @UserAccountId AS UserAccountId,@EmployeeId AS EmployeeId;
            END TRY
            BEGIN CATCH
                IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

        // PROCEDURE: dbo.usp_CompleteHandover — authoritative offline first-month V10 update from main/update.
        // Always replace legacy version, including when procedure already exists.
        CreateOrAlterProcedure(migrationBuilder, "dbo.usp_CompleteHandover", """
        CREATE OR ALTER PROCEDURE dbo.usp_CompleteHandover
            @ReservationId UNIQUEIDENTIFIER,
            @VisitId UNIQUEIDENTIFIER,
            @StorageUnitId UNIQUEIDENTIFIER,
            @EmployeeId UNIQUEIDENTIFIER,
            @DiscountId UNIQUEIDENTIFIER = NULL,
            @NowUtc DATETIME2(3) = NULL
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;
        
            IF @NowUtc IS NULL
                SET @NowUtc = SYSUTCDATETIME();
        
            BEGIN TRY
                BEGIN TRANSACTION;
        
                DECLARE
                    @CustomerId UNIQUEIDENTIFIER,
                    @FacilityId UNIQUEIDENTIFIER,
                    @UnitTypeId UNIQUEIDENTIFIER,
                    @ReservationPolicyId UNIQUEIDENTIFIER,
                    @StartMonth DATE,
                    @EndMonth DATE,
                    @ReservationStatus VARCHAR(50);
        
                SELECT
                    @CustomerId = CustomerId,
                    @FacilityId = FacilityId,
                    @UnitTypeId = UnitTypeId,
                    @ReservationPolicyId = PolicyId,
                    @StartMonth = StartMonth,
                    @EndMonth = EndMonth,
                    @ReservationStatus = Status
                FROM dbo.Reservation WITH (UPDLOCK, HOLDLOCK)
                WHERE ReservationId = @ReservationId;
        
                IF @CustomerId IS NULL
                    THROW 51101, 'RESOURCE_NOT_FOUND', 1;
        
                DECLARE @ExistingContractId UNIQUEIDENTIFIER;
        
                SELECT
                    @ExistingContractId = ContractId
                FROM dbo.Contract
                WHERE ReservationId = @ReservationId;
        
                -- Idempotent retry after a successful handover.
                IF @ExistingContractId IS NOT NULL
                BEGIN
                    COMMIT TRANSACTION;
        
                    SELECT
                        c.ContractId,
                        c.StorageUnitId,
                        c.Status AS ContractStatus,
                        'ALREADY_COMPLETED' AS Result
                    FROM dbo.Contract c
                    WHERE c.ContractId = @ExistingContractId;
        
                    RETURN;
                END;
        
                IF @ReservationStatus <> 'CONFIRMED'
                    THROW 51107, 'RESERVATION_INVALID_STATUS', 1;
        
                DECLARE
                    @VisitEntityId UNIQUEIDENTIFIER,
                    @VisitType VARCHAR(50),
                    @VisitStatus VARCHAR(50);
        
                SELECT
                    @VisitEntityId = EntityId,
                    @VisitType = VisitType,
                    @VisitStatus = Status
                FROM dbo.Visit WITH (UPDLOCK, HOLDLOCK)
                WHERE VisitId = @VisitId;
        
                IF @VisitEntityId IS NULL
                    THROW 51101, 'RESOURCE_NOT_FOUND', 1;
        
                IF @VisitEntityId <> @ReservationId
                   OR @VisitType <> 'RESERVATION'
                   OR @VisitStatus <> 'CHECKED_IN'
                    THROW 51112, 'VISIT_INVALID_STATUS', 1;
        
                IF NOT EXISTS
                (
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
                    THROW 51114, 'FORBIDDEN', 1;
        
                DECLARE
                    @UnitFacilityId UNIQUEIDENTIFIER,
                    @UnitUnitTypeId UNIQUEIDENTIFIER,
                    @UnitStatus VARCHAR(50);
        
                SELECT
                    @UnitFacilityId = FacilityId,
                    @UnitUnitTypeId = UnitTypeId,
                    @UnitStatus = Status
                FROM dbo.StorageUnit WITH (UPDLOCK, HOLDLOCK)
                WHERE StorageUnitId = @StorageUnitId;
        
                IF @UnitFacilityId IS NULL
                    THROW 51101, 'RESOURCE_NOT_FOUND', 1;
        
                IF @UnitFacilityId <> @FacilityId
                   OR @UnitUnitTypeId <> @UnitTypeId
                    THROW 51117, 'UNIT_FACILITY_TYPE_MISMATCH', 1;
        
                IF @UnitStatus <> 'AVAILABLE'
                    THROW 51117, 'UNIT_NOT_AVAILABLE', 1;
        
                IF @DiscountId IS NOT NULL
                BEGIN
                    DECLARE
                        @DiscountCustomerId UNIQUEIDENTIFIER,
                        @DiscountStatus VARCHAR(50),
                        @DiscountEffectiveFrom DATETIME2(3),
                        @DiscountEffectiveTo DATETIME2(3);
        
                    SELECT
                        @DiscountCustomerId = CustomerId,
                        @DiscountStatus = Status,
                        @DiscountEffectiveFrom = EffectiveFrom,
                        @DiscountEffectiveTo = EffectiveTo
                    FROM dbo.Discount
                    WHERE DiscountId = @DiscountId;
        
                    IF @DiscountCustomerId IS NULL
                        THROW 51118, 'DISCOUNT_NOT_VALID', 1;
        
                    IF @DiscountCustomerId <> @CustomerId
                        THROW 51120, 'DISCOUNT_NOT_OWNED_BY_CUSTOMER', 1;
        
                    IF @DiscountStatus <> 'ACTIVE'
                       OR @DiscountEffectiveFrom > @NowUtc
                       OR (
                            @DiscountEffectiveTo IS NOT NULL
                            AND @DiscountEffectiveTo < @NowUtc
                          )
                        THROW 51118, 'DISCOUNT_NOT_VALID', 1;
                END;
        
                DECLARE @ContractId UNIQUEIDENTIFIER = NEWID();
        
                INSERT INTO dbo.Contract
                (
                    ContractId,
                    ReservationId,
                    CustomerId,
                    FacilityId,
                    StorageUnitId,
                    PolicyId,
                    DiscountId,
                    StartMonth,
                    EndMonth,
                    Status
                )
                VALUES
                (
                    @ContractId,
                    @ReservationId,
                    @CustomerId,
                    @FacilityId,
                    @StorageUnitId,
                    @ReservationPolicyId,
                    @DiscountId,
                    @StartMonth,
                    @EndMonth,
                    'ACTIVE'
                );
        
                -- V10: no first-month Invoice and no first-month Payment.
        
                UPDATE dbo.StorageUnit
                SET Status = 'IN_USE'
                WHERE StorageUnitId = @StorageUnitId
                  AND Status = 'AVAILABLE';
        
                IF @@ROWCOUNT <> 1
                    THROW 51117, 'UNIT_NOT_AVAILABLE', 1;
        
                UPDATE dbo.Visit
                SET
                    Status = 'CHECKED_OUT',
                    EmployeeId = @EmployeeId
                WHERE VisitId = @VisitId
                  AND Status = 'CHECKED_IN';
        
                IF @@ROWCOUNT <> 1
                    THROW 51112, 'VISIT_INVALID_STATUS', 1;
        
                UPDATE dbo.Reservation
                SET Status = 'COMPLETED'
                WHERE ReservationId = @ReservationId
                  AND Status = 'CONFIRMED';
        
                IF @@ROWCOUNT <> 1
                    THROW 51107, 'RESERVATION_INVALID_STATUS', 1;
        
                COMMIT TRANSACTION;
        
                SELECT
                    @ContractId AS ContractId,
                    @StorageUnitId AS StorageUnitId,
                    'ACTIVE' AS ContractStatus,
                    'COMPLETED' AS Result;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0
                    ROLLBACK TRANSACTION;
        
                THROW;
            END CATCH
        END;
        """);

        // PROCEDURE: dbo.usp_CreateMonthlyInvoice — last definition from legacy source batch 173.
        CreateOrAlterProcedure(migrationBuilder, "dbo.usp_CreateMonthlyInvoice", """
        CREATE OR ALTER PROCEDURE dbo.usp_CreateMonthlyInvoice
            @ContractId UNIQUEIDENTIFIER,
            @BillingMonth DATE,
            @NowUtc DATETIME2(3) = NULL
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;
            IF @NowUtc IS NULL SET @NowUtc = SYSUTCDATETIME();

            IF DAY(@BillingMonth) <> 1
                THROW 51137, 'BILLING_MONTH_INVALID', 1;

            BEGIN TRY
                BEGIN TRANSACTION;

                DECLARE @ExistingInvoiceId UNIQUEIDENTIFIER;
                SELECT @ExistingInvoiceId = InvoiceId
                FROM dbo.Invoice
                WHERE EntityId = @ContractId
                  AND InvoiceType = 'RENTAL_FEE'
                  AND BillingMonth = @BillingMonth;

                IF @ExistingInvoiceId IS NOT NULL
                BEGIN
                    COMMIT TRANSACTION;
                    SELECT @ExistingInvoiceId AS InvoiceId, 'EXISTING' AS Result;
                    RETURN;
                END;

                DECLARE @StartMonth DATE, @EndMonth DATE, @ReservationId UNIQUEIDENTIFIER,
                        @PolicyId UNIQUEIDENTIFIER, @DiscountId UNIQUEIDENTIFIER, @Status VARCHAR(50);

                SELECT @StartMonth = StartMonth, @EndMonth = EndMonth,
                       @ReservationId = ReservationId, @PolicyId = PolicyId,
                       @DiscountId = DiscountId, @Status = Status
                FROM dbo.Contract WITH (UPDLOCK, HOLDLOCK)
                WHERE ContractId = @ContractId;

                IF @Status <> 'ACTIVE'
                    THROW 51120, 'CONTRACT_NOT_ACTIVE', 1;

                IF @BillingMonth < @StartMonth OR @BillingMonth > @EndMonth
                    THROW 51137, 'BILLING_MONTH_INVALID', 1;

                IF @BillingMonth = @StartMonth
                    THROW 51208, 'FIRST_MONTH_INVOICE_HANDOVER_ONLY', 1;

                DECLARE @InitialEndMonth DATE, @BaseAmount DECIMAL(18,2);
                SELECT @InitialEndMonth = EndMonth, @BaseAmount = LockedRentalPrice
                FROM dbo.Reservation
                WHERE ReservationId = @ReservationId;

                IF @BillingMonth > @InitialEndMonth
                BEGIN
                    SET @BaseAmount = NULL;
                    SELECT TOP (1) @BaseAmount = AppliedMonthlyPrice
                    FROM dbo.ContractExtension
                    WHERE ContractId = @ContractId
                      AND DATEADD(MONTH,1,OldEndMonth) <= @BillingMonth
                      AND NewEndMonth >= @BillingMonth
                    ORDER BY NewEndMonth ASC;

                    IF @BaseAmount IS NULL
                        THROW 51138, 'RENEWAL_PRICE_SNAPSHOT_NOT_FOUND', 1;
                END;

                DECLARE @DiscountPct DECIMAL(5,2) = 0,
                        @AppliedDiscountId UNIQUEIDENTIFIER = NULL;

                IF @DiscountId IS NOT NULL
                BEGIN
                    /* Existing Contract usage remains valid after Discount inactivation. */
                    SELECT @DiscountPct = Percentage,
                           @AppliedDiscountId = DiscountId
                    FROM dbo.Discount
                    WHERE DiscountId = @DiscountId;

                    IF @AppliedDiscountId IS NULL
                        THROW 51118, 'DISCOUNT_NOT_VALID', 1;
                END;

                DECLARE @DiscountAmount DECIMAL(18,2) =
                    ROUND(CAST(@BaseAmount AS DECIMAL(18,4)) * ISNULL(@DiscountPct,0) / 100.0, 2);
                DECLARE @AmountDue DECIMAL(18,2) =
                    CASE WHEN @BaseAmount - @DiscountAmount < 0 THEN 0
                         ELSE @BaseAmount - @DiscountAmount END;

                DECLARE @DueDay INT;
                SELECT @DueDay = MonthlyPaymentDueDay
                FROM dbo.Policy WHERE PolicyId = @PolicyId;

                DECLARE @DueDate DATE =
                    DATEFROMPARTS(
                        YEAR(@BillingMonth), MONTH(@BillingMonth),
                        CASE WHEN @DueDay > DAY(EOMONTH(@BillingMonth))
                             THEN DAY(EOMONTH(@BillingMonth)) ELSE @DueDay END
                    );

                DECLARE @InvoiceId UNIQUEIDENTIFIER = NEWID();
                INSERT INTO dbo.Invoice(
                    InvoiceId, EntityId, InvoiceType, BillingMonth, BaseAmount,
                    DiscountId, DiscountAmount, AmountDue, DueDate, Status, CreatedAt
                )
                VALUES (
                    @InvoiceId, @ContractId, 'RENTAL_FEE', @BillingMonth, @BaseAmount,
                    @AppliedDiscountId, @DiscountAmount, @AmountDue,
                    CAST(@DueDate AS DATETIME2(3)), 'UNPAID', @NowUtc
                );

                COMMIT TRANSACTION;
                SELECT @InvoiceId AS InvoiceId,
                       @BaseAmount AS BaseAmount,
                       @DiscountAmount AS DiscountAmount,
                       @AmountDue AS AmountDue,
                       @DueDate AS DueDate;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

        // PROCEDURE: dbo.usp_ConfirmActualReturn — last definition from legacy source batch 174.
        RestoreIfMissing(migrationBuilder, "dbo.usp_ConfirmActualReturn", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_ConfirmActualReturn
            @ContractId UNIQUEIDENTIFIER,
            @EmployeeId UNIQUEIDENTIFIER,
            @VisitId UNIQUEIDENTIFIER = NULL,
            @ActualReturnDate DATETIME2(3) = NULL
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;
            IF @ActualReturnDate IS NULL SET @ActualReturnDate = SYSUTCDATETIME();

            BEGIN TRY
                BEGIN TRANSACTION;

                DECLARE @FacilityId UNIQUEIDENTIFIER, @StorageUnitId UNIQUEIDENTIFIER,
                        @EndMonth DATE, @PolicyId UNIQUEIDENTIFIER, @ContractStatus VARCHAR(50),
                        @UnitStatus VARCHAR(50);

                SELECT @FacilityId = c.FacilityId,
                       @StorageUnitId = c.StorageUnitId,
                       @EndMonth = c.EndMonth,
                       @PolicyId = c.PolicyId,
                       @ContractStatus = c.Status,
                       @UnitStatus = su.Status
                FROM dbo.Contract c WITH (UPDLOCK, HOLDLOCK)
                JOIN dbo.StorageUnit su WITH (UPDLOCK, HOLDLOCK)
                  ON su.StorageUnitId = c.StorageUnitId
                WHERE c.ContractId = @ContractId;

                IF @ContractStatus <> 'ACTIVE'
                    THROW 51120, 'CONTRACT_NOT_ACTIVE', 1;

                DECLARE @ExistingInspectionId UNIQUEIDENTIFIER;

                IF @VisitId IS NOT NULL
                BEGIN
                    SELECT @ExistingInspectionId = InspectionId
                    FROM dbo.Inspection
                    WHERE ContractId = @ContractId
                      AND StorageUnitId = @StorageUnitId
                      AND VisitId = @VisitId;
                END
                ELSE IF @UnitStatus <> 'IN_USE'
                BEGIN
                    SELECT TOP (1) @ExistingInspectionId = InspectionId
                    FROM dbo.Inspection
                    WHERE ContractId = @ContractId
                      AND StorageUnitId = @StorageUnitId
                      AND VisitId IS NULL
                    ORDER BY CASE Status WHEN 'PENDING' THEN 1 WHEN 'IN_PROGRESS' THEN 2 ELSE 3 END,
                             CompletedAt DESC,
                             InspectionId DESC;
                END;

                IF @ExistingInspectionId IS NOT NULL
                BEGIN
                    COMMIT TRANSACTION;
                    SELECT @ExistingInspectionId AS InspectionId, 'ALREADY_CONFIRMED' AS Result;
                    RETURN;
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM dbo.Employee e
                    JOIN dbo.UserAccount ua ON ua.UserAccountId = e.UserAccountId
                    JOIN dbo.UserRole ur ON ur.RoleId = ua.RoleId
                    WHERE e.EmployeeId = @EmployeeId
                      AND e.FacilityId = @FacilityId
                      AND ua.Status = 'ACTIVE'
                      AND ur.RoleName = 'FACILITY_STAFF'
                )
                    THROW 51114, 'STAFF_FACILITY_OR_ROLE_INVALID', 1;

                IF @VisitId IS NOT NULL
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM dbo.Visit WITH (UPDLOCK, HOLDLOCK)
                        WHERE VisitId = @VisitId
                          AND EntityId = @ContractId
                          AND VisitType = 'RETURN'
                          AND Status = 'CHECKED_IN'
                    )
                        THROW 51112, 'VISIT_INVALID_STATUS', 1;

                    UPDATE dbo.Visit
                    SET ActualReturnDate = @ActualReturnDate,
                        EmployeeId = @EmployeeId
                    WHERE VisitId = @VisitId;
                END;

                UPDATE dbo.StorageUnit
                SET Status = 'INSPECTION'
                WHERE StorageUnitId = @StorageUnitId AND Status = 'IN_USE';

                IF @@ROWCOUNT <> 1
                    THROW 51124, 'STORAGE_UNIT_NOT_IN_USE', 1;

                DECLARE @InspectionId UNIQUEIDENTIFIER = NEWID();
                INSERT INTO dbo.Inspection(
                    InspectionId, ContractId, StorageUnitId, VisitId,
                    EmployeeId, Status, ConditionNote, CompletedAt
                )
                VALUES (
                    @InspectionId, @ContractId, @StorageUnitId, @VisitId,
                    NULL, 'PENDING', NULL, NULL
                );

                DECLARE @ContractEndDate DATE = EOMONTH(@EndMonth);
                DECLARE @ActorUserAccountId UNIQUEIDENTIFIER;
                SELECT @ActorUserAccountId = UserAccountId
                FROM dbo.Employee
                WHERE EmployeeId = @EmployeeId;

                INSERT INTO dbo.AuditLog(UserAccountId, Action, EntityType, EntityId, CreatedAt)
                VALUES (@ActorUserAccountId, 'CONFIRM_ACTUAL_RETURN', 'CONTRACT', @ContractId, @ActualReturnDate);

                IF CAST(@ActualReturnDate AS DATE) < @ContractEndDate
                BEGIN
                    DECLARE @WaiveUntilDay INT;
                    SELECT @WaiveUntilDay = EarlyReturnWaiveFeeUntilDay
                    FROM dbo.Policy WHERE PolicyId = @PolicyId;

                    IF DAY(@ActualReturnDate) <= @WaiveUntilDay
                    BEGIN
                        DECLARE @ReturnMonth DATE =
                            DATEFROMPARTS(YEAR(@ActualReturnDate), MONTH(@ActualReturnDate), 1);

                        DELETE lf
                        FROM dbo.LateFee lf
                        JOIN dbo.Invoice i ON i.InvoiceId = lf.InvoiceId
                        WHERE i.EntityId = @ContractId
                          AND i.InvoiceType = 'RENTAL_FEE'
                          AND i.BillingMonth = @ReturnMonth
                          AND i.Status = 'UNPAID';

                        UPDATE dbo.Invoice
                        SET Status = 'CANCELLED'
                        WHERE EntityId = @ContractId
                          AND InvoiceType = 'RENTAL_FEE'
                          AND BillingMonth = @ReturnMonth
                          AND Status = 'UNPAID';
                    END;
                END;

                COMMIT TRANSACTION;

                SELECT @InspectionId AS InspectionId,
                       CASE WHEN CAST(@ActualReturnDate AS DATE) < @ContractEndDate
                            THEN 'EARLY_RETURN' ELSE 'NORMAL_RETURN' END AS ReturnType;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

        // PROCEDURE: dbo.usp_DecideDamage — last definition from legacy source batch 175.
        RestoreIfMissing(migrationBuilder, "dbo.usp_DecideDamage", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_DecideDamage
            @DamageRecordId UNIQUEIDENTIFIER,
            @ManagerEmployeeId UNIQUEIDENTIFIER,
            @Decision VARCHAR(50)
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;

            IF @Decision NOT IN ('APPROVED','REJECTED')
                THROW 51209, 'DAMAGE_INVALID_STATUS', 1;

            BEGIN TRY
                BEGIN TRANSACTION;

                DECLARE @FacilityId UNIQUEIDENTIFIER,
                        @CurrentStatus VARCHAR(50),
                        @ManagerUserAccountId UNIQUEIDENTIFIER;

                SELECT @FacilityId = c.FacilityId,
                       @CurrentStatus = dr.Status
                FROM dbo.DamageRecord dr WITH (UPDLOCK,HOLDLOCK)
                JOIN dbo.Inspection i ON i.InspectionId = dr.InspectionId
                JOIN dbo.Contract c ON c.ContractId = i.ContractId
                WHERE dr.DamageRecordId = @DamageRecordId;

                IF @CurrentStatus IS NULL
                    THROW 51210, 'DAMAGE_RECORD_NOT_FOUND', 1;

                IF @CurrentStatus <> 'PENDING'
                    THROW 51209, 'DAMAGE_INVALID_STATUS', 1;

                SELECT @ManagerUserAccountId = e.UserAccountId
                FROM dbo.Employee e
                JOIN dbo.UserAccount ua ON ua.UserAccountId=e.UserAccountId
                JOIN dbo.UserRole ur ON ur.RoleId=ua.RoleId
                WHERE e.EmployeeId=@ManagerEmployeeId
                  AND e.FacilityId=@FacilityId
                  AND ua.Status='ACTIVE'
                  AND ur.RoleName='FACILITY_MANAGER';

                IF @ManagerUserAccountId IS NULL
                    THROW 51147,'FACILITY_MANAGER_SCOPE_INVALID',1;

                UPDATE dbo.DamageRecord
                SET Status = @Decision
                WHERE DamageRecordId = @DamageRecordId
                  AND Status = 'PENDING';

                IF @@ROWCOUNT <> 1
                    THROW 51209, 'DAMAGE_INVALID_STATUS', 1;

                INSERT INTO dbo.AuditLog(UserAccountId,Action,EntityType,EntityId,NewValue,CreatedAt)
                VALUES(
                    @ManagerUserAccountId,'DECIDE_DAMAGE','DAMAGE_RECORD',@DamageRecordId,
                    CONCAT(N'{"Status":"',@Decision,N'"}'),SYSUTCDATETIME()
                );

                COMMIT TRANSACTION;
                SELECT @DamageRecordId AS DamageRecordId,@Decision AS Status;
            END TRY
            BEGIN CATCH
                IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

        // PROCEDURE: dbo.usp_SetDamageRecordStatus — last definition from legacy source batch 176.
        RestoreIfMissing(migrationBuilder, "dbo.usp_SetDamageRecordStatus", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_SetDamageRecordStatus
            @DamageRecordId UNIQUEIDENTIFIER,
            @ManagerEmployeeId UNIQUEIDENTIFIER,
            @NewStatus VARCHAR(50)
        AS
        BEGIN
            SET NOCOUNT ON;
            EXEC dbo.usp_DecideDamage
                @DamageRecordId=@DamageRecordId,
                @ManagerEmployeeId=@ManagerEmployeeId,
                @Decision=@NewStatus;
        END;
        """);

        // SCOPE-LOCK: dbo.usp_FinalizeReturn intentionally excluded from this Reservation-only repair.
        // Reason: Legacy settlement omits atomic StorageUnit release required by SRS V10.1.
        // Requires explicit CHANGE SCOPE to review and implement the latest authoritative SQL.


        // PROCEDURE: dbo.usp_ReplaceInactiveEmployeeCredentialHash — last definition from legacy source batch 179.
        RestoreIfMissing(migrationBuilder, "dbo.usp_ReplaceInactiveEmployeeCredentialHash", "P", """
        CREATE OR ALTER PROCEDURE dbo.usp_ReplaceInactiveEmployeeCredentialHash
            @EmployeeId UNIQUEIDENTIFIER,
            @NewPasswordHash VARCHAR(500)
        AS
        BEGIN
            SET NOCOUNT ON;
            SET XACT_ABORT ON;

            IF NULLIF(LTRIM(RTRIM(@NewPasswordHash)), '') IS NULL
                THROW 51214, 'PASSWORD_HASH_REQUIRED', 1;

            BEGIN TRY
                BEGIN TRANSACTION;

                DECLARE @UserAccountId UNIQUEIDENTIFIER,
                        @AccountStatus VARCHAR(50);

                SELECT @UserAccountId = ua.UserAccountId,
                       @AccountStatus = ua.Status
                FROM dbo.Employee e
                JOIN dbo.UserAccount ua WITH (UPDLOCK, HOLDLOCK)
                  ON ua.UserAccountId = e.UserAccountId
                WHERE e.EmployeeId = @EmployeeId;

                IF @UserAccountId IS NULL
                    THROW 51150, 'USER_ACCOUNT_NOT_FOUND', 1;

                IF @AccountStatus <> 'INACTIVE'
                    THROW 51213, 'CREDENTIAL_PROVISIONING_INVALID_STATUS', 1;

                UPDATE dbo.UserAccount
                SET PasswordHash = @NewPasswordHash
                WHERE UserAccountId = @UserAccountId
                  AND Status = 'INACTIVE';

                IF @@ROWCOUNT <> 1
                    THROW 51213, 'CREDENTIAL_PROVISIONING_INVALID_STATUS', 1;

                COMMIT TRANSACTION;

                SELECT @EmployeeId AS EmployeeId,
                       @UserAccountId AS UserAccountId,
                       'INACTIVE' AS Status;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END;
        """);

    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Objects may have existed before this repair migration.
        // Dropping them would destroy valid historical SQL definitions.
        throw new NotSupportedException(
            "Automatic rollback is unsafe for restored legacy procedures/triggers. Review manually before downgrade.");
    }

    private static void CreateOrAlterProcedure(MigrationBuilder migrationBuilder, string qualifiedName, string sqlDefinition)
    {
        // SQL Server requires CREATE OR ALTER PROCEDURE as the first statement in a batch.
        // Dynamic EXEC provides a separate batch without using SQLCMD GO or changing DB.
        var escapedSql = sqlDefinition.Replace("'", "''", StringComparison.Ordinal);
        migrationBuilder.Sql($"EXEC(N'{escapedSql}');");
    }

    private static void RestoreIfMissing(
        MigrationBuilder migrationBuilder, string qualifiedName, string objectType, string sqlDefinition)
    {
        // All three arguments above are literal code authored with the migration.
        // Quote-escape preserves Unicode SQL without relying on GO batch commands.
        var escapedSql = sqlDefinition.Replace("'", "''", StringComparison.Ordinal);
        migrationBuilder.Sql(
            $"IF OBJECT_ID(N'{qualifiedName}', N'{objectType}') IS NULL EXEC(N'{escapedSql}');");
    }
}
