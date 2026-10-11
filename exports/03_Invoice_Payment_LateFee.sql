-- FRMS V10.1 development fixture (NOT PRODUCTION): Deposit/Rental Invoice + mocked PAYOS Payment + LateFee
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
