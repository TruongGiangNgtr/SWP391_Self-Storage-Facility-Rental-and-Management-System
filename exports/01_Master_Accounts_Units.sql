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
