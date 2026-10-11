/*
FRMS V2 -> additions present in FRMS_EPS01_Dev, inspected 2026-10-11.
Source: database/FRMS_Database_V2_Final_InspectionSafe.sql
SHA256: 71218DF0F8BAA331C659DEEDAB0B1EA2A1F56C89EC8748B32F2BEB02F3211FC2

This is a TEAM HANDOFF script, not a new EF migration/baseline adoption.
Apply AFTER the entire old V2 file, NEVER before its later procedure overrides.
Do NOT run the full V2 file against FRMS_EPS01_Dev: it contains USE FRMS,
CREATE TABLE statements and legacy procedure/trigger definitions.
No CREATE DATABASE, USE, business command, historical-data backfill or EF-history write.
No missing IdempotencyKey, Invoice link or provider timestamp is manufactured.
Default is NO CHANGES. The owner must review/backup and confirm the target.

@ReplacePaymentProcedure defaults to 0: preserves the legacy procedure until
the team's audit-ownership review. The old trg_Audit_Payment emits PAYMENT_RESULT;
the EPS procedure ALSO emits it. Enabling replacement with that trigger still
present blocks the entire transaction. This script NEVER disables/removes triggers.
A schema-only run is NOT sufficient to make old V2 payment integration EPS-compatible.

@AddOptionalForeignKeyIndexes defaults to 0: optional performance parity only.
29 additional single-column indexes exist on EPS (besides the two new Payment
unique indexes). Prefix-covered indexes are skipped to avoid redundant indexes.

Rollback: any failure rolls back this transaction. After successful deployment,
restore saved procedure definitions / backup through an approved rollback plan;
do not DROP the new key, provider-code, URL or timestamp columns once used.
Retain all larger legacy text/TransactionCode/PasswordHash widths. No truncation.
No claim of full SRS compliance for the remaining legacy V2 procedures/triggers.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

DECLARE @ApplyPatch bit = 0;
DECLARE @ExpectedDatabase sysname = NULL; -- Set to the actual reviewed target, e.g. N'FRMS'.
DECLARE @ReplacePaymentProcedure bit = 0;
DECLARE @AddOptionalForeignKeyIndexes bit = 0;

IF @ApplyPatch = 0
BEGIN
    SELECT DB_NAME() AS selectedDatabase,
        'DRY RUN / no changes. Review target and flags before applying.' AS result;
    RETURN;
END;
IF @ExpectedDatabase IS NULL OR @ExpectedDatabase <> DB_NAME()
    OR DB_NAME() IN ('master','model','msdb','tempdb')
    THROW 51980, 'Explicit non-system database confirmation is required.', 1;
IF @@TRANCOUNT <> 0
    THROW 51989, 'Run standalone; an existing outer transaction is not supported.', 1;
IF OBJECT_ID('dbo.Payment','U') IS NULL OR OBJECT_ID('dbo.Invoice','U') IS NULL
    OR OBJECT_ID('dbo.AuditLog','U') IS NULL OR OBJECT_ID('dbo.LoginHistory','U') IS NULL
    THROW 51981, 'Baseline Payment/Invoice/AuditLog/LoginHistory tables are required.', 1;

BEGIN TRY
    BEGIN TRANSACTION;
    -- No row data is exported or changed. Keep the validation + DDL interval locked.
    IF EXISTS (SELECT 1 FROM dbo.Payment WITH (TABLOCKX,HOLDLOCK) WHERE InvoiceId IS NULL)
        THROW 51001, 'NULL InvoiceId requires approved historical-data review; no links were fabricated.', 1;
    IF COL_LENGTH('dbo.Payment','IdempotencyKey') IS NULL
        AND EXISTS (SELECT 1 FROM dbo.Payment WITH (TABLOCKX,HOLDLOCK))
        THROW 51002, 'Payment rows without real lifetime IdempotencyKeys block deployment; no keys were fabricated.', 1;

    IF @ReplacePaymentProcedure = 1
    BEGIN
        IF EXISTS (
            SELECT 1 FROM sys.triggers t
            JOIN sys.sql_modules sm ON sm.object_id=t.object_id
            WHERE t.parent_id=OBJECT_ID('dbo.Payment') AND t.is_disabled=0
                AND sm.definition LIKE '%PAYMENT_RESULT%')
            THROW 51982, 'Payment audit trigger conflicts with EPS procedure. Team review required; no trigger was disabled.', 1;
        IF EXISTS (SELECT 1 FROM dbo.Payment WHERE DATALENGTH(TransactionCode)>150)
            THROW 51994, 'Historical TransactionCode longer than EPS procedure parameter requires review; nothing was truncated.', 1;
    END;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Payment') AND name='InvoiceId'
               AND TYPE_NAME(user_type_id)<>'uniqueidentifier')
        THROW 51983, 'Unexpected InvoiceId type requires review.', 1;
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

    -- Refuse silent adoption of same-name columns with incompatible definitions.
    DECLARE @ExpectedColumns TABLE (
        TableName sysname, ColumnName sysname, TypeName sysname,
        MaxLength smallint, ExpectedScale tinyint, IsNullable bit);
    INSERT @ExpectedColumns VALUES
        ('Payment','IdempotencyKey','uniqueidentifier',16,0,0),
        ('Payment','PaymentUrl','nvarchar',4096,0,1),
        ('Payment','PaymentUrlExpiresAt','datetime2',7,3,1),
        ('Payment','ProviderOrderCode','bigint',8,0,1),
        ('Invoice','PaidAt','datetime2',7,3,1);
    IF EXISTS (
        SELECT 1 FROM @ExpectedColumns e LEFT JOIN sys.columns c
            ON c.object_id=OBJECT_ID(N'dbo.'+e.TableName) AND c.name=e.ColumnName
        WHERE c.column_id IS NULL OR TYPE_NAME(c.user_type_id)<>e.TypeName
            OR c.max_length<>e.MaxLength OR c.scale<>e.ExpectedScale OR c.is_nullable<>e.IsNullable)
        THROW 51983, 'Existing/new payment columns do not match EPS definitions; changes rolled back.', 1;

    IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE object_id=OBJECT_ID('dbo.ProviderOrderCodeSequence'))
        EXEC(N'IF EXISTS (SELECT 1 FROM dbo.Payment WHERE ProviderOrderCode IS NOT NULL)
                   THROW 51984, ''Existing provider codes require reviewed sequence adoption.'', 1;
               CREATE SEQUENCE dbo.ProviderOrderCodeSequence AS bigint
                   START WITH 1000 INCREMENT BY 1 NO CYCLE;');
    IF EXISTS (SELECT 1 FROM sys.sequences WHERE object_id=OBJECT_ID('dbo.ProviderOrderCodeSequence')
               AND (TYPE_NAME(user_type_id)<>'bigint' OR CONVERT(bigint,start_value)<>1000
                    OR CONVERT(bigint,increment)<>1 OR is_cycling=1))
        THROW 51985, 'Existing provider sequence differs; it was not restarted or replaced.', 1;

    EXEC(N'
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID(''dbo.Payment'') AND i.is_unique=1 AND i.has_filter=0
                AND i.is_disabled=0 AND i.is_hypothetical=0 AND c.name=''IdempotencyKey''
                AND NOT EXISTS (SELECT 1 FROM sys.index_columns x
                    WHERE x.object_id=i.object_id AND x.index_id=i.index_id AND x.key_ordinal>1))
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(''dbo.Payment'') AND name=''IX_Payment_IdempotencyKey'')
                THROW 51993, ''Idempotency index-name collision requires review.'', 1;
            CREATE UNIQUE INDEX IX_Payment_IdempotencyKey ON dbo.Payment(IdempotencyKey);
        END;
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID(''dbo.Payment'') AND i.is_unique=1 AND i.has_filter=1
                AND i.is_disabled=0 AND i.is_hypothetical=0 AND c.name=''ProviderOrderCode''
                AND REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(UPPER(i.filter_definition),''['',''''),'']'',''''),''('',''''),'')'',''''),'' '','''')=''PROVIDERORDERCODEISNOTNULL''
                AND NOT EXISTS (SELECT 1 FROM sys.index_columns x
                    WHERE x.object_id=i.object_id AND x.index_id=i.index_id AND x.key_ordinal>1))
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(''dbo.Payment'') AND name=''IX_Payment_ProviderOrderCode'')
                THROW 51993, ''Provider-code index-name collision requires review.'', 1;
            CREATE UNIQUE INDEX IX_Payment_ProviderOrderCode ON dbo.Payment(ProviderOrderCode)
                WHERE ProviderOrderCode IS NOT NULL;
        END;');

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.Payment') AND name='CK_Payment_UrlExpiry')
        EXEC(N'ALTER TABLE dbo.Payment WITH CHECK ADD CONSTRAINT CK_Payment_UrlExpiry
                   CHECK (PaymentUrlExpiresAt IS NULL OR PaymentUrl IS NOT NULL);');
    ELSE IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.Payment')
                    AND name='CK_Payment_UrlExpiry'
                    AND (is_disabled=1 OR is_not_trusted=1 OR
                         REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(UPPER(definition),'[',''),']',''),'(',''),')',''),' ','')<>'PAYMENTURLEXPIRESATISNULLORPAYMENTURLISNOTNULL'))
        THROW 51986, 'Existing UrlExpiry constraint requires review; it was not silently replaced.', 1;

    -- Known old method constraint: allow approved legacy MOMO/VNPAY plus current PAYOS.
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.Payment') AND name='CK_Payment_Method')
        EXEC(N'ALTER TABLE dbo.Payment DROP CONSTRAINT CK_Payment_Method;');
    EXEC(N'ALTER TABLE dbo.Payment WITH CHECK ADD CONSTRAINT CK_Payment_Method
               CHECK (PaymentMethod IN (''MOMO'',''VNPAY'',''PAYOS''));');

    -- Only widen; never narrow larger existing columns.
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.AuditLog')
               AND name IN ('Action','EntityType') AND
                   (TYPE_NAME(user_type_id)<>'varchar' OR is_nullable=1))
        THROW 51987, 'Audit column type/nullability requires review.', 1;
    IF COL_LENGTH('dbo.AuditLog','Action') BETWEEN 1 AND 149
        EXEC(N'ALTER TABLE dbo.AuditLog ALTER COLUMN Action varchar(150) NOT NULL;');
    IF COL_LENGTH('dbo.AuditLog','EntityType') BETWEEN 1 AND 149
        EXEC(N'ALTER TABLE dbo.AuditLog ALTER COLUMN EntityType varchar(150) NOT NULL;');

    -- Default only for future inserts; no old LoginAt timestamp is rewritten.
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.LoginHistory')
               AND name='LoginAt' AND default_object_id=0)
        EXEC(N'ALTER TABLE dbo.LoginHistory ADD CONSTRAINT DF_LoginHistory_LoginAt_EPS
                   DEFAULT SYSUTCDATETIME() FOR LoginAt;');

    IF @AddOptionalForeignKeyIndexes = 1
    BEGIN
        -- AuditLog.IX_AuditLog_UserAccountId: skip when an unfiltered index already starts with UserAccountId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.AuditLog') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='UserAccountId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.AuditLog') AND name='IX_AuditLog_UserAccountId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_AuditLog_UserAccountId] ON dbo.[AuditLog]([UserAccountId]);');
        END;

        -- Contract.IX_Contract_CustomerId: skip when an unfiltered index already starts with CustomerId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.Contract') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='CustomerId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Contract') AND name='IX_Contract_CustomerId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_Contract_CustomerId] ON dbo.[Contract]([CustomerId]);');
        END;

        -- Contract.IX_Contract_DiscountId: skip when an unfiltered index already starts with DiscountId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.Contract') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='DiscountId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Contract') AND name='IX_Contract_DiscountId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_Contract_DiscountId] ON dbo.[Contract]([DiscountId]);');
        END;

        -- Contract.IX_Contract_FacilityId: skip when an unfiltered index already starts with FacilityId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.Contract') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='FacilityId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Contract') AND name='IX_Contract_FacilityId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_Contract_FacilityId] ON dbo.[Contract]([FacilityId]);');
        END;

        -- Contract.IX_Contract_PolicyId: skip when an unfiltered index already starts with PolicyId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.Contract') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='PolicyId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Contract') AND name='IX_Contract_PolicyId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_Contract_PolicyId] ON dbo.[Contract]([PolicyId]);');
        END;

        -- ContractExtension.IX_ContractExtension_ContractId: skip when an unfiltered index already starts with ContractId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.ContractExtension') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='ContractId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.ContractExtension') AND name='IX_ContractExtension_ContractId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_ContractExtension_ContractId] ON dbo.[ContractExtension]([ContractId]);');
        END;

        -- DamageRecord.IX_DamageRecord_DamageTypeId: skip when an unfiltered index already starts with DamageTypeId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.DamageRecord') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='DamageTypeId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.DamageRecord') AND name='IX_DamageRecord_DamageTypeId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_DamageRecord_DamageTypeId] ON dbo.[DamageRecord]([DamageTypeId]);');
        END;

        -- DamageRecord.IX_DamageRecord_InspectionId: skip when an unfiltered index already starts with InspectionId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.DamageRecord') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='InspectionId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.DamageRecord') AND name='IX_DamageRecord_InspectionId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_DamageRecord_InspectionId] ON dbo.[DamageRecord]([InspectionId]);');
        END;

        -- Discount.IX_Discount_CustomerId: skip when an unfiltered index already starts with CustomerId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.Discount') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='CustomerId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Discount') AND name='IX_Discount_CustomerId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_Discount_CustomerId] ON dbo.[Discount]([CustomerId]);');
        END;

        -- ExtraFee.IX_ExtraFee_ExtraFeeTypeId: skip when an unfiltered index already starts with ExtraFeeTypeId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.ExtraFee') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='ExtraFeeTypeId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.ExtraFee') AND name='IX_ExtraFee_ExtraFeeTypeId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_ExtraFee_ExtraFeeTypeId] ON dbo.[ExtraFee]([ExtraFeeTypeId]);');
        END;

        -- Inspection.IX_Inspection_ContractId: skip when an unfiltered index already starts with ContractId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.Inspection') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='ContractId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Inspection') AND name='IX_Inspection_ContractId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_Inspection_ContractId] ON dbo.[Inspection]([ContractId]);');
        END;

        -- Inspection.IX_Inspection_EmployeeId: skip when an unfiltered index already starts with EmployeeId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.Inspection') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='EmployeeId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Inspection') AND name='IX_Inspection_EmployeeId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_Inspection_EmployeeId] ON dbo.[Inspection]([EmployeeId]);');
        END;

        -- Inspection.IX_Inspection_StorageUnitId: skip when an unfiltered index already starts with StorageUnitId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.Inspection') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='StorageUnitId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Inspection') AND name='IX_Inspection_StorageUnitId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_Inspection_StorageUnitId] ON dbo.[Inspection]([StorageUnitId]);');
        END;

        -- Inspection.IX_Inspection_VisitId: skip when an unfiltered index already starts with VisitId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.Inspection') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='VisitId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Inspection') AND name='IX_Inspection_VisitId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_Inspection_VisitId] ON dbo.[Inspection]([VisitId]);');
        END;

        -- InspectionEvidence.IX_InspectionEvidence_InspectionId: skip when an unfiltered index already starts with InspectionId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.InspectionEvidence') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='InspectionId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.InspectionEvidence') AND name='IX_InspectionEvidence_InspectionId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_InspectionEvidence_InspectionId] ON dbo.[InspectionEvidence]([InspectionId]);');
        END;

        -- Invoice.IX_Invoice_DiscountId: skip when an unfiltered index already starts with DiscountId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.Invoice') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='DiscountId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Invoice') AND name='IX_Invoice_DiscountId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_Invoice_DiscountId] ON dbo.[Invoice]([DiscountId]);');
        END;

        -- LoginHistory.IX_LoginHistory_UserAccountId: skip when an unfiltered index already starts with UserAccountId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.LoginHistory') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='UserAccountId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.LoginHistory') AND name='IX_LoginHistory_UserAccountId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_LoginHistory_UserAccountId] ON dbo.[LoginHistory]([UserAccountId]);');
        END;

        -- NotificationLog.IX_NotificationLog_UserAccountId: skip when an unfiltered index already starts with UserAccountId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.NotificationLog') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='UserAccountId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.NotificationLog') AND name='IX_NotificationLog_UserAccountId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_NotificationLog_UserAccountId] ON dbo.[NotificationLog]([UserAccountId]);');
        END;

        -- Reservation.IX_Reservation_CustomerId: skip when an unfiltered index already starts with CustomerId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.Reservation') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='CustomerId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Reservation') AND name='IX_Reservation_CustomerId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_Reservation_CustomerId] ON dbo.[Reservation]([CustomerId]);');
        END;

        -- Reservation.IX_Reservation_FacilityId: skip when an unfiltered index already starts with FacilityId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.Reservation') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='FacilityId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Reservation') AND name='IX_Reservation_FacilityId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_Reservation_FacilityId] ON dbo.[Reservation]([FacilityId]);');
        END;

        -- Reservation.IX_Reservation_PolicyId: skip when an unfiltered index already starts with PolicyId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.Reservation') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='PolicyId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Reservation') AND name='IX_Reservation_PolicyId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_Reservation_PolicyId] ON dbo.[Reservation]([PolicyId]);');
        END;

        -- Reservation.IX_Reservation_UnitTypeId: skip when an unfiltered index already starts with UnitTypeId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.Reservation') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='UnitTypeId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Reservation') AND name='IX_Reservation_UnitTypeId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_Reservation_UnitTypeId] ON dbo.[Reservation]([UnitTypeId]);');
        END;

        -- StorageUnit.IX_StorageUnit_FacilityId: skip when an unfiltered index already starts with FacilityId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.StorageUnit') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='FacilityId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.StorageUnit') AND name='IX_StorageUnit_FacilityId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_StorageUnit_FacilityId] ON dbo.[StorageUnit]([FacilityId]);');
        END;

        -- StorageUnit.IX_StorageUnit_UnitTypeId: skip when an unfiltered index already starts with UnitTypeId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.StorageUnit') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='UnitTypeId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.StorageUnit') AND name='IX_StorageUnit_UnitTypeId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_StorageUnit_UnitTypeId] ON dbo.[StorageUnit]([UnitTypeId]);');
        END;

        -- SupportTicket.IX_SupportTicket_AssignedEmployeeId: skip when an unfiltered index already starts with AssignedEmployeeId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.SupportTicket') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='AssignedEmployeeId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.SupportTicket') AND name='IX_SupportTicket_AssignedEmployeeId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_SupportTicket_AssignedEmployeeId] ON dbo.[SupportTicket]([AssignedEmployeeId]);');
        END;

        -- SupportTicket.IX_SupportTicket_ContractId: skip when an unfiltered index already starts with ContractId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.SupportTicket') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='ContractId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.SupportTicket') AND name='IX_SupportTicket_ContractId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_SupportTicket_ContractId] ON dbo.[SupportTicket]([ContractId]);');
        END;

        -- SupportTicket.IX_SupportTicket_CustomerId: skip when an unfiltered index already starts with CustomerId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.SupportTicket') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='CustomerId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.SupportTicket') AND name='IX_SupportTicket_CustomerId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_SupportTicket_CustomerId] ON dbo.[SupportTicket]([CustomerId]);');
        END;

        -- UserAccount.IX_UserAccount_RoleId: skip when an unfiltered index already starts with RoleId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.UserAccount') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='RoleId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.UserAccount') AND name='IX_UserAccount_RoleId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_UserAccount_RoleId] ON dbo.[UserAccount]([RoleId]);');
        END;

        -- Visit.IX_Visit_EmployeeId: skip when an unfiltered index already starts with EmployeeId.
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes i
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal=1
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.Visit') AND i.is_disabled=0
                AND i.is_hypothetical=0 AND i.has_filter=0 AND c.name='EmployeeId')
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Visit') AND name='IX_Visit_EmployeeId')
                THROW 51993, 'Index-name collision requires review; existing indexes were not replaced.', 1;
            EXEC(N'CREATE INDEX [IX_Visit_EmployeeId] ON dbo.[Visit]([EmployeeId]);');
        END;
    END;

    IF @ReplacePaymentProcedure = 1
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
    SELECT DB_NAME() AS selectedDatabase,
        'Missing EPS schema additions applied; historical rows preserved.' AS result,
        @ReplacePaymentProcedure AS paymentProcedureReplaced,
        @AddOptionalForeignKeyIndexes AS optionalIndexesRequested;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
