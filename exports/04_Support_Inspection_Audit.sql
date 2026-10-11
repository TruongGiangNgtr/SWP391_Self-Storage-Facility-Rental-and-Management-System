-- FRMS V10.1 development fixture (NOT PRODUCTION): Support + Inspection + Damage + Fee + Settlement + Logs
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
