using Frms.DataAccess.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frms.Migrations.PostgreSql.Persistence.Configurations;

internal static class HandoverAssignmentModelConfiguration {
    internal static void Configure(ModelBuilder modelBuilder) {
        modelBuilder.Entity<HandoverUnitAssignment>(e => {
            e.ToTable("HandoverUnitAssignment", t => {
                t.HasCheckConstraint("CK_HandoverUnitAssignment_Status", "\"Status\" IN ('ACTIVE','REPLACED','CONSUMED','RELEASED')");
                t.HasCheckConstraint("CK_HandoverUnitAssignment_ClosedAt", "(\"Status\"='ACTIVE') = (\"ClosedAt\" IS NULL)");
            });
            e.HasKey(x => x.AssignmentId);
            e.HasOne<Reservation>().WithMany().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<StorageUnit>().WithMany().HasForeignKey(x => x.StorageUnitId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Employee>().WithMany().HasForeignKey(x => x.AssignedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.AssignedAt).HasColumnType("timestamp(3) with time zone").HasDefaultValueSql("clock_timestamp()");
            e.Property(x => x.ClosedAt).HasColumnType("timestamp(3) with time zone");
            e.Property(x => x.Status).HasColumnType("varchar(12)").HasDefaultValue("ACTIVE");
            e.HasIndex(x => x.ReservationId).IsUnique().HasFilter("\"Status\"='ACTIVE'").HasDatabaseName("UX_HandoverUnitAssignment_ActiveReservation");
            e.HasIndex(x => x.StorageUnitId).IsUnique().HasFilter("\"Status\"='ACTIVE'").HasDatabaseName("UX_HandoverUnitAssignment_ActiveUnit");
            e.HasIndex(x => new { x.ReservationId, x.AssignedAt }).HasDatabaseName("IX_HandoverUnitAssignment_History").IsDescending(false,true);
        });
    }
}
