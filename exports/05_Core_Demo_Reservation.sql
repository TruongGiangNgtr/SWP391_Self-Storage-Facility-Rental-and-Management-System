-- FRMS V10.1 development fixture (NOT PRODUCTION): Core demo: 2 Reservation Deposit invoices + capacity + Visit scenario
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
