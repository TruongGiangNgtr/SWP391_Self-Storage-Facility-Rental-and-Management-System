using Frms.DataAccess.Persistence.Configurations;
using Frms.DataAccess.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Persistence;

public sealed class FrmsDbContext(DbContextOptions<FrmsDbContext> options) : DbContext(options)
{
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<UnitType> UnitTypes => Set<UnitType>();
    public DbSet<StorageUnit> StorageUnits => Set<StorageUnit>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractExtension> ContractExtensions => Set<ContractExtension>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<LateFee> LateFees => Set<LateFee>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<Inspection> Inspections => Set<Inspection>();
    public DbSet<DamageType> DamageTypes => Set<DamageType>();
    public DbSet<DamageRecord> DamageRecords => Set<DamageRecord>();
    public DbSet<InspectionEvidence> InspectionEvidence => Set<InspectionEvidence>();
    public DbSet<ExtraFeeType> ExtraFeeTypes => Set<ExtraFeeType>();
    public DbSet<ExtraFee> ExtraFees => Set<ExtraFee>();
    public DbSet<DepositSettlement> DepositSettlements => Set<DepositSettlement>();
    public DbSet<LoginHistory> LoginHistory => Set<LoginHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyFrmsConfiguration();
}
