using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Frms.Migrations.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class Pg001_InitialSchemaAndMasterSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:frms_ci_as", "und-u-ks-level2,und-u-ks-level2,icu,False");

            migrationBuilder.CreateSequence(
                name: "ProviderOrderCodeSequence",
                schema: "public",
                startValue: 1000L);

            migrationBuilder.CreateTable(
                name: "DamageType",
                schema: "public",
                columns: table => new
                {
                    DamageTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, collation: "frms_ci_as"),
                    DefaultAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: "ACTIVE", collation: "frms_ci_as")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DamageType", x => x.DamageTypeId);
                    table.CheckConstraint("CK_DamageType_DefaultAmount", "\"DefaultAmount\" IS NULL OR \"DefaultAmount\">=0");
                    table.CheckConstraint("CK_DamageType_Status", "\"Status\" IN ('ACTIVE','INACTIVE')");
                });

            migrationBuilder.CreateTable(
                name: "ExtraFeeType",
                schema: "public",
                columns: table => new
                {
                    ExtraFeeTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "varchar(50)", nullable: false, collation: "frms_ci_as"),
                    DefaultAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: "ACTIVE", collation: "frms_ci_as")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtraFeeType", x => x.ExtraFeeTypeId);
                    table.CheckConstraint("CK_ExtraFeeType_DefaultAmount", "\"DefaultAmount\">=0");
                    table.CheckConstraint("CK_ExtraFeeType_Name", "\"Name\" IN ('KEY_REPLACEMENT','ACCESS_CARD_REPLACEMENT','LOCK_REPLACEMENT','CLEANING_FEE','OTHER')");
                    table.CheckConstraint("CK_ExtraFeeType_Status", "\"Status\" IN ('ACTIVE','INACTIVE')");
                });

            migrationBuilder.CreateTable(
                name: "Facility",
                schema: "public",
                columns: table => new
                {
                    FacilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, collation: "frms_ci_as"),
                    Address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, collation: "frms_ci_as"),
                    ContactInfo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, collation: "frms_ci_as"),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, collation: "frms_ci_as"),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: "INACTIVE", collation: "frms_ci_as")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facility", x => x.FacilityId);
                    table.CheckConstraint("CK_Facility_Status", "\"Status\" IN ('ACTIVE','INACTIVE')");
                });

            migrationBuilder.CreateTable(
                name: "Policy",
                schema: "public",
                columns: table => new
                {
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: "ACTIVE", collation: "frms_ci_as"),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: false, defaultValueSql: "clock_timestamp()"),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: true),
                    DepositTimeoutHours = table.Column<int>(type: "integer", nullable: false),
                    ReservationVisitStartDay = table.Column<int>(type: "integer", nullable: false),
                    ReservationVisitEndDay = table.Column<int>(type: "integer", nullable: false),
                    MonthlyPaymentDueDay = table.Column<int>(type: "integer", nullable: false),
                    OverdueStartDay = table.Column<int>(type: "integer", nullable: false),
                    LateFeeDivisorDays = table.Column<int>(type: "integer", nullable: false),
                    EarlyReturnWaiveFeeUntilDay = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: false, defaultValueSql: "clock_timestamp()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Policy", x => x.PolicyId);
                    table.CheckConstraint("CK_Policy_Days", "\"DepositTimeoutHours\">0 AND \"ReservationVisitStartDay\" BETWEEN 1 AND 31 AND \"ReservationVisitEndDay\" BETWEEN 1 AND 31 AND \"ReservationVisitStartDay\" <= \"ReservationVisitEndDay\" AND \"MonthlyPaymentDueDay\" BETWEEN 1 AND 31 AND \"OverdueStartDay\" BETWEEN 1 AND 31 AND \"LateFeeDivisorDays\">0 AND \"EarlyReturnWaiveFeeUntilDay\" BETWEEN 1 AND 31");
                    table.CheckConstraint("CK_Policy_Status", "\"Status\" IN ('ACTIVE','INACTIVE')");
                });

            migrationBuilder.CreateTable(
                name: "UnitType",
                schema: "public",
                columns: table => new
                {
                    UnitTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, collation: "frms_ci_as"),
                    Mode = table.Column<string>(type: "varchar(50)", nullable: false, collation: "frms_ci_as"),
                    Size = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, collation: "frms_ci_as"),
                    RentalPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, collation: "frms_ci_as")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitType", x => x.UnitTypeId);
                    table.CheckConstraint("CK_UnitType_Mode", "\"Mode\" IN ('PUBLIC','PRIVATE')");
                    table.CheckConstraint("CK_UnitType_RentalPrice", "\"RentalPrice\">=0");
                });

            migrationBuilder.CreateTable(
                name: "UserRole",
                schema: "public",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleName = table.Column<string>(type: "varchar(50)", nullable: false, collation: "frms_ci_as"),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, collation: "frms_ci_as")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRole", x => x.RoleId);
                    table.CheckConstraint("CK_UserRole_RoleName", "\"RoleName\" IN ('CUSTOMER','FACILITY_STAFF','FACILITY_MANAGER','BUSINESS_OPERATIONS_MANAGER','SYSTEM_ADMINISTRATOR')");
                });

            migrationBuilder.CreateTable(
                name: "StorageUnit",
                schema: "public",
                columns: table => new
                {
                    StorageUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitCode = table.Column<string>(type: "varchar(50)", nullable: false, collation: "frms_ci_as"),
                    LocationInfo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, collation: "frms_ci_as"),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: "AVAILABLE", collation: "frms_ci_as")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageUnit", x => x.StorageUnitId);
                    table.CheckConstraint("CK_StorageUnit_Status", "\"Status\" IN ('AVAILABLE','IN_USE','INSPECTION','MAINTENANCE')");
                    table.ForeignKey(
                        name: "FK_StorageUnit_Facility_FacilityId",
                        column: x => x.FacilityId,
                        principalSchema: "public",
                        principalTable: "Facility",
                        principalColumn: "FacilityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StorageUnit_UnitType_UnitTypeId",
                        column: x => x.UnitTypeId,
                        principalSchema: "public",
                        principalTable: "UnitType",
                        principalColumn: "UnitTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserAccount",
                schema: "public",
                columns: table => new
                {
                    UserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "varchar(254)", nullable: false, collation: "frms_ci_as"),
                    PhoneNumber = table.Column<string>(type: "varchar(30)", nullable: false, collation: "frms_ci_as"),
                    PasswordHash = table.Column<string>(type: "varchar(255)", nullable: false, collation: "frms_ci_as"),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: "ACTIVE", collation: "frms_ci_as"),
                    EmailVerifiedAt = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: false, defaultValueSql: "clock_timestamp()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccount", x => x.UserAccountId);
                    table.CheckConstraint("CK_UserAccount_Status", "\"Status\" IN ('ACTIVE','INACTIVE')");
                    table.ForeignKey(
                        name: "FK_UserAccount_UserRole_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "public",
                        principalTable: "UserRole",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                schema: "public",
                columns: table => new
                {
                    AuditLogId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "varchar(150)", nullable: false, collation: "frms_ci_as"),
                    EntityType = table.Column<string>(type: "varchar(150)", nullable: false, collation: "frms_ci_as"),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    OldValue = table.Column<string>(type: "text", nullable: true, collation: "frms_ci_as"),
                    NewValue = table.Column<string>(type: "text", nullable: true, collation: "frms_ci_as"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: false, defaultValueSql: "clock_timestamp()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.AuditLogId);
                    table.CheckConstraint("CK_AuditLog_NewValueJson", "\"NewValue\" IS NULL OR (\"NewValue\" IS JSON OBJECT OR \"NewValue\" IS JSON ARRAY)");
                    table.CheckConstraint("CK_AuditLog_OldValueJson", "\"OldValue\" IS NULL OR (\"OldValue\" IS JSON OBJECT OR \"OldValue\" IS JSON ARRAY)");
                    table.ForeignKey(
                        name: "FK_AuditLog_UserAccount_UserAccountId",
                        column: x => x.UserAccountId,
                        principalSchema: "public",
                        principalTable: "UserAccount",
                        principalColumn: "UserAccountId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Customer",
                schema: "public",
                columns: table => new
                {
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, collation: "frms_ci_as"),
                    Address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, collation: "frms_ci_as"),
                    CCCD = table.Column<string>(type: "varchar(20)", nullable: true, collation: "frms_ci_as")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customer", x => x.CustomerId);
                    table.ForeignKey(
                        name: "FK_Customer_UserAccount_UserAccountId",
                        column: x => x.UserAccountId,
                        principalSchema: "public",
                        principalTable: "UserAccount",
                        principalColumn: "UserAccountId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Employee",
                schema: "public",
                columns: table => new
                {
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uuid", nullable: true),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, collation: "frms_ci_as")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employee", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_Employee_Facility_FacilityId",
                        column: x => x.FacilityId,
                        principalSchema: "public",
                        principalTable: "Facility",
                        principalColumn: "FacilityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employee_UserAccount_UserAccountId",
                        column: x => x.UserAccountId,
                        principalSchema: "public",
                        principalTable: "UserAccount",
                        principalColumn: "UserAccountId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoginHistory",
                schema: "public",
                columns: table => new
                {
                    LoginHistoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginAt = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: false, defaultValueSql: "clock_timestamp()"),
                    IpAddress = table.Column<string>(type: "varchar(45)", nullable: true, collation: "frms_ci_as"),
                    DeviceInfo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, collation: "frms_ci_as"),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false, collation: "frms_ci_as")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginHistory", x => x.LoginHistoryId);
                    table.CheckConstraint("CK_LoginHistory_Status", "\"Status\" IN ('SUCCESS','FAILED')");
                    table.ForeignKey(
                        name: "FK_LoginHistory_UserAccount_UserAccountId",
                        column: x => x.UserAccountId,
                        principalSchema: "public",
                        principalTable: "UserAccount",
                        principalColumn: "UserAccountId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationLog",
                schema: "public",
                columns: table => new
                {
                    NotificationLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationType = table.Column<string>(type: "varchar(50)", nullable: false, collation: "frms_ci_as"),
                    Content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, collation: "frms_ci_as"),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: "PENDING", collation: "frms_ci_as"),
                    SentAt = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: false, defaultValueSql: "clock_timestamp()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationLog", x => x.NotificationLogId);
                    table.CheckConstraint("CK_NotificationLog_SentAt", "(\"Status\"='PENDING' AND \"SentAt\" IS NULL) OR (\"Status\"='SENT' AND \"SentAt\" IS NOT NULL)");
                    table.CheckConstraint("CK_NotificationLog_Status", "\"Status\" IN ('PENDING','SENT')");
                    table.ForeignKey(
                        name: "FK_NotificationLog_UserAccount_UserAccountId",
                        column: x => x.UserAccountId,
                        principalSchema: "public",
                        principalTable: "UserAccount",
                        principalColumn: "UserAccountId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Discount",
                schema: "public",
                columns: table => new
                {
                    DiscountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, collation: "frms_ci_as"),
                    Percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: "ACTIVE", collation: "frms_ci_as"),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discount", x => x.DiscountId);
                    table.CheckConstraint("CK_Discount_Percentage", "\"Percentage\" BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_Discount_Status", "\"Status\" IN ('ACTIVE','INACTIVE')");
                    table.ForeignKey(
                        name: "FK_Discount_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "public",
                        principalTable: "Customer",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reservation",
                schema: "public",
                columns: table => new
                {
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    EndMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    LockedRentalPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: "PENDING_DEPOSIT", collation: "frms_ci_as"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: false, defaultValueSql: "clock_timestamp()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservation", x => x.ReservationId);
                    table.CheckConstraint("CK_Reservation_Amounts", "\"LockedRentalPrice\">=0 AND \"DepositAmount\">=0");
                    table.CheckConstraint("CK_Reservation_Months", "\"EndMonth\">=\"StartMonth\"");
                    table.CheckConstraint("CK_Reservation_Status", "\"Status\" IN ('PENDING_DEPOSIT','CONFIRMED','COMPLETED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_Reservation_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "public",
                        principalTable: "Customer",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_Facility_FacilityId",
                        column: x => x.FacilityId,
                        principalSchema: "public",
                        principalTable: "Facility",
                        principalColumn: "FacilityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_Policy_PolicyId",
                        column: x => x.PolicyId,
                        principalSchema: "public",
                        principalTable: "Policy",
                        principalColumn: "PolicyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_UnitType_UnitTypeId",
                        column: x => x.UnitTypeId,
                        principalSchema: "public",
                        principalTable: "UnitType",
                        principalColumn: "UnitTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Visit",
                schema: "public",
                columns: table => new
                {
                    VisitId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    VisitType = table.Column<string>(type: "varchar(50)", nullable: false, collation: "frms_ci_as"),
                    VisitDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ActualReturnDate = table.Column<DateTime>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: "SCHEDULED", collation: "frms_ci_as")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visit", x => x.VisitId);
                    table.CheckConstraint("CK_Visit_Status", "\"Status\" IN ('SCHEDULED','CHECKED_IN','CHECKED_OUT','CANCELLED')");
                    table.CheckConstraint("CK_Visit_Type", "\"VisitType\" IN ('RESERVATION','ACCESS','RETURN')");
                    table.ForeignKey(
                        name: "FK_Visit_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "public",
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Invoice",
                schema: "public",
                columns: table => new
                {
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceType = table.Column<string>(type: "varchar(50)", nullable: false, collation: "frms_ci_as"),
                    BillingMonth = table.Column<DateOnly>(type: "date", nullable: true),
                    BaseAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    AmountDue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: "UNPAID", collation: "frms_ci_as"),
                    PaidAt = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: false, defaultValueSql: "clock_timestamp()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoice", x => x.InvoiceId);
                    table.CheckConstraint("CK_Invoice_Amounts", "\"BaseAmount\">=0 AND \"DiscountAmount\">=0 AND \"AmountDue\">=0");
                    table.CheckConstraint("CK_Invoice_BillingMonth", "(\"InvoiceType\"='DEPOSIT' AND \"BillingMonth\" IS NULL) OR (\"InvoiceType\"='RENTAL_FEE' AND \"BillingMonth\" IS NOT NULL)");
                    table.CheckConstraint("CK_Invoice_Status", "\"Status\" IN ('UNPAID','PAID','OVERDUE','CANCELLED')");
                    table.CheckConstraint("CK_Invoice_Type", "\"InvoiceType\" IN ('DEPOSIT','RENTAL_FEE')");
                    table.ForeignKey(
                        name: "FK_Invoice_Discount_DiscountId",
                        column: x => x.DiscountId,
                        principalSchema: "public",
                        principalTable: "Discount",
                        principalColumn: "DiscountId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Contract",
                schema: "public",
                columns: table => new
                {
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscountId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    EndMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: "ACTIVE", collation: "frms_ci_as")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contract", x => x.ContractId);
                    table.CheckConstraint("CK_Contract_Months", "\"EndMonth\">=\"StartMonth\"");
                    table.CheckConstraint("CK_Contract_Status", "\"Status\" IN ('ACTIVE','COMPLETED','TERMINATED')");
                    table.ForeignKey(
                        name: "FK_Contract_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "public",
                        principalTable: "Customer",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contract_Discount_DiscountId",
                        column: x => x.DiscountId,
                        principalSchema: "public",
                        principalTable: "Discount",
                        principalColumn: "DiscountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contract_Facility_FacilityId",
                        column: x => x.FacilityId,
                        principalSchema: "public",
                        principalTable: "Facility",
                        principalColumn: "FacilityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contract_Policy_PolicyId",
                        column: x => x.PolicyId,
                        principalSchema: "public",
                        principalTable: "Policy",
                        principalColumn: "PolicyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contract_Reservation_ReservationId",
                        column: x => x.ReservationId,
                        principalSchema: "public",
                        principalTable: "Reservation",
                        principalColumn: "ReservationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contract_StorageUnit_StorageUnitId",
                        column: x => x.StorageUnitId,
                        principalSchema: "public",
                        principalTable: "StorageUnit",
                        principalColumn: "StorageUnitId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LateFee",
                schema: "public",
                columns: table => new
                {
                    LateFeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverdueDays = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LateFee", x => x.LateFeeId);
                    table.CheckConstraint("CK_LateFee_Amount", "\"Amount\">=0");
                    table.CheckConstraint("CK_LateFee_Days", "\"OverdueDays\">=1");
                    table.ForeignKey(
                        name: "FK_LateFee_Invoice_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "public",
                        principalTable: "Invoice",
                        principalColumn: "InvoiceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                schema: "public",
                columns: table => new
                {
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderOrderCode = table.Column<long>(type: "bigint", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "varchar(50)", nullable: false, collation: "frms_ci_as"),
                    TransactionCode = table.Column<string>(type: "varchar(150)", nullable: true, collation: "frms_ci_as"),
                    PaymentUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true, collation: "frms_ci_as"),
                    PaymentUrlExpiresAt = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: "PENDING", collation: "frms_ci_as"),
                    PaidAt = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: false, defaultValueSql: "clock_timestamp()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.PaymentId);
                    table.CheckConstraint("CK_Payment_Amount", "\"Amount\">0");
                    table.CheckConstraint("CK_Payment_Method", "\"PaymentMethod\" IN ('MOMO','VNPAY','PAYOS')");
                    table.CheckConstraint("CK_Payment_PaidAt", "(\"Status\"='SUCCESS' AND \"PaidAt\" IS NOT NULL) OR (\"Status\"<>'SUCCESS' AND \"PaidAt\" IS NULL)");
                    table.CheckConstraint("CK_Payment_Status", "\"Status\" IN ('PENDING','SUCCESS','FAILED')");
                    table.CheckConstraint("CK_Payment_UrlExpiry", "\"PaymentUrlExpiresAt\" IS NULL OR \"PaymentUrl\" IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_Payment_Invoice_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "public",
                        principalTable: "Invoice",
                        principalColumn: "InvoiceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContractExtension",
                schema: "public",
                columns: table => new
                {
                    ContractExtensionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldEndMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    NewEndMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    AppliedMonthlyPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: false, defaultValueSql: "clock_timestamp()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractExtension", x => x.ContractExtensionId);
                    table.CheckConstraint("CK_ContractExtension_Months", "\"NewEndMonth\">\"OldEndMonth\"");
                    table.CheckConstraint("CK_ContractExtension_Price", "\"AppliedMonthlyPrice\">=0");
                    table.ForeignKey(
                        name: "FK_ContractExtension_Contract_ContractId",
                        column: x => x.ContractId,
                        principalSchema: "public",
                        principalTable: "Contract",
                        principalColumn: "ContractId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DepositSettlement",
                schema: "public",
                columns: table => new
                {
                    DepositSettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalDeduction = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RefundAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AdditionalAmountDue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: "PENDING", collation: "frms_ci_as"),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepositSettlement", x => x.DepositSettlementId);
                    table.CheckConstraint("CK_DepositSettlement_Amounts", "\"TotalDeduction\">=0 AND \"RefundAmount\">=0 AND \"AdditionalAmountDue\">=0");
                    table.CheckConstraint("CK_DepositSettlement_Status", "\"Status\" IN ('PENDING','FINALIZED')");
                    table.ForeignKey(
                        name: "FK_DepositSettlement_Contract_ContractId",
                        column: x => x.ContractId,
                        principalSchema: "public",
                        principalTable: "Contract",
                        principalColumn: "ContractId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Inspection",
                schema: "public",
                columns: table => new
                {
                    InspectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: "PENDING", collation: "frms_ci_as"),
                    ConditionNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, collation: "frms_ci_as"),
                    CompletedAt = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspection", x => x.InspectionId);
                    table.CheckConstraint("CK_Inspection_Status", "\"Status\" IN ('PENDING','IN_PROGRESS','COMPLETED')");
                    table.ForeignKey(
                        name: "FK_Inspection_Contract_ContractId",
                        column: x => x.ContractId,
                        principalSchema: "public",
                        principalTable: "Contract",
                        principalColumn: "ContractId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inspection_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "public",
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inspection_StorageUnit_StorageUnitId",
                        column: x => x.StorageUnitId,
                        principalSchema: "public",
                        principalTable: "StorageUnit",
                        principalColumn: "StorageUnitId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inspection_Visit_VisitId",
                        column: x => x.VisitId,
                        principalSchema: "public",
                        principalTable: "Visit",
                        principalColumn: "VisitId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupportTicket",
                schema: "public",
                columns: table => new
                {
                    SupportTicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<string>(type: "varchar(50)", nullable: false, collation: "frms_ci_as"),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, collation: "frms_ci_as"),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: "OPEN", collation: "frms_ci_as"),
                    ResultNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, collation: "frms_ci_as"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: false, defaultValueSql: "clock_timestamp()"),
                    CompletedAt = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTicket", x => x.SupportTicketId);
                    table.CheckConstraint("CK_SupportTicket_Category", "\"Category\" IN ('UNIT_ISSUE','LOCK_KEY_ISSUE','ACCESS_CARD_CODE_ISSUE','PAYMENT_ISSUE','STORED_ITEM_ISSUE','DAMAGE_ISSUE','OTHER')");
                    table.CheckConstraint("CK_SupportTicket_Status", "\"Status\" IN ('OPEN','IN_PROGRESS','COMPLETED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_SupportTicket_Contract_ContractId",
                        column: x => x.ContractId,
                        principalSchema: "public",
                        principalTable: "Contract",
                        principalColumn: "ContractId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportTicket_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "public",
                        principalTable: "Customer",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportTicket_Employee_AssignedEmployeeId",
                        column: x => x.AssignedEmployeeId,
                        principalSchema: "public",
                        principalTable: "Employee",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DamageRecord",
                schema: "public",
                columns: table => new
                {
                    DamageRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DamageTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DamageAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, collation: "frms_ci_as"),
                    Status = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: "PENDING", collation: "frms_ci_as"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: false, defaultValueSql: "clock_timestamp()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DamageRecord", x => x.DamageRecordId);
                    table.CheckConstraint("CK_DamageRecord_Amount", "\"DamageAmount\">=0");
                    table.CheckConstraint("CK_DamageRecord_Status", "\"Status\" IN ('PENDING','APPROVED','REJECTED')");
                    table.ForeignKey(
                        name: "FK_DamageRecord_DamageType_DamageTypeId",
                        column: x => x.DamageTypeId,
                        principalSchema: "public",
                        principalTable: "DamageType",
                        principalColumn: "DamageTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DamageRecord_Inspection_InspectionId",
                        column: x => x.InspectionId,
                        principalSchema: "public",
                        principalTable: "Inspection",
                        principalColumn: "InspectionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExtraFee",
                schema: "public",
                columns: table => new
                {
                    ExtraFeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExtraFeeTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, collation: "frms_ci_as"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: false, defaultValueSql: "clock_timestamp()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtraFee", x => x.ExtraFeeId);
                    table.CheckConstraint("CK_ExtraFee_Amount", "\"Amount\">=0");
                    table.ForeignKey(
                        name: "FK_ExtraFee_ExtraFeeType_ExtraFeeTypeId",
                        column: x => x.ExtraFeeTypeId,
                        principalSchema: "public",
                        principalTable: "ExtraFeeType",
                        principalColumn: "ExtraFeeTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExtraFee_Inspection_InspectionId",
                        column: x => x.InspectionId,
                        principalSchema: "public",
                        principalTable: "Inspection",
                        principalColumn: "InspectionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InspectionEvidence",
                schema: "public",
                columns: table => new
                {
                    InspectionEvidenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileData = table.Column<byte[]>(type: "bytea", nullable: false),
                    EvidenceType = table.Column<string>(type: "varchar(50)", nullable: false, collation: "frms_ci_as"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp(3) with time zone", nullable: false, defaultValueSql: "clock_timestamp()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionEvidence", x => x.InspectionEvidenceId);
                    table.CheckConstraint("CK_InspectionEvidence_Type", "\"EvidenceType\" IN ('IMAGE','VIDEO','DOCUMENT')");
                    table.ForeignKey(
                        name: "FK_InspectionEvidence_Inspection_InspectionId",
                        column: x => x.InspectionId,
                        principalSchema: "public",
                        principalTable: "Inspection",
                        principalColumn: "InspectionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "public",
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
                schema: "public",
                table: "Policy",
                columns: new[] { "PolicyId", "DepositTimeoutHours", "EarlyReturnWaiveFeeUntilDay", "EffectiveTo", "LateFeeDivisorDays", "MonthlyPaymentDueDay", "OverdueStartDay", "ReservationVisitEndDay", "ReservationVisitStartDay", "Status", "Version" },
                values: new object[] { new Guid("20000000-0000-0000-0000-000000000001"), 1, 5, null, 31, 5, 6, 5, 1, "ACTIVE", 1 });

            migrationBuilder.InsertData(
                schema: "public",
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
                schema: "public",
                table: "AuditLog",
                column: "UserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Contract_CustomerId",
                schema: "public",
                table: "Contract",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Contract_DiscountId",
                schema: "public",
                table: "Contract",
                column: "DiscountId");

            migrationBuilder.CreateIndex(
                name: "IX_Contract_FacilityId",
                schema: "public",
                table: "Contract",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Contract_PolicyId",
                schema: "public",
                table: "Contract",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Contract_ReservationId",
                schema: "public",
                table: "Contract",
                column: "ReservationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contract_StorageUnitId",
                schema: "public",
                table: "Contract",
                column: "StorageUnitId",
                unique: true,
                filter: "\"Status\" = 'ACTIVE'");

            migrationBuilder.CreateIndex(
                name: "IX_ContractExtension_ContractId",
                schema: "public",
                table: "ContractExtension",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_CCCD",
                schema: "public",
                table: "Customer",
                column: "CCCD",
                unique: true,
                filter: "\"CCCD\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_UserAccountId",
                schema: "public",
                table: "Customer",
                column: "UserAccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DamageRecord_DamageTypeId",
                schema: "public",
                table: "DamageRecord",
                column: "DamageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DamageRecord_InspectionId",
                schema: "public",
                table: "DamageRecord",
                column: "InspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_DamageType_Name",
                schema: "public",
                table: "DamageType",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DepositSettlement_ContractId",
                schema: "public",
                table: "DepositSettlement",
                column: "ContractId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Discount_CustomerId",
                schema: "public",
                table: "Discount",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_FacilityId",
                schema: "public",
                table: "Employee",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_UserAccountId",
                schema: "public",
                table: "Employee",
                column: "UserAccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExtraFee_ExtraFeeTypeId",
                schema: "public",
                table: "ExtraFee",
                column: "ExtraFeeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtraFee_InspectionId",
                schema: "public",
                table: "ExtraFee",
                column: "InspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtraFeeType_Name",
                schema: "public",
                table: "ExtraFeeType",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inspection_ContractId",
                schema: "public",
                table: "Inspection",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspection_EmployeeId",
                schema: "public",
                table: "Inspection",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspection_StorageUnitId",
                schema: "public",
                table: "Inspection",
                column: "StorageUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspection_VisitId",
                schema: "public",
                table: "Inspection",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionEvidence_InspectionId",
                schema: "public",
                table: "InspectionEvidence",
                column: "InspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_DiscountId",
                schema: "public",
                table: "Invoice",
                column: "DiscountId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_EntityId",
                schema: "public",
                table: "Invoice",
                column: "EntityId",
                unique: true,
                filter: "\"InvoiceType\" = 'DEPOSIT'");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_EntityId_BillingMonth",
                schema: "public",
                table: "Invoice",
                columns: new[] { "EntityId", "BillingMonth" },
                unique: true,
                filter: "\"InvoiceType\" = 'RENTAL_FEE'");

            migrationBuilder.CreateIndex(
                name: "IX_LateFee_InvoiceId",
                schema: "public",
                table: "LateFee",
                column: "InvoiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistory_UserAccountId",
                schema: "public",
                table: "LoginHistory",
                column: "UserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLog_UserAccountId",
                schema: "public",
                table: "NotificationLog",
                column: "UserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_IdempotencyKey",
                schema: "public",
                table: "Payment",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payment_InvoiceId_Status_CreatedAt",
                schema: "public",
                table: "Payment",
                columns: new[] { "InvoiceId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Payment_ProviderOrderCode",
                schema: "public",
                table: "Payment",
                column: "ProviderOrderCode",
                unique: true,
                filter: "\"ProviderOrderCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_TransactionCode",
                schema: "public",
                table: "Payment",
                column: "TransactionCode",
                unique: true,
                filter: "\"TransactionCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Policy_Status",
                schema: "public",
                table: "Policy",
                column: "Status",
                unique: true,
                filter: "\"Status\" = 'ACTIVE'");

            migrationBuilder.CreateIndex(
                name: "IX_Policy_Version",
                schema: "public",
                table: "Policy",
                column: "Version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_CustomerId",
                schema: "public",
                table: "Reservation",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_FacilityId",
                schema: "public",
                table: "Reservation",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_PolicyId",
                schema: "public",
                table: "Reservation",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_UnitTypeId",
                schema: "public",
                table: "Reservation",
                column: "UnitTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageUnit_FacilityId",
                schema: "public",
                table: "StorageUnit",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageUnit_UnitTypeId",
                schema: "public",
                table: "StorageUnit",
                column: "UnitTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicket_AssignedEmployeeId",
                schema: "public",
                table: "SupportTicket",
                column: "AssignedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicket_ContractId",
                schema: "public",
                table: "SupportTicket",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicket_CustomerId",
                schema: "public",
                table: "SupportTicket",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccount_Email",
                schema: "public",
                table: "UserAccount",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccount_PhoneNumber",
                schema: "public",
                table: "UserAccount",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccount_RoleId",
                schema: "public",
                table: "UserAccount",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_RoleName",
                schema: "public",
                table: "UserRole",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Visit_EmployeeId",
                schema: "public",
                table: "Visit",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $guard$ BEGIN
                  IF
                    EXISTS (SELECT 1 FROM public."UserAccount") OR
                    EXISTS (SELECT 1 FROM public."Customer") OR
                    EXISTS (SELECT 1 FROM public."Employee") OR
                    EXISTS (SELECT 1 FROM public."Facility") OR
                    EXISTS (SELECT 1 FROM public."UnitType") OR
                    EXISTS (SELECT 1 FROM public."StorageUnit") OR
                    EXISTS (SELECT 1 FROM public."Reservation") OR
                    EXISTS (SELECT 1 FROM public."Visit") OR
                    EXISTS (SELECT 1 FROM public."Contract") OR
                    EXISTS (SELECT 1 FROM public."ContractExtension") OR
                    EXISTS (SELECT 1 FROM public."Invoice") OR
                    EXISTS (SELECT 1 FROM public."Payment") OR
                    EXISTS (SELECT 1 FROM public."LateFee") OR
                    EXISTS (SELECT 1 FROM public."Discount") OR
                    EXISTS (SELECT 1 FROM public."SupportTicket") OR
                    EXISTS (SELECT 1 FROM public."Inspection") OR
                    EXISTS (SELECT 1 FROM public."DamageRecord") OR
                    EXISTS (SELECT 1 FROM public."InspectionEvidence") OR
                    EXISTS (SELECT 1 FROM public."ExtraFeeType") OR
                    EXISTS (SELECT 1 FROM public."ExtraFee") OR
                    EXISTS (SELECT 1 FROM public."DepositSettlement") OR
                    EXISTS (SELECT 1 FROM public."LoginHistory") OR
                    EXISTS (SELECT 1 FROM public."AuditLog") OR
                    EXISTS (SELECT 1 FROM public."NotificationLog")
                    OR (SELECT count(*) FROM public."Policy") > 1 THEN
                    RAISE EXCEPTION 'Cannot drop PostgreSQL schema while business/history data exists';
                  END IF;
                END $guard$;
                """);

            migrationBuilder.DropTable(
                name: "AuditLog",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ContractExtension",
                schema: "public");

            migrationBuilder.DropTable(
                name: "DamageRecord",
                schema: "public");

            migrationBuilder.DropTable(
                name: "DepositSettlement",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ExtraFee",
                schema: "public");

            migrationBuilder.DropTable(
                name: "InspectionEvidence",
                schema: "public");

            migrationBuilder.DropTable(
                name: "LateFee",
                schema: "public");

            migrationBuilder.DropTable(
                name: "LoginHistory",
                schema: "public");

            migrationBuilder.DropTable(
                name: "NotificationLog",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Payment",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SupportTicket",
                schema: "public");

            migrationBuilder.DropTable(
                name: "DamageType",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ExtraFeeType",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Inspection",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Invoice",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Contract",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Visit",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Discount",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Reservation",
                schema: "public");

            migrationBuilder.DropTable(
                name: "StorageUnit",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Employee",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Customer",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Policy",
                schema: "public");

            migrationBuilder.DropTable(
                name: "UnitType",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Facility",
                schema: "public");

            migrationBuilder.DropTable(
                name: "UserAccount",
                schema: "public");

            migrationBuilder.DropTable(
                name: "UserRole",
                schema: "public");

            migrationBuilder.DropSequence(
                name: "ProviderOrderCodeSequence",
                schema: "public");
        }
    }
}
