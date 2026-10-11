-- EPS-01 development fixture; owner-approved price/deposit: 2,000 VND (2026-10-10).
-- Apply the approved EF migrations first. Run only against an approved development/test DB.
-- This script is append-only/idempotent. It never creates Payment or marks an Invoice PAID.
-- Before first execution, provide your own BCrypt work-factor-12 password hash in the SAME session:
-- EXEC sys.sp_set_session_context @key=N'EPS01_FIXTURE_PASSWORD_HASH', @value=N'<your BCrypt hash>';
-- No password, hash, JWT or provider credentials are included in this shareable file.
-- Login phone: 0990002000 (synthetic fixture). Password: the one you hashed yourself.
-- StartMonth uses the current GMT+7 calendar month; persisted timestamps use UTC.

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() NOT IN (N'FRMS', N'FRMS_EPS01_Dev') AND DB_NAME() NOT LIKE N'Frms[_]Test[_]%'
    THROW 51100, 'Run this fixture only on the approved FRMS development/test database.', 1;
IF COL_LENGTH('dbo.Invoice', 'PaidAt') IS NULL
    OR COL_LENGTH('dbo.Payment', 'IdempotencyKey') IS NULL
    OR COL_LENGTH('dbo.Payment', 'ProviderOrderCode') IS NULL
    OR OBJECT_ID('dbo.ProviderOrderCodeSequence', 'SO') IS NULL
    THROW 51101, 'Apply approved EPS-01 EF migrations before creating the fixture.', 1;

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
DECLARE @StartMonth date = DATEFROMPARTS(YEAR(@LocalDate), MONTH(@LocalDate), 1);
DECLARE @EndMonth date = DATEADD(month, 1, @StartMonth);

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @LockResult int;
    EXEC @LockResult = sys.sp_getapplock @Resource=N'FRMS:EPS01:Fixture2000',
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
    SELECT @AccountId AS UserAccountId,@CustomerId AS CustomerId,@ReservationId AS ReservationId,
        InvoiceId,AmountDue,Status,PaidAt FROM dbo.Invoice WHERE InvoiceId=@InvoiceId;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    THROW;
END CATCH;
