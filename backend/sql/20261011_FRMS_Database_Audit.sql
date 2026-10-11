/*
FRMS database audit -- 2026-10-11 (GMT+7)
Source: database/FRMS_Database_V2_Final_InspectionSafe.sql
READ ONLY. Select the intended database in SSMS before running.
No USE / CREATE DATABASE / migration-history adoption / business data updates.
Presence is NOT proof of SRS correctness. In particular, do not blindly install
the 40 old triggers: trg_Audit_Payment duplicates the canonical callback audit.
*/
SET NOCOUNT ON;

SELECT DB_NAME() AS databaseName,
       (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0 AND name <> '__EFMigrationsHistory') AS businessTableCount,
       (SELECT COUNT(*) FROM sys.procedures WHERE is_ms_shipped=0) AS procedureCount,
       (SELECT COUNT(*) FROM sys.triggers WHERE is_ms_shipped=0) AS triggerCount;

DECLARE @Expected TABLE (kind varchar(20), objectName sysname);
INSERT @Expected (kind, objectName) VALUES
('TABLE', 'dbo.AuditLog'),
('TABLE', 'dbo.Contract'),
('TABLE', 'dbo.ContractExtension'),
('TABLE', 'dbo.Customer'),
('TABLE', 'dbo.DamageRecord'),
('TABLE', 'dbo.DamageType'),
('TABLE', 'dbo.DepositSettlement'),
('TABLE', 'dbo.Discount'),
('TABLE', 'dbo.Employee'),
('TABLE', 'dbo.ExtraFee'),
('TABLE', 'dbo.ExtraFeeType'),
('TABLE', 'dbo.Facility'),
('TABLE', 'dbo.Inspection'),
('TABLE', 'dbo.InspectionEvidence'),
('TABLE', 'dbo.Invoice'),
('TABLE', 'dbo.LateFee'),
('TABLE', 'dbo.LoginHistory'),
('TABLE', 'dbo.NotificationLog'),
('TABLE', 'dbo.Payment'),
('TABLE', 'dbo.Policy'),
('TABLE', 'dbo.Reservation'),
('TABLE', 'dbo.StorageUnit'),
('TABLE', 'dbo.SupportTicket'),
('TABLE', 'dbo.UnitType'),
('TABLE', 'dbo.UserAccount'),
('TABLE', 'dbo.UserRole'),
('TABLE', 'dbo.Visit'),
('PROCEDURE', 'dbo.usp_AddInspectionEvidence'),
('PROCEDURE', 'dbo.usp_ApplyPaymentResult'),
('PROCEDURE', 'dbo.usp_AssignSupportTicket'),
('PROCEDURE', 'dbo.usp_CalculateLateFee'),
('PROCEDURE', 'dbo.usp_CancelReservation'),
('PROCEDURE', 'dbo.usp_CancelSupportTicket'),
('PROCEDURE', 'dbo.usp_CancelVisit'),
('PROCEDURE', 'dbo.usp_CheckInVisit'),
('PROCEDURE', 'dbo.usp_ClaimInspection'),
('PROCEDURE', 'dbo.usp_CompleteHandover'),
('PROCEDURE', 'dbo.usp_CompleteInspection'),
('PROCEDURE', 'dbo.usp_CompleteSupportTicket'),
('PROCEDURE', 'dbo.usp_ConfirmActualReturn'),
('PROCEDURE', 'dbo.usp_ConfirmReservation'),
('PROCEDURE', 'dbo.usp_CreateAccessVisit'),
('PROCEDURE', 'dbo.usp_CreateCustomerAccount'),
('PROCEDURE', 'dbo.usp_CreateEmployeeAccount'),
('PROCEDURE', 'dbo.usp_CreateMonthlyInvoice'),
('PROCEDURE', 'dbo.usp_CreatePaymentAttempt'),
('PROCEDURE', 'dbo.usp_CreatePolicyVersion'),
('PROCEDURE', 'dbo.usp_CreateReservation'),
('PROCEDURE', 'dbo.usp_CreateReturnVisit'),
('PROCEDURE', 'dbo.usp_CreateSupportTicket'),
('PROCEDURE', 'dbo.usp_DecideDamage'),
('PROCEDURE', 'dbo.usp_ExpirePendingReservations'),
('PROCEDURE', 'dbo.usp_FinalizeReturn'),
('PROCEDURE', 'dbo.usp_GenerateMonthlyInvoices'),
('PROCEDURE', 'dbo.usp_MarkNotificationSent'),
('PROCEDURE', 'dbo.usp_MarkOverdueInvoices'),
('PROCEDURE', 'dbo.usp_ProcessReservationNoShow'),
('PROCEDURE', 'dbo.usp_QueueNotification'),
('PROCEDURE', 'dbo.usp_RecalculateOpenLateFees'),
('PROCEDURE', 'dbo.usp_RecordDamage'),
('PROCEDURE', 'dbo.usp_RecordExtraFee'),
('PROCEDURE', 'dbo.usp_RenewContract'),
('PROCEDURE', 'dbo.usp_ReplaceInactiveEmployeeCredentialHash'),
('PROCEDURE', 'dbo.usp_RescheduleVisit'),
('PROCEDURE', 'dbo.usp_RetryPendingNotification'),
('PROCEDURE', 'dbo.usp_SetDamageRecordStatus'),
('PROCEDURE', 'dbo.usp_SetUserAccountStatus'),
('PROCEDURE', 'dbo.usp_UpdateEmployeeRoleFacility'),
('TRIGGER', 'dbo.trg_Audit_Contract'),
('TRIGGER', 'dbo.trg_Audit_ContractExtension'),
('TRIGGER', 'dbo.trg_Audit_DamageRecord'),
('TRIGGER', 'dbo.trg_Audit_DepositSettlement'),
('TRIGGER', 'dbo.trg_Audit_Discount'),
('TRIGGER', 'dbo.trg_Audit_Employee'),
('TRIGGER', 'dbo.trg_Audit_ExtraFee'),
('TRIGGER', 'dbo.trg_Audit_Inspection'),
('TRIGGER', 'dbo.trg_Audit_Invoice'),
('TRIGGER', 'dbo.trg_Audit_Payment'),
('TRIGGER', 'dbo.trg_Audit_Policy'),
('TRIGGER', 'dbo.trg_Audit_Reservation'),
('TRIGGER', 'dbo.trg_Audit_SupportTicket'),
('TRIGGER', 'dbo.trg_Audit_UnitType'),
('TRIGGER', 'dbo.trg_Audit_UserAccount'),
('TRIGGER', 'dbo.trg_Audit_Visit'),
('TRIGGER', 'dbo.trg_AuditLog_AppendOnly'),
('TRIGGER', 'dbo.trg_Contract_StatusTransition'),
('TRIGGER', 'dbo.trg_Contract_ValidateDiscountOwnership'),
('TRIGGER', 'dbo.trg_ContractExtension_AppendOnly'),
('TRIGGER', 'dbo.trg_DamageRecord_StatusTransition'),
('TRIGGER', 'dbo.trg_Discount_ProtectReferencedSemantics'),
('TRIGGER', 'dbo.trg_Employee_ValidateFacilityByRole'),
('TRIGGER', 'dbo.trg_Inspection_StatusTransition'),
('TRIGGER', 'dbo.trg_Inspection_ValidateVisit'),
('TRIGGER', 'dbo.trg_Invoice_ProtectSnapshot'),
('TRIGGER', 'dbo.trg_Invoice_StatusTransition'),
('TRIGGER', 'dbo.trg_Invoice_ValidateEntity'),
('TRIGGER', 'dbo.trg_LateFee_ValidateInvoice'),
('TRIGGER', 'dbo.trg_LoginHistory_AppendOnly'),
('TRIGGER', 'dbo.trg_Policy_ProtectBusinessValues'),
('TRIGGER', 'dbo.trg_Policy_ProtectHistoricalVersion'),
('TRIGGER', 'dbo.trg_Reservation_StatusTransition'),
('TRIGGER', 'dbo.trg_StorageUnit_ProtectUnitTypeReassignment'),
('TRIGGER', 'dbo.trg_StorageUnit_StatusTransition'),
('TRIGGER', 'dbo.trg_SupportTicket_StatusTransition'),
('TRIGGER', 'dbo.trg_UserAccount_ProtectLoginIdentifiers'),
('TRIGGER', 'dbo.trg_UserAccount_ValidateRoleProfile'),
('TRIGGER', 'dbo.trg_Visit_StatusTransition'),
('TRIGGER', 'dbo.trg_Visit_ValidateEntity');

SELECT kind, objectName,
       CASE WHEN OBJECT_ID(objectName, CASE kind WHEN 'TABLE' THEN 'U' WHEN 'PROCEDURE' THEN 'P' ELSE 'TR' END) IS NULL
            THEN 'MISSING' ELSE 'PRESENT - review definition' END AS deploymentStatus
FROM @Expected ORDER BY kind, objectName;

SELECT tableName, columnName,
       CASE WHEN COL_LENGTH(tableName, columnName) IS NULL THEN 'MISSING' ELSE 'PRESENT' END AS deploymentStatus
FROM (VALUES ('dbo.Payment','IdempotencyKey'), ('dbo.Payment','PaymentUrl'),
             ('dbo.Payment','PaymentUrlExpiresAt'), ('dbo.Payment','ProviderOrderCode'),
             ('dbo.Invoice','PaidAt')) expected(tableName,columnName);

SELECT OBJECT_NAME(p.object_id) AS procedureName, p.name AS parameterName,
       TYPE_NAME(p.user_type_id) AS sqlType, p.max_length, p.is_output
FROM sys.parameters p
WHERE p.object_id IN (OBJECT_ID('dbo.usp_CancelReservation'), OBJECT_ID('dbo.usp_CheckInVisit'),
                     OBJECT_ID('dbo.usp_CompleteHandover'), OBJECT_ID('dbo.usp_ApplyPaymentResult'),
                     OBJECT_ID('dbo.usp_CompleteInspection'), OBJECT_ID('dbo.usp_FinalizeReturn'))
ORDER BY procedureName,p.parameter_id;

SELECT OBJECT_NAME(c.object_id) AS tableName,c.name AS columnName,t.name AS sqlType,c.max_length,c.is_nullable
FROM sys.columns c JOIN sys.types t ON t.user_type_id=c.user_type_id
WHERE c.object_id IN (OBJECT_ID('dbo.Payment'),OBJECT_ID('dbo.Invoice'))
ORDER BY tableName,c.column_id;

SELECT OBJECT_NAME(parent_object_id) AS tableName,name,definition,is_disabled,is_not_trusted
FROM sys.check_constraints WHERE parent_object_id IN (OBJECT_ID('dbo.Payment'),OBJECT_ID('dbo.Invoice'));
SELECT name,OBJECT_NAME(object_id) AS tableName,is_unique,filter_definition
FROM sys.indexes WHERE object_id IN (OBJECT_ID('dbo.Payment'),OBJECT_ID('dbo.Visit'),OBJECT_ID('dbo.Contract'));
SELECT name,start_value,current_value,increment,is_cycling FROM sys.sequences
WHERE name='ProviderOrderCodeSequence';

IF OBJECT_ID('dbo.__EFMigrationsHistory','U') IS NOT NULL
    EXEC(N'SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;');

-- Definition red flags. These checks are clues, not behavioral tests.
SELECT 'handover requires online first-month PaymentId' AS issue
WHERE OBJECT_DEFINITION(OBJECT_ID('dbo.usp_CompleteHandover')) LIKE '%@PaymentId%'
UNION ALL SELECT 'handover still requires CustomerSignedConfirmed'
WHERE OBJECT_DEFINITION(OBJECT_ID('dbo.usp_CompleteHandover')) LIKE '%@CustomerSignedConfirmed%'
UNION ALL SELECT 'finalize lacks @StorageUnitStatus expected by backend'
WHERE OBJECT_ID('dbo.usp_FinalizeReturn','P') IS NOT NULL AND NOT EXISTS
 (SELECT 1 FROM sys.parameters WHERE object_id=OBJECT_ID('dbo.usp_FinalizeReturn') AND name='@StorageUnitStatus')
UNION ALL SELECT 'legacy payment trigger can duplicate PAYMENT_RESULT from canonical procedure'
WHERE OBJECT_DEFINITION(OBJECT_ID('dbo.trg_Audit_Payment')) LIKE '%PAYMENT_RESULT%';
