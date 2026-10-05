using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Frms.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialPhaseZero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DamageType",
                columns: table => new
                {
                    DamageTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DefaultAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DamageType", x => x.DamageTypeId);
                    table.CheckConstraint("CK_DamageType_DefaultAmount", "[DefaultAmount] IS NULL OR [DefaultAmount]>=0");
                    table.CheckConstraint("CK_DamageType_Status", "[Status] IN ('ACTIVE','INACTIVE')");
                });

            migrationBuilder.CreateTable(
                name: "ExtraFeeType",
                columns: table => new
                {
                    ExtraFeeTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "varchar(50)", nullable: false),
                    DefaultAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtraFeeType", x => x.ExtraFeeTypeId);
                    table.CheckConstraint("CK_ExtraFeeType_DefaultAmount", "[DefaultAmount]>=0");
                    table.CheckConstraint("CK_ExtraFeeType_Name", "[Name] IN ('KEY_REPLACEMENT','ACCESS_CARD_REPLACEMENT','LOCK_REPLACEMENT','CLEANING_FEE','OTHER')");
                    table.CheckConstraint("CK_ExtraFeeType_Status", "[Status] IN ('ACTIVE','INACTIVE')");
                });

            migrationBuilder.CreateTable(
                name: "Facility",
                columns: table => new
                {
                    FacilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContactInfo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facility", x => x.FacilityId);
                    table.CheckConstraint("CK_Facility_Status", "[Status] IN ('ACTIVE','INACTIVE')");
                });

            migrationBuilder.CreateTable(
                name: "Policy",
                columns: table => new
                {
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    DepositTimeoutHours = table.Column<int>(type: "int", nullable: false),
                    ReservationVisitStartDay = table.Column<int>(type: "int", nullable: false),
                    ReservationVisitEndDay = table.Column<int>(type: "int", nullable: false),
                    MonthlyPaymentDueDay = table.Column<int>(type: "int", nullable: false),
                    OverdueStartDay = table.Column<int>(type: "int", nullable: false),
                    LateFeeDivisorDays = table.Column<int>(type: "int", nullable: false),
                    EarlyReturnWaiveFeeUntilDay = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Policy", x => x.PolicyId);
                    table.CheckConstraint("CK_Policy_Days", "[DepositTimeoutHours]>0 AND [ReservationVisitStartDay] BETWEEN 1 AND 31 AND [ReservationVisitEndDay] BETWEEN 1 AND 31 AND [MonthlyPaymentDueDay] BETWEEN 1 AND 31 AND [OverdueStartDay] BETWEEN 1 AND 31 AND [LateFeeDivisorDays]>0 AND [EarlyReturnWaiveFeeUntilDay] BETWEEN 1 AND 31");
                    table.CheckConstraint("CK_Policy_Status", "[Status] IN ('ACTIVE','INACTIVE')");
                });

            migrationBuilder.CreateTable(
                name: "UnitType",
                columns: table => new
                {
                    UnitTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Mode = table.Column<string>(type: "varchar(50)", nullable: false),
                    Size = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RentalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitType", x => x.UnitTypeId);
                    table.CheckConstraint("CK_UnitType_Mode", "[Mode] IN ('PUBLIC','PRIVATE')");
                    table.CheckConstraint("CK_UnitType_RentalPrice", "[RentalPrice]>=0");
                });

            migrationBuilder.CreateTable(
                name: "UserRole",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleName = table.Column<string>(type: "varchar(50)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRole", x => x.RoleId);
                    table.CheckConstraint("CK_UserRole_RoleName", "[RoleName] IN ('CUSTOMER','FACILITY_STAFF','FACILITY_MANAGER','BUSINESS_OPERATIONS_MANAGER','SYSTEM_ADMINISTRATOR')");
                });

            migrationBuilder.CreateTable(
                name: "StorageUnit",
                columns: table => new
                {
                    StorageUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    LocationInfo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageUnit", x => x.StorageUnitId);
                    table.CheckConstraint("CK_StorageUnit_Status", "[Status] IN ('AVAILABLE','IN_USE','INSPECTION','MAINTENANCE')");
                    table.ForeignKey(
                        name: "FK_StorageUnit_Facility_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facility",
                        principalColumn: "FacilityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StorageUnit_UnitType_UnitTypeId",
                        column: x => x.UnitTypeId,
                        principalTable: "UnitType",
                        principalColumn: "UnitTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserAccount",
                columns: table => new
                {
                    UserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "varchar(254)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(30)", nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(255)", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false),
                    EmailVerifiedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccount", x => x.UserAccountId);
                    table.CheckConstraint("CK_UserAccount_Status", "[Status] IN ('ACTIVE','INACTIVE')");
                    table.ForeignKey(
                        name: "FK_UserAccount_UserRole_RoleId",
                        column: x => x.RoleId,
                        principalTable: "UserRole",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    AuditLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "varchar(150)", nullable: false),
                    EntityType = table.Column<string>(type: "varchar(150)", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.AuditLogId);
                    table.CheckConstraint("CK_AuditLog_NewValueJson", "[NewValue] IS NULL OR ISJSON([NewValue])=1");
                    table.CheckConstraint("CK_AuditLog_OldValueJson", "[OldValue] IS NULL OR ISJSON([OldValue])=1");
                    table.ForeignKey(
                        name: "FK_AuditLog_UserAccount_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "UserAccount",
                        principalColumn: "UserAccountId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Customer",
                columns: table => new
                {
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CCCD = table.Column<string>(type: "varchar(20)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customer", x => x.CustomerId);
                    table.ForeignKey(
                        name: "FK_Customer_UserAccount_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "UserAccount",
                        principalColumn: "UserAccountId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Employee",
                columns: table => new
                {
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employee", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_Employee_Facility_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facility",
                        principalColumn: "FacilityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_UserAccount_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "UserAccount",
                        principalColumn: "UserAccountId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoginHistory",
                columns: table => new
                {
                    LoginHistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    IpAddress = table.Column<string>(type: "varchar(45)", nullable: true),
                    DeviceInfo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginHistory", x => x.LoginHistoryId);
                    table.CheckConstraint("CK_LoginHistory_Status", "[Status] IN ('SUCCESS','FAILED')");
                    table.ForeignKey(
                        name: "FK_LoginHistory_UserAccount_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "UserAccount",
                        principalColumn: "UserAccountId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationLog",
                columns: table => new
                {
                    NotificationLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationType = table.Column<string>(type: "varchar(50)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationLog", x => x.NotificationLogId);
                    table.CheckConstraint("CK_NotificationLog_Status", "[Status] IN ('PENDING','SENT')");
                    table.ForeignKey(
                        name: "FK_NotificationLog_UserAccount_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "UserAccount",
                        principalColumn: "UserAccountId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Discount",
                columns: table => new
                {
                    DiscountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discount", x => x.DiscountId);
                    table.CheckConstraint("CK_Discount_Percentage", "[Percentage] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_Discount_Status", "[Status] IN ('ACTIVE','INACTIVE')");
                    table.ForeignKey(
                        name: "FK_Discount_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reservation",
                columns: table => new
                {
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    EndMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    LockedRentalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DepositAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservation", x => x.ReservationId);
                    table.CheckConstraint("CK_Reservation_Amounts", "[LockedRentalPrice]>=0 AND [DepositAmount]>=0");
                    table.CheckConstraint("CK_Reservation_Months", "[EndMonth]>=[StartMonth]");
                    table.CheckConstraint("CK_Reservation_Status", "[Status] IN ('PENDING_DEPOSIT','CONFIRMED','COMPLETED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_Reservation_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_Facility_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facility",
                        principalColumn: "FacilityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_Policy_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Policy",
                        principalColumn: "PolicyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_UnitType_UnitTypeId",
                        column: x => x.UnitTypeId,
                        principalTable: "UnitType",
                        principalColumn: "UnitTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Visit",
                columns: table => new
                {
                    VisitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VisitType = table.Column<string>(type: "varchar(50)", nullable: false),
                    VisitDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ActualReturnDate = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visit", x => x.VisitId);
                    table.CheckConstraint("CK_Visit_Status", "[Status] IN ('SCHEDULED','CHECKED_IN','CHECKED_OUT','CANCELLED')");
                    table.CheckConstraint("CK_Visit_Type", "[VisitType] IN ('RESERVATION','ACCESS','RETURN')");
                    table.ForeignKey(
                        name: "FK_Visit_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Invoice",
                columns: table => new
                {
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceType = table.Column<string>(type: "varchar(50)", nullable: false),
                    BillingMonth = table.Column<DateOnly>(type: "date", nullable: true),
                    BaseAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    AmountDue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoice", x => x.InvoiceId);
                    table.CheckConstraint("CK_Invoice_Amounts", "[BaseAmount]>=0 AND [DiscountAmount]>=0 AND [AmountDue]>=0");
                    table.CheckConstraint("CK_Invoice_BillingMonth", "([InvoiceType]='DEPOSIT' AND [BillingMonth] IS NULL) OR ([InvoiceType]='RENTAL_FEE' AND [BillingMonth] IS NOT NULL)");
                    table.CheckConstraint("CK_Invoice_Status", "[Status] IN ('UNPAID','PAID','OVERDUE','CANCELLED')");
                    table.CheckConstraint("CK_Invoice_Type", "[InvoiceType] IN ('DEPOSIT','RENTAL_FEE')");
                    table.ForeignKey(
                        name: "FK_Invoice_Discount_DiscountId",
                        column: x => x.DiscountId,
                        principalTable: "Discount",
                        principalColumn: "DiscountId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Contract",
                columns: table => new
                {
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiscountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    EndMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contract", x => x.ContractId);
                    table.CheckConstraint("CK_Contract_Months", "[EndMonth]>=[StartMonth]");
                    table.CheckConstraint("CK_Contract_Status", "[Status] IN ('ACTIVE','COMPLETED','TERMINATED')");
                    table.ForeignKey(
                        name: "FK_Contract_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contract_Discount_DiscountId",
                        column: x => x.DiscountId,
                        principalTable: "Discount",
                        principalColumn: "DiscountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contract_Facility_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facility",
                        principalColumn: "FacilityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contract_Policy_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Policy",
                        principalColumn: "PolicyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contract_Reservation_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservation",
                        principalColumn: "ReservationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contract_StorageUnit_StorageUnitId",
                        column: x => x.StorageUnitId,
                        principalTable: "StorageUnit",
                        principalColumn: "StorageUnitId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LateFee",
                columns: table => new
                {
                    LateFeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OverdueDays = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LateFee", x => x.LateFeeId);
                    table.CheckConstraint("CK_LateFee_Amount", "[Amount]>=0");
                    table.CheckConstraint("CK_LateFee_Days", "[OverdueDays]>=1");
                    table.ForeignKey(
                        name: "FK_LateFee_Invoice_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoice",
                        principalColumn: "InvoiceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "varchar(50)", nullable: false),
                    TransactionCode = table.Column<string>(type: "varchar(150)", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.PaymentId);
                    table.CheckConstraint("CK_Payment_Amount", "[Amount]>0");
                    table.CheckConstraint("CK_Payment_Method", "[PaymentMethod]='MOMO'");
                    table.CheckConstraint("CK_Payment_PaidAt", "([Status]='SUCCESS' AND [PaidAt] IS NOT NULL) OR ([Status]<>'SUCCESS' AND [PaidAt] IS NULL)");
                    table.CheckConstraint("CK_Payment_Status", "[Status] IN ('PENDING','SUCCESS','FAILED')");
                    table.ForeignKey(
                        name: "FK_Payment_Invoice_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoice",
                        principalColumn: "InvoiceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContractExtension",
                columns: table => new
                {
                    ContractExtensionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldEndMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    NewEndMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    AppliedMonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractExtension", x => x.ContractExtensionId);
                    table.CheckConstraint("CK_ContractExtension_Months", "[NewEndMonth]>[OldEndMonth]");
                    table.CheckConstraint("CK_ContractExtension_Price", "[AppliedMonthlyPrice]>=0");
                    table.ForeignKey(
                        name: "FK_ContractExtension_Contract_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contract",
                        principalColumn: "ContractId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DepositSettlement",
                columns: table => new
                {
                    DepositSettlementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalDeduction = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AdditionalAmountDue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepositSettlement", x => x.DepositSettlementId);
                    table.CheckConstraint("CK_DepositSettlement_Amounts", "[TotalDeduction]>=0 AND [RefundAmount]>=0 AND [AdditionalAmountDue]>=0");
                    table.CheckConstraint("CK_DepositSettlement_Status", "[Status] IN ('PENDING','FINALIZED')");
                    table.ForeignKey(
                        name: "FK_DepositSettlement_Contract_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contract",
                        principalColumn: "ContractId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Inspection",
                columns: table => new
                {
                    InspectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VisitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false),
                    ConditionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspection", x => x.InspectionId);
                    table.CheckConstraint("CK_Inspection_Status", "[Status] IN ('PENDING','IN_PROGRESS','COMPLETED')");
                    table.ForeignKey(
                        name: "FK_Inspection_Contract_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contract",
                        principalColumn: "ContractId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inspection_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inspection_StorageUnit_StorageUnitId",
                        column: x => x.StorageUnitId,
                        principalTable: "StorageUnit",
                        principalColumn: "StorageUnitId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inspection_Visit_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visit",
                        principalColumn: "VisitId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupportTicket",
                columns: table => new
                {
                    SupportTicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Category = table.Column<string>(type: "varchar(50)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false),
                    ResultNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CompletedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTicket", x => x.SupportTicketId);
                    table.CheckConstraint("CK_SupportTicket_Category", "[Category] IN ('UNIT_ISSUE','LOCK_KEY_ISSUE','ACCESS_CARD_CODE_ISSUE','PAYMENT_ISSUE','STORED_ITEM_ISSUE','DAMAGE_ISSUE','OTHER')");
                    table.CheckConstraint("CK_SupportTicket_Status", "[Status] IN ('OPEN','IN_PROGRESS','COMPLETED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_SupportTicket_Contract_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contract",
                        principalColumn: "ContractId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportTicket_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportTicket_Employee_AssignedEmployeeId",
                        column: x => x.AssignedEmployeeId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DamageRecord",
                columns: table => new
                {
                    DamageRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DamageTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DamageAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DamageRecord", x => x.DamageRecordId);
                    table.CheckConstraint("CK_DamageRecord_Amount", "[DamageAmount]>=0");
                    table.CheckConstraint("CK_DamageRecord_Status", "[Status] IN ('PENDING','APPROVED','REJECTED')");
                    table.ForeignKey(
                        name: "FK_DamageRecord_DamageType_DamageTypeId",
                        column: x => x.DamageTypeId,
                        principalTable: "DamageType",
                        principalColumn: "DamageTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DamageRecord_Inspection_InspectionId",
                        column: x => x.InspectionId,
                        principalTable: "Inspection",
                        principalColumn: "InspectionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExtraFee",
                columns: table => new
                {
                    ExtraFeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtraFeeTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtraFee", x => x.ExtraFeeId);
                    table.CheckConstraint("CK_ExtraFee_Amount", "[Amount]>=0");
                    table.ForeignKey(
                        name: "FK_ExtraFee_ExtraFeeType_ExtraFeeTypeId",
                        column: x => x.ExtraFeeTypeId,
                        principalTable: "ExtraFeeType",
                        principalColumn: "ExtraFeeTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExtraFee_Inspection_InspectionId",
                        column: x => x.InspectionId,
                        principalTable: "Inspection",
                        principalColumn: "InspectionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InspectionEvidence",
                columns: table => new
                {
                    InspectionEvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    EvidenceType = table.Column<string>(type: "varchar(50)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionEvidence", x => x.InspectionEvidenceId);
                    table.CheckConstraint("CK_InspectionEvidence_Type", "[EvidenceType] IN ('IMAGE','VIDEO','DOCUMENT')");
                    table.ForeignKey(
                        name: "FK_InspectionEvidence_Inspection_InspectionId",
                        column: x => x.InspectionId,
                        principalTable: "Inspection",
                        principalColumn: "InspectionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "DamageType",
                columns: new[] { "DamageTypeId", "DefaultAmount", "Name", "Status" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000001"), null, "LOCK_DAMAGE", "ACTIVE" },
                    { new Guid("30000000-0000-0000-0000-000000000002"), null, "DOOR_DAMAGE", "ACTIVE" },
                    { new Guid("30000000-0000-0000-0000-000000000003"), null, "WALL_DAMAGE", "ACTIVE" },
                    { new Guid("30000000-0000-0000-0000-000000000004"), null, "FLOOR_DAMAGE", "ACTIVE" },
                    { new Guid("30000000-0000-0000-0000-000000000005"), null, "WATER_DAMAGE", "ACTIVE" },
                    { new Guid("30000000-0000-0000-0000-000000000006"), null, "OTHER", "ACTIVE" }
                });

            migrationBuilder.InsertData(
                table: "Policy",
                columns: new[] { "PolicyId", "CreatedAt", "DepositTimeoutHours", "EarlyReturnWaiveFeeUntilDay", "EffectiveFrom", "EffectiveTo", "LateFeeDivisorDays", "MonthlyPaymentDueDay", "OverdueStartDay", "ReservationVisitEndDay", "ReservationVisitStartDay", "Status", "Version" },
                values: new object[] { new Guid("20000000-0000-0000-0000-000000000001"), new DateTime(2026, 10, 4, 0, 0, 0, 0, DateTimeKind.Utc), 1, 5, new DateTime(2026, 10, 4, 0, 0, 0, 0, DateTimeKind.Utc), null, 31, 5, 6, 5, 1, "ACTIVE", 1 });

            migrationBuilder.InsertData(
                table: "UserRole",
                columns: new[] { "RoleId", "Description", "RoleName" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), null, "CUSTOMER" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), null, "FACILITY_STAFF" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), null, "FACILITY_MANAGER" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), null, "BUSINESS_OPERATIONS_MANAGER" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), null, "SYSTEM_ADMINISTRATOR" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_UserAccountId",
                table: "AuditLog",
                column: "UserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Contract_CustomerId",
                table: "Contract",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Contract_DiscountId",
                table: "Contract",
                column: "DiscountId");

            migrationBuilder.CreateIndex(
                name: "IX_Contract_FacilityId",
                table: "Contract",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Contract_PolicyId",
                table: "Contract",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Contract_ReservationId",
                table: "Contract",
                column: "ReservationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contract_StorageUnitId",
                table: "Contract",
                column: "StorageUnitId",
                unique: true,
                filter: "[Status] = 'ACTIVE'");

            migrationBuilder.CreateIndex(
                name: "IX_ContractExtension_ContractId",
                table: "ContractExtension",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_CCCD",
                table: "Customer",
                column: "CCCD",
                unique: true,
                filter: "[CCCD] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_UserAccountId",
                table: "Customer",
                column: "UserAccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DamageRecord_DamageTypeId",
                table: "DamageRecord",
                column: "DamageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DamageRecord_InspectionId",
                table: "DamageRecord",
                column: "InspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_DamageType_Name",
                table: "DamageType",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DepositSettlement_ContractId",
                table: "DepositSettlement",
                column: "ContractId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Discount_CustomerId",
                table: "Discount",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_FacilityId",
                table: "Employee",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_UserAccountId",
                table: "Employee",
                column: "UserAccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExtraFee_ExtraFeeTypeId",
                table: "ExtraFee",
                column: "ExtraFeeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtraFee_InspectionId",
                table: "ExtraFee",
                column: "InspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtraFeeType_Name",
                table: "ExtraFeeType",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inspection_ContractId",
                table: "Inspection",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspection_EmployeeId",
                table: "Inspection",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspection_StorageUnitId",
                table: "Inspection",
                column: "StorageUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspection_VisitId",
                table: "Inspection",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionEvidence_InspectionId",
                table: "InspectionEvidence",
                column: "InspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_DiscountId",
                table: "Invoice",
                column: "DiscountId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_EntityId",
                table: "Invoice",
                column: "EntityId",
                unique: true,
                filter: "[InvoiceType] = 'DEPOSIT'");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_EntityId_BillingMonth",
                table: "Invoice",
                columns: new[] { "EntityId", "BillingMonth" },
                unique: true,
                filter: "[InvoiceType] = 'RENTAL_FEE'");

            migrationBuilder.CreateIndex(
                name: "IX_LateFee_InvoiceId",
                table: "LateFee",
                column: "InvoiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistory_UserAccountId",
                table: "LoginHistory",
                column: "UserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLog_UserAccountId",
                table: "NotificationLog",
                column: "UserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_InvoiceId",
                table: "Payment",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_TransactionCode",
                table: "Payment",
                column: "TransactionCode",
                unique: true,
                filter: "[TransactionCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Policy_Status",
                table: "Policy",
                column: "Status",
                unique: true,
                filter: "[Status] = 'ACTIVE'");

            migrationBuilder.CreateIndex(
                name: "IX_Policy_Version",
                table: "Policy",
                column: "Version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_CustomerId",
                table: "Reservation",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_FacilityId",
                table: "Reservation",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_PolicyId",
                table: "Reservation",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_UnitTypeId",
                table: "Reservation",
                column: "UnitTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageUnit_FacilityId",
                table: "StorageUnit",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageUnit_UnitTypeId",
                table: "StorageUnit",
                column: "UnitTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicket_AssignedEmployeeId",
                table: "SupportTicket",
                column: "AssignedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicket_ContractId",
                table: "SupportTicket",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicket_CustomerId",
                table: "SupportTicket",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccount_Email",
                table: "UserAccount",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccount_PhoneNumber",
                table: "UserAccount",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccount_RoleId",
                table: "UserAccount",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_RoleName",
                table: "UserRole",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Visit_EmployeeId",
                table: "Visit",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "ContractExtension");

            migrationBuilder.DropTable(
                name: "DamageRecord");

            migrationBuilder.DropTable(
                name: "DepositSettlement");

            migrationBuilder.DropTable(
                name: "ExtraFee");

            migrationBuilder.DropTable(
                name: "InspectionEvidence");

            migrationBuilder.DropTable(
                name: "LateFee");

            migrationBuilder.DropTable(
                name: "LoginHistory");

            migrationBuilder.DropTable(
                name: "NotificationLog");

            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropTable(
                name: "SupportTicket");

            migrationBuilder.DropTable(
                name: "DamageType");

            migrationBuilder.DropTable(
                name: "ExtraFeeType");

            migrationBuilder.DropTable(
                name: "Inspection");

            migrationBuilder.DropTable(
                name: "Invoice");

            migrationBuilder.DropTable(
                name: "Contract");

            migrationBuilder.DropTable(
                name: "Visit");

            migrationBuilder.DropTable(
                name: "Discount");

            migrationBuilder.DropTable(
                name: "Reservation");

            migrationBuilder.DropTable(
                name: "StorageUnit");

            migrationBuilder.DropTable(
                name: "Employee");

            migrationBuilder.DropTable(
                name: "Customer");

            migrationBuilder.DropTable(
                name: "Policy");

            migrationBuilder.DropTable(
                name: "UnitType");

            migrationBuilder.DropTable(
                name: "Facility");

            migrationBuilder.DropTable(
                name: "UserAccount");

            migrationBuilder.DropTable(
                name: "UserRole");
        }
    }
}
