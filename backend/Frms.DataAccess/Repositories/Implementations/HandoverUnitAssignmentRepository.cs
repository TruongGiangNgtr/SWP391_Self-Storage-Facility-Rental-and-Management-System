using System.Data;
using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using Frms.DataAccess.StoredProcedures;
using Npgsql;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class HandoverUnitAssignmentRepository(FrmsDbContext dbContext)
    : IHandoverUnitAssignmentRepository {
    public Task<Reservation?> GetReservationAsync(Guid reservationId, CancellationToken cancellationToken) =>
        dbContext.Reservations.AsNoTracking().SingleOrDefaultAsync(
            x => x.ReservationId == reservationId, cancellationToken);

    public Task<HandoverUnitAssignmentRecord?> GetActiveAsync(Guid reservationId, CancellationToken cancellationToken) =>
        (from assignment in dbContext.HandoverUnitAssignments.AsNoTracking()
         join unit in dbContext.StorageUnits.AsNoTracking()
             on assignment.StorageUnitId equals unit.StorageUnitId
         where assignment.ReservationId == reservationId && assignment.Status == "ACTIVE"
         select new HandoverUnitAssignmentRecord(
             assignment.AssignmentId, assignment.ReservationId, assignment.StorageUnitId,
             unit.UnitCode, unit.LocationInfo, assignment.AssignedByEmployeeId,
             assignment.AssignedAt, assignment.Status))
        .SingleOrDefaultAsync(cancellationToken);

    public Task AssignAsync(Guid reservationId, Guid storageUnitId, Guid userAccountId,
        CancellationToken cancellationToken) => RunAsync(
            "SELECT public.\"usp_AssignHandoverUnit\"(@reservationId,@storageUnitId,@userAccountId)",
            reservationId, storageUnitId, userAccountId, cancellationToken);

    public Task ReleaseAsync(Guid reservationId, Guid userAccountId,
        CancellationToken cancellationToken) => RunAsync(
            "SELECT public.\"usp_ReleaseHandoverUnit\"(@reservationId,@userAccountId)",
            reservationId, null, userAccountId, cancellationToken);

    private async Task RunAsync(string sql, Guid reservationId, Guid? storageUnitId,
        Guid userAccountId, CancellationToken cancellationToken) {
        var connection = dbContext.Database.GetDbConnection();
        var ownsConnection = connection.State != ConnectionState.Open;
        try {
            if (ownsConnection) await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = sql;
            AddGuid(command, "@reservationId", reservationId);
            if (storageUnitId.HasValue) AddGuid(command, "@storageUnitId", storageUnitId.Value);
            AddGuid(command, "@userAccountId", userAccountId);
            try {
                await command.ExecuteScalarAsync(cancellationToken);
            }
            catch (PostgresException ex) when (ex.MessageText is
                "FORBIDDEN" or "RESOURCE_NOT_FOUND" or "RESERVATION_INVALID_STATUS" or
                "VISIT_INVALID_STATUS" or "UNIT_FACILITY_TYPE_MISMATCH" or
                "UNIT_NOT_AVAILABLE" or "HANDOVER_UNIT_ALREADY_ASSIGNED") {
                throw new StoredProcedureBusinessException(ex.MessageText, ex.MessageText);
            }
        }
        finally {
            if (ownsConnection && connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }
    }

    private static void AddGuid(System.Data.Common.DbCommand command, string name, Guid value) {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = DbType.Guid;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
