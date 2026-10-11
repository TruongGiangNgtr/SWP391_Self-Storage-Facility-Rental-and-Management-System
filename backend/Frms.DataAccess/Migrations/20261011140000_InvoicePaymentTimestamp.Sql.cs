namespace Frms.DataAccess.Migrations;

public partial class InvoicePaymentTimestamp
{
    // Frozen with this migration: future procedure revisions need a new migration.
    private const string ApplyPaymentResultSql = """
        CREATE OR ALTER PROCEDURE dbo.usp_ApplyPaymentResult
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
            SET @Outcome = 'InvalidResult';
            SET @Reason = NULL;

            IF @Status IS NULL OR @Status NOT IN ('SUCCESS', 'FAILED')
                OR @Source IS NULL OR @Source NOT IN ('VERIFIED_CALLBACK', 'DEFINITIVE_PRE_SESSION_FAILURE')
                RETURN;
            IF @Source = 'DEFINITIVE_PRE_SESSION_FAILURE' AND @Status <> 'FAILED' RETURN;
            IF @Source = 'VERIFIED_CALLBACK' AND NULLIF(LTRIM(RTRIM(@TransactionCode)), '') IS NULL RETURN;
            IF @TransactionCode IS NOT NULL AND NULLIF(LTRIM(RTRIM(@TransactionCode)), '') IS NULL RETURN;
            IF @Status = 'SUCCESS' AND (@VerifiedAmount IS NULL OR @VerifiedPaidAtUtc IS NULL) RETURN;

            BEGIN TRY
                BEGIN TRANSACTION;
                -- Also used by SaveSession. A short DB-scoped lock prevents cross-Invoice
                -- reference races/deadlocks independently of the database string collation.
                -- The filtered unique index remains the final protection for every writer.
                DECLARE @lockResult int;
                EXEC @lockResult = sys.sp_getapplock @Resource = N'FRMS:PaymentReferenceBinding',
                    @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000;
                IF @lockResult < 0 THROW 51000, 'Payment reference lock could not be acquired.', 1;

                DECLARE @invoiceId uniqueidentifier, @invoiceStatus varchar(50), @oldStatus varchar(50),
                    @oldReference varchar(150), @amount decimal(18,2), @paidAt datetime2(3), @hasSession bit;
                SELECT @invoiceId = InvoiceId FROM dbo.Payment WHERE PaymentId = @PaymentId;
                IF @invoiceId IS NULL
                BEGIN
                    SET @Outcome = 'NotFound';
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
                    SET @Outcome = 'NotFound';
                    COMMIT; RETURN;
                END;

                IF @oldStatus IN ('SUCCESS', 'FAILED')
                BEGIN
                    IF @oldStatus = @Status
                        AND (@oldReference = @TransactionCode OR (@oldReference IS NULL AND @TransactionCode IS NULL))
                        AND (@Status = 'FAILED' OR (@amount = @VerifiedAmount AND @paidAt = @VerifiedPaidAtUtc))
                    BEGIN
                        SET @Outcome = 'Duplicate';
                        SET @Reason = 'PAYMENT_CALLBACK_DUPLICATE';
                    END
                    ELSE
                    BEGIN
                        SET @Outcome = 'TerminalConflict';
                        SET @Reason = 'PAYMENT_RESULT_TERMINAL_CONFLICT';
                    END;
                    COMMIT; RETURN;
                END;

                IF @TransactionCode IS NOT NULL AND EXISTS (
                    SELECT 1 FROM dbo.Payment WHERE TransactionCode = @TransactionCode AND PaymentId <> @PaymentId)
                BEGIN
                    SET @Outcome = 'ReferenceConflict';
                    SET @Reason = 'PAYMENT_REFERENCE_CONFLICT';
                    COMMIT; RETURN;
                END;

                IF @Source = 'DEFINITIVE_PRE_SESSION_FAILURE' AND @hasSession = 1
                BEGIN
                    COMMIT; RETURN;
                END;

                IF @Status = 'SUCCESS' AND @amount <> @VerifiedAmount
                BEGIN
                    SET @Outcome = 'AmountMismatch';
                    SET @Reason = 'PAYMENT_AMOUNT_MISMATCH';
                    DECLARE @mismatch nvarchar(max) = (
                        SELECT @oldStatus AS [Status], @Reason AS Reason,
                            @VerifiedAmount AS VerifiedAmount, @TransactionCode AS TransactionCode
                        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
                    -- Repeating the same rejected amount/reference is one diagnostic, not an audit flood.
                    IF NOT EXISTS (SELECT 1 FROM dbo.AuditLog WHERE EntityId = @PaymentId
                        AND EntityType = 'PAYMENT' AND Action = 'PAYMENT_RESULT' AND NewValue = @mismatch)
                        INSERT dbo.AuditLog (AuditLogId, UserAccountId, Action, EntityType, EntityId, OldValue, NewValue, CreatedAt)
                        VALUES (NEWID(), NULL, 'PAYMENT_RESULT', 'PAYMENT', @PaymentId,
                            N'{"Status":"PENDING"}', @mismatch, SYSUTCDATETIME());
                    COMMIT; RETURN;
                END;

                DECLARE @oldValue nvarchar(max) = (
                    SELECT @oldStatus AS [Status], @invoiceStatus AS InvoiceStatus
                    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
                UPDATE dbo.Payment SET Status = @Status, TransactionCode = @TransactionCode,
                    PaidAt = CASE WHEN @Status = 'SUCCESS' THEN @VerifiedPaidAtUtc ELSE NULL END
                    WHERE PaymentId = @PaymentId;

                IF @Status = 'SUCCESS'
                BEGIN
                    IF @invoiceStatus IN ('UNPAID', 'OVERDUE')
                        UPDATE dbo.Invoice SET Status = 'PAID', PaidAt = @VerifiedPaidAtUtc WHERE InvoiceId = @invoiceId;
                    ELSE IF @invoiceStatus = 'CANCELLED'
                        SET @Reason = 'LATE_SUCCESS_ON_CANCELLED_INVOICE';
                END;

                DECLARE @newValue nvarchar(max) = (
                    SELECT @Status AS [Status],
                        CASE WHEN @Status = 'SUCCESS' AND @invoiceStatus IN ('UNPAID', 'OVERDUE')
                            THEN 'PAID' ELSE @invoiceStatus END AS InvoiceStatus,
                        @Reason AS Reason, @Source AS Source
                    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
                INSERT dbo.AuditLog (AuditLogId, UserAccountId, Action, EntityType, EntityId, OldValue, NewValue, CreatedAt)
                    VALUES (NEWID(), NULL, 'PAYMENT_RESULT', 'PAYMENT', @PaymentId, @oldValue, @newValue, SYSUTCDATETIME());
                SET @Outcome = 'Applied';
                COMMIT;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0 ROLLBACK;
                IF ERROR_NUMBER() IN (2601, 2627)
                BEGIN
                    SET @Outcome = 'ReferenceConflict';
                    SET @Reason = 'PAYMENT_REFERENCE_CONFLICT';
                    RETURN;
                END;
                THROW;
            END CATCH;
        END;
        """;
}
