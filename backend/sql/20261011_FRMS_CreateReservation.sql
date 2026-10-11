/* RES-001 / BR-RES-01..05, SRS V10 §§12.6,17.4.
Only usp_CreateReservation; no table, payment-procedure or EF-history change.
Reuse V2 procedure with GMT+7/null-month/range-end fixes; no hard-coded legacy
index name; active CUSTOMER recheck; atomic non-secret CREATE_RESERVATION audit.
No physical unit assignment, discount, payment or provider call.
Default read-only. Set explicit target + Apply=1 only after reviewed deployment.
Future EF migration can adopt this exact definition, never an unknown one.
Rollback: drop ONLY this known procedure (or use EF Down). Existing historical
rows survive. Redeploy uses the same definition; cannot overwrite unknown SQL.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @Apply bit=0;
DECLARE @ExpectedDatabase sysname=NULL;
IF @Apply=0
BEGIN
    SELECT DB_NAME() AS databaseName,'DRY RUN / no changes' AS result;
    RETURN;
END;
IF @ExpectedDatabase IS NULL OR DB_NAME()<>@ExpectedDatabase
    OR DB_NAME() NOT IN (N'FRMS_EPS01_Dev',N'Frms_Test_CWP04')
    THROW 51962,'Only the explicitly confirmed EPS or approved test database is allowed.',1;
IF @@TRANCOUNT<>0 THROW 51963,'Deploy outside an existing transaction.',1;
BEGIN TRY
    BEGIN TRANSACTION;
IF EXISTS (SELECT 1 FROM sys.triggers t JOIN sys.sql_modules m ON m.object_id=t.object_id
           WHERE t.parent_id=OBJECT_ID(N'dbo.Reservation') AND t.is_disabled=0
             AND m.definition LIKE '%CREATE_RESERVATION%')
    THROW 51961, 'Legacy reservation audit trigger requires review; no duplicate audit owner was installed.', 1;
DECLARE @ExpectedDefinition nvarchar(max)=N'CREATE PROCEDURE dbo.usp_CreateReservation
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

    IF @StartMonth IS NULL OR @EndMonth IS NULL
       OR DAY(@StartMonth) <> 1 OR DAY(@EndMonth) <> 1 OR @EndMonth < @StartMonth
        THROW 51101, ''INVALID_MONTH_RANGE'', 1;

    DECLARE @BusinessNow DATETIME2(3) = DATEADD(HOUR, 7, @NowUtc);
    DECLARE @CurrentMonth DATE = DATEFROMPARTS(YEAR(@BusinessNow), MONTH(@BusinessNow), 1);
    IF @StartMonth <= @CurrentMonth
        THROW 51101, ''INVALID_MONTH_RANGE'', 1;

    -- HOLDLOCK supplies transaction-scoped serializable range protection without
    -- changing the caller''s session isolation level.

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @ActorId UNIQUEIDENTIFIER;
        SELECT @ActorId = ua.UserAccountId
        FROM dbo.Customer c WITH (HOLDLOCK)
        JOIN dbo.UserAccount ua WITH (HOLDLOCK) ON ua.UserAccountId = c.UserAccountId
        JOIN dbo.UserRole ur WITH (HOLDLOCK) ON ur.RoleId = ua.RoleId
        WHERE c.CustomerId = @CustomerId AND ua.Status = ''ACTIVE'' AND ur.RoleName = ''CUSTOMER'';
        IF @ActorId IS NULL
            THROW 51102, ''ACCOUNT_INACTIVE_OR_CUSTOMER_NOT_FOUND'', 1;

        IF NOT EXISTS (
            SELECT 1 FROM dbo.Facility WITH (UPDLOCK, HOLDLOCK)
            WHERE FacilityId = @FacilityId AND Status = ''ACTIVE''
        )
            THROW 51103, ''FACILITY_INACTIVE'', 1;

        DECLARE @RentalPrice DECIMAL(18,2);
        SELECT @RentalPrice = RentalPrice
        FROM dbo.UnitType WITH (UPDLOCK, HOLDLOCK)
        WHERE UnitTypeId = @UnitTypeId;

        IF @RentalPrice IS NULL
            THROW 51104, ''UNIT_TYPE_NOT_FOUND'', 1;

        DECLARE @PolicyId UNIQUEIDENTIFIER, @DepositTimeoutHours INT;
        SELECT TOP (1)
            @PolicyId = PolicyId,
            @DepositTimeoutHours = DepositTimeoutHours
        FROM dbo.Policy WITH (UPDLOCK, HOLDLOCK)
        WHERE Status = ''ACTIVE''
        ORDER BY Version DESC;

        IF @PolicyId IS NULL
            THROW 51105, ''ACTIVE_POLICY_NOT_FOUND'', 1;

        DECLARE @Month DATE = @StartMonth;
        WHILE @Month <= @EndMonth
        BEGIN
            DECLARE @Capacity BIGINT, @ReservationHolds BIGINT, @ContractOccupancy BIGINT;

            SELECT @Capacity = COUNT_BIG(*)
            FROM dbo.StorageUnit WITH (UPDLOCK, HOLDLOCK)
            WHERE FacilityId = @FacilityId
              AND UnitTypeId = @UnitTypeId
              AND Status IN (''AVAILABLE'',''IN_USE'',''INSPECTION'');

            SELECT @ReservationHolds = COUNT_BIG(*)
            FROM dbo.Reservation WITH (UPDLOCK, HOLDLOCK)
            WHERE FacilityId = @FacilityId
              AND UnitTypeId = @UnitTypeId
              AND Status IN (''PENDING_DEPOSIT'',''CONFIRMED'')
              AND StartMonth <= @Month
              AND EndMonth >= @Month;

            SELECT @ContractOccupancy = COUNT_BIG(*)
            FROM dbo.Contract c WITH (UPDLOCK, HOLDLOCK)
            JOIN dbo.StorageUnit su WITH (HOLDLOCK) ON su.StorageUnitId = c.StorageUnitId
            WHERE c.FacilityId = @FacilityId
              AND su.UnitTypeId = @UnitTypeId
              AND c.Status = ''ACTIVE''
              AND c.StartMonth <= @Month
              AND c.EndMonth >= @Month;

            IF ISNULL(@Capacity,0) <= ISNULL(@ReservationHolds,0) + ISNULL(@ContractOccupancy,0)
                THROW 51106, ''CAPACITY_NOT_AVAILABLE'', 1;

            IF @Month = @EndMonth BREAK;
            SET @Month = DATEADD(MONTH, 1, @Month);
        END;

        DECLARE @ReservationId UNIQUEIDENTIFIER = NEWID();
        INSERT INTO dbo.Reservation(
            ReservationId, CustomerId, FacilityId, UnitTypeId, PolicyId,
            StartMonth, EndMonth, LockedRentalPrice, DepositAmount, Status, CreatedAt
        )
        VALUES (
            @ReservationId, @CustomerId, @FacilityId, @UnitTypeId, @PolicyId,
            @StartMonth, @EndMonth, @RentalPrice, @RentalPrice, ''PENDING_DEPOSIT'', @NowUtc
        );

        DECLARE @DepositInvoiceId UNIQUEIDENTIFIER = NEWID();
        INSERT INTO dbo.Invoice(
            InvoiceId, EntityId, InvoiceType, BillingMonth, BaseAmount,
            DiscountId, DiscountAmount, AmountDue, DueDate, Status, CreatedAt
        )
        VALUES (
            @DepositInvoiceId, @ReservationId, ''DEPOSIT'', NULL, @RentalPrice,
            NULL, 0, @RentalPrice, DATEADD(HOUR, @DepositTimeoutHours, @NowUtc), ''UNPAID'', @NowUtc
        );

        -- EPS schema has no legacy audit trigger. One non-secret business audit
        -- is part of this same transaction, so a failed Invoice/audit rolls back all.
        DECLARE @AuditNewValue NVARCHAR(MAX) = (
            SELECT ''PENDING_DEPOSIT'' AS Status, @PolicyId AS PolicyId,
                   @RentalPrice AS LockedRentalPrice, @RentalPrice AS DepositAmount
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        INSERT dbo.AuditLog(AuditLogId,UserAccountId,Action,EntityType,EntityId,OldValue,NewValue,CreatedAt)
        VALUES(NEWID(),@ActorId,''CREATE_RESERVATION'',''RESERVATION'',@ReservationId,NULL,@AuditNewValue,@NowUtc);

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
END;';
SET @ExpectedDefinition=REPLACE(@ExpectedDefinition,CHAR(13),N'');
IF OBJECT_ID(N'dbo.usp_CreateReservation',N'P') IS NULL
    EXEC(@ExpectedDefinition);
ELSE IF REPLACE(OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_CreateReservation')),CHAR(13),N'') COLLATE Latin1_General_100_BIN2
        <> @ExpectedDefinition COLLATE Latin1_General_100_BIN2
    THROW 51960, 'Unknown existing usp_CreateReservation definition was not overwritten.', 1;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    THROW;
END CATCH;
