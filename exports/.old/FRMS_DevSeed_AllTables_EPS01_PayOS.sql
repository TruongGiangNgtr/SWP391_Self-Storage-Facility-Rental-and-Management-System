-- FRMS full-development seed for the current EF schema after EPS-01/payOS.
-- ALL 27 domain tables; apply all EF migrations FIRST (this is DATA, not schema deployment).
-- Derived from the owner's FRMS_DevSeed_AllTables_FIXED.sql. Monetary demo values are retained.
-- New mock Payment rows have explicit fixture UUID keys and sequence-allocated provider codes.
-- SUCCESS/PAID rows are SIMULATED HISTORICAL TEST DATA, never evidence of a real provider payment.
-- No HTTP call, webhook fabrication, refund execution, first-month Invoice/Payment, or live PaymentUrl.
-- No DELETE/UPDATE/DROP, historical backfill, fake EF migration history, or sequence reset.
-- Existing 2,000 VND live fixture/session is not overwritten. Keep this file development-only.
-- Before running in SSMS, SELECT the migrated development DB and set IN THE SAME SESSION:
-- EXEC sys.sp_set_session_context @key=N'EPS01_FIXTURE_PASSWORD_HASH', @value=N'<your BCrypt cost-12 hash>';
-- Accounts: customers 0901000001 / 0901000002; employees staff.demo@frms.local,
-- manager.demo@frms.local, bom.demo@frms.local, admin.demo@frms.local.
-- All seeded account passwords correspond to YOUR supplied hash, never an embedded password.
-- A separate 0990002000 Customer + UNPAID 2,000 VND Invoice is included for approved live testing.
-- Calendar months are GMT+7; timestamps and mock historical PaidAt values are UTC.

IF DB_NAME()<>N'FRMS_EPS01_Dev' AND DB_NAME() NOT LIKE N'Frms[_]Test[_]%'
    THROW 51200, 'Select FRMS_EPS01_Dev or a separate Frms_Test_* migrated development DB. Original FRMS is not modified.', 1;
IF COL_LENGTH('dbo.Invoice','PaidAt') IS NULL OR COL_LENGTH('dbo.Payment','IdempotencyKey') IS NULL
    OR COL_LENGTH('dbo.Payment','ProviderOrderCode') IS NULL OR COL_LENGTH('dbo.Payment','PaymentUrl') IS NULL
    OR COL_LENGTH('dbo.Payment','PaymentUrlExpiresAt') IS NULL OR OBJECT_ID('dbo.ProviderOrderCodeSequence','SO') IS NULL
    OR OBJECT_ID('dbo.usp_ApplyPaymentResult','P') IS NULL
    THROW 51206, 'Current EPS-01 EF migrations and authoritative Payment procedure are required.', 1;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    -- Repeat inside this batch: SSMS may continue after an error before a GO separator.
    IF DB_NAME()<>N'FRMS_EPS01_Dev' AND DB_NAME() NOT LIKE N'Frms[_]Test[_]%'
        THROW 51200, 'This batch is restricted to the migrated EPS-01 development/test DB.', 1;
    IF COL_LENGTH('dbo.Invoice','PaidAt') IS NULL OR COL_LENGTH('dbo.Payment','IdempotencyKey') IS NULL
        OR COL_LENGTH('dbo.Payment','ProviderOrderCode') IS NULL OR COL_LENGTH('dbo.Payment','PaymentUrl') IS NULL
        OR COL_LENGTH('dbo.Payment','PaymentUrlExpiresAt') IS NULL OR OBJECT_ID('dbo.ProviderOrderCodeSequence','SO') IS NULL
        OR OBJECT_ID('dbo.usp_ApplyPaymentResult','P') IS NULL
        THROW 51206, 'Current EPS-01 EF migrations are required before seeding this batch.', 1;
    BEGIN TRAN;
    DECLARE @SeedLock int;
    EXEC @SeedLock=sys.sp_getapplock @Resource=N'FRMS:AllTables:EPS01Seed', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
    IF @SeedLock<0 THROW 51204, 'Could not acquire development seed lock.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Payment WHERE PaymentId BETWEEN '84000000-0000-0000-0000-000000000001' AND '84000000-0000-0000-0000-000000000009' AND (PaymentMethod<>'PAYOS' OR IdempotencyKey<>PaymentId OR ProviderOrderCode IS NULL))
        THROW 51205, 'Legacy fixture IDs already exist. Do not backfill or overwrite them; use a separate migrated development DB.', 1;

    DECLARE @PasswordHash nvarchar(255) = CONVERT(nvarchar(255), SESSION_CONTEXT(N'EPS01_FIXTURE_PASSWORD_HASH'));
    IF @PasswordHash IS NULL OR LEN(@PasswordHash)<>60 OR LEFT(@PasswordHash,7) NOT IN ('$2a$12$', '$2b$12$', '$2y$12$')
        THROW 51201, 'Supply a BCrypt cost-12 hash in EPS01_FIXTURE_PASSWORD_HASH session context.', 1;

    /* =========================================================
       1) MASTER / BASELINE DATA
       ========================================================= */

    IF (SELECT COUNT(*) FROM dbo.UserRole WHERE RoleName IN ('CUSTOMER','FACILITY_STAFF','FACILITY_MANAGER','BUSINESS_OPERATIONS_MANAGER','SYSTEM_ADMINISTRATOR'))<>5
        THROW 51202, 'Apply EF baseline seeds before this development fixture.', 1;
    DECLARE @RoleCustomer uniqueidentifier = (SELECT RoleId FROM dbo.UserRole WHERE RoleName='CUSTOMER');
    DECLARE @RoleStaff uniqueidentifier = (SELECT RoleId FROM dbo.UserRole WHERE RoleName='FACILITY_STAFF');
    DECLARE @RoleManager uniqueidentifier = (SELECT RoleId FROM dbo.UserRole WHERE RoleName='FACILITY_MANAGER');
    DECLARE @RoleBom uniqueidentifier = (SELECT RoleId FROM dbo.UserRole WHERE RoleName='BUSINESS_OPERATIONS_MANAGER');
    DECLARE @RoleAdmin uniqueidentifier = (SELECT RoleId FROM dbo.UserRole WHERE RoleName='SYSTEM_ADMINISTRATOR');
    IF (SELECT COUNT(*) FROM dbo.Policy WHERE Status='ACTIVE')<>1
        THROW 51203, 'Exactly one EF-seeded ACTIVE Policy is required.', 1;
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
       Password supplied privately by the operator; no credential is embedded.
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

    -- Mock financial fixtures are intentionally NOT treated as verified gateway callbacks.
    COMMIT;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK;
    THROW;
END CATCH;
GO

-- Approved minimal live fixture is separate from the mock historical dataset.
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
    IF DB_NAME()<>N'FRMS_EPS01_Dev' AND DB_NAME() NOT LIKE N'Frms[_]Test[_]%'
        THROW 51200, 'This fixture batch must not change the original FRMS database.', 1;
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
