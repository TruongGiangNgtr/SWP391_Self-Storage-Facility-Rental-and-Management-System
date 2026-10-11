/*
FRMS team database: full logical schema of FRMS_EPS01_Dev captured 2026-10-11.
27 tables / 193 columns / 49 CHECKs / 38 FKs / 78 indexes / 1 sequence / 2 procedures.
Master seed: five roles, one initial policy, six damage types, reused from
FrmsModelConfiguration.ConfigureSeed. Owner-requested development demo seed:
two facilities, three unit types and four AVAILABLE units, reused from EPS.
All account, employee and transaction/history tables start empty.
NO EF migrations/history, logins, credentials, personal or transaction data.
CURRENT-DB SNAPSHOT: this does not mean all SRS workflows are implemented.
Not in source DB: usp_CancelReservation, usp_ConfirmReservation,
usp_CheckInVisit, usp_CompleteHandover or the remaining SRS procedures/triggers.

Run THE ENTIRE FILE in SSMS or sqlcmd with database-create permission.
Default target: FRMS. For another name, replace database identifiers [FRMS]
and guard literals N'FRMS' only; do not change demo row names.
Existing databases are refused: choose a new name. This is not an upgrade script.
Schema + seed deployment is transactional. On failure stop; ROLLBACK if needed.
No EF command is required to create this database.
*/
USE [master];
GO

SET NOEXEC OFF;
IF DB_ID(N'FRMS') IS NOT NULL
BEGIN
    RAISERROR(N'Target database already exists. Choose a new name; overwrite is forbidden.', 16, 1);
    SET NOEXEC ON;
END;
ELSE
    EXEC(N'CREATE DATABASE [FRMS] COLLATE SQL_Latin1_General_CP1_CI_AS');
GO

USE [FRMS];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

CREATE TABLE [dbo].[AuditLog] (
    [AuditLogId] uniqueidentifier NOT NULL,
    [UserAccountId] uniqueidentifier NULL,
    [Action] varchar(150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [EntityType] varchar(150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [EntityId] uniqueidentifier NULL,
    [OldValue] nvarchar(max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [NewValue] nvarchar(max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [CreatedAt] datetime2(3) NOT NULL CONSTRAINT [DF__AuditLog__Create__7E37BEF6] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_AuditLog] PRIMARY KEY CLUSTERED ([AuditLogId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_AuditLog_NewValueJson] CHECK ([NewValue] IS NULL OR isjson([NewValue])=(1)),
    CONSTRAINT [CK_AuditLog_OldValueJson] CHECK ([OldValue] IS NULL OR isjson([OldValue])=(1))
);
GO

CREATE TABLE [dbo].[Contract] (
    [ContractId] uniqueidentifier NOT NULL,
    [ReservationId] uniqueidentifier NOT NULL,
    [CustomerId] uniqueidentifier NOT NULL,
    [FacilityId] uniqueidentifier NOT NULL,
    [StorageUnitId] uniqueidentifier NOT NULL,
    [PolicyId] uniqueidentifier NOT NULL,
    [DiscountId] uniqueidentifier NULL,
    [StartMonth] date NOT NULL,
    [EndMonth] date NOT NULL,
    [Status] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF__Contract__Status__7E02B4CC] DEFAULT ('ACTIVE'),
    CONSTRAINT [PK_Contract] PRIMARY KEY CLUSTERED ([ContractId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_Contract_Months] CHECK ([EndMonth]>=[StartMonth]),
    CONSTRAINT [CK_Contract_Status] CHECK ([Status]='TERMINATED' OR [Status]='COMPLETED' OR [Status]='ACTIVE')
);
GO

CREATE TABLE [dbo].[ContractExtension] (
    [ContractExtensionId] uniqueidentifier NOT NULL,
    [ContractId] uniqueidentifier NOT NULL,
    [OldEndMonth] date NOT NULL,
    [NewEndMonth] date NOT NULL,
    [AppliedMonthlyPrice] decimal(18,2) NOT NULL,
    [CreatedAt] datetime2(3) NOT NULL CONSTRAINT [DF__ContractE__Creat__45BE5BA9] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_ContractExtension] PRIMARY KEY CLUSTERED ([ContractExtensionId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_ContractExtension_Months] CHECK ([NewEndMonth]>[OldEndMonth]),
    CONSTRAINT [CK_ContractExtension_Price] CHECK ([AppliedMonthlyPrice]>=(0))
);
GO

CREATE TABLE [dbo].[Customer] (
    [CustomerId] uniqueidentifier NOT NULL,
    [UserAccountId] uniqueidentifier NOT NULL,
    [FullName] nvarchar(150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Address] nvarchar(1000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [CCCD] varchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    CONSTRAINT [PK_Customer] PRIMARY KEY CLUSTERED ([CustomerId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
);
GO

CREATE TABLE [dbo].[DamageRecord] (
    [DamageRecordId] uniqueidentifier NOT NULL,
    [InspectionId] uniqueidentifier NOT NULL,
    [DamageTypeId] uniqueidentifier NOT NULL,
    [DamageAmount] decimal(18,2) NOT NULL,
    [Note] nvarchar(1000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Status] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF__DamageRec__Statu__7D0E9093] DEFAULT ('PENDING'),
    [CreatedAt] datetime2(3) NOT NULL CONSTRAINT [DF__DamageRec__Creat__5E8A0973] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_DamageRecord] PRIMARY KEY CLUSTERED ([DamageRecordId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_DamageRecord_Amount] CHECK ([DamageAmount]>=(0)),
    CONSTRAINT [CK_DamageRecord_Status] CHECK ([Status]='REJECTED' OR [Status]='APPROVED' OR [Status]='PENDING')
);
GO

CREATE TABLE [dbo].[DamageType] (
    [DamageTypeId] uniqueidentifier NOT NULL,
    [Name] nvarchar(150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [DefaultAmount] decimal(18,2) NULL,
    [Status] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF__DamageTyp__Statu__7C1A6C5A] DEFAULT ('ACTIVE'),
    CONSTRAINT [PK_DamageType] PRIMARY KEY CLUSTERED ([DamageTypeId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_DamageType_DefaultAmount] CHECK ([DefaultAmount] IS NULL OR [DefaultAmount]>=(0)),
    CONSTRAINT [CK_DamageType_Status] CHECK ([Status]='INACTIVE' OR [Status]='ACTIVE')
);
GO

CREATE TABLE [dbo].[DepositSettlement] (
    [DepositSettlementId] uniqueidentifier NOT NULL,
    [ContractId] uniqueidentifier NOT NULL,
    [TotalDeduction] decimal(18,2) NOT NULL,
    [RefundAmount] decimal(18,2) NOT NULL,
    [AdditionalAmountDue] decimal(18,2) NOT NULL,
    [Status] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF__DepositSe__Statu__7B264821] DEFAULT ('PENDING'),
    [CalculatedAt] datetime2(3) NOT NULL,
    CONSTRAINT [PK_DepositSettlement] PRIMARY KEY CLUSTERED ([DepositSettlementId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_DepositSettlement_Amounts] CHECK ([TotalDeduction]>=(0) AND [RefundAmount]>=(0) AND [AdditionalAmountDue]>=(0)),
    CONSTRAINT [CK_DepositSettlement_Status] CHECK ([Status]='FINALIZED' OR [Status]='PENDING')
);
GO

CREATE TABLE [dbo].[Discount] (
    [DiscountId] uniqueidentifier NOT NULL,
    [CustomerId] uniqueidentifier NOT NULL,
    [Name] nvarchar(150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Percentage] decimal(5,2) NOT NULL,
    [Status] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF__Discount__Status__7A3223E8] DEFAULT ('ACTIVE'),
    [EffectiveFrom] datetime2(3) NOT NULL,
    [EffectiveTo] datetime2(3) NULL,
    CONSTRAINT [PK_Discount] PRIMARY KEY CLUSTERED ([DiscountId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_Discount_Percentage] CHECK ([Percentage]>=(0) AND [Percentage]<=(100)),
    CONSTRAINT [CK_Discount_Status] CHECK ([Status]='INACTIVE' OR [Status]='ACTIVE')
);
GO

CREATE TABLE [dbo].[Employee] (
    [EmployeeId] uniqueidentifier NOT NULL,
    [UserAccountId] uniqueidentifier NOT NULL,
    [FacilityId] uniqueidentifier NULL,
    [FullName] nvarchar(150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    CONSTRAINT [PK_Employee] PRIMARY KEY CLUSTERED ([EmployeeId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
);
GO

CREATE TABLE [dbo].[ExtraFee] (
    [ExtraFeeId] uniqueidentifier NOT NULL,
    [InspectionId] uniqueidentifier NOT NULL,
    [ExtraFeeTypeId] uniqueidentifier NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Reason] nvarchar(1000) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [CreatedAt] datetime2(3) NOT NULL CONSTRAINT [DF__ExtraFee__Create__65370702] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_ExtraFee] PRIMARY KEY CLUSTERED ([ExtraFeeId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_ExtraFee_Amount] CHECK ([Amount]>=(0))
);
GO

CREATE TABLE [dbo].[ExtraFeeType] (
    [ExtraFeeTypeId] uniqueidentifier NOT NULL,
    [Name] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [DefaultAmount] decimal(18,2) NOT NULL,
    [Status] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF__ExtraFeeT__Statu__793DFFAF] DEFAULT ('ACTIVE'),
    CONSTRAINT [PK_ExtraFeeType] PRIMARY KEY CLUSTERED ([ExtraFeeTypeId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_ExtraFeeType_DefaultAmount] CHECK ([DefaultAmount]>=(0)),
    CONSTRAINT [CK_ExtraFeeType_Name] CHECK ([Name]='OTHER' OR [Name]='CLEANING_FEE' OR [Name]='LOCK_REPLACEMENT' OR [Name]='ACCESS_CARD_REPLACEMENT' OR [Name]='KEY_REPLACEMENT'),
    CONSTRAINT [CK_ExtraFeeType_Status] CHECK ([Status]='INACTIVE' OR [Status]='ACTIVE')
);
GO

CREATE TABLE [dbo].[Facility] (
    [FacilityId] uniqueidentifier NOT NULL,
    [Name] nvarchar(150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Address] nvarchar(1000) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [ContactInfo] nvarchar(1000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Description] nvarchar(1000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Status] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF__Facility__Status__7849DB76] DEFAULT ('INACTIVE'),
    CONSTRAINT [PK_Facility] PRIMARY KEY CLUSTERED ([FacilityId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_Facility_Status] CHECK ([Status]='INACTIVE' OR [Status]='ACTIVE')
);
GO

CREATE TABLE [dbo].[Inspection] (
    [InspectionId] uniqueidentifier NOT NULL,
    [ContractId] uniqueidentifier NOT NULL,
    [StorageUnitId] uniqueidentifier NOT NULL,
    [VisitId] uniqueidentifier NULL,
    [EmployeeId] uniqueidentifier NULL,
    [Status] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF__Inspectio__Statu__7755B73D] DEFAULT ('PENDING'),
    [ConditionNote] nvarchar(1000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [CompletedAt] datetime2(3) NULL,
    CONSTRAINT [PK_Inspection] PRIMARY KEY CLUSTERED ([InspectionId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_Inspection_Status] CHECK ([Status]='COMPLETED' OR [Status]='IN_PROGRESS' OR [Status]='PENDING')
);
GO

CREATE TABLE [dbo].[InspectionEvidence] (
    [InspectionEvidenceId] uniqueidentifier NOT NULL,
    [InspectionId] uniqueidentifier NOT NULL,
    [FileData] varbinary(max) NOT NULL,
    [EvidenceType] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [CreatedAt] datetime2(3) NOT NULL CONSTRAINT [DF__Inspectio__Creat__6AEFE058] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_InspectionEvidence] PRIMARY KEY CLUSTERED ([InspectionEvidenceId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_InspectionEvidence_Type] CHECK ([EvidenceType]='DOCUMENT' OR [EvidenceType]='VIDEO' OR [EvidenceType]='IMAGE')
);
GO

CREATE TABLE [dbo].[Invoice] (
    [InvoiceId] uniqueidentifier NOT NULL,
    [EntityId] uniqueidentifier NOT NULL,
    [InvoiceType] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [BillingMonth] date NULL,
    [BaseAmount] decimal(18,2) NOT NULL,
    [DiscountId] uniqueidentifier NULL,
    [DiscountAmount] decimal(18,2) NOT NULL CONSTRAINT [DF__Invoice__Discoun__2739D489] DEFAULT ((0.0)),
    [AmountDue] decimal(18,2) NOT NULL,
    [DueDate] datetime2(3) NOT NULL,
    [Status] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF__Invoice__Status__76619304] DEFAULT ('UNPAID'),
    [CreatedAt] datetime2(3) NOT NULL CONSTRAINT [DF__Invoice__Created__282DF8C2] DEFAULT (sysutcdatetime()),
    [PaidAt] datetime2(3) NULL,
    CONSTRAINT [PK_Invoice] PRIMARY KEY CLUSTERED ([InvoiceId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_Invoice_Amounts] CHECK ([BaseAmount]>=(0) AND [DiscountAmount]>=(0) AND [AmountDue]>=(0)),
    CONSTRAINT [CK_Invoice_BillingMonth] CHECK ([InvoiceType]='DEPOSIT' AND [BillingMonth] IS NULL OR [InvoiceType]='RENTAL_FEE' AND [BillingMonth] IS NOT NULL),
    CONSTRAINT [CK_Invoice_Status] CHECK ([Status]='CANCELLED' OR [Status]='OVERDUE' OR [Status]='PAID' OR [Status]='UNPAID'),
    CONSTRAINT [CK_Invoice_Type] CHECK ([InvoiceType]='RENTAL_FEE' OR [InvoiceType]='DEPOSIT')
);
GO

CREATE TABLE [dbo].[LateFee] (
    [LateFeeId] uniqueidentifier NOT NULL,
    [InvoiceId] uniqueidentifier NOT NULL,
    [OverdueDays] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [CalculatedAt] datetime2(3) NOT NULL,
    CONSTRAINT [PK_LateFee] PRIMARY KEY CLUSTERED ([LateFeeId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_LateFee_Amount] CHECK ([Amount]>=(0)),
    CONSTRAINT [CK_LateFee_Days] CHECK ([OverdueDays]>=(1))
);
GO

CREATE TABLE [dbo].[LoginHistory] (
    [LoginHistoryId] uniqueidentifier NOT NULL,
    [UserAccountId] uniqueidentifier NOT NULL,
    [LoginAt] datetime2(3) NOT NULL CONSTRAINT [DF__LoginHist__Login__0A9D95DB] DEFAULT (sysutcdatetime()),
    [IpAddress] varchar(45) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [DeviceInfo] nvarchar(1000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Status] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    CONSTRAINT [PK_LoginHistory] PRIMARY KEY CLUSTERED ([LoginHistoryId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_LoginHistory_Status] CHECK ([Status]='FAILED' OR [Status]='SUCCESS')
);
GO

CREATE TABLE [dbo].[NotificationLog] (
    [NotificationLogId] uniqueidentifier NOT NULL,
    [UserAccountId] uniqueidentifier NOT NULL,
    [NotificationType] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Content] nvarchar(1000) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Status] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF__Notificat__Statu__756D6ECB] DEFAULT ('PENDING'),
    [SentAt] datetime2(3) NULL,
    [CreatedAt] datetime2(3) NOT NULL CONSTRAINT [DF__Notificat__Creat__0F624AF8] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_NotificationLog] PRIMARY KEY CLUSTERED ([NotificationLogId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_NotificationLog_SentAt] CHECK ([Status]='PENDING' AND [SentAt] IS NULL OR [Status]='SENT' AND [SentAt] IS NOT NULL),
    CONSTRAINT [CK_NotificationLog_Status] CHECK ([Status]='SENT' OR [Status]='PENDING')
);
GO

CREATE TABLE [dbo].[Payment] (
    [PaymentId] uniqueidentifier NOT NULL,
    [InvoiceId] uniqueidentifier NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [PaymentMethod] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [TransactionCode] varchar(150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Status] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF__Payment__Status__74794A92] DEFAULT ('PENDING'),
    [PaidAt] datetime2(3) NULL,
    [CreatedAt] datetime2(3) NOT NULL CONSTRAINT [DF__Payment__Created__3E1D39E1] DEFAULT (sysutcdatetime()),
    [IdempotencyKey] uniqueidentifier NOT NULL,
    [PaymentUrl] nvarchar(2048) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [PaymentUrlExpiresAt] datetime2(3) NULL,
    [ProviderOrderCode] bigint NULL,
    CONSTRAINT [PK_Payment] PRIMARY KEY CLUSTERED ([PaymentId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_Payment_Amount] CHECK ([Amount]>(0)),
    CONSTRAINT [CK_Payment_Method] CHECK ([PaymentMethod]='PAYOS' OR [PaymentMethod]='VNPAY' OR [PaymentMethod]='MOMO'),
    CONSTRAINT [CK_Payment_PaidAt] CHECK ([Status]='SUCCESS' AND [PaidAt] IS NOT NULL OR [Status]<>'SUCCESS' AND [PaidAt] IS NULL),
    CONSTRAINT [CK_Payment_Status] CHECK ([Status]='FAILED' OR [Status]='SUCCESS' OR [Status]='PENDING'),
    CONSTRAINT [CK_Payment_UrlExpiry] CHECK ([PaymentUrlExpiresAt] IS NULL OR [PaymentUrl] IS NOT NULL)
);
GO

CREATE TABLE [dbo].[Policy] (
    [PolicyId] uniqueidentifier NOT NULL,
    [Version] int NOT NULL,
    [Status] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF__Policy__Status__72910220] DEFAULT ('ACTIVE'),
    [EffectiveFrom] datetime2(3) NOT NULL,
    [EffectiveTo] datetime2(3) NULL,
    [DepositTimeoutHours] int NOT NULL,
    [ReservationVisitStartDay] int NOT NULL,
    [ReservationVisitEndDay] int NOT NULL,
    [MonthlyPaymentDueDay] int NOT NULL,
    [OverdueStartDay] int NOT NULL,
    [LateFeeDivisorDays] int NOT NULL,
    [EarlyReturnWaiveFeeUntilDay] int NOT NULL,
    [CreatedAt] datetime2(3) NOT NULL CONSTRAINT [DF__Policy__CreatedA__73852659] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_Policy] PRIMARY KEY CLUSTERED ([PolicyId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_Policy_Days] CHECK ([DepositTimeoutHours]>(0) AND ([ReservationVisitStartDay]>=(1) AND [ReservationVisitStartDay]<=(31)) AND ([ReservationVisitEndDay]>=(1) AND [ReservationVisitEndDay]<=(31)) AND [ReservationVisitStartDay]<=[ReservationVisitEndDay] AND ([MonthlyPaymentDueDay]>=(1) AND [MonthlyPaymentDueDay]<=(31)) AND ([OverdueStartDay]>=(1) AND [OverdueStartDay]<=(31)) AND [LateFeeDivisorDays]>(0) AND ([EarlyReturnWaiveFeeUntilDay]>=(1) AND [EarlyReturnWaiveFeeUntilDay]<=(31))),
    CONSTRAINT [CK_Policy_Status] CHECK ([Status]='INACTIVE' OR [Status]='ACTIVE')
);
GO

CREATE TABLE [dbo].[Reservation] (
    [ReservationId] uniqueidentifier NOT NULL,
    [CustomerId] uniqueidentifier NOT NULL,
    [FacilityId] uniqueidentifier NOT NULL,
    [UnitTypeId] uniqueidentifier NOT NULL,
    [PolicyId] uniqueidentifier NOT NULL,
    [StartMonth] date NOT NULL,
    [EndMonth] date NOT NULL,
    [LockedRentalPrice] decimal(18,2) NOT NULL,
    [DepositAmount] decimal(18,2) NOT NULL,
    [Status] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF__Reservati__Statu__719CDDE7] DEFAULT ('PENDING_DEPOSIT'),
    [CreatedAt] datetime2(3) NOT NULL CONSTRAINT [DF__Reservati__Creat__18EBB532] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_Reservation] PRIMARY KEY CLUSTERED ([ReservationId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_Reservation_Amounts] CHECK ([LockedRentalPrice]>=(0) AND [DepositAmount]>=(0)),
    CONSTRAINT [CK_Reservation_Months] CHECK ([EndMonth]>=[StartMonth]),
    CONSTRAINT [CK_Reservation_Status] CHECK ([Status]='CANCELLED' OR [Status]='COMPLETED' OR [Status]='CONFIRMED' OR [Status]='PENDING_DEPOSIT')
);
GO

CREATE TABLE [dbo].[StorageUnit] (
    [StorageUnitId] uniqueidentifier NOT NULL,
    [FacilityId] uniqueidentifier NOT NULL,
    [UnitTypeId] uniqueidentifier NOT NULL,
    [UnitCode] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [LocationInfo] nvarchar(1000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Status] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF__StorageUn__Statu__70A8B9AE] DEFAULT ('AVAILABLE'),
    CONSTRAINT [PK_StorageUnit] PRIMARY KEY CLUSTERED ([StorageUnitId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_StorageUnit_Status] CHECK ([Status]='MAINTENANCE' OR [Status]='INSPECTION' OR [Status]='IN_USE' OR [Status]='AVAILABLE')
);
GO

CREATE TABLE [dbo].[SupportTicket] (
    [SupportTicketId] uniqueidentifier NOT NULL,
    [ContractId] uniqueidentifier NOT NULL,
    [CustomerId] uniqueidentifier NOT NULL,
    [AssignedEmployeeId] uniqueidentifier NULL,
    [Category] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Description] nvarchar(1000) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Status] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF__SupportTi__Statu__6FB49575] DEFAULT ('OPEN'),
    [ResultNote] nvarchar(1000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [CreatedAt] datetime2(3) NOT NULL CONSTRAINT [DF__SupportTi__Creat__56E8E7AB] DEFAULT (sysutcdatetime()),
    [CompletedAt] datetime2(3) NULL,
    CONSTRAINT [PK_SupportTicket] PRIMARY KEY CLUSTERED ([SupportTicketId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_SupportTicket_Category] CHECK ([Category]='OTHER' OR [Category]='DAMAGE_ISSUE' OR [Category]='STORED_ITEM_ISSUE' OR [Category]='PAYMENT_ISSUE' OR [Category]='ACCESS_CARD_CODE_ISSUE' OR [Category]='LOCK_KEY_ISSUE' OR [Category]='UNIT_ISSUE'),
    CONSTRAINT [CK_SupportTicket_Status] CHECK ([Status]='CANCELLED' OR [Status]='COMPLETED' OR [Status]='IN_PROGRESS' OR [Status]='OPEN')
);
GO

CREATE TABLE [dbo].[UnitType] (
    [UnitTypeId] uniqueidentifier NOT NULL,
    [Name] nvarchar(150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Mode] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Size] nvarchar(150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [RentalPrice] decimal(18,2) NOT NULL,
    [Description] nvarchar(1000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    CONSTRAINT [PK_UnitType] PRIMARY KEY CLUSTERED ([UnitTypeId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_UnitType_Mode] CHECK ([Mode]='PRIVATE' OR [Mode]='PUBLIC'),
    CONSTRAINT [CK_UnitType_RentalPrice] CHECK ([RentalPrice]>=(0))
);
GO

CREATE TABLE [dbo].[UserAccount] (
    [UserAccountId] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    [Email] varchar(254) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [PhoneNumber] varchar(30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [PasswordHash] varchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Status] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF__UserAccou__Statu__6EC0713C] DEFAULT ('ACTIVE'),
    [EmailVerifiedAt] datetime2(3) NULL,
    [CreatedAt] datetime2(3) NOT NULL CONSTRAINT [DF__UserAccou__Creat__797309D9] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_UserAccount] PRIMARY KEY CLUSTERED ([UserAccountId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_UserAccount_Status] CHECK ([Status]='INACTIVE' OR [Status]='ACTIVE')
);
GO

CREATE TABLE [dbo].[UserRole] (
    [RoleId] uniqueidentifier NOT NULL,
    [RoleName] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Description] nvarchar(1000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    CONSTRAINT [PK_UserRole] PRIMARY KEY CLUSTERED ([RoleId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_UserRole_RoleName] CHECK ([RoleName]='SYSTEM_ADMINISTRATOR' OR [RoleName]='BUSINESS_OPERATIONS_MANAGER' OR [RoleName]='FACILITY_MANAGER' OR [RoleName]='FACILITY_STAFF' OR [RoleName]='CUSTOMER')
);
GO

CREATE TABLE [dbo].[Visit] (
    [VisitId] uniqueidentifier NOT NULL,
    [EntityId] uniqueidentifier NOT NULL,
    [EmployeeId] uniqueidentifier NULL,
    [VisitType] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [VisitDate] date NOT NULL,
    [ActualReturnDate] datetime2(3) NULL,
    [Status] varchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF__Visit__Status__6DCC4D03] DEFAULT ('SCHEDULED'),
    CONSTRAINT [PK_Visit] PRIMARY KEY CLUSTERED ([VisitId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF),
    CONSTRAINT [CK_Visit_Status] CHECK ([Status]='CANCELLED' OR [Status]='CHECKED_OUT' OR [Status]='CHECKED_IN' OR [Status]='SCHEDULED'),
    CONSTRAINT [CK_Visit_Type] CHECK ([VisitType]='RETURN' OR [VisitType]='ACCESS' OR [VisitType]='RESERVATION')
);
GO

CREATE NONCLUSTERED INDEX [IX_AuditLog_UserAccountId] ON [dbo].[AuditLog] ([UserAccountId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_Contract_CustomerId] ON [dbo].[Contract] ([CustomerId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_Contract_DiscountId] ON [dbo].[Contract] ([DiscountId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_Contract_FacilityId] ON [dbo].[Contract] ([FacilityId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_Contract_PolicyId] ON [dbo].[Contract] ([PolicyId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Contract_ReservationId] ON [dbo].[Contract] ([ReservationId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Contract_StorageUnitId] ON [dbo].[Contract] ([StorageUnitId] ASC) WHERE ([Status]='ACTIVE') WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_ContractExtension_ContractId] ON [dbo].[ContractExtension] ([ContractId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Customer_CCCD] ON [dbo].[Customer] ([CCCD] ASC) WHERE ([CCCD] IS NOT NULL) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Customer_UserAccountId] ON [dbo].[Customer] ([UserAccountId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_DamageRecord_DamageTypeId] ON [dbo].[DamageRecord] ([DamageTypeId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_DamageRecord_InspectionId] ON [dbo].[DamageRecord] ([InspectionId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_DamageType_Name] ON [dbo].[DamageType] ([Name] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_DepositSettlement_ContractId] ON [dbo].[DepositSettlement] ([ContractId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_Discount_CustomerId] ON [dbo].[Discount] ([CustomerId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_Employee_FacilityId] ON [dbo].[Employee] ([FacilityId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Employee_UserAccountId] ON [dbo].[Employee] ([UserAccountId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_ExtraFee_ExtraFeeTypeId] ON [dbo].[ExtraFee] ([ExtraFeeTypeId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_ExtraFee_InspectionId] ON [dbo].[ExtraFee] ([InspectionId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_ExtraFeeType_Name] ON [dbo].[ExtraFeeType] ([Name] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_Inspection_ContractId] ON [dbo].[Inspection] ([ContractId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_Inspection_EmployeeId] ON [dbo].[Inspection] ([EmployeeId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_Inspection_StorageUnitId] ON [dbo].[Inspection] ([StorageUnitId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_Inspection_VisitId] ON [dbo].[Inspection] ([VisitId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_InspectionEvidence_InspectionId] ON [dbo].[InspectionEvidence] ([InspectionId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_Invoice_DiscountId] ON [dbo].[Invoice] ([DiscountId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Invoice_EntityId] ON [dbo].[Invoice] ([EntityId] ASC) WHERE ([InvoiceType]='DEPOSIT') WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Invoice_EntityId_BillingMonth] ON [dbo].[Invoice] ([EntityId] ASC, [BillingMonth] ASC) WHERE ([InvoiceType]='RENTAL_FEE') WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_LateFee_InvoiceId] ON [dbo].[LateFee] ([InvoiceId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_LoginHistory_UserAccountId] ON [dbo].[LoginHistory] ([UserAccountId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_NotificationLog_UserAccountId] ON [dbo].[NotificationLog] ([UserAccountId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Payment_IdempotencyKey] ON [dbo].[Payment] ([IdempotencyKey] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_Payment_InvoiceId_Status_CreatedAt] ON [dbo].[Payment] ([InvoiceId] ASC, [Status] ASC, [CreatedAt] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Payment_ProviderOrderCode] ON [dbo].[Payment] ([ProviderOrderCode] ASC) WHERE ([ProviderOrderCode] IS NOT NULL) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Payment_TransactionCode] ON [dbo].[Payment] ([TransactionCode] ASC) WHERE ([TransactionCode] IS NOT NULL) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Policy_Status] ON [dbo].[Policy] ([Status] ASC) WHERE ([Status]='ACTIVE') WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Policy_Version] ON [dbo].[Policy] ([Version] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_Reservation_CustomerId] ON [dbo].[Reservation] ([CustomerId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_Reservation_FacilityId] ON [dbo].[Reservation] ([FacilityId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_Reservation_PolicyId] ON [dbo].[Reservation] ([PolicyId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_Reservation_UnitTypeId] ON [dbo].[Reservation] ([UnitTypeId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_StorageUnit_FacilityId] ON [dbo].[StorageUnit] ([FacilityId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_StorageUnit_UnitTypeId] ON [dbo].[StorageUnit] ([UnitTypeId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_SupportTicket_AssignedEmployeeId] ON [dbo].[SupportTicket] ([AssignedEmployeeId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_SupportTicket_ContractId] ON [dbo].[SupportTicket] ([ContractId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_SupportTicket_CustomerId] ON [dbo].[SupportTicket] ([CustomerId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_UserAccount_Email] ON [dbo].[UserAccount] ([Email] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_UserAccount_PhoneNumber] ON [dbo].[UserAccount] ([PhoneNumber] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_UserAccount_RoleId] ON [dbo].[UserAccount] ([RoleId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_UserRole_RoleName] ON [dbo].[UserRole] ([RoleName] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

CREATE NONCLUSTERED INDEX [IX_Visit_EmployeeId] ON [dbo].[Visit] ([EmployeeId] ASC) WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF);
GO

ALTER TABLE [dbo].[AuditLog] WITH CHECK ADD CONSTRAINT [FK_AuditLog_UserAccount_UserAccountId] FOREIGN KEY ([UserAccountId]) REFERENCES [dbo].[UserAccount] ([UserAccountId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[Contract] WITH CHECK ADD CONSTRAINT [FK_Contract_Customer_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customer] ([CustomerId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[Contract] WITH CHECK ADD CONSTRAINT [FK_Contract_Discount_DiscountId] FOREIGN KEY ([DiscountId]) REFERENCES [dbo].[Discount] ([DiscountId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[Contract] WITH CHECK ADD CONSTRAINT [FK_Contract_Facility_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [dbo].[Facility] ([FacilityId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[Contract] WITH CHECK ADD CONSTRAINT [FK_Contract_Policy_PolicyId] FOREIGN KEY ([PolicyId]) REFERENCES [dbo].[Policy] ([PolicyId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[Contract] WITH CHECK ADD CONSTRAINT [FK_Contract_Reservation_ReservationId] FOREIGN KEY ([ReservationId]) REFERENCES [dbo].[Reservation] ([ReservationId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[Contract] WITH CHECK ADD CONSTRAINT [FK_Contract_StorageUnit_StorageUnitId] FOREIGN KEY ([StorageUnitId]) REFERENCES [dbo].[StorageUnit] ([StorageUnitId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[ContractExtension] WITH CHECK ADD CONSTRAINT [FK_ContractExtension_Contract_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [dbo].[Contract] ([ContractId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[Customer] WITH CHECK ADD CONSTRAINT [FK_Customer_UserAccount_UserAccountId] FOREIGN KEY ([UserAccountId]) REFERENCES [dbo].[UserAccount] ([UserAccountId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[DamageRecord] WITH CHECK ADD CONSTRAINT [FK_DamageRecord_DamageType_DamageTypeId] FOREIGN KEY ([DamageTypeId]) REFERENCES [dbo].[DamageType] ([DamageTypeId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[DamageRecord] WITH CHECK ADD CONSTRAINT [FK_DamageRecord_Inspection_InspectionId] FOREIGN KEY ([InspectionId]) REFERENCES [dbo].[Inspection] ([InspectionId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[DepositSettlement] WITH CHECK ADD CONSTRAINT [FK_DepositSettlement_Contract_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [dbo].[Contract] ([ContractId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[Discount] WITH CHECK ADD CONSTRAINT [FK_Discount_Customer_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customer] ([CustomerId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[Employee] WITH CHECK ADD CONSTRAINT [FK_Employee_Facility_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [dbo].[Facility] ([FacilityId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[Employee] WITH CHECK ADD CONSTRAINT [FK_Employee_UserAccount_UserAccountId] FOREIGN KEY ([UserAccountId]) REFERENCES [dbo].[UserAccount] ([UserAccountId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[ExtraFee] WITH CHECK ADD CONSTRAINT [FK_ExtraFee_ExtraFeeType_ExtraFeeTypeId] FOREIGN KEY ([ExtraFeeTypeId]) REFERENCES [dbo].[ExtraFeeType] ([ExtraFeeTypeId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[ExtraFee] WITH CHECK ADD CONSTRAINT [FK_ExtraFee_Inspection_InspectionId] FOREIGN KEY ([InspectionId]) REFERENCES [dbo].[Inspection] ([InspectionId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[Inspection] WITH CHECK ADD CONSTRAINT [FK_Inspection_Contract_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [dbo].[Contract] ([ContractId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[Inspection] WITH CHECK ADD CONSTRAINT [FK_Inspection_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employee] ([EmployeeId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[Inspection] WITH CHECK ADD CONSTRAINT [FK_Inspection_StorageUnit_StorageUnitId] FOREIGN KEY ([StorageUnitId]) REFERENCES [dbo].[StorageUnit] ([StorageUnitId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[Inspection] WITH CHECK ADD CONSTRAINT [FK_Inspection_Visit_VisitId] FOREIGN KEY ([VisitId]) REFERENCES [dbo].[Visit] ([VisitId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[InspectionEvidence] WITH CHECK ADD CONSTRAINT [FK_InspectionEvidence_Inspection_InspectionId] FOREIGN KEY ([InspectionId]) REFERENCES [dbo].[Inspection] ([InspectionId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[Invoice] WITH CHECK ADD CONSTRAINT [FK_Invoice_Discount_DiscountId] FOREIGN KEY ([DiscountId]) REFERENCES [dbo].[Discount] ([DiscountId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[LateFee] WITH CHECK ADD CONSTRAINT [FK_LateFee_Invoice_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [dbo].[Invoice] ([InvoiceId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[LoginHistory] WITH CHECK ADD CONSTRAINT [FK_LoginHistory_UserAccount_UserAccountId] FOREIGN KEY ([UserAccountId]) REFERENCES [dbo].[UserAccount] ([UserAccountId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[NotificationLog] WITH CHECK ADD CONSTRAINT [FK_NotificationLog_UserAccount_UserAccountId] FOREIGN KEY ([UserAccountId]) REFERENCES [dbo].[UserAccount] ([UserAccountId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[Payment] WITH CHECK ADD CONSTRAINT [FK_Payment_Invoice_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [dbo].[Invoice] ([InvoiceId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[Reservation] WITH CHECK ADD CONSTRAINT [FK_Reservation_Customer_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customer] ([CustomerId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[Reservation] WITH CHECK ADD CONSTRAINT [FK_Reservation_Facility_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [dbo].[Facility] ([FacilityId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[Reservation] WITH CHECK ADD CONSTRAINT [FK_Reservation_Policy_PolicyId] FOREIGN KEY ([PolicyId]) REFERENCES [dbo].[Policy] ([PolicyId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[Reservation] WITH CHECK ADD CONSTRAINT [FK_Reservation_UnitType_UnitTypeId] FOREIGN KEY ([UnitTypeId]) REFERENCES [dbo].[UnitType] ([UnitTypeId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[StorageUnit] WITH CHECK ADD CONSTRAINT [FK_StorageUnit_Facility_FacilityId] FOREIGN KEY ([FacilityId]) REFERENCES [dbo].[Facility] ([FacilityId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[StorageUnit] WITH CHECK ADD CONSTRAINT [FK_StorageUnit_UnitType_UnitTypeId] FOREIGN KEY ([UnitTypeId]) REFERENCES [dbo].[UnitType] ([UnitTypeId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[SupportTicket] WITH CHECK ADD CONSTRAINT [FK_SupportTicket_Contract_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [dbo].[Contract] ([ContractId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[SupportTicket] WITH CHECK ADD CONSTRAINT [FK_SupportTicket_Customer_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customer] ([CustomerId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[SupportTicket] WITH CHECK ADD CONSTRAINT [FK_SupportTicket_Employee_AssignedEmployeeId] FOREIGN KEY ([AssignedEmployeeId]) REFERENCES [dbo].[Employee] ([EmployeeId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[UserAccount] WITH CHECK ADD CONSTRAINT [FK_UserAccount_UserRole_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[UserRole] ([RoleId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

ALTER TABLE [dbo].[Visit] WITH CHECK ADD CONSTRAINT [FK_Visit_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employee] ([EmployeeId]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

CREATE SEQUENCE [dbo].[ProviderOrderCodeSequence] AS bigint
START WITH 1000
INCREMENT BY 1
MINVALUE -9223372036854775808
MAXVALUE 9223372036854775807
NO CYCLE
CACHE;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE   PROCEDURE dbo.usp_ApplyPaymentResult
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
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE PROCEDURE dbo.usp_CreateReservation
    @CustomerId UNIQUEIDENTIFIER,
    @FacilityId UNIQUEIDENTIFIER,
    @UnitTypeId UNIQUEIDENTIFIER,
    @StartMonth DATE,
    @EndMonth DATE,
    @NowUtc DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @NowUtc IS NULL SET @NowUtc = SYSUTCDATETIME();

    IF @StartMonth IS NULL OR @EndMonth IS NULL
       OR DAY(@StartMonth) <> 1 OR DAY(@EndMonth) <> 1 OR @EndMonth < @StartMonth
        THROW 51101, 'INVALID_MONTH_RANGE', 1;

    DECLARE @BusinessNow DATETIME2(3) = DATEADD(HOUR, 7, @NowUtc);
    DECLARE @CurrentMonth DATE = DATEFROMPARTS(YEAR(@BusinessNow), MONTH(@BusinessNow), 1);
    IF @StartMonth <= @CurrentMonth
        THROW 51101, 'INVALID_MONTH_RANGE', 1;

    -- HOLDLOCK supplies transaction-scoped serializable range protection without
    -- changing the caller's session isolation level.

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @ActorId UNIQUEIDENTIFIER;
        SELECT @ActorId = ua.UserAccountId
        FROM dbo.Customer c WITH (HOLDLOCK)
        JOIN dbo.UserAccount ua WITH (HOLDLOCK) ON ua.UserAccountId = c.UserAccountId
        JOIN dbo.UserRole ur WITH (HOLDLOCK) ON ur.RoleId = ua.RoleId
        WHERE c.CustomerId = @CustomerId AND ua.Status = 'ACTIVE' AND ur.RoleName = 'CUSTOMER';
        IF @ActorId IS NULL
            THROW 51102, 'ACCOUNT_INACTIVE_OR_CUSTOMER_NOT_FOUND', 1;

        IF NOT EXISTS (
            SELECT 1 FROM dbo.Facility WITH (UPDLOCK, HOLDLOCK)
            WHERE FacilityId = @FacilityId AND Status = 'ACTIVE'
        )
            THROW 51103, 'FACILITY_INACTIVE', 1;

        DECLARE @RentalPrice DECIMAL(18,2);
        SELECT @RentalPrice = RentalPrice
        FROM dbo.UnitType WITH (UPDLOCK, HOLDLOCK)
        WHERE UnitTypeId = @UnitTypeId;

        IF @RentalPrice IS NULL
            THROW 51104, 'UNIT_TYPE_NOT_FOUND', 1;

        DECLARE @PolicyId UNIQUEIDENTIFIER, @DepositTimeoutHours INT;
        SELECT TOP (1)
            @PolicyId = PolicyId,
            @DepositTimeoutHours = DepositTimeoutHours
        FROM dbo.Policy WITH (UPDLOCK, HOLDLOCK)
        WHERE Status = 'ACTIVE'
        ORDER BY Version DESC;

        IF @PolicyId IS NULL
            THROW 51105, 'ACTIVE_POLICY_NOT_FOUND', 1;

        DECLARE @Month DATE = @StartMonth;
        WHILE @Month <= @EndMonth
        BEGIN
            DECLARE @Capacity BIGINT, @ReservationHolds BIGINT, @ContractOccupancy BIGINT;

            SELECT @Capacity = COUNT_BIG(*)
            FROM dbo.StorageUnit WITH (UPDLOCK, HOLDLOCK)
            WHERE FacilityId = @FacilityId
              AND UnitTypeId = @UnitTypeId
              AND Status IN ('AVAILABLE','IN_USE','INSPECTION');

            SELECT @ReservationHolds = COUNT_BIG(*)
            FROM dbo.Reservation WITH (UPDLOCK, HOLDLOCK)
            WHERE FacilityId = @FacilityId
              AND UnitTypeId = @UnitTypeId
              AND Status IN ('PENDING_DEPOSIT','CONFIRMED')
              AND StartMonth <= @Month
              AND EndMonth >= @Month;

            SELECT @ContractOccupancy = COUNT_BIG(*)
            FROM dbo.Contract c WITH (UPDLOCK, HOLDLOCK)
            JOIN dbo.StorageUnit su WITH (HOLDLOCK) ON su.StorageUnitId = c.StorageUnitId
            WHERE c.FacilityId = @FacilityId
              AND su.UnitTypeId = @UnitTypeId
              AND c.Status = 'ACTIVE'
              AND c.StartMonth <= @Month
              AND c.EndMonth >= @Month;

            IF ISNULL(@Capacity,0) <= ISNULL(@ReservationHolds,0) + ISNULL(@ContractOccupancy,0)
                THROW 51106, 'CAPACITY_NOT_AVAILABLE', 1;

            IF @Month = @EndMonth BREAK;
            SET @Month = DATEADD(MONTH, 1, @Month);
        END;

        DECLARE @ReservationId UNIQUEIDENTIFIER = NEWID();
        INSERT INTO dbo.Reservation(
            ReservationId, CustomerId, FacilityId, UnitTypeId, PolicyId,
            StartMonth, EndMonth, LockedRentalPrice, DepositAmount, Status, CreatedAt
        )
        VALUES (
            @ReservationId, @CustomerId, @FacilityId, @UnitTypeId, @PolicyId,
            @StartMonth, @EndMonth, @RentalPrice, @RentalPrice, 'PENDING_DEPOSIT', @NowUtc
        );

        DECLARE @DepositInvoiceId UNIQUEIDENTIFIER = NEWID();
        INSERT INTO dbo.Invoice(
            InvoiceId, EntityId, InvoiceType, BillingMonth, BaseAmount,
            DiscountId, DiscountAmount, AmountDue, DueDate, Status, CreatedAt
        )
        VALUES (
            @DepositInvoiceId, @ReservationId, 'DEPOSIT', NULL, @RentalPrice,
            NULL, 0, @RentalPrice, DATEADD(HOUR, @DepositTimeoutHours, @NowUtc), 'UNPAID', @NowUtc
        );

        -- EPS schema has no legacy audit trigger. One non-secret business audit
        -- is part of this same transaction, so a failed Invoice/audit rolls back all.
        DECLARE @AuditNewValue NVARCHAR(MAX) = (
            SELECT 'PENDING_DEPOSIT' AS Status, @PolicyId AS PolicyId,
                   @RentalPrice AS LockedRentalPrice, @RentalPrice AS DepositAmount
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        INSERT dbo.AuditLog(AuditLogId,UserAccountId,Action,EntityType,EntityId,OldValue,NewValue,CreatedAt)
        VALUES(NEWID(),@ActorId,'CREATE_RESERVATION','RESERVATION',@ReservationId,NULL,@AuditNewValue,@NowUtc);

        COMMIT TRANSACTION;

        SELECT @ReservationId AS ReservationId,
               @DepositInvoiceId AS DepositInvoiceId,
               @RentalPrice AS DepositAmount,
               DATEADD(HOUR, @DepositTimeoutHours, @NowUtc) AS DepositDueAt;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- Approved master seed only; no authentication or operational records.
INSERT INTO dbo.UserRole (RoleId, RoleName) VALUES
('10000000-0000-0000-0000-000000000001', 'CUSTOMER'),
('10000000-0000-0000-0000-000000000002', 'FACILITY_STAFF'),
('10000000-0000-0000-0000-000000000003', 'FACILITY_MANAGER'),
('10000000-0000-0000-0000-000000000004', 'BUSINESS_OPERATIONS_MANAGER'),
('10000000-0000-0000-0000-000000000005', 'SYSTEM_ADMINISTRATOR');

INSERT INTO dbo.Policy
(PolicyId, Version, Status, EffectiveFrom, DepositTimeoutHours,
 ReservationVisitStartDay, ReservationVisitEndDay, MonthlyPaymentDueDay,
 OverdueStartDay, LateFeeDivisorDays, EarlyReturnWaiveFeeUntilDay, CreatedAt)
VALUES ('20000000-0000-0000-0000-000000000001', 1, 'ACTIVE',
 '2026-10-04T00:00:00.000', 1, 1, 5, 5, 6, 31, 5, '2026-10-04T00:00:00.000');

INSERT INTO dbo.DamageType (DamageTypeId, Name, DefaultAmount, Status) VALUES
('30000000-0000-0000-0000-000000000001', N'LOCK_DAMAGE', NULL, 'ACTIVE'),
('30000000-0000-0000-0000-000000000002', N'DOOR_DAMAGE', NULL, 'ACTIVE'),
('30000000-0000-0000-0000-000000000003', N'WALL_DAMAGE', NULL, 'ACTIVE'),
('30000000-0000-0000-0000-000000000004', N'FLOOR_DAMAGE', NULL, 'ACTIVE'),
('30000000-0000-0000-0000-000000000005', N'WATER_DAMAGE', NULL, 'ACTIVE'),
('30000000-0000-0000-0000-000000000006', N'OTHER', NULL, 'ACTIVE');

-- ExtraFeeType amounts are not approved as automatic seed in the source code.
-- Accounts require approved provisioning, not a copy of customer information,
-- passwords or historical transactions.
GO

/*
Development/demo seed, explicitly requested by the owner on 2026-10-11.
Resource values reused from the FRMS Demo facilities in FRMS_EPS01_Dev.
Only AVAILABLE units without an ACTIVE Contract in that source were selected.
Not production pricing/configuration. No migration, account/password, employee,
contract, reservation, invoice, payment, policy update or history is included.
Run while connected to FRMS. To target a NEW disposable test database, change
only the @ExpectedDatabase literal. Never run this on production.
Rerun: insert missing IDs, refuse conflicting IDs/names/unit codes, never UPDATE.
Facility ContactInfo/Description are intentionally not copied.
*/
SET XACT_ABORT ON;
DECLARE @ExpectedDatabase sysname=N'FRMS';
IF DB_NAME() <> @ExpectedDatabase
    THROW 51983, 'Wrong database for development seed.', 1;

BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.Facility',N'U') IS NULL
       OR OBJECT_ID(N'dbo.UnitType',N'U') IS NULL
       OR OBJECT_ID(N'dbo.StorageUnit',N'U') IS NULL
        THROW 51984, 'Import the FRMS schema before applying demo seed.', 1;

    DECLARE @Facility TABLE (
        FacilityId uniqueidentifier PRIMARY KEY, Name nvarchar(150),
        Address nvarchar(1000), Status varchar(50));
    INSERT @Facility VALUES
    ('F1000000-0000-0000-0000-000000000001',N'FRMS Demo Facility - District 1',N'123 Nguyễn Huệ, Quận 1, TP.HCM','ACTIVE'),
    ('F1000000-0000-0000-0000-000000000002',N'FRMS Demo Facility - Thủ Đức',N'456 Võ Văn Ngân, TP. Thủ Đức, TP.HCM','INACTIVE');

    DECLARE @UnitType TABLE (
        UnitTypeId uniqueidentifier PRIMARY KEY, Name nvarchar(150),
        Mode varchar(50), Size nvarchar(150), RentalPrice decimal(18,2),
        Description nvarchar(1000));
    INSERT @UnitType VALUES
    ('B1000000-0000-0000-0000-000000000001',N'Small Private','PRIVATE',N'2 m²',500000.00,N'Boxes, documents, small household items.'),
    ('B1000000-0000-0000-0000-000000000002',N'Medium Private','PRIVATE',N'5 m²',900000.00,N'Furniture and medium household storage.'),
    ('B1000000-0000-0000-0000-000000000003',N'Shared Public','PUBLIC',N'3 m² equivalent',350000.00,N'Shared storage area handled by facility staff.');

    DECLARE @StorageUnit TABLE (
        StorageUnitId uniqueidentifier PRIMARY KEY, FacilityId uniqueidentifier,
        UnitTypeId uniqueidentifier, UnitCode varchar(50),
        LocationInfo nvarchar(1000), Status varchar(50));
    INSERT @StorageUnit VALUES
    ('71000000-0000-0000-0000-000000000002','F1000000-0000-0000-0000-000000000001','B1000000-0000-0000-0000-000000000001','A-102',N'Tầng 1 - Dãy A','AVAILABLE'),
    ('71000000-0000-0000-0000-000000000003','F1000000-0000-0000-0000-000000000001','B1000000-0000-0000-0000-000000000002','B-201',N'Tầng 2 - Dãy B','AVAILABLE'),
    ('71000000-0000-0000-0000-000000000005','F1000000-0000-0000-0000-000000000001','B1000000-0000-0000-0000-000000000003','P-001',N'Khu Public - Zone P','AVAILABLE'),
    ('71000000-0000-0000-0000-000000000006','F1000000-0000-0000-0000-000000000002','B1000000-0000-0000-0000-000000000001','A-101',N'Tầng 1 - Dãy A','AVAILABLE');

    IF EXISTS (
        SELECT 1 FROM dbo.Facility f WITH (UPDLOCK,HOLDLOCK)
        JOIN @Facility s ON s.FacilityId=f.FacilityId
        WHERE EXISTS (SELECT f.Name,f.Address,f.Status
                      EXCEPT SELECT s.Name,s.Address,s.Status))
       OR EXISTS (
        SELECT 1 FROM dbo.Facility f WITH (UPDLOCK,HOLDLOCK)
        JOIN @Facility s ON s.Name=f.Name WHERE s.FacilityId<>f.FacilityId)
        THROW 51985, 'Facility seed conflicts with existing data; no overwrite.', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.UnitType t WITH (UPDLOCK,HOLDLOCK)
        JOIN @UnitType s ON s.UnitTypeId=t.UnitTypeId
        WHERE EXISTS (SELECT t.Name,t.Mode,t.Size,t.RentalPrice,t.Description
                      EXCEPT SELECT s.Name,s.Mode,s.Size,s.RentalPrice,s.Description))
       OR EXISTS (
        SELECT 1 FROM dbo.UnitType t WITH (UPDLOCK,HOLDLOCK)
        JOIN @UnitType s ON s.Name=t.Name WHERE s.UnitTypeId<>t.UnitTypeId)
        THROW 51986, 'UnitType seed conflicts with existing data; no overwrite.', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.StorageUnit u WITH (UPDLOCK,HOLDLOCK)
        JOIN @StorageUnit s ON s.StorageUnitId=u.StorageUnitId
        WHERE EXISTS (SELECT u.FacilityId,u.UnitTypeId,u.UnitCode,u.LocationInfo,u.Status
                      EXCEPT SELECT s.FacilityId,s.UnitTypeId,s.UnitCode,s.LocationInfo,s.Status))
       OR EXISTS (
        SELECT 1 FROM dbo.StorageUnit u WITH (UPDLOCK,HOLDLOCK)
        JOIN @StorageUnit s ON s.FacilityId=u.FacilityId AND s.UnitCode=u.UnitCode
        WHERE s.StorageUnitId<>u.StorageUnitId)
        THROW 51987, 'StorageUnit seed conflicts with existing data; no overwrite.', 1;

    INSERT dbo.Facility(FacilityId,Name,Address,Status)
    SELECT s.FacilityId,s.Name,s.Address,s.Status FROM @Facility s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Facility f WITH (UPDLOCK,HOLDLOCK)
                      WHERE f.FacilityId=s.FacilityId);
    DECLARE @AddedFacilities int=@@ROWCOUNT;

    INSERT dbo.UnitType(UnitTypeId,Name,Mode,Size,RentalPrice,Description)
    SELECT s.UnitTypeId,s.Name,s.Mode,s.Size,s.RentalPrice,s.Description FROM @UnitType s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.UnitType t WITH (UPDLOCK,HOLDLOCK)
                      WHERE t.UnitTypeId=s.UnitTypeId);
    DECLARE @AddedTypes int=@@ROWCOUNT;

    INSERT dbo.StorageUnit(StorageUnitId,FacilityId,UnitTypeId,UnitCode,LocationInfo,Status)
    SELECT s.StorageUnitId,s.FacilityId,s.UnitTypeId,s.UnitCode,s.LocationInfo,s.Status
    FROM @StorageUnit s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.StorageUnit u WITH (UPDLOCK,HOLDLOCK)
                      WHERE u.StorageUnitId=s.StorageUnitId);
    DECLARE @AddedUnits int=@@ROWCOUNT;

    COMMIT TRANSACTION;
    SELECT @AddedFacilities AS AddedFacilities,@AddedTypes AS AddedUnitTypes,
           @AddedUnits AS AddedStorageUnits;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

IF (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0) <> 27
 OR (SELECT COUNT(*) FROM sys.procedures WHERE is_ms_shipped = 0) <> 2
 OR (SELECT COUNT(*) FROM dbo.UserRole) <> 5
 OR (SELECT COUNT(*) FROM dbo.Policy) <> 1
 OR (SELECT COUNT(*) FROM dbo.DamageType) <> 6
 OR (SELECT COUNT(*) FROM dbo.Facility) <> 2
 OR (SELECT COUNT(*) FROM dbo.UnitType) <> 3
 OR (SELECT COUNT(*) FROM dbo.StorageUnit) <> 4
 OR OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
BEGIN
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW 51971, 'Schema/seed verification failed. Deployment rolled back.', 1;
END;
COMMIT;
PRINT N'FRMS created: current EPS schema + approved seed, without migrations.';
GO

SET NOEXEC OFF;
GO

