/*
 FRMS V10.1 Unified Development Seed -- RUN_ALL (SSMS STANDARD MODE)
 ==================================================================
 CONFIGURE THE TWO SETTINGS BELOW, THEN PRESS F5 ON THIS WHOLE FILE:
  - @ConfirmThisIsDevFrms = 1 (ONLY if FRMS is your DEVELOPMENT copy)
  - @FixtureBcryptHash = preconfigured BCrypt cost-12 hash for demo password 11111111
   DO NOT USE this shared demo password in production.
 Only target: FRMS database on your DEVELOPMENT instance; no other DB name accepted.

 Only database FRMS is accepted (must be a DEVELOPMENT copy).
 Nothing runs on FRMS without explicit confirmation in THIS script.
 Every module is guarded and runs in a separate transaction.
 If config/preflight fails, later modules are SKIPPED, not partially executed.
 No SQLCMD mode, :r, other tabs or connection-session preparation required.
 No DROP / DELETE / live payOS call.
 Only UPDATE: PasswordHash of 7 exact synthetic fixture UserAccounts (development only).
 */
DECLARE @ConfirmThisIsDevFrms bit = 1;  -- CHANGE TO 1 only after verifying DB_NAME()='FRMS' is your DEVELOPMENT database.
DECLARE @FixtureBcryptHash nvarchar(255) = N'$2y$12$8V9Q3T6rhU8m2rkRVbVibuEORO7M2Pk0TOGT1A9K/kxfgv459UMn2';  -- BCrypt cost 12 for DEMO password 11111111 only.
SET NOCOUNT ON;
EXEC sys.sp_set_session_context @key=N'FRMS_SEED_FAILED', @value=0;
EXEC sys.sp_set_session_context @key=N'FRMS_RUN_ALL_MODE', @value=1;
EXEC sys.sp_set_session_context @key=N'FRMS_SEED_STEP', @value=0;
-- Reset authorization to prevent inherited session state from bypassing this file's explicit confirmation.
EXEC sys.sp_set_session_context @key=N'FRMS_ALLOW_DEV_SEED', @value=0;

IF DB_NAME() <> N'FRMS'
BEGIN
    EXEC sys.sp_set_session_context @key=N'FRMS_SEED_FAILED', @value=1;
    THROW 51400, 'Seed blocked: select database FRMS (development copy) in SSMS.', 1;
END;
IF @ConfirmThisIsDevFrms<>1
BEGIN
    EXEC sys.sp_set_session_context @key=N'FRMS_SEED_FAILED', @value=1;
    THROW 51401, 'Seed blocked: confirm DEVELOPMENT FRMS by setting @ConfirmThisIsDevFrms = 1 at the top of 00_RUN_ALL.sql.', 1;
END;
IF @FixtureBcryptHash IS NULL OR LEN(@FixtureBcryptHash)<>60
   OR LEFT(@FixtureBcryptHash,7) NOT IN (N'$2a$12$',N'$2b$12$',N'$2y$12$')
BEGIN
    EXEC sys.sp_set_session_context @key=N'FRMS_SEED_FAILED', @value=1;
    THROW 51405, 'Seed blocked: set @FixtureBcryptHash to valid BCrypt cost-12 hash (60 chars) at the top of 00_RUN_ALL.sql.', 1;
END;
EXEC sys.sp_set_session_context @key=N'FRMS_ALLOW_DEV_SEED', @value=1;
EXEC sys.sp_set_session_context @key=N'EPS01_FIXTURE_PASSWORD_HASH', @value=@FixtureBcryptHash;
GO

-- ########## START 01_Master_Accounts_Units.sql ##########
-- FRMS V10.1 development fixture (NOT PRODUCTION): Master + UserRole + Policy + Facility + UnitType + Accounts + Discount + StorageUnit
-- Standalone: select database FRMS (development copy) in SSMS. See README for session authorization.
-- INSERT-only except explicit password reset of seven known demo accounts; per-file transaction.
-- Do not run old source seed files after running this bundle.
SET NOCOUNT ON;
SET XACT_ABORT ON;

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
    IF COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_RUN_ALL_MODE')),0)=1 AND COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_SEED_STEP')),0)<>0
        THROW 51408, 'RUN_ALL sequence stopped: previous module did not complete successfully.', 1;
    BEGIN TRANSACTION;
    DECLARE @BundleLock int;
    EXEC @BundleLock = sys.sp_getapplock @Resource=N'FRMS:UnifiedSeedV10_1',
         @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
    IF @BundleLock<0 THROW 51404, 'Could not acquire seed transaction lock.', 1;
    DECLARE @PasswordHash nvarchar(255) = CONVERT(nvarchar(255), SESSION_CONTEXT(N'EPS01_FIXTURE_PASSWORD_HASH'));
    IF @PasswordHash IS NULL OR LEN(@PasswordHash)<>60 OR LEFT(@PasswordHash,7) NOT IN ('$2a$12$', '$2b$12$', '$2y$12$')
        THROW 51405, 'Provide BCrypt cost-12 EPS01_FIXTURE_PASSWORD_HASH via session context.', 1;
/* =========================================================
       1) MASTER / BASELINE DATA
       ========================================================= */

    IF NOT EXISTS (SELECT 1 FROM dbo.UserRole WHERE RoleName = 'CUSTOMER')
        INSERT dbo.UserRole(RoleId, RoleName, Description)
        VALUES ('11111111-1111-1111-1111-111111111111','CUSTOMER',N'Storage customer');

    IF NOT EXISTS (SELECT 1 FROM dbo.UserRole WHERE RoleName = 'FACILITY_STAFF')
        INSERT dbo.UserRole(RoleId, RoleName, Description)
        VALUES ('22222222-2222-2222-2222-222222222222','FACILITY_STAFF',N'Facility staff');

    IF NOT EXISTS (SELECT 1 FROM dbo.UserRole WHERE RoleName = 'FACILITY_MANAGER')
        INSERT dbo.UserRole(RoleId, RoleName, Description)
        VALUES ('33333333-3333-3333-3333-333333333333','FACILITY_MANAGER',N'Facility manager');

    IF NOT EXISTS (SELECT 1 FROM dbo.UserRole WHERE RoleName = 'BUSINESS_OPERATIONS_MANAGER')
        INSERT dbo.UserRole(RoleId, RoleName, Description)
        VALUES ('44444444-4444-4444-4444-444444444444','BUSINESS_OPERATIONS_MANAGER',N'Business operations manager');

    IF NOT EXISTS (SELECT 1 FROM dbo.UserRole WHERE RoleName = 'SYSTEM_ADMINISTRATOR')
        INSERT dbo.UserRole(RoleId, RoleName, Description)
        VALUES ('55555555-5555-5555-5555-555555555555','SYSTEM_ADMINISTRATOR',N'System administrator');


    IF (SELECT COUNT(*) FROM dbo.Policy WHERE Status='ACTIVE')=0
    BEGIN
        DECLARE @SeedPolicyId uniqueidentifier = 'a1000000-0000-0000-0000-000000000001';
        IF EXISTS (SELECT 1 FROM dbo.Policy WHERE PolicyId=@SeedPolicyId)
            SET @SeedPolicyId=NEWID();
        DECLARE @SeedPolicyVersion int = ISNULL((SELECT MAX(Version) FROM dbo.Policy),0)+1;
        INSERT dbo.Policy(
            PolicyId,Version,Status,EffectiveFrom,EffectiveTo,
            DepositTimeoutHours,ReservationVisitStartDay,ReservationVisitEndDay,
            MonthlyPaymentDueDay,OverdueStartDay,LateFeeDivisorDays,
            EarlyReturnWaiveFeeUntilDay,CreatedAt
        ) VALUES(
            @SeedPolicyId,@SeedPolicyVersion,'ACTIVE','2026-01-01T00:00:00',NULL,
            1,1,5,5,6,31,5,'2026-01-01T00:00:00'
        );
    END;

    IF (SELECT COUNT(*) FROM dbo.UserRole WHERE RoleName IN ('CUSTOMER','FACILITY_STAFF','FACILITY_MANAGER','BUSINESS_OPERATIONS_MANAGER','SYSTEM_ADMINISTRATOR'))<>5
        THROW 51202, 'Expected five distinct business roles after baseline initialization.', 1;
    DECLARE @RoleCustomer uniqueidentifier = (SELECT RoleId FROM dbo.UserRole WHERE RoleName='CUSTOMER');
    DECLARE @RoleStaff uniqueidentifier = (SELECT RoleId FROM dbo.UserRole WHERE RoleName='FACILITY_STAFF');
    DECLARE @RoleManager uniqueidentifier = (SELECT RoleId FROM dbo.UserRole WHERE RoleName='FACILITY_MANAGER');
    DECLARE @RoleBom uniqueidentifier = (SELECT RoleId FROM dbo.UserRole WHERE RoleName='BUSINESS_OPERATIONS_MANAGER');
    DECLARE @RoleAdmin uniqueidentifier = (SELECT RoleId FROM dbo.UserRole WHERE RoleName='SYSTEM_ADMINISTRATOR');
    IF (SELECT COUNT(*) FROM dbo.Policy WHERE Status='ACTIVE')<>1
        THROW 51203, 'Expected exactly one ACTIVE Policy after baseline initialization.', 1;
    DECLARE @PolicyId uniqueidentifier = (SELECT PolicyId FROM dbo.Policy WHERE Status='ACTIVE');
    /* Facility */
    DECLARE @Facility1 UNIQUEIDENTIFIER = 'f1000000-0000-0000-0000-000000000001';
    DECLARE @Facility2 UNIQUEIDENTIFIER = 'f1000000-0000-0000-0000-000000000002';

    IF NOT EXISTS (SELECT 1 FROM dbo.Facility WHERE FacilityId=@Facility1)
        INSERT dbo.Facility(FacilityId,Name,Address,ContactInfo,Description,Status)
        VALUES(@Facility1,N'FRMS Demo Facility - District 1',N'123 Nguyễn Huệ, Quận 1, TP.HCM',N'028-7300-1001',N'Primary facility for frontend testing.','ACTIVE');

    IF NOT EXISTS (SELECT 1 FROM dbo.Facility WHERE FacilityId=@Facility2)
        INSERT dbo.Facility(FacilityId,Name,Address,ContactInfo,Description,Status)
        VALUES(@Facility2,N'FRMS Demo Facility - Thủ Đức',N'456 Võ Văn Ngân, TP. Thủ Đức, TP.HCM',N'028-7300-1002',N'Second facility; intentionally inactive for UI state testing.','INACTIVE');

    /* UnitType */
    DECLARE @UnitSmall UNIQUEIDENTIFIER = 'b1000000-0000-0000-0000-000000000001';
    DECLARE @UnitMedium UNIQUEIDENTIFIER = 'b1000000-0000-0000-0000-000000000002';
    DECLARE @UnitPublic UNIQUEIDENTIFIER = 'b1000000-0000-0000-0000-000000000003';

    IF NOT EXISTS (SELECT 1 FROM dbo.UnitType WHERE UnitTypeId=@UnitSmall)
        INSERT dbo.UnitType(UnitTypeId,Name,Mode,Size,RentalPrice,Description)
        VALUES(@UnitSmall,N'Small Private','PRIVATE',N'2 m²',500000,N'Boxes, documents, small household items.');

    IF NOT EXISTS (SELECT 1 FROM dbo.UnitType WHERE UnitTypeId=@UnitMedium)
        INSERT dbo.UnitType(UnitTypeId,Name,Mode,Size,RentalPrice,Description)
        VALUES(@UnitMedium,N'Medium Private','PRIVATE',N'5 m²',900000,N'Furniture and medium household storage.');

    IF NOT EXISTS (SELECT 1 FROM dbo.UnitType WHERE UnitTypeId=@UnitPublic)
        INSERT dbo.UnitType(UnitTypeId,Name,Mode,Size,RentalPrice,Description)
        VALUES(@UnitPublic,N'Shared Public','PUBLIC',N'3 m² equivalent',350000,N'Shared storage area handled by facility staff.');

    /* DEVELOPMENT catalogue amounts retained from the owner-provided seed; not production defaults. */
    IF NOT EXISTS (SELECT 1 FROM dbo.ExtraFeeType WHERE Name='KEY_REPLACEMENT')
        INSERT dbo.ExtraFeeType(ExtraFeeTypeId,Name,DefaultAmount,Status) VALUES('ef010000-0000-0000-0000-000000000001','KEY_REPLACEMENT',50000,'ACTIVE');
    IF NOT EXISTS (SELECT 1 FROM dbo.ExtraFeeType WHERE Name='ACCESS_CARD_REPLACEMENT')
        INSERT dbo.ExtraFeeType(ExtraFeeTypeId,Name,DefaultAmount,Status) VALUES('ef010000-0000-0000-0000-000000000002','ACCESS_CARD_REPLACEMENT',80000,'ACTIVE');
    IF NOT EXISTS (SELECT 1 FROM dbo.ExtraFeeType WHERE Name='LOCK_REPLACEMENT')
        INSERT dbo.ExtraFeeType(ExtraFeeTypeId,Name,DefaultAmount,Status) VALUES('ef010000-0000-0000-0000-000000000003','LOCK_REPLACEMENT',150000,'ACTIVE');
    IF NOT EXISTS (SELECT 1 FROM dbo.ExtraFeeType WHERE Name='CLEANING_FEE')
        INSERT dbo.ExtraFeeType(ExtraFeeTypeId,Name,DefaultAmount,Status) VALUES('ef010000-0000-0000-0000-000000000004','CLEANING_FEE',50000,'ACTIVE');
    IF NOT EXISTS (SELECT 1 FROM dbo.ExtraFeeType WHERE Name='OTHER')
        INSERT dbo.ExtraFeeType(ExtraFeeTypeId,Name,DefaultAmount,Status) VALUES('ef010000-0000-0000-0000-000000000005','OTHER',0,'ACTIVE');

    /* DamageType baseline */
    IF NOT EXISTS (SELECT 1 FROM dbo.DamageType WHERE Name=N'LOCK_DAMAGE')
        INSERT dbo.DamageType(DamageTypeId,Name,DefaultAmount,Status) VALUES('d0000000-0000-0000-0000-000000000001',N'LOCK_DAMAGE',NULL,'ACTIVE');
    IF NOT EXISTS (SELECT 1 FROM dbo.DamageType WHERE Name=N'DOOR_DAMAGE')
        INSERT dbo.DamageType(DamageTypeId,Name,DefaultAmount,Status) VALUES('d0000000-0000-0000-0000-000000000002',N'DOOR_DAMAGE',NULL,'ACTIVE');
    IF NOT EXISTS (SELECT 1 FROM dbo.DamageType WHERE Name=N'WALL_DAMAGE')
        INSERT dbo.DamageType(DamageTypeId,Name,DefaultAmount,Status) VALUES('d0000000-0000-0000-0000-000000000003',N'WALL_DAMAGE',NULL,'ACTIVE');
    IF NOT EXISTS (SELECT 1 FROM dbo.DamageType WHERE Name=N'FLOOR_DAMAGE')
        INSERT dbo.DamageType(DamageTypeId,Name,DefaultAmount,Status) VALUES('d0000000-0000-0000-0000-000000000004',N'FLOOR_DAMAGE',NULL,'ACTIVE');
    IF NOT EXISTS (SELECT 1 FROM dbo.DamageType WHERE Name=N'WATER_DAMAGE')
        INSERT dbo.DamageType(DamageTypeId,Name,DefaultAmount,Status) VALUES('d0000000-0000-0000-0000-000000000005',N'WATER_DAMAGE',NULL,'ACTIVE');
    IF NOT EXISTS (SELECT 1 FROM dbo.DamageType WHERE Name=N'OTHER')
        INSERT dbo.DamageType(DamageTypeId,Name,DefaultAmount,Status) VALUES('d0000000-0000-0000-0000-000000000006',N'OTHER',NULL,'ACTIVE');

/* =========================================================
       2) LOGIN ACCOUNTS + PROFILES
       Demo password = 11111111; BCrypt cost 12 supplied via session context.
       Only known fixture accounts (matching ID, role, email and phone) have their PasswordHash reset.
       ========================================================= */

    DECLARE @UaCustomer1 UNIQUEIDENTIFIER = '61000000-0000-0000-0000-000000000001';
    DECLARE @UaCustomer2 UNIQUEIDENTIFIER = '61000000-0000-0000-0000-000000000002';
    DECLARE @UaStaff UNIQUEIDENTIFIER     = '61000000-0000-0000-0000-000000000003';
    DECLARE @UaManager UNIQUEIDENTIFIER   = '61000000-0000-0000-0000-000000000004';
    DECLARE @UaBom UNIQUEIDENTIFIER       = '61000000-0000-0000-0000-000000000005';
    DECLARE @UaAdmin UNIQUEIDENTIFIER     = '61000000-0000-0000-0000-000000000006';

    IF NOT EXISTS (SELECT 1 FROM dbo.UserAccount WHERE UserAccountId=@UaCustomer1)
        INSERT dbo.UserAccount(UserAccountId,RoleId,Email,PhoneNumber,PasswordHash,Status,EmailVerifiedAt,CreatedAt)
        VALUES(@UaCustomer1,@RoleCustomer,'customer1.demo@frms.local','0901000001',@PasswordHash,'ACTIVE',NULL,'2026-06-01T02:00:00');

    IF NOT EXISTS (SELECT 1 FROM dbo.UserAccount WHERE UserAccountId=@UaCustomer2)
        INSERT dbo.UserAccount(UserAccountId,RoleId,Email,PhoneNumber,PasswordHash,Status,EmailVerifiedAt,CreatedAt)
        VALUES(@UaCustomer2,@RoleCustomer,'customer2.demo@frms.local','0901000002',@PasswordHash,'ACTIVE',NULL,'2026-06-02T02:00:00');

    IF NOT EXISTS (SELECT 1 FROM dbo.UserAccount WHERE UserAccountId=@UaStaff)
        INSERT dbo.UserAccount(UserAccountId,RoleId,Email,PhoneNumber,PasswordHash,Status,EmailVerifiedAt,CreatedAt)
        VALUES(@UaStaff,@RoleStaff,'staff.demo@frms.local','0902000001',@PasswordHash,'ACTIVE',NULL,'2026-06-01T02:00:00');

    IF NOT EXISTS (SELECT 1 FROM dbo.UserAccount WHERE UserAccountId=@UaManager)
        INSERT dbo.UserAccount(UserAccountId,RoleId,Email,PhoneNumber,PasswordHash,Status,EmailVerifiedAt,CreatedAt)
        VALUES(@UaManager,@RoleManager,'manager.demo@frms.local','0902000002',@PasswordHash,'ACTIVE',NULL,'2026-06-01T02:00:00');

    IF NOT EXISTS (SELECT 1 FROM dbo.UserAccount WHERE UserAccountId=@UaBom)
        INSERT dbo.UserAccount(UserAccountId,RoleId,Email,PhoneNumber,PasswordHash,Status,EmailVerifiedAt,CreatedAt)
        VALUES(@UaBom,@RoleBom,'bom.demo@frms.local','0902000003',@PasswordHash,'ACTIVE',NULL,'2026-06-01T02:00:00');

    IF NOT EXISTS (SELECT 1 FROM dbo.UserAccount WHERE UserAccountId=@UaAdmin)
        INSERT dbo.UserAccount(UserAccountId,RoleId,Email,PhoneNumber,PasswordHash,Status,EmailVerifiedAt,CreatedAt)
        VALUES(@UaAdmin,@RoleAdmin,'admin.demo@frms.local','0902000004',@PasswordHash,'ACTIVE',NULL,'2026-06-01T02:00:00');

    /* Explicit development-only password setup for the six canonical demo accounts.
       Validates fixture identity first; never changes an unrelated account or business data.
       Re-running the seed resets these six fixture passwords to the configured BCrypt hash. */
    IF EXISTS
    (
        SELECT 1
        FROM (VALUES
            (@UaCustomer1, @RoleCustomer, 'customer1.demo@frms.local', '0901000001'),
            (@UaCustomer2, @RoleCustomer, 'customer2.demo@frms.local', '0901000002'),
            (@UaStaff, @RoleStaff, 'staff.demo@frms.local', '0902000001'),
            (@UaManager, @RoleManager, 'manager.demo@frms.local', '0902000002'),
            (@UaBom, @RoleBom, 'bom.demo@frms.local', '0902000003'),
            (@UaAdmin, @RoleAdmin, 'admin.demo@frms.local', '0902000004')
        ) expected(UserAccountId, RoleId, Email, PhoneNumber)
        INNER JOIN dbo.UserAccount AS ua ON ua.UserAccountId=expected.UserAccountId
        WHERE ua.RoleId<>expected.RoleId
           OR ua.Email<>expected.Email
           OR ua.PhoneNumber<>expected.PhoneNumber
    )
        THROW 51413, 'Existing demo UserAccount ID has unexpected identity; password reset refused.', 1;

    UPDATE ua
       SET PasswordHash=@PasswordHash
    FROM dbo.UserAccount AS ua
    INNER JOIN (VALUES
        (@UaCustomer1, @RoleCustomer, 'customer1.demo@frms.local', '0901000001'),
        (@UaCustomer2, @RoleCustomer, 'customer2.demo@frms.local', '0901000002'),
        (@UaStaff, @RoleStaff, 'staff.demo@frms.local', '0902000001'),
        (@UaManager, @RoleManager, 'manager.demo@frms.local', '0902000002'),
        (@UaBom, @RoleBom, 'bom.demo@frms.local', '0902000003'),
        (@UaAdmin, @RoleAdmin, 'admin.demo@frms.local', '0902000004')
    ) expected(UserAccountId, RoleId, Email, PhoneNumber)
       ON ua.UserAccountId=expected.UserAccountId
      AND ua.RoleId=expected.RoleId
      AND ua.Email=expected.Email
      AND ua.PhoneNumber=expected.PhoneNumber
    WHERE ua.PasswordHash<>@PasswordHash;

    DECLARE @Customer1 UNIQUEIDENTIFIER = 'c1000000-0000-0000-0000-000000000001';
    DECLARE @Customer2 UNIQUEIDENTIFIER = 'c1000000-0000-0000-0000-000000000002';
    DECLARE @EmployeeStaff UNIQUEIDENTIFIER = 'e1000000-0000-0000-0000-000000000001';
    DECLARE @EmployeeManager UNIQUEIDENTIFIER = 'e1000000-0000-0000-0000-000000000002';
    DECLARE @EmployeeBom UNIQUEIDENTIFIER = 'e1000000-0000-0000-0000-000000000003';
    DECLARE @EmployeeAdmin UNIQUEIDENTIFIER = 'e1000000-0000-0000-0000-000000000004';

    IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerId=@Customer1)
        INSERT dbo.Customer(CustomerId,UserAccountId,FullName,Address,CCCD)
        VALUES(@Customer1,@UaCustomer1,N'Nguyễn Minh An',N'Quận 3, TP.HCM','079200000001');

    IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerId=@Customer2)
        INSERT dbo.Customer(CustomerId,UserAccountId,FullName,Address,CCCD)
        VALUES(@Customer2,@UaCustomer2,N'Trần Gia Hân',N'Bình Thạnh, TP.HCM','079200000002');

    IF NOT EXISTS (SELECT 1 FROM dbo.Employee WHERE EmployeeId=@EmployeeStaff)
        INSERT dbo.Employee(EmployeeId,UserAccountId,FacilityId,FullName)
        VALUES(@EmployeeStaff,@UaStaff,@Facility1,N'Lê Hoàng Staff');

    IF NOT EXISTS (SELECT 1 FROM dbo.Employee WHERE EmployeeId=@EmployeeManager)
        INSERT dbo.Employee(EmployeeId,UserAccountId,FacilityId,FullName)
        VALUES(@EmployeeManager,@UaManager,@Facility1,N'Phạm Mai Manager');

    IF NOT EXISTS (SELECT 1 FROM dbo.Employee WHERE EmployeeId=@EmployeeBom)
        INSERT dbo.Employee(EmployeeId,UserAccountId,FacilityId,FullName)
        VALUES(@EmployeeBom,@UaBom,NULL,N'Vũ Anh Operations');

    IF NOT EXISTS (SELECT 1 FROM dbo.Employee WHERE EmployeeId=@EmployeeAdmin)
        INSERT dbo.Employee(EmployeeId,UserAccountId,FacilityId,FullName)
        VALUES(@EmployeeAdmin,@UaAdmin,NULL,N'Đỗ Quang Admin');

/* =========================================================
       3) DISCOUNT + STORAGE UNITS
       ========================================================= */

    DECLARE @Discount1 UNIQUEIDENTIFIER = 'd1000000-0000-0000-0000-000000000001';
    IF NOT EXISTS (SELECT 1 FROM dbo.Discount WHERE DiscountId=@Discount1)
        INSERT dbo.Discount(DiscountId,CustomerId,Name,Percentage,Status,EffectiveFrom,EffectiveTo)
        VALUES(@Discount1,@Customer1,N'Loyal Customer 10%',10.00,'ACTIVE','2026-01-01T00:00:00','2027-12-31T23:59:59');

    DECLARE @Su1 UNIQUEIDENTIFIER = '71000000-0000-0000-0000-000000000001';
    DECLARE @Su2 UNIQUEIDENTIFIER = '71000000-0000-0000-0000-000000000002';
    DECLARE @Su3 UNIQUEIDENTIFIER = '71000000-0000-0000-0000-000000000003';
    DECLARE @Su4 UNIQUEIDENTIFIER = '71000000-0000-0000-0000-000000000004';
    DECLARE @Su5 UNIQUEIDENTIFIER = '71000000-0000-0000-0000-000000000005';
    DECLARE @Su6 UNIQUEIDENTIFIER = '71000000-0000-0000-0000-000000000006';
    DECLARE @Su7 UNIQUEIDENTIFIER = '71000000-0000-0000-0000-000000000007';

    IF NOT EXISTS (SELECT 1 FROM dbo.StorageUnit WHERE StorageUnitId=@Su1)
        INSERT dbo.StorageUnit(StorageUnitId,FacilityId,UnitTypeId,UnitCode,LocationInfo,Status)
        VALUES(@Su1,@Facility1,@UnitSmall,'A-101',N'Tầng 1 - Dãy A','IN_USE');
    IF NOT EXISTS (SELECT 1 FROM dbo.StorageUnit WHERE StorageUnitId=@Su2)
        INSERT dbo.StorageUnit(StorageUnitId,FacilityId,UnitTypeId,UnitCode,LocationInfo,Status)
        VALUES(@Su2,@Facility1,@UnitSmall,'A-102',N'Tầng 1 - Dãy A','AVAILABLE');
    IF NOT EXISTS (SELECT 1 FROM dbo.StorageUnit WHERE StorageUnitId=@Su3)
        INSERT dbo.StorageUnit(StorageUnitId,FacilityId,UnitTypeId,UnitCode,LocationInfo,Status)
        VALUES(@Su3,@Facility1,@UnitMedium,'B-201',N'Tầng 2 - Dãy B','AVAILABLE');
    IF NOT EXISTS (SELECT 1 FROM dbo.StorageUnit WHERE StorageUnitId=@Su4)
        INSERT dbo.StorageUnit(StorageUnitId,FacilityId,UnitTypeId,UnitCode,LocationInfo,Status)
        VALUES(@Su4,@Facility1,@UnitMedium,'B-202',N'Tầng 2 - Dãy B','INSPECTION');
    IF NOT EXISTS (SELECT 1 FROM dbo.StorageUnit WHERE StorageUnitId=@Su5)
        INSERT dbo.StorageUnit(StorageUnitId,FacilityId,UnitTypeId,UnitCode,LocationInfo,Status)
        VALUES(@Su5,@Facility1,@UnitPublic,'P-001',N'Khu Public - Zone P','AVAILABLE');
    IF NOT EXISTS (SELECT 1 FROM dbo.StorageUnit WHERE StorageUnitId=@Su6)
        INSERT dbo.StorageUnit(StorageUnitId,FacilityId,UnitTypeId,UnitCode,LocationInfo,Status)
        VALUES(@Su6,@Facility2,@UnitSmall,'A-101',N'Tầng 1 - Dãy A','AVAILABLE');
    IF NOT EXISTS (SELECT 1 FROM dbo.StorageUnit WHERE StorageUnitId=@Su7)
        INSERT dbo.StorageUnit(StorageUnitId,FacilityId,UnitTypeId,UnitCode,LocationInfo,Status)
        VALUES(@Su7,@Facility2,@UnitPublic,'P-001',N'Khu Public - Zone P','MAINTENANCE');
    COMMIT TRANSACTION;
    IF COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_RUN_ALL_MODE')),0)=1
        EXEC sys.sp_set_session_context @key=N'FRMS_SEED_STEP', @value=1;
    PRINT N'PASS: 01_Master_Accounts_Units.sql';
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    EXEC sys.sp_set_session_context @key=N'FRMS_SEED_FAILED', @value=1;
    THROW;
END CATCH;
-- ########## END 01_Master_Accounts_Units.sql ##########
GO

-- ########## START 02_Reservation_Contract_Visit.sql ##########
-- FRMS V10.1 development fixture (NOT PRODUCTION): Reservation + Contract + Extension + Visit
-- Standalone: select database FRMS (development copy) in SSMS. See README for session authorization.
-- All writes are INSERT-only, checked for existing primary keys; per-file transaction.
-- Do not run old source seed files after running this bundle.
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    IF COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_SEED_FAILED')),0)=1
    BEGIN
        PRINT N'SKIP: Seed configuration or earlier module failed; no write performed in this module.';
        RETURN;
    END;
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
    IF COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_RUN_ALL_MODE')),0)=1 AND COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_SEED_STEP')),0)<>1
        THROW 51408, 'RUN_ALL sequence stopped: previous module did not complete successfully.', 1;
    BEGIN TRANSACTION;
    DECLARE @BundleLock int;
    EXEC @BundleLock = sys.sp_getapplock @Resource=N'FRMS:UnifiedSeedV10_1',
         @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
    IF @BundleLock<0 THROW 51404, 'Could not acquire seed transaction lock.', 1;

    -- Fixture ID context of prerequisite modules (not a new row insertion).
    DECLARE @Customer1 UNIQUEIDENTIFIER = 'c1000000-0000-0000-0000-000000000001';
    DECLARE @Customer2 UNIQUEIDENTIFIER = 'c1000000-0000-0000-0000-000000000002';
    DECLARE @Discount1 UNIQUEIDENTIFIER = 'd1000000-0000-0000-0000-000000000001';
    DECLARE @EmployeeStaff UNIQUEIDENTIFIER = 'e1000000-0000-0000-0000-000000000001';
    DECLARE @Facility1 UNIQUEIDENTIFIER = 'f1000000-0000-0000-0000-000000000001';
    DECLARE @PolicyId uniqueidentifier = (SELECT PolicyId FROM dbo.Policy WHERE Status='ACTIVE');
    DECLARE @Su1 UNIQUEIDENTIFIER = '71000000-0000-0000-0000-000000000001';
    DECLARE @Su3 UNIQUEIDENTIFIER = '71000000-0000-0000-0000-000000000003';
    DECLARE @Su4 UNIQUEIDENTIFIER = '71000000-0000-0000-0000-000000000004';
    DECLARE @UnitMedium UNIQUEIDENTIFIER = 'b1000000-0000-0000-0000-000000000002';
    DECLARE @UnitPublic UNIQUEIDENTIFIER = 'b1000000-0000-0000-0000-000000000003';
    DECLARE @UnitSmall UNIQUEIDENTIFIER = 'b1000000-0000-0000-0000-000000000001';
/* =========================================================
       4) RESERVATIONS: cover all 4 UI statuses
       ========================================================= */

    DECLARE @R1 UNIQUEIDENTIFIER = '81000000-0000-0000-0000-000000000001'; -- COMPLETED -> active contract
    DECLARE @R2 UNIQUEIDENTIFIER = '81000000-0000-0000-0000-000000000002'; -- COMPLETED -> completed contract
    DECLARE @R3 UNIQUEIDENTIFIER = '81000000-0000-0000-0000-000000000003'; -- COMPLETED -> returning/inspection
    DECLARE @R4 UNIQUEIDENTIFIER = '81000000-0000-0000-0000-000000000004'; -- CONFIRMED future
    DECLARE @R5 UNIQUEIDENTIFIER = '81000000-0000-0000-0000-000000000005'; -- PENDING_DEPOSIT
    DECLARE @R6 UNIQUEIDENTIFIER = '81000000-0000-0000-0000-000000000006'; -- CANCELLED

    IF NOT EXISTS (SELECT 1 FROM dbo.Reservation WHERE ReservationId=@R1)
        INSERT dbo.Reservation(ReservationId,CustomerId,FacilityId,UnitTypeId,PolicyId,StartMonth,EndMonth,LockedRentalPrice,DepositAmount,Status,CreatedAt)
        VALUES(@R1,@Customer1,@Facility1,@UnitSmall,@PolicyId,'2026-09-01','2026-11-01',500000,500000,'COMPLETED','2026-08-10T03:00:00');

    IF NOT EXISTS (SELECT 1 FROM dbo.Reservation WHERE ReservationId=@R2)
        INSERT dbo.Reservation(ReservationId,CustomerId,FacilityId,UnitTypeId,PolicyId,StartMonth,EndMonth,LockedRentalPrice,DepositAmount,Status,CreatedAt)
        VALUES(@R2,@Customer2,@Facility1,@UnitMedium,@PolicyId,'2026-06-01','2026-08-01',900000,900000,'COMPLETED','2026-05-15T03:00:00');

    IF NOT EXISTS (SELECT 1 FROM dbo.Reservation WHERE ReservationId=@R3)
        INSERT dbo.Reservation(ReservationId,CustomerId,FacilityId,UnitTypeId,PolicyId,StartMonth,EndMonth,LockedRentalPrice,DepositAmount,Status,CreatedAt)
        VALUES(@R3,@Customer2,@Facility1,@UnitMedium,@PolicyId,'2026-08-01','2026-10-01',900000,900000,'COMPLETED','2026-07-15T03:00:00');

    IF NOT EXISTS (SELECT 1 FROM dbo.Reservation WHERE ReservationId=@R4)
        INSERT dbo.Reservation(ReservationId,CustomerId,FacilityId,UnitTypeId,PolicyId,StartMonth,EndMonth,LockedRentalPrice,DepositAmount,Status,CreatedAt)
        VALUES(@R4,@Customer1,@Facility1,@UnitPublic,@PolicyId,'2026-11-01','2026-12-01',350000,350000,'CONFIRMED','2026-10-01T03:00:00');

    IF NOT EXISTS (SELECT 1 FROM dbo.Reservation WHERE ReservationId=@R5)
        INSERT dbo.Reservation(ReservationId,CustomerId,FacilityId,UnitTypeId,PolicyId,StartMonth,EndMonth,LockedRentalPrice,DepositAmount,Status,CreatedAt)
        VALUES(@R5,@Customer2,@Facility1,@UnitSmall,@PolicyId,'2026-12-01','2027-01-01',500000,500000,'PENDING_DEPOSIT','2026-10-06T03:00:00');

    IF NOT EXISTS (SELECT 1 FROM dbo.Reservation WHERE ReservationId=@R6)
        INSERT dbo.Reservation(ReservationId,CustomerId,FacilityId,UnitTypeId,PolicyId,StartMonth,EndMonth,LockedRentalPrice,DepositAmount,Status,CreatedAt)
        VALUES(@R6,@Customer1,@Facility1,@UnitPublic,@PolicyId,'2026-11-01','2026-11-01',350000,350000,'CANCELLED','2026-09-20T03:00:00');

/* =========================================================
       5) CONTRACTS + EXTENSION
       ========================================================= */

    DECLARE @C1 UNIQUEIDENTIFIER = '91000000-0000-0000-0000-000000000001'; -- ACTIVE
    DECLARE @C2 UNIQUEIDENTIFIER = '91000000-0000-0000-0000-000000000002'; -- COMPLETED
    DECLARE @C3 UNIQUEIDENTIFIER = '91000000-0000-0000-0000-000000000003'; -- ACTIVE, return in progress

    IF NOT EXISTS (SELECT 1 FROM dbo.Contract WHERE ContractId=@C1)
        INSERT dbo.Contract(ContractId,ReservationId,CustomerId,FacilityId,StorageUnitId,PolicyId,DiscountId,StartMonth,EndMonth,Status)
        VALUES(@C1,@R1,@Customer1,@Facility1,@Su1,@PolicyId,@Discount1,'2026-09-01','2026-12-01','ACTIVE');

    IF NOT EXISTS (SELECT 1 FROM dbo.Contract WHERE ContractId=@C2)
        INSERT dbo.Contract(ContractId,ReservationId,CustomerId,FacilityId,StorageUnitId,PolicyId,DiscountId,StartMonth,EndMonth,Status)
        VALUES(@C2,@R2,@Customer2,@Facility1,@Su3,@PolicyId,NULL,'2026-06-01','2026-08-01','COMPLETED');

    IF NOT EXISTS (SELECT 1 FROM dbo.Contract WHERE ContractId=@C3)
        INSERT dbo.Contract(ContractId,ReservationId,CustomerId,FacilityId,StorageUnitId,PolicyId,DiscountId,StartMonth,EndMonth,Status)
        VALUES(@C3,@R3,@Customer2,@Facility1,@Su4,@PolicyId,NULL,'2026-08-01','2026-10-01','ACTIVE');

    DECLARE @Ext1 UNIQUEIDENTIFIER = 'a2000000-0000-0000-0000-000000000001';
    IF NOT EXISTS (SELECT 1 FROM dbo.ContractExtension WHERE ContractExtensionId=@Ext1)
        INSERT dbo.ContractExtension(ContractExtensionId,ContractId,OldEndMonth,NewEndMonth,AppliedMonthlyPrice,CreatedAt)
        VALUES(@Ext1,@C1,'2026-11-01','2026-12-01',500000,'2026-10-02T04:00:00');

/* =========================================================
       6) VISITS
       ========================================================= */

    DECLARE @VRes1 UNIQUEIDENTIFIER = '82000000-0000-0000-0000-000000000001';
    DECLARE @VRes2 UNIQUEIDENTIFIER = '82000000-0000-0000-0000-000000000002';
    DECLARE @VRes3 UNIQUEIDENTIFIER = '82000000-0000-0000-0000-000000000003';
    DECLARE @VRes4 UNIQUEIDENTIFIER = '82000000-0000-0000-0000-000000000004';
    DECLARE @VAccess1 UNIQUEIDENTIFIER = '82000000-0000-0000-0000-000000000005';
    DECLARE @VReturn2 UNIQUEIDENTIFIER = '82000000-0000-0000-0000-000000000006';
    DECLARE @VReturn3 UNIQUEIDENTIFIER = '82000000-0000-0000-0000-000000000007';

    IF NOT EXISTS (SELECT 1 FROM dbo.Visit WHERE VisitId=@VRes1)
        INSERT dbo.Visit(VisitId,EntityId,EmployeeId,VisitType,VisitDate,ActualReturnDate,Status)
        VALUES(@VRes1,@R1,@EmployeeStaff,'RESERVATION','2026-09-02',NULL,'CHECKED_OUT');

    IF NOT EXISTS (SELECT 1 FROM dbo.Visit WHERE VisitId=@VRes2)
        INSERT dbo.Visit(VisitId,EntityId,EmployeeId,VisitType,VisitDate,ActualReturnDate,Status)
        VALUES(@VRes2,@R2,@EmployeeStaff,'RESERVATION','2026-06-02',NULL,'CHECKED_OUT');

    IF NOT EXISTS (SELECT 1 FROM dbo.Visit WHERE VisitId=@VRes3)
        INSERT dbo.Visit(VisitId,EntityId,EmployeeId,VisitType,VisitDate,ActualReturnDate,Status)
        VALUES(@VRes3,@R3,@EmployeeStaff,'RESERVATION','2026-08-03',NULL,'CHECKED_OUT');

    IF NOT EXISTS (SELECT 1 FROM dbo.Visit WHERE VisitId=@VRes4)
        INSERT dbo.Visit(VisitId,EntityId,EmployeeId,VisitType,VisitDate,ActualReturnDate,Status)
        VALUES(@VRes4,@R4,NULL,'RESERVATION','2026-11-03',NULL,'SCHEDULED');

    IF NOT EXISTS (SELECT 1 FROM dbo.Visit WHERE VisitId=@VAccess1)
        INSERT dbo.Visit(VisitId,EntityId,EmployeeId,VisitType,VisitDate,ActualReturnDate,Status)
        VALUES(@VAccess1,@C1,@EmployeeStaff,'ACCESS','2026-10-04',NULL,'CHECKED_OUT');

    IF NOT EXISTS (SELECT 1 FROM dbo.Visit WHERE VisitId=@VReturn2)
        INSERT dbo.Visit(VisitId,EntityId,EmployeeId,VisitType,VisitDate,ActualReturnDate,Status)
        VALUES(@VReturn2,@C2,@EmployeeStaff,'RETURN','2026-08-31','2026-08-31T03:30:00','CHECKED_OUT');

    IF NOT EXISTS (SELECT 1 FROM dbo.Visit WHERE VisitId=@VReturn3)
        INSERT dbo.Visit(VisitId,EntityId,EmployeeId,VisitType,VisitDate,ActualReturnDate,Status)
        VALUES(@VReturn3,@C3,@EmployeeStaff,'RETURN','2026-10-07','2026-10-06T18:10:00','CHECKED_IN');
    COMMIT TRANSACTION;
    IF COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_RUN_ALL_MODE')),0)=1
        EXEC sys.sp_set_session_context @key=N'FRMS_SEED_STEP', @value=2;
    PRINT N'PASS: 02_Reservation_Contract_Visit.sql';
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    EXEC sys.sp_set_session_context @key=N'FRMS_SEED_FAILED', @value=1;
    THROW;
END CATCH;

-- ########## END 02_Reservation_Contract_Visit.sql ##########
GO

-- ########## START 03_Invoice_Payment_LateFee.sql ##########
-- FRMS V10.1 development fixture (NOT PRODUCTION): Deposit/Rental Invoice + mocked PAYOS Payment + LateFee
-- Standalone: select database FRMS (development copy) in SSMS. See README for session authorization.
-- All writes are INSERT-only, checked for existing primary keys; per-file transaction.
-- Do not run old source seed files after running this bundle.
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    IF COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_SEED_FAILED')),0)=1
    BEGIN
        PRINT N'SKIP: Seed configuration or earlier module failed; no write performed in this module.';
        RETURN;
    END;
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
    IF EXISTS (
        SELECT 1 FROM dbo.Payment
        WHERE PaymentId BETWEEN '84000000-0000-0000-0000-000000000001' AND '84000000-0000-0000-0000-000000000009'
          AND (PaymentMethod <> 'PAYOS' OR IdempotencyKey <> PaymentId OR ProviderOrderCode IS NULL)
    ) THROW 51406, 'Existing mock Payment fixture IDs are incompatible; no overwrite will occur.', 1;
    IF COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_RUN_ALL_MODE')),0)=1 AND COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_SEED_STEP')),0)<>2
        THROW 51408, 'RUN_ALL sequence stopped: previous module did not complete successfully.', 1;
    BEGIN TRANSACTION;
    DECLARE @BundleLock int;
    EXEC @BundleLock = sys.sp_getapplock @Resource=N'FRMS:UnifiedSeedV10_1',
         @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
    IF @BundleLock<0 THROW 51404, 'Could not acquire seed transaction lock.', 1;

    -- Fixture ID context of prerequisite modules (not a new row insertion).
    DECLARE @C1 UNIQUEIDENTIFIER = '91000000-0000-0000-0000-000000000001';
    DECLARE @Discount1 UNIQUEIDENTIFIER = 'd1000000-0000-0000-0000-000000000001';
    DECLARE @R1 UNIQUEIDENTIFIER = '81000000-0000-0000-0000-000000000001';
    DECLARE @R2 UNIQUEIDENTIFIER = '81000000-0000-0000-0000-000000000002';
    DECLARE @R3 UNIQUEIDENTIFIER = '81000000-0000-0000-0000-000000000003';
    DECLARE @R4 UNIQUEIDENTIFIER = '81000000-0000-0000-0000-000000000004';
    DECLARE @R5 UNIQUEIDENTIFIER = '81000000-0000-0000-0000-000000000005';
    DECLARE @R6 UNIQUEIDENTIFIER = '81000000-0000-0000-0000-000000000006';
/* =========================================================
       7) INVOICES + PAYMENTS + LATE FEE
       ========================================================= */

    DECLARE @InvDep1 UNIQUEIDENTIFIER = '83000000-0000-0000-0000-000000000001';
    DECLARE @InvDep2 UNIQUEIDENTIFIER = '83000000-0000-0000-0000-000000000002';
    DECLARE @InvDep3 UNIQUEIDENTIFIER = '83000000-0000-0000-0000-000000000003';
    DECLARE @InvDep4 UNIQUEIDENTIFIER = '83000000-0000-0000-0000-000000000004';
    DECLARE @InvDep5 UNIQUEIDENTIFIER = '83000000-0000-0000-0000-000000000005';
    DECLARE @InvDep6 UNIQUEIDENTIFIER = '83000000-0000-0000-0000-000000000006';
    DECLARE @InvC1Oct UNIQUEIDENTIFIER = '83000000-0000-0000-0000-000000000008';

    IF NOT EXISTS (SELECT 1 FROM dbo.Invoice WHERE InvoiceId=@InvDep1)
        INSERT dbo.Invoice(InvoiceId,EntityId,InvoiceType,BillingMonth,BaseAmount,DiscountId,DiscountAmount,AmountDue,DueDate,Status,PaidAt,CreatedAt)
        VALUES(@InvDep1,@R1,'DEPOSIT',NULL,500000,NULL,0,500000,'2026-08-10T04:00:00','PAID','2026-08-10T03:10:00','2026-08-10T03:00:00');

    IF NOT EXISTS (SELECT 1 FROM dbo.Invoice WHERE InvoiceId=@InvDep2)
        INSERT dbo.Invoice(InvoiceId,EntityId,InvoiceType,BillingMonth,BaseAmount,DiscountId,DiscountAmount,AmountDue,DueDate,Status,PaidAt,CreatedAt)
        VALUES(@InvDep2,@R2,'DEPOSIT',NULL,900000,NULL,0,900000,'2026-05-15T04:00:00','PAID','2026-05-15T03:10:00','2026-05-15T03:00:00');

    IF NOT EXISTS (SELECT 1 FROM dbo.Invoice WHERE InvoiceId=@InvDep3)
        INSERT dbo.Invoice(InvoiceId,EntityId,InvoiceType,BillingMonth,BaseAmount,DiscountId,DiscountAmount,AmountDue,DueDate,Status,PaidAt,CreatedAt)
        VALUES(@InvDep3,@R3,'DEPOSIT',NULL,900000,NULL,0,900000,'2026-07-15T04:00:00','PAID','2026-07-15T03:10:00','2026-07-15T03:00:00');

    IF NOT EXISTS (SELECT 1 FROM dbo.Invoice WHERE InvoiceId=@InvDep4)
        INSERT dbo.Invoice(InvoiceId,EntityId,InvoiceType,BillingMonth,BaseAmount,DiscountId,DiscountAmount,AmountDue,DueDate,Status,PaidAt,CreatedAt)
        VALUES(@InvDep4,@R4,'DEPOSIT',NULL,350000,NULL,0,350000,'2026-10-01T04:00:00','PAID','2026-10-01T03:10:00','2026-10-01T03:00:00');

    IF NOT EXISTS (SELECT 1 FROM dbo.Invoice WHERE InvoiceId=@InvDep5)
        INSERT dbo.Invoice(InvoiceId,EntityId,InvoiceType,BillingMonth,BaseAmount,DiscountId,DiscountAmount,AmountDue,DueDate,Status,PaidAt,CreatedAt)
        VALUES(@InvDep5,@R5,'DEPOSIT',NULL,500000,NULL,0,500000,'2026-10-06T04:00:00','UNPAID',NULL,'2026-10-06T03:00:00');

    IF NOT EXISTS (SELECT 1 FROM dbo.Invoice WHERE InvoiceId=@InvDep6)
        INSERT dbo.Invoice(InvoiceId,EntityId,InvoiceType,BillingMonth,BaseAmount,DiscountId,DiscountAmount,AmountDue,DueDate,Status,PaidAt,CreatedAt)
        VALUES(@InvDep6,@R6,'DEPOSIT',NULL,350000,NULL,0,350000,'2026-09-20T04:00:00','CANCELLED',NULL,'2026-09-20T03:00:00');

    /* First-month rent is offline: no Invoice or Payment is seeded. */

    /* Subsequent month: Contract discount applies. */
    IF NOT EXISTS (SELECT 1 FROM dbo.Invoice WHERE InvoiceId=@InvC1Oct)
        INSERT dbo.Invoice(InvoiceId,EntityId,InvoiceType,BillingMonth,BaseAmount,DiscountId,DiscountAmount,AmountDue,DueDate,Status,PaidAt,CreatedAt)
        VALUES(@InvC1Oct,@C1,'RENTAL_FEE','2026-10-01',500000,@Discount1,50000,450000,'2026-10-05T16:59:59','OVERDUE',NULL,'2026-10-01T00:10:00');



    DECLARE @PayDep1 UNIQUEIDENTIFIER = '84000000-0000-0000-0000-000000000001';
    DECLARE @PayDep2 UNIQUEIDENTIFIER = '84000000-0000-0000-0000-000000000002';
    DECLARE @PayDep3 UNIQUEIDENTIFIER = '84000000-0000-0000-0000-000000000003';
    DECLARE @PayDep4 UNIQUEIDENTIFIER = '84000000-0000-0000-0000-000000000004';
    DECLARE @PayDep5 UNIQUEIDENTIFIER = '84000000-0000-0000-0000-000000000005';
    DECLARE @PayC1Oct UNIQUEIDENTIFIER = '84000000-0000-0000-0000-000000000007';

    IF NOT EXISTS (SELECT 1 FROM dbo.Payment WHERE PaymentId=@PayDep1)
        INSERT dbo.Payment(PaymentId,InvoiceId,IdempotencyKey,ProviderOrderCode,PaymentUrl,PaymentUrlExpiresAt,Amount,PaymentMethod,TransactionCode,Status,PaidAt,CreatedAt)
        VALUES(@PayDep1,@InvDep1,@PayDep1,NEXT VALUE FOR dbo.ProviderOrderCodeSequence,NULL,NULL,500000,'PAYOS','MOCK-PAYOS-DEP-001','SUCCESS','2026-08-10T03:10:00','2026-08-10T03:05:00');
    IF NOT EXISTS (SELECT 1 FROM dbo.Payment WHERE PaymentId=@PayDep2)
        INSERT dbo.Payment(PaymentId,InvoiceId,IdempotencyKey,ProviderOrderCode,PaymentUrl,PaymentUrlExpiresAt,Amount,PaymentMethod,TransactionCode,Status,PaidAt,CreatedAt)
        VALUES(@PayDep2,@InvDep2,@PayDep2,NEXT VALUE FOR dbo.ProviderOrderCodeSequence,NULL,NULL,900000,'PAYOS','MOCK-PAYOS-DEP-002','SUCCESS','2026-05-15T03:10:00','2026-05-15T03:05:00');
    IF NOT EXISTS (SELECT 1 FROM dbo.Payment WHERE PaymentId=@PayDep3)
        INSERT dbo.Payment(PaymentId,InvoiceId,IdempotencyKey,ProviderOrderCode,PaymentUrl,PaymentUrlExpiresAt,Amount,PaymentMethod,TransactionCode,Status,PaidAt,CreatedAt)
        VALUES(@PayDep3,@InvDep3,@PayDep3,NEXT VALUE FOR dbo.ProviderOrderCodeSequence,NULL,NULL,900000,'PAYOS','MOCK-PAYOS-DEP-003','SUCCESS','2026-07-15T03:10:00','2026-07-15T03:05:00');
    IF NOT EXISTS (SELECT 1 FROM dbo.Payment WHERE PaymentId=@PayDep4)
        INSERT dbo.Payment(PaymentId,InvoiceId,IdempotencyKey,ProviderOrderCode,PaymentUrl,PaymentUrlExpiresAt,Amount,PaymentMethod,TransactionCode,Status,PaidAt,CreatedAt)
        VALUES(@PayDep4,@InvDep4,@PayDep4,NEXT VALUE FOR dbo.ProviderOrderCodeSequence,NULL,NULL,350000,'PAYOS','MOCK-PAYOS-DEP-004','SUCCESS','2026-10-01T03:10:00','2026-10-01T03:05:00');
    IF NOT EXISTS (SELECT 1 FROM dbo.Payment WHERE PaymentId=@PayDep5)
        INSERT dbo.Payment(PaymentId,InvoiceId,IdempotencyKey,ProviderOrderCode,PaymentUrl,PaymentUrlExpiresAt,Amount,PaymentMethod,TransactionCode,Status,PaidAt,CreatedAt)
        VALUES(@PayDep5,@InvDep5,@PayDep5,NEXT VALUE FOR dbo.ProviderOrderCodeSequence,NULL,NULL,500000,'PAYOS','MOCK-PAYOS-DEP-005-FAILED','FAILED',NULL,'2026-10-06T03:10:00');

    IF NOT EXISTS (SELECT 1 FROM dbo.Payment WHERE PaymentId=@PayC1Oct)
        INSERT dbo.Payment(PaymentId,InvoiceId,IdempotencyKey,ProviderOrderCode,PaymentUrl,PaymentUrlExpiresAt,Amount,PaymentMethod,TransactionCode,Status,PaidAt,CreatedAt)
        VALUES(@PayC1Oct,@InvC1Oct,@PayC1Oct,NEXT VALUE FOR dbo.ProviderOrderCodeSequence,NULL,NULL,450000,'PAYOS',NULL,'PENDING',NULL,'2026-10-05T12:00:00');

    DECLARE @LateFee1 UNIQUEIDENTIFIER = '85000000-0000-0000-0000-000000000001';
    IF NOT EXISTS (SELECT 1 FROM dbo.LateFee WHERE LateFeeId=@LateFee1)
        INSERT dbo.LateFee(LateFeeId,InvoiceId,OverdueDays,Amount,CalculatedAt)
        VALUES(@LateFee1,@InvC1Oct,2,29032.26,'2026-10-06T18:15:00');
    COMMIT TRANSACTION;
    IF COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_RUN_ALL_MODE')),0)=1
        EXEC sys.sp_set_session_context @key=N'FRMS_SEED_STEP', @value=3;
    PRINT N'PASS: 03_Invoice_Payment_LateFee.sql';
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    EXEC sys.sp_set_session_context @key=N'FRMS_SEED_FAILED', @value=1;
    THROW;
END CATCH;

-- ########## END 03_Invoice_Payment_LateFee.sql ##########
GO

-- ########## START 04_Support_Inspection_Audit.sql ##########
-- FRMS V10.1 development fixture (NOT PRODUCTION): Support + Inspection + Damage + Fee + Settlement + Logs
-- Standalone: select database FRMS (development copy) in SSMS. See README for session authorization.
-- All writes are INSERT-only, checked for existing primary keys; per-file transaction.
-- Do not run old source seed files after running this bundle.
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    IF COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_SEED_FAILED')),0)=1
    BEGIN
        PRINT N'SKIP: Seed configuration or earlier module failed; no write performed in this module.';
        RETURN;
    END;
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
    IF COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_RUN_ALL_MODE')),0)=1 AND COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_SEED_STEP')),0)<>3
        THROW 51408, 'RUN_ALL sequence stopped: previous module did not complete successfully.', 1;
    BEGIN TRANSACTION;
    DECLARE @BundleLock int;
    EXEC @BundleLock = sys.sp_getapplock @Resource=N'FRMS:UnifiedSeedV10_1',
         @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
    IF @BundleLock<0 THROW 51404, 'Could not acquire seed transaction lock.', 1;

    -- Fixture ID context of prerequisite modules (not a new row insertion).
    DECLARE @C1 UNIQUEIDENTIFIER = '91000000-0000-0000-0000-000000000001';
    DECLARE @C2 UNIQUEIDENTIFIER = '91000000-0000-0000-0000-000000000002';
    DECLARE @C3 UNIQUEIDENTIFIER = '91000000-0000-0000-0000-000000000003';
    DECLARE @Customer1 UNIQUEIDENTIFIER = 'c1000000-0000-0000-0000-000000000001';
    DECLARE @Customer2 UNIQUEIDENTIFIER = 'c1000000-0000-0000-0000-000000000002';
    DECLARE @EmployeeStaff UNIQUEIDENTIFIER = 'e1000000-0000-0000-0000-000000000001';
    DECLARE @Su3 UNIQUEIDENTIFIER = '71000000-0000-0000-0000-000000000003';
    DECLARE @Su4 UNIQUEIDENTIFIER = '71000000-0000-0000-0000-000000000004';
    DECLARE @UaAdmin UNIQUEIDENTIFIER     = '61000000-0000-0000-0000-000000000006';
    DECLARE @UaCustomer1 UNIQUEIDENTIFIER = '61000000-0000-0000-0000-000000000001';
    DECLARE @UaCustomer2 UNIQUEIDENTIFIER = '61000000-0000-0000-0000-000000000002';
    DECLARE @UaStaff UNIQUEIDENTIFIER     = '61000000-0000-0000-0000-000000000003';
    DECLARE @VReturn2 UNIQUEIDENTIFIER = '82000000-0000-0000-0000-000000000006';
    DECLARE @VReturn3 UNIQUEIDENTIFIER = '82000000-0000-0000-0000-000000000007';
/* =========================================================
       8) SUPPORT TICKETS
       Current EF schema accepts the SRS OTHER category.
       ========================================================= */

    DECLARE @Ticket1 UNIQUEIDENTIFIER = '86000000-0000-0000-0000-000000000001';
    DECLARE @Ticket2 UNIQUEIDENTIFIER = '86000000-0000-0000-0000-000000000002';
    DECLARE @Ticket3 UNIQUEIDENTIFIER = '86000000-0000-0000-0000-000000000003';

    IF NOT EXISTS (SELECT 1 FROM dbo.SupportTicket WHERE SupportTicketId=@Ticket1)
        INSERT dbo.SupportTicket(SupportTicketId,ContractId,CustomerId,AssignedEmployeeId,Category,Description,Status,ResultNote,CreatedAt,CompletedAt)
        VALUES(@Ticket1,@C1,@Customer1,NULL,'UNIT_ISSUE',N'Cửa kho phát tiếng kêu khi đóng.', 'OPEN',NULL,'2026-10-06T08:00:00',NULL);

    IF NOT EXISTS (SELECT 1 FROM dbo.SupportTicket WHERE SupportTicketId=@Ticket2)
        INSERT dbo.SupportTicket(SupportTicketId,ContractId,CustomerId,AssignedEmployeeId,Category,Description,Status,ResultNote,CreatedAt,CompletedAt)
        VALUES(@Ticket2,@C1,@Customer1,@EmployeeStaff,'ACCESS_CARD_CODE_ISSUE',N'Mã truy cập không hoạt động ổn định.', 'IN_PROGRESS',N'Staff đang kiểm tra quyền truy cập.','2026-10-05T08:00:00',NULL);

    IF NOT EXISTS (SELECT 1 FROM dbo.SupportTicket WHERE SupportTicketId=@Ticket3)
        INSERT dbo.SupportTicket(SupportTicketId,ContractId,CustomerId,AssignedEmployeeId,Category,Description,Status,ResultNote,CreatedAt,CompletedAt)
        VALUES(@Ticket3,@C2,@Customer2,@EmployeeStaff,'LOCK_KEY_ISSUE',N'Khóa khó xoay trước khi trả kho.', 'COMPLETED',N'Đã vệ sinh và kiểm tra khóa.','2026-08-25T08:00:00','2026-08-25T09:30:00');

/* =========================================================
       9) INSPECTION + DAMAGE + EVIDENCE + EXTRA FEE
       ========================================================= */

    DECLARE @Inspection2 UNIQUEIDENTIFIER = '87000000-0000-0000-0000-000000000001';
    DECLARE @Inspection3 UNIQUEIDENTIFIER = '87000000-0000-0000-0000-000000000002';

    IF NOT EXISTS (SELECT 1 FROM dbo.Inspection WHERE InspectionId=@Inspection2)
        INSERT dbo.Inspection(InspectionId,ContractId,StorageUnitId,VisitId,EmployeeId,Status,ConditionNote,CompletedAt)
        VALUES(@Inspection2,@C2,@Su3,@VReturn2,@EmployeeStaff,'COMPLETED',N'Kho sạch; có xước nhẹ ở cửa.','2026-08-31T04:20:00');

    IF NOT EXISTS (SELECT 1 FROM dbo.Inspection WHERE InspectionId=@Inspection3)
        INSERT dbo.Inspection(InspectionId,ContractId,StorageUnitId,VisitId,EmployeeId,Status,ConditionNote,CompletedAt)
        VALUES(@Inspection3,@C3,@Su4,@VReturn3,@EmployeeStaff,'IN_PROGRESS',N'Đang kiểm tra khóa và bề mặt cửa.',NULL);

    DECLARE @DoorDamage UNIQUEIDENTIFIER = (SELECT DamageTypeId FROM dbo.DamageType WHERE Name=N'DOOR_DAMAGE');
    DECLARE @LockDamage UNIQUEIDENTIFIER = (SELECT DamageTypeId FROM dbo.DamageType WHERE Name=N'LOCK_DAMAGE');

    DECLARE @Damage1 UNIQUEIDENTIFIER = '88000000-0000-0000-0000-000000000001';
    DECLARE @Damage2 UNIQUEIDENTIFIER = '88000000-0000-0000-0000-000000000002';

    IF NOT EXISTS (SELECT 1 FROM dbo.DamageRecord WHERE DamageRecordId=@Damage1)
        INSERT dbo.DamageRecord(DamageRecordId,InspectionId,DamageTypeId,DamageAmount,Note,Status,CreatedAt)
        VALUES(@Damage1,@Inspection2,@DoorDamage,120000,N'Vết xước dài ở mặt trong cửa.','APPROVED','2026-08-31T04:00:00');

    IF NOT EXISTS (SELECT 1 FROM dbo.DamageRecord WHERE DamageRecordId=@Damage2)
        INSERT dbo.DamageRecord(DamageRecordId,InspectionId,DamageTypeId,DamageAmount,Note,Status,CreatedAt)
        VALUES(@Damage2,@Inspection3,@LockDamage,80000,N'Khóa có dấu hiệu kẹt; chờ Manager quyết định.','PENDING','2026-10-06T18:13:00');

    DECLARE @Evidence1 UNIQUEIDENTIFIER = '89000000-0000-0000-0000-000000000001';
    DECLARE @Evidence2 UNIQUEIDENTIFIER = '89000000-0000-0000-0000-000000000002';

    IF NOT EXISTS (SELECT 1 FROM dbo.InspectionEvidence WHERE InspectionEvidenceId=@Evidence1)
        INSERT dbo.InspectionEvidence(InspectionEvidenceId,InspectionId,FileData,EvidenceType,CreatedAt)
        VALUES(@Evidence1,@Inspection2,0x89504E470D0A1A0A0000000D4948445200000001000000010804000000B51C0C020000000B4944415478DA6364F80F00010501012718E3660000000049454E44AE426082,'IMAGE','2026-08-31T04:05:00');

    IF NOT EXISTS (SELECT 1 FROM dbo.InspectionEvidence WHERE InspectionEvidenceId=@Evidence2)
        INSERT dbo.InspectionEvidence(InspectionEvidenceId,InspectionId,FileData,EvidenceType,CreatedAt)
        VALUES(@Evidence2,@Inspection3,0x89504E470D0A1A0A0000000D4948445200000001000000010804000000B51C0C020000000B4944415478DA6364F80F00010501012718E3660000000049454E44AE426082,'IMAGE','2026-10-06T18:14:00');

    DECLARE @CleaningFeeType UNIQUEIDENTIFIER = (SELECT ExtraFeeTypeId FROM dbo.ExtraFeeType WHERE Name='CLEANING_FEE');
    DECLARE @KeyFeeType UNIQUEIDENTIFIER = (SELECT ExtraFeeTypeId FROM dbo.ExtraFeeType WHERE Name='KEY_REPLACEMENT');
    DECLARE @Extra1 UNIQUEIDENTIFIER = '8a000000-0000-0000-0000-000000000001';
    DECLARE @Extra2 UNIQUEIDENTIFIER = '8a000000-0000-0000-0000-000000000002';

    IF NOT EXISTS (SELECT 1 FROM dbo.ExtraFee WHERE ExtraFeeId=@Extra1)
        INSERT dbo.ExtraFee(ExtraFeeId,InspectionId,ExtraFeeTypeId,Amount,Reason,CreatedAt)
        VALUES(@Extra1,@Inspection2,@CleaningFeeType,50000,N'Vệ sinh bổ sung sau khi trả kho.','2026-08-31T04:10:00');

    IF NOT EXISTS (SELECT 1 FROM dbo.ExtraFee WHERE ExtraFeeId=@Extra2)
        INSERT dbo.ExtraFee(ExtraFeeId,InspectionId,ExtraFeeTypeId,Amount,Reason,CreatedAt)
        VALUES(@Extra2,@Inspection3,@KeyFeeType,50000,N'Kiểm tra/thay chìa dự phòng nếu cần.','2026-10-06T18:15:00');

/* =========================================================
       10) DEPOSIT SETTLEMENT
       C2: deposit 900,000 - (120,000 damage + 50,000 cleaning) = 730,000 refund
       ========================================================= */

    DECLARE @Settlement1 UNIQUEIDENTIFIER = '8b000000-0000-0000-0000-000000000001';
    IF NOT EXISTS (SELECT 1 FROM dbo.DepositSettlement WHERE DepositSettlementId=@Settlement1)
        INSERT dbo.DepositSettlement(DepositSettlementId,ContractId,TotalDeduction,RefundAmount,AdditionalAmountDue,Status,CalculatedAt)
        VALUES(@Settlement1,@C2,170000,730000,0,'FINALIZED','2026-08-31T04:30:00');

/* =========================================================
       11) LOGIN HISTORY
       ========================================================= */

    IF NOT EXISTS (SELECT 1 FROM dbo.LoginHistory WHERE LoginHistoryId='8c000000-0000-0000-0000-000000000001')
        INSERT dbo.LoginHistory(LoginHistoryId,UserAccountId,LoginAt,IpAddress,DeviceInfo,Status)
        VALUES('8c000000-0000-0000-0000-000000000001',@UaAdmin,'2026-10-06T17:55:00','127.0.0.1',N'Chrome DEV - Admin','SUCCESS');

    IF NOT EXISTS (SELECT 1 FROM dbo.LoginHistory WHERE LoginHistoryId='8c000000-0000-0000-0000-000000000002')
        INSERT dbo.LoginHistory(LoginHistoryId,UserAccountId,LoginAt,IpAddress,DeviceInfo,Status)
        VALUES('8c000000-0000-0000-0000-000000000002',@UaStaff,'2026-10-06T18:00:00','127.0.0.1',N'Chrome DEV - Staff','SUCCESS');

    IF NOT EXISTS (SELECT 1 FROM dbo.LoginHistory WHERE LoginHistoryId='8c000000-0000-0000-0000-000000000003')
        INSERT dbo.LoginHistory(LoginHistoryId,UserAccountId,LoginAt,IpAddress,DeviceInfo,Status)
        VALUES('8c000000-0000-0000-0000-000000000003',@UaCustomer1,'2026-10-06T18:02:00','127.0.0.1',N'Chrome DEV - Customer','SUCCESS');

    IF NOT EXISTS (SELECT 1 FROM dbo.LoginHistory WHERE LoginHistoryId='8c000000-0000-0000-0000-000000000004')
        INSERT dbo.LoginHistory(LoginHistoryId,UserAccountId,LoginAt,IpAddress,DeviceInfo,Status)
        VALUES('8c000000-0000-0000-0000-000000000004',@UaCustomer1,'2026-10-06T17:50:00','127.0.0.1',N'Chrome DEV - Customer','FAILED');

/* =========================================================
       12) AUDIT LOG (development fixture provenance, not provider evidence)
       ========================================================= */

    IF NOT EXISTS (SELECT 1 FROM dbo.AuditLog WHERE AuditLogId='8d000000-0000-0000-0000-000000000001')
        INSERT dbo.AuditLog(AuditLogId,UserAccountId,Action,EntityType,EntityId,OldValue,NewValue,CreatedAt)
        VALUES(
            '8d000000-0000-0000-0000-000000000001',NULL,
            'DEV_SEED','SYSTEM',NULL,NULL,
            N'{"source":"FRMS_DevSeed_AllTables_EPS01_PayOS.sql","purpose":"frontend-test-data"}',
            '2026-10-06T18:16:00'
        );

/* =========================================================
       13) NOTIFICATIONS
       ========================================================= */

    IF NOT EXISTS (SELECT 1 FROM dbo.NotificationLog WHERE NotificationLogId='8e000000-0000-0000-0000-000000000001')
        INSERT dbo.NotificationLog(NotificationLogId,UserAccountId,NotificationType,Content,Status,SentAt,CreatedAt)
        VALUES('8e000000-0000-0000-0000-000000000001',@UaCustomer1,'PAYMENT_OVERDUE',N'Hóa đơn tháng 10 đang quá hạn.','PENDING',NULL,'2026-10-06T18:15:00');

    IF NOT EXISTS (SELECT 1 FROM dbo.NotificationLog WHERE NotificationLogId='8e000000-0000-0000-0000-000000000002')
        INSERT dbo.NotificationLog(NotificationLogId,UserAccountId,NotificationType,Content,Status,SentAt,CreatedAt)
        VALUES('8e000000-0000-0000-0000-000000000002',@UaCustomer2,'RETURN',N'Kết quả hoàn trả kho đã được ghi nhận.','SENT','2026-08-31T04:35:00','2026-08-31T04:31:00');
    COMMIT TRANSACTION;
    IF COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_RUN_ALL_MODE')),0)=1
        EXEC sys.sp_set_session_context @key=N'FRMS_SEED_STEP', @value=4;
    PRINT N'PASS: 04_Support_Inspection_Audit.sql';
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    EXEC sys.sp_set_session_context @key=N'FRMS_SEED_FAILED', @value=1;
    THROW;
END CATCH;

-- ########## END 04_Support_Inspection_Audit.sql ##########
GO

-- ########## START 05_Core_Demo_Reservation.sql ##########
-- FRMS V10.1 development fixture (NOT PRODUCTION): Core demo: 2 Reservation Deposit invoices + capacity + Visit scenario
-- Standalone: select database FRMS (development copy) in SSMS. See README for session authorization.
-- All writes are INSERT-only, checked for existing primary keys; per-file transaction.
-- Do not run old source seed files after running this bundle.
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    IF COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_SEED_FAILED')),0)=1
    BEGIN
        PRINT N'SKIP: Seed configuration or earlier module failed; no write performed in this module.';
        RETURN;
    END;
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
    IF COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_RUN_ALL_MODE')),0)=1 AND COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_SEED_STEP')),0)<>4
        THROW 51408, 'RUN_ALL sequence stopped: previous module did not complete successfully.', 1;
    BEGIN TRANSACTION;
    DECLARE @BundleLock int;
    EXEC @BundleLock = sys.sp_getapplock @Resource=N'FRMS:UnifiedSeedV10_1',
         @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
    IF @BundleLock<0 THROW 51404, 'Could not acquire seed transaction lock.', 1;

    -- Existing main-demo fixture IDs referenced from prior modules.
    DECLARE @C1 UNIQUEIDENTIFIER = '91000000-0000-0000-0000-000000000001';
    DECLARE @Customer1 UNIQUEIDENTIFIER = 'c1000000-0000-0000-0000-000000000001';
    DECLARE @Facility1 UNIQUEIDENTIFIER = 'f1000000-0000-0000-0000-000000000001';
    DECLARE @PolicyId uniqueidentifier = (SELECT PolicyId FROM dbo.Policy WHERE Status='ACTIVE');
    DECLARE @UnitSmall UNIQUEIDENTIFIER = 'b1000000-0000-0000-0000-000000000001';
    /* =========================================================
       CORE DEMO ADDITIONS - 2026-10-08, SRS V10
       Only: real capacity -> Reservation; confirm after deposit;
             ACTIVE Contract -> ACCESS; reschedule SCHEDULED Visit.

       Existing all-table/legacy fixtures above are intentionally retained.
       No existing snapshot, deadline, appointment, or historical row is reset.
       The PAID deposit below is an owner-approved INITIAL TEST FIXTURE,
       NOT evidence that MoMo/VNPay or a callback has run successfully.
       No first-month RENTAL_FEE Invoice/Payment is added by this section.
       Re-running does not undo actions performed through the application.
       ========================================================= */

    DECLARE @DemoNowUtc DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @DemoToday DATE = CONVERT(DATE, DATEADD(HOUR, 7, @DemoNowUtc));
    DECLARE @DemoCurrentMonth DATE = DATEFROMPARTS(YEAR(@DemoToday), MONTH(@DemoToday), 1);
    DECLARE @DemoStartMonth DATE = DATEADD(MONTH, 1, @DemoCurrentMonth);
    DECLARE @DemoEndMonth DATE = DATEADD(MONTH, 2, @DemoStartMonth);
    DECLARE @DemoRentalPrice DECIMAL(18,2);
    DECLARE @DemoDepositTimeoutHours INT;
    DECLARE @DemoVisitStartDay INT;
    DECLARE @DemoVisitEndDay INT;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.Facility WITH (UPDLOCK, HOLDLOCK)
        WHERE FacilityId=@Facility1 AND Status='ACTIVE'
    )
        THROW 51901, 'Core demo requires the existing demo Facility to be ACTIVE; this seed will not activate it.', 1;

    SELECT @DemoRentalPrice=RentalPrice
    FROM dbo.UnitType WITH (UPDLOCK, HOLDLOCK)
    WHERE UnitTypeId=@UnitSmall;

    SELECT @DemoDepositTimeoutHours=DepositTimeoutHours,
           @DemoVisitStartDay=ReservationVisitStartDay,
           @DemoVisitEndDay=ReservationVisitEndDay
    FROM dbo.Policy WHERE PolicyId=@PolicyId;

    IF @DemoRentalPrice IS NULL OR @DemoRentalPrice<=0
       OR @DemoDepositTimeoutHours IS NULL OR @DemoDepositTimeoutHours<=0
       OR @DemoVisitStartDay IS NULL OR @DemoVisitEndDay IS NULL
       OR @DemoVisitStartDay<1 OR @DemoVisitEndDay>31
       OR @DemoVisitStartDay>@DemoVisitEndDay
        THROW 51902, 'Core demo needs valid captured Policy values and a positive price for its deposit Payment fixture; no master values are changed.', 1;

    DECLARE @DemoUnpaidReservation UNIQUEIDENTIFIER = '81000000-0000-0000-0000-000000000101';
    DECLARE @DemoPaidReservation UNIQUEIDENTIFIER   = '81000000-0000-0000-0000-000000000102';
    DECLARE @DemoUnpaidInvoice UNIQUEIDENTIFIER     = '83000000-0000-0000-0000-000000000101';
    DECLARE @DemoPaidInvoice UNIQUEIDENTIFIER       = '83000000-0000-0000-0000-000000000102';
    DECLARE @DemoPaidPayment UNIQUEIDENTIFIER       = '84000000-0000-0000-0000-000000000102';
    DECLARE @DemoAccessVisit UNIQUEIDENTIFIER       = '82000000-0000-0000-0000-000000000101';

    DECLARE @DemoMonths TABLE (RentalMonth DATE PRIMARY KEY);
    INSERT @DemoMonths(RentalMonth)
    VALUES(@DemoStartMonth),(DATEADD(MONTH,1,@DemoStartMonth)),(@DemoEndMonth);

    /* Same capacity components as CAT-003/usp_CreateReservation.
       Existing reservations/contracts are counted, never deleted/cancelled.
       Add only enough physical units for missing fixtures + one new booking. */
    DECLARE @DemoPhysicalCapacity INT;
    DECLARE @DemoPeakLoad INT;
    DECLARE @DemoMissingHolds INT =
        CASE WHEN EXISTS (SELECT 1 FROM dbo.Reservation WHERE ReservationId=@DemoUnpaidReservation) THEN 0 ELSE 1 END
        + CASE WHEN EXISTS (SELECT 1 FROM dbo.Reservation WHERE ReservationId=@DemoPaidReservation) THEN 0 ELSE 1 END;

    SELECT @DemoPhysicalCapacity=COUNT(*) FROM dbo.StorageUnit
    WHERE FacilityId=@Facility1 AND UnitTypeId=@UnitSmall
      AND Status IN ('AVAILABLE','IN_USE','INSPECTION');

    SELECT @DemoPeakLoad=MAX(r.Held+c.Occupied)
    FROM @DemoMonths m
    CROSS APPLY (
        SELECT COUNT(*) AS Held FROM dbo.Reservation
        WHERE FacilityId=@Facility1 AND UnitTypeId=@UnitSmall
          AND Status IN ('PENDING_DEPOSIT','CONFIRMED')
          AND StartMonth<=m.RentalMonth AND EndMonth>=m.RentalMonth
    ) r
    CROSS APPLY (
        SELECT COUNT(*) AS Occupied FROM dbo.Contract ct
        JOIN dbo.StorageUnit su ON su.StorageUnitId=ct.StorageUnitId
        WHERE ct.FacilityId=@Facility1 AND su.UnitTypeId=@UnitSmall
          AND ct.Status='ACTIVE'
          AND ct.StartMonth<=m.RentalMonth AND ct.EndMonth>=m.RentalMonth
    ) c;

    DECLARE @DemoUnitsNeeded INT = @DemoPeakLoad+@DemoMissingHolds+1-@DemoPhysicalCapacity;
    DECLARE @DemoUnitsAdded INT = 0;
    DECLARE @DemoUnitOrdinal INT = 101;
    DECLARE @DemoStorageUnit UNIQUEIDENTIFIER;
    DECLARE @DemoUnitCode VARCHAR(50);

    /* Fixture safety guard, NOT a production capacity/business limit. */
    IF @DemoUnitsNeeded>10
        THROW 51903, 'Core demo would need more than ten added units. Review the current demo bookings instead of expanding this seed automatically.', 1;

    WHILE @DemoUnitsAdded<@DemoUnitsNeeded
    BEGIN
        IF @DemoUnitOrdinal>199
            THROW 51904, 'Reserved core-demo Unit identifiers are already occupied; existing Units will not be overwritten.', 1;

        SET @DemoStorageUnit=CONVERT(UNIQUEIDENTIFIER,
            '71000000-0000-0000-0000-'+RIGHT('000000000000'+CONVERT(VARCHAR(12),@DemoUnitOrdinal),12));
        SET @DemoUnitCode='A-DEMO-'+CONVERT(VARCHAR(12),@DemoUnitOrdinal);

        IF NOT EXISTS (SELECT 1 FROM dbo.StorageUnit WHERE StorageUnitId=@DemoStorageUnit)
           AND NOT EXISTS (SELECT 1 FROM dbo.StorageUnit WHERE FacilityId=@Facility1 AND UnitCode=@DemoUnitCode)
        BEGIN
            INSERT dbo.StorageUnit(StorageUnitId,FacilityId,UnitTypeId,UnitCode,LocationInfo,Status)
            VALUES(@DemoStorageUnit,@Facility1,@UnitSmall,@DemoUnitCode,N'Core demo - Small capacity','AVAILABLE');
            SET @DemoUnitsAdded=@DemoUnitsAdded+1;
        END;
        SET @DemoUnitOrdinal=@DemoUnitOrdinal+1;
    END;

    /* Unpaid starting point for the real payment implementation later.
       Never extend an existing Invoice deadline on a subsequent seed run. */
    IF NOT EXISTS (SELECT 1 FROM dbo.Reservation WHERE ReservationId=@DemoUnpaidReservation)
    BEGIN
        INSERT dbo.Reservation(ReservationId,CustomerId,FacilityId,UnitTypeId,PolicyId,StartMonth,EndMonth,LockedRentalPrice,DepositAmount,Status,CreatedAt)
        VALUES(@DemoUnpaidReservation,@Customer1,@Facility1,@UnitSmall,@PolicyId,
               @DemoStartMonth,@DemoEndMonth,@DemoRentalPrice,@DemoRentalPrice,'PENDING_DEPOSIT',@DemoNowUtc);

        INSERT dbo.Invoice(InvoiceId,EntityId,InvoiceType,BillingMonth,BaseAmount,DiscountId,DiscountAmount,AmountDue,DueDate,Status,CreatedAt)
        VALUES(@DemoUnpaidInvoice,@DemoUnpaidReservation,'DEPOSIT',NULL,@DemoRentalPrice,NULL,0,@DemoRentalPrice,
               DATEADD(HOUR,@DemoDepositTimeoutHours,@DemoNowUtc),'UNPAID',@DemoNowUtc);
    END;

    /* Approved payment-ready fixture: continue directly with RES-004.
       Keep Reservation PENDING_DEPOSIT and do NOT pre-create its Visit.
       Existing paid fixtures are not re-paid/reset if BE changes them later. */
    IF NOT EXISTS (SELECT 1 FROM dbo.Reservation WHERE ReservationId=@DemoPaidReservation)
    BEGIN
        INSERT dbo.Reservation(ReservationId,CustomerId,FacilityId,UnitTypeId,PolicyId,StartMonth,EndMonth,LockedRentalPrice,DepositAmount,Status,CreatedAt)
        VALUES(@DemoPaidReservation,@Customer1,@Facility1,@UnitSmall,@PolicyId,
               @DemoStartMonth,@DemoEndMonth,@DemoRentalPrice,@DemoRentalPrice,'PENDING_DEPOSIT',@DemoNowUtc);

        INSERT dbo.Invoice(InvoiceId,EntityId,InvoiceType,BillingMonth,BaseAmount,DiscountId,DiscountAmount,AmountDue,DueDate,Status,PaidAt,CreatedAt)
        VALUES(@DemoPaidInvoice,@DemoPaidReservation,'DEPOSIT',NULL,@DemoRentalPrice,NULL,0,@DemoRentalPrice,
               DATEADD(HOUR,@DemoDepositTimeoutHours,@DemoNowUtc),'PAID',@DemoNowUtc,@DemoNowUtc);

        /* PAYOS mock fixture ONLY; synthetic success is not evidence of a real webhook. */
        INSERT dbo.Payment(PaymentId,InvoiceId,IdempotencyKey,ProviderOrderCode,PaymentUrl,PaymentUrlExpiresAt,Amount,PaymentMethod,TransactionCode,Status,PaidAt,CreatedAt)
        VALUES(@DemoPaidPayment,@DemoPaidInvoice,@DemoPaidPayment,NEXT VALUE FOR dbo.ProviderOrderCodeSequence,NULL,NULL,@DemoRentalPrice,'PAYOS','MOCK-PAYOS-CORE-DEMO-DEP',
               'SUCCESS',@DemoNowUtc,@DemoNowUtc);
    END;

    /* Reuse the existing ACTIVE Contract C1. Do not invent a new Contract,
       reset a RETURN Visit, or change Contract dates just to pass ACCESS. */
    DECLARE @DemoAccessDate DATE = DATEADD(DAY,1,@DemoToday);
    DECLARE @DemoAccessRescheduleDate DATE = DATEADD(DAY,2,@DemoToday);
    IF NOT EXISTS (SELECT 1 FROM dbo.Visit WHERE VisitId=@DemoAccessVisit)
    BEGIN
        IF EXISTS (
            SELECT 1 FROM dbo.Contract
            WHERE ContractId=@C1 AND CustomerId=@Customer1 AND Status='ACTIVE'
              AND StartMonth<=@DemoAccessDate AND EOMONTH(EndMonth)>=@DemoAccessRescheduleDate
        ) AND NOT EXISTS (
            SELECT 1 FROM dbo.Visit WHERE EntityId=@C1 AND VisitType='RETURN'
              AND Status IN ('SCHEDULED','CHECKED_IN')
        )
        BEGIN
            INSERT dbo.Visit(VisitId,EntityId,EmployeeId,VisitType,VisitDate,ActualReturnDate,Status)
            VALUES(@DemoAccessVisit,@C1,NULL,'ACCESS',@DemoAccessDate,NULL,'SCHEDULED');
        END
        ELSE PRINT N'SKIP: Core Demo Access Visit (historical Contract no longer eligible).';
    END;


    /* Read-only demo guide: actual IDs/states, no gateway-success claim.
       New reservations through the app will get their own IDs/invoices. */
    SELECT 'SEARCH_AND_CREATE_RESERVATION' AS DemoStep,@Facility1 AS FacilityId,@UnitSmall AS UnitTypeId,
           CONVERT(CHAR(7),@DemoStartMonth,126) AS StartMonth,CONVERT(CHAR(7),@DemoEndMonth,126) AS EndMonth,
           @DemoUnitsAdded AS PhysicalUnitsAdded;

    SELECT CASE r.ReservationId WHEN @DemoUnpaidReservation THEN 'UNPAID_FOR_PAYMENT_BE'
                ELSE 'SEEDED_PAID_DEPOSIT_NOT_GATEWAY_TEST' END AS DemoScenario,
           r.ReservationId,r.CustomerId,r.Status AS ReservationStatus,
           CONVERT(CHAR(7),r.StartMonth,126) AS StartMonth,CONVERT(CHAR(7),r.EndMonth,126) AS EndMonth,
           i.InvoiceId,i.Status AS DepositStatus,i.AmountDue,i.DueDate AS DepositDeadlineUtc,
           DATEADD(DAY,CASE WHEN p.ReservationVisitStartDay>DAY(EOMONTH(r.StartMonth))
                           THEN DAY(EOMONTH(r.StartMonth)) ELSE p.ReservationVisitStartDay END-1,r.StartMonth)
               AS AllowedHandoverFrom,
           DATEADD(DAY,CASE WHEN p.ReservationVisitEndDay>DAY(EOMONTH(r.StartMonth))
                           THEN DAY(EOMONTH(r.StartMonth)) ELSE p.ReservationVisitEndDay END-1,r.StartMonth)
               AS AllowedHandoverTo
    FROM dbo.Reservation r
    JOIN dbo.Policy p ON p.PolicyId=r.PolicyId
    LEFT JOIN dbo.Invoice i ON i.EntityId=r.ReservationId AND i.InvoiceType='DEPOSIT'
    WHERE r.ReservationId IN (@DemoUnpaidReservation,@DemoPaidReservation);

    SELECT 'ACCESS_AND_RESCHEDULE' AS DemoScenario,c.ContractId,c.CustomerId,c.Status AS ContractStatus,
           v.VisitId,v.VisitDate,v.Status AS VisitStatus,@DemoAccessRescheduleDate AS SuggestedNewDate,
           CONVERT(CHAR(7),c.EndMonth,126) AS ContractEndMonth
    FROM dbo.Contract c LEFT JOIN dbo.Visit v ON v.EntityId=c.ContractId AND v.VisitId=@DemoAccessVisit
    WHERE c.ContractId=@C1;

    /* Measured monthly capacity, not a fabricated "available" flag.
       CAT-003 uses the minimum remaining value across the requested months. */
    SELECT CONVERT(CHAR(7),m.RentalMonth,126) AS RentalMonth,
           physical.Capacity AS EligiblePhysicalUnits,r.Held AS ReservationHolds,c.Occupied AS ActiveContractOccupancy,
           physical.Capacity-r.Held-c.Occupied AS RemainingSmallCapacity
    FROM @DemoMonths m
    CROSS APPLY (SELECT COUNT(*) AS Capacity FROM dbo.StorageUnit
                 WHERE FacilityId=@Facility1 AND UnitTypeId=@UnitSmall
                   AND Status IN ('AVAILABLE','IN_USE','INSPECTION')) physical
    CROSS APPLY (SELECT COUNT(*) AS Held FROM dbo.Reservation
                 WHERE FacilityId=@Facility1 AND UnitTypeId=@UnitSmall
                   AND Status IN ('PENDING_DEPOSIT','CONFIRMED')
                   AND StartMonth<=m.RentalMonth AND EndMonth>=m.RentalMonth) r
    CROSS APPLY (SELECT COUNT(*) AS Occupied FROM dbo.Contract ct
                 JOIN dbo.StorageUnit su ON su.StorageUnitId=ct.StorageUnitId
                 WHERE ct.FacilityId=@Facility1 AND su.UnitTypeId=@UnitSmall AND ct.Status='ACTIVE'
                   AND ct.StartMonth<=m.RentalMonth AND ct.EndMonth>=m.RentalMonth) c
    ORDER BY m.RentalMonth;
    COMMIT TRANSACTION;
    IF COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_RUN_ALL_MODE')),0)=1
        EXEC sys.sp_set_session_context @key=N'FRMS_SEED_STEP', @value=5;
    PRINT N'PASS: 05_Core_Demo_Reservation.sql';
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    EXEC sys.sp_set_session_context @key=N'FRMS_SEED_FAILED', @value=1;
    THROW;
END CATCH;

-- ########## END 05_Core_Demo_Reservation.sql ##########
GO

-- ########## START 06_EPS01_2000VND.sql ##########
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
-- ########## END 06_EPS01_2000VND.sql ##########
GO

IF COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_SEED_FAILED')),0)<>0 OR COALESCE(TRY_CONVERT(int,SESSION_CONTEXT(N'FRMS_SEED_STEP')),0)<>6
    THROW 51407, 'Unified seed incomplete; inspect first failed module (subsequent modules blocked).', 1;
EXEC sys.sp_set_session_context @key=N'FRMS_RUN_ALL_MODE', @value=0;
PRINT N'PASS: All unified V10.1 seed modules executed.';
SELECT v.TableName,v.TotalRows FROM (
  SELECT 'UserRole' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[UserRole]
UNION ALL
  SELECT 'Policy' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[Policy]
UNION ALL
  SELECT 'Facility' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[Facility]
UNION ALL
  SELECT 'UnitType' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[UnitType]
UNION ALL
  SELECT 'UserAccount' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[UserAccount]
UNION ALL
  SELECT 'Customer' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[Customer]
UNION ALL
  SELECT 'Employee' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[Employee]
UNION ALL
  SELECT 'Discount' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[Discount]
UNION ALL
  SELECT 'StorageUnit' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[StorageUnit]
UNION ALL
  SELECT 'Reservation' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[Reservation]
UNION ALL
  SELECT 'Visit' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[Visit]
UNION ALL
  SELECT 'Contract' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[Contract]
UNION ALL
  SELECT 'ContractExtension' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[ContractExtension]
UNION ALL
  SELECT 'Invoice' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[Invoice]
UNION ALL
  SELECT 'Payment' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[Payment]
UNION ALL
  SELECT 'LateFee' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[LateFee]
UNION ALL
  SELECT 'SupportTicket' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[SupportTicket]
UNION ALL
  SELECT 'Inspection' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[Inspection]
UNION ALL
  SELECT 'DamageType' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[DamageType]
UNION ALL
  SELECT 'DamageRecord' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[DamageRecord]
UNION ALL
  SELECT 'InspectionEvidence' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[InspectionEvidence]
UNION ALL
  SELECT 'ExtraFeeType' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[ExtraFeeType]
UNION ALL
  SELECT 'ExtraFee' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[ExtraFee]
UNION ALL
  SELECT 'DepositSettlement' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[DepositSettlement]
UNION ALL
  SELECT 'LoginHistory' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[LoginHistory]
UNION ALL
  SELECT 'AuditLog' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[AuditLog]
UNION ALL
  SELECT 'NotificationLog' AS TableName, COUNT_BIG(*) AS TotalRows FROM dbo.[NotificationLog]
) v ORDER BY v.TableName;
GO
