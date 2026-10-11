-- FRMS V10.1 development fixture (NOT PRODUCTION): Reservation + Contract + Extension + Visit
-- Standalone: select database FRMS (development copy) in SSMS. See README for session authorization.
-- All writes are INSERT-only, checked for existing primary keys; per-file transaction.
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
