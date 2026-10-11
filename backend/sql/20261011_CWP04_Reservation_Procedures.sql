/*
CWP-04 procedure-only forward deployment.
Generated SQL operations: 20261011150000_Cwp04ReservationProcedures.
Prerequisite: the existing eight-migration EF baseline, including legacy
restore and AuditLogId default. Does not create/adopt a database or fabricate
earlier migration history. No Invoice/Payment/gateway or historical-row edits.
DEFAULT: dry run. Before applying, back up current procedure definitions and
set BOTH values below. For normal EF deployment, prefer database update.
Rollback: automatic downgrade is blocked; redeploy reviewed compatible
procedure definitions after verifying ownership, lifecycle and locking rules.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @ApplyPatch bit = 0;
DECLARE @ExpectedDatabase sysname = NULL;

IF @ApplyPatch = 0
BEGIN
    SELECT DB_NAME() AS SelectedDatabase, 'DRY RUN - no changes' AS Result;
    RETURN;
END;
IF @ExpectedDatabase IS NULL OR @ExpectedDatabase <> DB_NAME()
    THROW 51980, 'Explicit database selection and confirmation are required.', 1;
IF DB_NAME() IN ('master','model','msdb','tempdb')
    THROW 51980, 'System databases are not deployment targets.', 1;
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
    THROW 51981, 'Existing EF baseline is required; do not fabricate migration history.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N'20261011120000_RestoreLegacySqlObjects')
    OR NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N'20261011133000_FixAuditLogIdDefault')
    THROW 51981, 'Apply the approved legacy baseline and AuditLog default migration first.', 1;
IF EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N'20261011150000_Cwp04ReservationProcedures')
BEGIN
    SELECT 'CWP-04 migration already applied - no changes' AS Result;
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;
    EXEC(N'CREATE OR ALTER PROCEDURE dbo.usp_CancelReservation
        @UserAccountId uniqueidentifier,
        @CustomerId uniqueidentifier,
        @ReservationId uniqueidentifier
    AS
    BEGIN
        SET NOCOUNT ON;
        SET XACT_ABORT ON;
        BEGIN TRY
            BEGIN TRANSACTION;

            DECLARE @Status varchar(50), @OwnerId uniqueidentifier;
            SELECT @Status = Status, @OwnerId = CustomerId
            FROM dbo.Reservation WITH (UPDLOCK, HOLDLOCK)
            WHERE ReservationId = @ReservationId;
            IF @Status IS NULL OR @OwnerId <> @CustomerId
                THROW 51201, ''RESOURCE_NOT_FOUND'', 1;

            DECLARE @AccountStatus varchar(50), @Role varchar(50);
            SELECT @AccountStatus = a.Status, @Role = r.RoleName
            FROM dbo.Customer c WITH (HOLDLOCK)
            JOIN dbo.UserAccount a WITH (HOLDLOCK) ON a.UserAccountId = c.UserAccountId
            JOIN dbo.UserRole r WITH (HOLDLOCK) ON r.RoleId = a.RoleId
            WHERE c.CustomerId = @CustomerId AND a.UserAccountId = @UserAccountId;
            IF @AccountStatus IS NULL
                THROW 51201, ''RESOURCE_NOT_FOUND'', 1;
            IF @AccountStatus <> ''ACTIVE''
                THROW 51202, ''ACCOUNT_INACTIVE'', 1;
            IF @Role <> ''CUSTOMER''
                THROW 51203, ''FORBIDDEN'', 1;

            IF @Status NOT IN (''PENDING_DEPOSIT'', ''CONFIRMED'', ''CANCELLED'')
                THROW 51204, ''RESERVATION_INVALID_STATUS'', 1;

            -- Lock both existing visits and the absent-visit range against
            -- concurrent confirm/check-in, and reject any handover history.
            IF EXISTS (
                SELECT 1 FROM dbo.Visit WITH (UPDLOCK, HOLDLOCK)
                WHERE EntityId = @ReservationId AND VisitType = ''RESERVATION''
                    AND Status IN (''CHECKED_IN'', ''CHECKED_OUT'')
            )
                THROW 51205, ''VISIT_INVALID_STATUS'', 1;
            IF EXISTS (
                SELECT 1 FROM dbo.Contract WITH (UPDLOCK, HOLDLOCK)
                WHERE ReservationId = @ReservationId
            )
                THROW 51204, ''RESERVATION_INVALID_STATUS'', 1;

            UPDATE dbo.Visit SET Status = ''CANCELLED''
            WHERE EntityId = @ReservationId AND VisitType = ''RESERVATION''
                AND Status = ''SCHEDULED'';
            UPDATE dbo.Reservation SET Status = ''CANCELLED''
            WHERE ReservationId = @ReservationId
                AND Status IN (''PENDING_DEPOSIT'', ''CONFIRMED'');
            -- CapacityRepository excludes CANCELLED holds. There is no
            -- capacity counter to decrement, including on an idempotent retry.
            -- Invoice/Payment and physical StorageUnit are never changed.
            COMMIT TRANSACTION;
        END TRY
        BEGIN CATCH
            IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
            THROW;
        END CATCH
    END;');

    EXEC(N'CREATE OR ALTER PROCEDURE dbo.usp_ConfirmReservation
        @ReservationId uniqueidentifier,
        @ReservationVisitDate date,
        @NowUtc datetime2(3) = NULL
    AS
    BEGIN
        SET NOCOUNT ON;
        SET XACT_ABORT ON;
        BEGIN TRY
            BEGIN TRANSACTION;
            -- Match cancellation/handover lock order: Reservation before Visit.
            DECLARE @Status varchar(50), @PolicyId uniqueidentifier, @StartMonth date;
            SELECT @Status = Status, @PolicyId = PolicyId, @StartMonth = StartMonth
            FROM dbo.Reservation WITH (UPDLOCK, HOLDLOCK)
            WHERE ReservationId = @ReservationId;

            DECLARE @VisitId uniqueidentifier;
            IF @Status = ''CONFIRMED''
            BEGIN
                SELECT @VisitId = VisitId
                FROM dbo.Visit WITH (UPDLOCK, HOLDLOCK)
                WHERE EntityId = @ReservationId AND VisitType = ''RESERVATION'';
                IF @VisitId IS NULL
                    THROW 51110, ''RESERVATION_INVALID_STATUS'', 1;
                -- Retry returns the saved date/Visit, not a new schedule.
                COMMIT TRANSACTION;
                SELECT @VisitId AS VisitId, @Status AS Status;
                RETURN;
            END;
            IF @Status IS NULL OR @Status <> ''PENDING_DEPOSIT''
                THROW 51107, ''RESERVATION_INVALID_STATUS'', 1;

            IF NOT EXISTS (
                SELECT 1 FROM dbo.Invoice WITH (HOLDLOCK)
                WHERE EntityId = @ReservationId AND InvoiceType = ''DEPOSIT'' AND Status = ''PAID''
            )
                THROW 51108, ''DEPOSIT_NOT_PAID'', 1;

            DECLARE @StartDay int, @EndDay int, @Days int = DAY(EOMONTH(@StartMonth));
            SELECT @StartDay = ReservationVisitStartDay, @EndDay = ReservationVisitEndDay
            FROM dbo.Policy WITH (HOLDLOCK) WHERE PolicyId = @PolicyId;
            -- Clamp to the captured month length, matching existing scheduling.
            SET @StartDay = CASE WHEN @StartDay > @Days THEN @Days ELSE @StartDay END;
            SET @EndDay = CASE WHEN @EndDay > @Days THEN @Days ELSE @EndDay END;
            IF @ReservationVisitDate IS NULL OR @StartDay IS NULL OR @EndDay IS NULL
                OR @ReservationVisitDate < DATEFROMPARTS(YEAR(@StartMonth), MONTH(@StartMonth), @StartDay)
                OR @ReservationVisitDate > DATEFROMPARTS(YEAR(@StartMonth), MONTH(@StartMonth), @EndDay)
                THROW 51109, ''VISIT_DATE_OUT_OF_POLICY'', 1;

            IF EXISTS (
                SELECT 1 FROM dbo.Visit WITH (UPDLOCK, HOLDLOCK)
                WHERE EntityId = @ReservationId AND VisitType = ''RESERVATION''
            )
                THROW 51110, ''RESERVATION_INVALID_STATUS'', 1;
            SET @VisitId = NEWID();
            INSERT dbo.Visit (VisitId, EntityId, EmployeeId, VisitType, VisitDate, ActualReturnDate, Status)
            VALUES (@VisitId, @ReservationId, NULL, ''RESERVATION'', @ReservationVisitDate, NULL, ''SCHEDULED'');
            UPDATE dbo.Reservation SET Status = ''CONFIRMED'' WHERE ReservationId = @ReservationId;
            COMMIT TRANSACTION;
            SELECT @VisitId AS VisitId, CAST(''CONFIRMED'' AS varchar(50)) AS Status;
        END TRY
        BEGIN CATCH
            IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
            THROW;
        END CATCH;
    END;');

    EXEC(N'CREATE OR ALTER PROCEDURE dbo.usp_CheckInVisit
        @VisitId uniqueidentifier,
        @EmployeeId uniqueidentifier,
        @NowUtc datetime2(3) = NULL
    AS
    BEGIN
        SET NOCOUNT ON;
        SET XACT_ABORT ON;
        BEGIN TRY
            -- Read only the routing key before the transaction. Revalidate it
            -- after locking the parent first, preventing Visit->Reservation
            -- inversion against cancellation/confirm/handover transactions.
            DECLARE @EntityId uniqueidentifier, @Type varchar(50);
            SELECT @EntityId = EntityId, @Type = VisitType FROM dbo.Visit WHERE VisitId = @VisitId;
            IF @EntityId IS NULL OR @Type NOT IN (''RESERVATION'', ''ACCESS'', ''RETURN'')
                THROW 51301, ''VISIT_ENTITY_MISMATCH'', 1;

            BEGIN TRANSACTION;
            DECLARE @FacilityId uniqueidentifier, @EntityStatus varchar(50);
            IF @Type = ''RESERVATION''
                SELECT @FacilityId = FacilityId, @EntityStatus = Status
                FROM dbo.Reservation WITH (UPDLOCK, HOLDLOCK) WHERE ReservationId = @EntityId;
            ELSE
                SELECT @FacilityId = FacilityId, @EntityStatus = Status
                FROM dbo.Contract WITH (UPDLOCK, HOLDLOCK) WHERE ContractId = @EntityId;
            IF @FacilityId IS NULL
                THROW 51301, ''VISIT_ENTITY_MISMATCH'', 1;

            DECLARE @LockedEntityId uniqueidentifier, @LockedType varchar(50),
                @VisitStatus varchar(50), @VisitDate date;
            SELECT @LockedEntityId = EntityId, @LockedType = VisitType,
                @VisitStatus = Status, @VisitDate = VisitDate
            FROM dbo.Visit WITH (UPDLOCK, HOLDLOCK) WHERE VisitId = @VisitId;
            IF @LockedEntityId IS NULL OR @LockedEntityId <> @EntityId OR @LockedType <> @Type
                THROW 51301, ''VISIT_ENTITY_MISMATCH'', 1;

            IF NOT EXISTS (
                SELECT 1 FROM dbo.Employee e WITH (HOLDLOCK)
                JOIN dbo.UserAccount a WITH (HOLDLOCK) ON a.UserAccountId = e.UserAccountId
                JOIN dbo.UserRole r WITH (HOLDLOCK) ON r.RoleId = a.RoleId
                WHERE e.EmployeeId = @EmployeeId AND e.FacilityId = @FacilityId
                    AND a.Status = ''ACTIVE'' AND r.RoleName = ''FACILITY_STAFF''
            )
                THROW 51302, ''FORBIDDEN'', 1;
            IF @VisitStatus <> ''SCHEDULED''
                OR (@Type = ''RESERVATION'' AND @EntityStatus <> ''CONFIRMED'')
                OR (@Type IN (''ACCESS'', ''RETURN'') AND @EntityStatus <> ''ACTIVE'')
                THROW 51303, ''VISIT_INVALID_STATUS'', 1;
            -- BR-VIS-05 / REV-2026-10-11-VISIT-CHECKIN-DATE. SQL Server''s
            -- clock supplies the current date; a client cannot spoof it.
            IF @VisitDate <> CONVERT(date, DATEADD(hour, 7, SYSUTCDATETIME()))
                THROW 51303, ''VISIT_INVALID_STATUS'', 1;

            UPDATE dbo.Visit SET Status = ''CHECKED_IN'', EmployeeId = @EmployeeId
            WHERE VisitId = @VisitId;
            -- ActualReturnDate belongs to actual return, not reception.
            COMMIT TRANSACTION;
        END TRY
        BEGIN CATCH
            IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
            THROW;
        END CATCH;
    END;');

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261011150000_Cwp04ReservationProcedures', N'10.0.12');

    COMMIT;
    SELECT 'CWP-04 procedure upgrade applied' AS Result;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

