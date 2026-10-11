/*
FRMS payment schema / procedure alignment REFERENCE -- 2026-10-11
Reuses approved backend migrations; does not reimplement the gateway.
NOT an EF baseline-adoption script and NOT a production deployment approval.
The configured FRMS_EPS01_Dev already has these columns/sequence and the current
payment procedure. Do not overwrite it with the old database file.
On an old V2 DB, populated Payment rows without real lifetime IdempotencyKeys
BLOCK this script. No UUID/Invoice link/timestamp is manufactured or backfilled.
The legacy payment audit trigger also BLOCKS deployment: canonical SP + legacy
trg_Audit_Payment would produce duplicate PAYMENT_RESULT events. Team must review
that audit ownership before an approved migration; this script never disables it.
TransactionCode width 255 vs current-model 150 is deliberately not shrunk here:
review historical lengths/index dependencies rather than truncating data.
DEFAULT READ ONLY. Review/backup and explicitly set both values below.
Rollback/redeploy must preserve any subsequently captured key/code/timestamp data.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @ApplyPatch bit = 0;
DECLARE @ExpectedDatabase sysname = NULL;
IF @ApplyPatch = 0
BEGIN
    SELECT DB_NAME() AS selectedDatabase, 'REFERENCE ONLY - no changes' AS result;
    RETURN;
END;
IF @ExpectedDatabase IS NULL OR @ExpectedDatabase <> DB_NAME()
    OR DB_NAME() IN ('master','model','msdb','tempdb')
    THROW 51980, 'Explicit non-system database confirmation is required.', 1;
IF OBJECT_ID('dbo.Payment','U') IS NULL OR OBJECT_ID('dbo.Invoice','U') IS NULL
    OR OBJECT_ID('dbo.AuditLog','U') IS NULL
    THROW 51981, 'Required baseline tables are missing.', 1;
IF OBJECT_DEFINITION(OBJECT_ID('dbo.trg_Audit_Payment')) LIKE '%PAYMENT_RESULT%'
    THROW 51982, 'Legacy trigger conflicts with canonical payment audit. Review before deployment; it was not disabled.', 1;

BEGIN TRY
    BEGIN TRANSACTION;
    -- Locks protect the validation/DDL interval. No row values are changed.
    IF EXISTS (SELECT 1 FROM dbo.Payment WITH (TABLOCKX,HOLDLOCK) WHERE InvoiceId IS NULL)
        THROW 51001, 'Existing NULL InvoiceId requires owner-approved historical-data review.', 1;
    IF COL_LENGTH('dbo.Payment','IdempotencyKey') IS NULL
        AND EXISTS (SELECT 1 FROM dbo.Payment WITH (TABLOCKX,HOLDLOCK))
        THROW 51002, 'Existing Payment rows have no approved lifetime keys; no keys were fabricated.', 1;
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Payment')
               AND name='IdempotencyKey' AND (TYPE_NAME(user_type_id)<>'uniqueidentifier' OR is_nullable=1))
        THROW 51983, 'Existing IdempotencyKey type/nullability needs explicit review.', 1;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Payment') AND name='InvoiceId' AND is_nullable=1)
        EXEC(N'ALTER TABLE dbo.Payment ALTER COLUMN InvoiceId uniqueidentifier NOT NULL;');
    IF COL_LENGTH('dbo.Payment','IdempotencyKey') IS NULL
        EXEC(N'ALTER TABLE dbo.Payment ADD IdempotencyKey uniqueidentifier NOT NULL;');
    IF COL_LENGTH('dbo.Payment','PaymentUrl') IS NULL
        EXEC(N'ALTER TABLE dbo.Payment ADD PaymentUrl nvarchar(2048) NULL;');
    IF COL_LENGTH('dbo.Payment','PaymentUrlExpiresAt') IS NULL
        EXEC(N'ALTER TABLE dbo.Payment ADD PaymentUrlExpiresAt datetime2(3) NULL;');
    IF COL_LENGTH('dbo.Payment','ProviderOrderCode') IS NULL
        EXEC(N'ALTER TABLE dbo.Payment ADD ProviderOrderCode bigint NULL;');
    IF COL_LENGTH('dbo.Invoice','PaidAt') IS NULL
        EXEC(N'ALTER TABLE dbo.Invoice ADD PaidAt datetime2(3) NULL;');

    IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE object_id=OBJECT_ID('dbo.ProviderOrderCodeSequence'))
        EXEC(N'IF EXISTS (SELECT 1 FROM dbo.Payment WHERE ProviderOrderCode IS NOT NULL)
                   THROW 51984, ''Existing provider codes require reviewed sequence adoption.'', 1;
               CREATE SEQUENCE dbo.ProviderOrderCodeSequence AS bigint START WITH 1000 INCREMENT BY 1 NO CYCLE;');
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Payment') AND name='IX_Payment_IdempotencyKey')
        EXEC(N'CREATE UNIQUE INDEX IX_Payment_IdempotencyKey ON dbo.Payment(IdempotencyKey);');
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Payment') AND name='IX_Payment_ProviderOrderCode')
        EXEC(N'CREATE UNIQUE INDEX IX_Payment_ProviderOrderCode ON dbo.Payment(ProviderOrderCode) WHERE ProviderOrderCode IS NOT NULL;');
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.Payment') AND name='CK_Payment_UrlExpiry')
        EXEC(N'ALTER TABLE dbo.Payment WITH CHECK ADD CONSTRAINT CK_Payment_UrlExpiry CHECK (PaymentUrlExpiresAt IS NULL OR PaymentUrl IS NOT NULL);');
    -- Keep historical MOMO/VNPAY rows. Never rewrite their method or code.
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.Payment') AND name='CK_Payment_Method')
        EXEC(N'ALTER TABLE dbo.Payment DROP CONSTRAINT CK_Payment_Method;');
    EXEC(N'ALTER TABLE dbo.Payment WITH CHECK ADD CONSTRAINT CK_Payment_Method CHECK (PaymentMethod IN (''MOMO'',''VNPAY'',''PAYOS''));');

    -- Exact existing authoritative callback procedure, not the legacy four-parameter version.
    EXEC(N'CREATE OR ALTER PROCEDURE dbo.usp_ApplyPaymentResult
    @PaymentId uniqueidentifier,
    @Status varchar(50),
    @TransactionCode varchar(150),
    @VerifiedAmount decimal(38,18),
    @VerifiedPaidAtUtc datetime2(3),
    @Source varchar(50),
    @Outcome varchar(30) OUTPUT,
    @Reason varchar(80) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @Outcome = ''InvalidResult'';
    SET @Reason = NULL;

    IF @Status IS NULL OR @Status NOT IN (''SUCCESS'', ''FAILED'')
        OR @Source IS NULL OR @Source NOT IN (''VERIFIED_CALLBACK'', ''DEFINITIVE_PRE_SESSION_FAILURE'')
        RETURN;
    IF @Source = ''DEFINITIVE_PRE_SESSION_FAILURE'' AND @Status <> ''FAILED'' RETURN;
    IF @Source = ''VERIFIED_CALLBACK'' AND NULLIF(LTRIM(RTRIM(@TransactionCode)), '''') IS NULL RETURN;
    IF @TransactionCode IS NOT NULL AND NULLIF(LTRIM(RTRIM(@TransactionCode)), '''') IS NULL RETURN;
    IF @Status = ''SUCCESS'' AND (@VerifiedAmount IS NULL OR @VerifiedPaidAtUtc IS NULL) RETURN;

    BEGIN TRY
        BEGIN TRANSACTION;
        -- Also used by SaveSession. A short DB-scoped lock prevents cross-Invoice
        -- reference races/deadlocks independently of the database string collation.
        -- The filtered unique index remains the final protection for every writer.
        DECLARE @lockResult int;
        EXEC @lockResult = sys.sp_getapplock @Resource = N''FRMS:PaymentReferenceBinding'',
            @LockMode = ''Exclusive'', @LockOwner = ''Transaction'', @LockTimeout = 15000;
        IF @lockResult < 0 THROW 51000, ''Payment reference lock could not be acquired.'', 1;

        DECLARE @invoiceId uniqueidentifier, @invoiceStatus varchar(50), @oldStatus varchar(50),
            @oldReference varchar(150), @amount decimal(18,2), @paidAt datetime2(3), @hasSession bit;
        SELECT @invoiceId = InvoiceId FROM dbo.Payment WHERE PaymentId = @PaymentId;
        IF @invoiceId IS NULL
        BEGIN
            SET @Outcome = ''NotFound'';
            COMMIT; RETURN;
        END;

        -- Invoice then Payment: same lock order as attempt creation.
        SELECT @invoiceStatus = Status FROM dbo.Invoice WITH (UPDLOCK, HOLDLOCK)
            WHERE InvoiceId = @invoiceId;
        SELECT @oldStatus = Status, @oldReference = TransactionCode, @amount = Amount, @paidAt = PaidAt,
            @hasSession = CASE WHEN PaymentUrl IS NULL THEN 0 ELSE 1 END
            FROM dbo.Payment WITH (UPDLOCK, HOLDLOCK) WHERE PaymentId = @PaymentId AND InvoiceId = @invoiceId;
        IF @oldStatus IS NULL OR @invoiceStatus IS NULL
        BEGIN
            SET @Outcome = ''NotFound'';
            COMMIT; RETURN;
        END;

        IF @oldStatus IN (''SUCCESS'', ''FAILED'')
        BEGIN
            IF @oldStatus = @Status
                AND (@oldReference = @TransactionCode OR (@oldReference IS NULL AND @TransactionCode IS NULL))
                AND (@Status = ''FAILED'' OR (@amount = @VerifiedAmount AND @paidAt = @VerifiedPaidAtUtc))
            BEGIN
                SET @Outcome = ''Duplicate'';
                SET @Reason = ''PAYMENT_CALLBACK_DUPLICATE'';
            END
            ELSE
            BEGIN
                SET @Outcome = ''TerminalConflict'';
                SET @Reason = ''PAYMENT_RESULT_TERMINAL_CONFLICT'';
            END;
            COMMIT; RETURN;
        END;

        IF @TransactionCode IS NOT NULL AND EXISTS (
            SELECT 1 FROM dbo.Payment WHERE TransactionCode = @TransactionCode AND PaymentId <> @PaymentId)
        BEGIN
            SET @Outcome = ''ReferenceConflict'';
            SET @Reason = ''PAYMENT_REFERENCE_CONFLICT'';
            COMMIT; RETURN;
        END;

        IF @Source = ''DEFINITIVE_PRE_SESSION_FAILURE'' AND @hasSession = 1
        BEGIN
            COMMIT; RETURN;
        END;

        IF @Status = ''SUCCESS'' AND @amount <> @VerifiedAmount
        BEGIN
            SET @Outcome = ''AmountMismatch'';
            SET @Reason = ''PAYMENT_AMOUNT_MISMATCH'';
            DECLARE @mismatch nvarchar(max) = (
                SELECT @oldStatus AS [Status], @Reason AS Reason,
                    @VerifiedAmount AS VerifiedAmount, @TransactionCode AS TransactionCode
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
            -- Repeating the same rejected amount/reference is one diagnostic, not an audit flood.
            IF NOT EXISTS (SELECT 1 FROM dbo.AuditLog WHERE EntityId = @PaymentId
                AND EntityType = ''PAYMENT'' AND Action = ''PAYMENT_RESULT'' AND NewValue = @mismatch)
                INSERT dbo.AuditLog (AuditLogId, UserAccountId, Action, EntityType, EntityId, OldValue, NewValue, CreatedAt)
                VALUES (NEWID(), NULL, ''PAYMENT_RESULT'', ''PAYMENT'', @PaymentId,
                    N''{"Status":"PENDING"}'', @mismatch, SYSUTCDATETIME());
            COMMIT; RETURN;
        END;

        DECLARE @oldValue nvarchar(max) = (
            SELECT @oldStatus AS [Status], @invoiceStatus AS InvoiceStatus
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        UPDATE dbo.Payment SET Status = @Status, TransactionCode = @TransactionCode,
            PaidAt = CASE WHEN @Status = ''SUCCESS'' THEN @VerifiedPaidAtUtc ELSE NULL END
            WHERE PaymentId = @PaymentId;

        IF @Status = ''SUCCESS''
        BEGIN
            IF @invoiceStatus IN (''UNPAID'', ''OVERDUE'')
                UPDATE dbo.Invoice SET Status = ''PAID'', PaidAt = @VerifiedPaidAtUtc WHERE InvoiceId = @invoiceId;
            ELSE IF @invoiceStatus = ''CANCELLED''
                SET @Reason = ''LATE_SUCCESS_ON_CANCELLED_INVOICE'';
        END;

        DECLARE @newValue nvarchar(max) = (
            SELECT @Status AS [Status],
                CASE WHEN @Status = ''SUCCESS'' AND @invoiceStatus IN (''UNPAID'', ''OVERDUE'')
                    THEN ''PAID'' ELSE @invoiceStatus END AS InvoiceStatus,
                @Reason AS Reason, @Source AS Source
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        INSERT dbo.AuditLog (AuditLogId, UserAccountId, Action, EntityType, EntityId, OldValue, NewValue, CreatedAt)
            VALUES (NEWID(), NULL, ''PAYMENT_RESULT'', ''PAYMENT'', @PaymentId, @oldValue, @newValue, SYSUTCDATETIME());
        SET @Outcome = ''Applied'';
        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        IF ERROR_NUMBER() IN (2601, 2627)
        BEGIN
            SET @Outcome = ''ReferenceConflict'';
            SET @Reason = ''PAYMENT_REFERENCE_CONFLICT'';
            RETURN;
        END;
        THROW;
    END CATCH;
END;');
    COMMIT TRANSACTION;
    SELECT 'Approved missing fields/sequence and canonical callback definition aligned; no business command executed' AS result;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
