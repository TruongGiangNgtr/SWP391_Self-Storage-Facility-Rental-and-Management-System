/*
FRMS Reservation / Visit / Handover source-alignment patch -- 2026-10-11
ONLY four procedure definitions. Does not execute a business operation.
Sources:
  20261010120000_ReservationCancellation.cs
  20261011090000_ReservationVisitProcedures.cs
  update/update_usp_CompleteHandover.sql (USE frms removed)
The bodies are reused, not new financial logic. Previous tests: CWP-04 SQL /
concurrency on EF-created SQL Server schema, not the old V2 trigger deployment.

DEFAULT IS READ ONLY: set BOTH values below after team review and DB backup.
Use this only for a reviewed, manually managed SQL database.
For an EF-managed database, prefer the existing migrations. Do NOT fabricate
__EFMigrationsHistory. These CREATE OR ALTER definitions would otherwise conflict
with the CREATE-only new EF migrations until adoption is explicitly reviewed.
Do not rerun the full old database script afterwards: its later definitions
would overwrite the corrected procedures and restore online first-month logic.
Rollback: restore the team's backed-up procedure definitions (DDL only).
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @ApplyPatch bit = 0;
DECLARE @ExpectedDatabase sysname = NULL; -- team supplies the exact DB name
IF @ApplyPatch = 0
BEGIN
    SELECT DB_NAME() AS selectedDatabase, 'DRY RUN - no definitions changed' AS result;
    RETURN;
END;
IF @ExpectedDatabase IS NULL OR @ExpectedDatabase <> DB_NAME()
    THROW 51980, 'Explicit database selection/confirmation is required.', 1;
IF DB_NAME() IN ('master','model','msdb','tempdb')
    THROW 51980, 'System databases are not patch targets.', 1;
IF OBJECT_ID('dbo.UserRole','U') IS NULL THROW 51981, 'Required table dbo.UserRole is missing. Do not create/adopt a database with this patch.', 1;
IF OBJECT_ID('dbo.UserAccount','U') IS NULL THROW 51981, 'Required table dbo.UserAccount is missing. Do not create/adopt a database with this patch.', 1;
IF OBJECT_ID('dbo.Customer','U') IS NULL THROW 51981, 'Required table dbo.Customer is missing. Do not create/adopt a database with this patch.', 1;
IF OBJECT_ID('dbo.Employee','U') IS NULL THROW 51981, 'Required table dbo.Employee is missing. Do not create/adopt a database with this patch.', 1;
IF OBJECT_ID('dbo.Facility','U') IS NULL THROW 51981, 'Required table dbo.Facility is missing. Do not create/adopt a database with this patch.', 1;
IF OBJECT_ID('dbo.Policy','U') IS NULL THROW 51981, 'Required table dbo.Policy is missing. Do not create/adopt a database with this patch.', 1;
IF OBJECT_ID('dbo.Reservation','U') IS NULL THROW 51981, 'Required table dbo.Reservation is missing. Do not create/adopt a database with this patch.', 1;
IF OBJECT_ID('dbo.Visit','U') IS NULL THROW 51981, 'Required table dbo.Visit is missing. Do not create/adopt a database with this patch.', 1;
IF OBJECT_ID('dbo.StorageUnit','U') IS NULL THROW 51981, 'Required table dbo.StorageUnit is missing. Do not create/adopt a database with this patch.', 1;
IF OBJECT_ID('dbo.UnitType','U') IS NULL THROW 51981, 'Required table dbo.UnitType is missing. Do not create/adopt a database with this patch.', 1;
IF OBJECT_ID('dbo.Contract','U') IS NULL THROW 51981, 'Required table dbo.Contract is missing. Do not create/adopt a database with this patch.', 1;
IF OBJECT_ID('dbo.Discount','U') IS NULL THROW 51981, 'Required table dbo.Discount is missing. Do not create/adopt a database with this patch.', 1;

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
    @ReservationVisitDate date
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
    @EmployeeId uniqueidentifier
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

    EXEC(N'CREATE OR ALTER PROCEDURE dbo.usp_CompleteHandover
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
            THROW 51101, ''RESOURCE_NOT_FOUND'', 1;

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
                ''ALREADY_COMPLETED'' AS Result
            FROM dbo.Contract c
            WHERE c.ContractId = @ExistingContractId;

            RETURN;
        END;

        IF @ReservationStatus <> ''CONFIRMED''
            THROW 51107, ''RESERVATION_INVALID_STATUS'', 1;

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
            THROW 51101, ''RESOURCE_NOT_FOUND'', 1;

        IF @VisitEntityId <> @ReservationId
           OR @VisitType <> ''RESERVATION''
           OR @VisitStatus <> ''CHECKED_IN''
            THROW 51112, ''VISIT_INVALID_STATUS'', 1;

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
              AND ua.Status = ''ACTIVE''
              AND ur.RoleName = ''FACILITY_STAFF''
        )
            THROW 51114, ''FORBIDDEN'', 1;

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
            THROW 51101, ''RESOURCE_NOT_FOUND'', 1;

        IF @UnitFacilityId <> @FacilityId
           OR @UnitUnitTypeId <> @UnitTypeId
            THROW 51117, ''UNIT_FACILITY_TYPE_MISMATCH'', 1;

        IF @UnitStatus <> ''AVAILABLE''
            THROW 51117, ''UNIT_NOT_AVAILABLE'', 1;

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
                THROW 51118, ''DISCOUNT_NOT_VALID'', 1;

            IF @DiscountCustomerId <> @CustomerId
                THROW 51120, ''DISCOUNT_NOT_OWNED_BY_CUSTOMER'', 1;

            IF @DiscountStatus <> ''ACTIVE''
               OR @DiscountEffectiveFrom > @NowUtc
               OR (
                    @DiscountEffectiveTo IS NOT NULL
                    AND @DiscountEffectiveTo < @NowUtc
                  )
                THROW 51118, ''DISCOUNT_NOT_VALID'', 1;
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
            ''ACTIVE''
        );

        -- V10: no first-month Invoice and no first-month Payment.

        UPDATE dbo.StorageUnit
        SET Status = ''IN_USE''
        WHERE StorageUnitId = @StorageUnitId
          AND Status = ''AVAILABLE'';

        IF @@ROWCOUNT <> 1
            THROW 51117, ''UNIT_NOT_AVAILABLE'', 1;

        UPDATE dbo.Visit
        SET
            Status = ''CHECKED_OUT'',
            EmployeeId = @EmployeeId
        WHERE VisitId = @VisitId
          AND Status = ''CHECKED_IN'';

        IF @@ROWCOUNT <> 1
            THROW 51112, ''VISIT_INVALID_STATUS'', 1;

        UPDATE dbo.Reservation
        SET Status = ''COMPLETED''
        WHERE ReservationId = @ReservationId
          AND Status = ''CONFIRMED'';

        IF @@ROWCOUNT <> 1
            THROW 51107, ''RESERVATION_INVALID_STATUS'', 1;

        COMMIT TRANSACTION;

        SELECT
            @ContractId AS ContractId,
            @StorageUnitId AS StorageUnitId,
            ''ACTIVE'' AS ContractStatus,
            ''COMPLETED'' AS Result;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END;');

    COMMIT TRANSACTION;
    SELECT 'Four procedure definitions aligned; no business rows were changed' AS result;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
