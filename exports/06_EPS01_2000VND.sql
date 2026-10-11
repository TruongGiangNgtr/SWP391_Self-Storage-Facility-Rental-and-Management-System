-- FRMS V10.1: isolated 2,000 VND deposit-only UNPAID test fixture.
-- Historical/source code mentioned live testing; this package does NOT authorize real payOS calls.
-- Does NOT insert Payment or change Invoice to PAID. No real customer or provider data.
-- EPS-01 is a payment FEATURE identifier, not an additional database. Deposit fixture: 2,000 VND.
-- Apply the approved EF migrations first. Run only against database FRMS on your development instance.
-- This script is idempotent. It only updates the known demo account PasswordHash;
-- never creates Payment or marks an Invoice PAID.
-- Demo password = 11111111; hash is supplied through EPS01_FIXTURE_PASSWORD_HASH session context.
-- RUN_ALL initializes it. For standalone runs see README (requires same-session preflight).
-- Login phone: 0990002000 (synthetic fixture).
-- StartMonth uses NEXT GMT+7 calendar month (SRS BR-RES-01); persisted timestamps use UTC.

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @AccountId uniqueidentifier = 'e5010000-0000-0000-0000-000000000001',
        @CustomerId uniqueidentifier = 'e5010000-0000-0000-0000-000000000002',
        @FacilityId uniqueidentifier = 'e5010000-0000-0000-0000-000000000003',
        @UnitTypeId uniqueidentifier = 'e5010000-0000-0000-0000-000000000004',
        @StorageUnitId uniqueidentifier = 'e5010000-0000-0000-0000-000000000005',
        @ReservationId uniqueidentifier = 'e5010000-0000-0000-0000-000000000006',
        @InvoiceId uniqueidentifier = 'e5010000-0000-0000-0000-000000000007',
        @RoleId uniqueidentifier, @PolicyId uniqueidentifier, @DepositTimeoutHours int,
        @Price decimal(18,2) = 2000.00,
        @NowUtc datetime2(3) = SYSUTCDATETIME(),
        @LocalDate date = CONVERT(date, DATEADD(hour, 7, SYSUTCDATETIME())),
        @PasswordHash nvarchar(255) = CONVERT(nvarchar(255), SESSION_CONTEXT(N'EPS01_FIXTURE_PASSWORD_HASH'));
DECLARE @StartMonth date = DATEADD(month, 1, DATEFROMPARTS(YEAR(@LocalDate), MONTH(@LocalDate), 1));
DECLARE @EndMonth date = DATEADD(month, 1, @StartMonth);

BEGIN TRY
    IF DB_NAME() <> N'FRMS'
        THROW 51400, 'Seed blocked: select database FRMS (development copy) in SSMS.', 1;
    IF COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_ALLOW_DEV_SEED')),0)<>1
        THROW 51401, 'FRMS requires explicit FRMS_ALLOW_DEV_SEED=1 session authorization.', 1;
    IF COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_SEED_FAILED')),0)=1
        THROW 51402, 'An earlier seed module failed in this session; subsequent modules are blocked.', 1;
    IF COL_LENGTH('dbo.Invoice','PaidAt') IS NULL
       OR COL_LENGTH('dbo.Payment','IdempotencyKey') IS NULL
       OR COL_LENGTH('dbo.Payment','ProviderOrderCode') IS NULL
       OR COL_LENGTH('dbo.Payment','PaymentUrl') IS NULL
       OR COL_LENGTH('dbo.Payment','PaymentUrlExpiresAt') IS NULL
       OR OBJECT_ID('dbo.ProviderOrderCodeSequence','SO') IS NULL
       OR OBJECT_ID('dbo.usp_ApplyPaymentResult','P') IS NULL
        THROW 51403, 'EPS01/payOS EF migrations and payment stored procedure required before seed.', 1;
    IF COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_RUN_ALL_MODE')),0)=1 AND COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_SEED_STEP')),0)<>5
        THROW 51408, 'RUN_ALL sequence stopped: previous module did not complete successfully.', 1;
    BEGIN TRANSACTION;
    DECLARE @LockResult int;
    EXEC @LockResult = sys.sp_getapplock @Resource=N'FRMS:UnifiedSeedV10_1',
        @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
    IF @LockResult < 0 THROW 51102, 'Fixture lock could not be acquired.', 1;

    IF (SELECT COUNT(*) FROM dbo.UserRole WHERE RoleName='CUSTOMER') <> 1
        THROW 51103, 'Exactly one existing CUSTOMER role is required; do not fabricate role seed IDs.', 1;
    SELECT @RoleId=RoleId FROM dbo.UserRole WHERE RoleName='CUSTOMER';
    IF (SELECT COUNT(*) FROM dbo.Policy WHERE Status='ACTIVE') <> 1
        THROW 51104, 'Exactly one approved ACTIVE Policy is required.', 1;
    SELECT @PolicyId=PolicyId, @DepositTimeoutHours=DepositTimeoutHours FROM dbo.Policy WHERE Status='ACTIVE';
    IF @DepositTimeoutHours IS NULL OR @DepositTimeoutHours <= 0
        THROW 51105, 'The existing Policy must provide a positive DepositTimeoutHours.', 1;

    IF EXISTS (SELECT 1 FROM dbo.UserAccount WHERE (PhoneNumber='0990002000' OR Email='eps01.fixture@frms.invalid') AND UserAccountId<>@AccountId)
        THROW 51106, 'Fixture login collides with another account; no account was changed.', 1;
    IF EXISTS (SELECT 1 FROM dbo.UserAccount WHERE UserAccountId=@AccountId AND
        (RoleId<>@RoleId OR PhoneNumber<>'0990002000' OR Email<>'eps01.fixture@frms.invalid' OR Status<>'ACTIVE'))
        THROW 51107, 'Existing fixture account differs; inspect it without overwriting it.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.UserAccount WHERE UserAccountId=@AccountId)
    BEGIN
        IF @PasswordHash IS NULL OR LEN(@PasswordHash)<>60 OR
            LEFT(@PasswordHash,7) NOT IN ('$2a$12$', '$2b$12$', '$2y$12$')
            THROW 51108, 'Provide a BCrypt work-factor-12 hash through EPS01_FIXTURE_PASSWORD_HASH session context.', 1;
        INSERT dbo.UserAccount (UserAccountId,RoleId,Email,PhoneNumber,PasswordHash,Status,CreatedAt)
        VALUES (@AccountId,@RoleId,'eps01.fixture@frms.invalid','0990002000',@PasswordHash,'ACTIVE',@NowUtc);
    END;
    -- Reset only this known synthetic fixture account; existing identity already validated above.
    IF @PasswordHash IS NULL OR LEN(@PasswordHash)<>60 OR
        LEFT(@PasswordHash,7) NOT IN ('$2a$12$', '$2b$12$', '$2y$12$')
        THROW 51108, 'Provide BCrypt work-factor-12 hash through EPS01_FIXTURE_PASSWORD_HASH.', 1;
    UPDATE dbo.UserAccount
       SET PasswordHash=@PasswordHash
    WHERE UserAccountId=@AccountId
      AND RoleId=@RoleId
      AND Email='eps01.fixture@frms.invalid'
      AND PhoneNumber='0990002000'
      AND Status='ACTIVE'
      AND PasswordHash<>@PasswordHash;

    IF EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerId=@CustomerId AND UserAccountId<>@AccountId)
        THROW 51109, 'Customer fixture identity collision.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerId=@CustomerId)
        INSERT dbo.Customer (CustomerId,UserAccountId,FullName,Address)
        VALUES (@CustomerId,@AccountId,N'EPS-01 Test Customer',N'Development fixture only');

    IF EXISTS (SELECT 1 FROM dbo.Facility WHERE FacilityId=@FacilityId AND (Name<>N'EPS-01 Test Facility' OR Status<>'ACTIVE'))
        THROW 51110, 'Facility fixture differs; no historical field was overwritten.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Facility WHERE FacilityId=@FacilityId)
        INSERT dbo.Facility (FacilityId,Name,Address,Status)
        VALUES (@FacilityId,N'EPS-01 Test Facility',N'Development fixture only','ACTIVE');
    IF EXISTS (SELECT 1 FROM dbo.UnitType WHERE UnitTypeId=@UnitTypeId AND (RentalPrice<>@Price OR Mode<>'PUBLIC'))
        THROW 51111, 'UnitType fixture differs; captured prices were not changed.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.UnitType WHERE UnitTypeId=@UnitTypeId)
        INSERT dbo.UnitType (UnitTypeId,Name,Mode,Size,RentalPrice)
        VALUES (@UnitTypeId,N'EPS-01 2000 VND Test Unit','PUBLIC',N'Test only',@Price);
    IF EXISTS (SELECT 1 FROM dbo.StorageUnit WHERE StorageUnitId=@StorageUnitId AND (FacilityId<>@FacilityId OR UnitTypeId<>@UnitTypeId))
        THROW 51112, 'StorageUnit fixture identity collision.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.StorageUnit WHERE StorageUnitId=@StorageUnitId)
        INSERT dbo.StorageUnit (StorageUnitId,FacilityId,UnitTypeId,UnitCode,Status)
        VALUES (@StorageUnitId,@FacilityId,@UnitTypeId,'EPS01-2000-TEST','AVAILABLE');

    IF EXISTS (SELECT 1 FROM dbo.Reservation WHERE ReservationId=@ReservationId AND
        (CustomerId<>@CustomerId OR FacilityId<>@FacilityId OR UnitTypeId<>@UnitTypeId OR LockedRentalPrice<>@Price OR DepositAmount<>@Price))
        THROW 51113, 'Reservation fixture differs; no captured value was changed.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Reservation WHERE ReservationId=@ReservationId)
        INSERT dbo.Reservation (ReservationId,CustomerId,FacilityId,UnitTypeId,PolicyId,StartMonth,EndMonth,LockedRentalPrice,DepositAmount,Status,CreatedAt)
        VALUES (@ReservationId,@CustomerId,@FacilityId,@UnitTypeId,@PolicyId,@StartMonth,@EndMonth,@Price,@Price,'PENDING_DEPOSIT',@NowUtc);
    IF EXISTS (SELECT 1 FROM dbo.Invoice WHERE InvoiceId=@InvoiceId AND (EntityId<>@ReservationId OR InvoiceType<>'DEPOSIT' OR AmountDue<>@Price))
        THROW 51114, 'Invoice fixture differs; no issued Invoice was overwritten.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Invoice WHERE InvoiceId=@InvoiceId)
        INSERT dbo.Invoice (InvoiceId,EntityId,InvoiceType,BillingMonth,BaseAmount,DiscountId,DiscountAmount,AmountDue,DueDate,Status,PaidAt,CreatedAt)
        VALUES (@InvoiceId,@ReservationId,'DEPOSIT',NULL,@Price,NULL,0,@Price,DATEADD(hour,@DepositTimeoutHours,@NowUtc),'UNPAID',NULL,@NowUtc);

    COMMIT;
    IF COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_RUN_ALL_MODE')),0)=1
        EXEC sys.sp_set_session_context @key=N'FRMS_SEED_STEP', @value=6;
    SELECT @AccountId AS UserAccountId,@CustomerId AS CustomerId,@ReservationId AS ReservationId,
        InvoiceId,AmountDue,Status,PaidAt FROM dbo.Invoice WHERE InvoiceId=@InvoiceId;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    EXEC sys.sp_set_session_context @key=N'FRMS_SEED_FAILED', @value=1;
    THROW;
END CATCH;

