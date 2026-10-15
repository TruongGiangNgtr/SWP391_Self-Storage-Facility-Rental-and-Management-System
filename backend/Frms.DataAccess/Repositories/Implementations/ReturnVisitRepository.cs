using System.Data;
using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Frms.DataAccess.StoredProcedures;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class ReturnVisitRepository(FrmsDbContext db) : IReturnVisitRepository
{
    public Task<ReturnContractRecord?> GetContractAsync(Guid id, CancellationToken ct) =>
        db.Contracts.AsNoTracking().Where(x => x.ContractId == id)
            .Select(x => new ReturnContractRecord(x.ContractId, x.CustomerId, x.FacilityId, x.Status)).SingleOrDefaultAsync(ct);

    public Task<ReturnVisitRecord?> GetOwnedVisitAsync(Guid owner, Guid id, CancellationToken ct) =>
        (from v in db.Visits.AsNoTracking()
         join c in db.Contracts.AsNoTracking() on v.EntityId equals c.ContractId
         where v.VisitId == id && v.VisitType == "RETURN" && c.CustomerId == owner
         select new ReturnVisitRecord(v.VisitId, v.EntityId, v.EmployeeId, v.VisitType, v.VisitDate,
             v.ActualReturnDate, v.Status)).SingleOrDefaultAsync(ct);

    public Task<ReturnVisitRecord> CreateAsync(Guid account, Guid owner, Guid contract, DateOnly date,
        DateTimeOffset now, CancellationToken ct) => ExecuteVisitAsync("usp_CreateReturnVisit", "@ContractId", contract,
            account, owner, date, now, ct);
    public Task<ReturnVisitRecord> RescheduleAsync(Guid account, Guid owner, Guid visit, DateOnly date,
        DateTimeOffset now, CancellationToken ct) => ExecuteVisitAsync("usp_RescheduleReturnVisit", "@VisitId", visit,
            account, owner, date, now, ct);

    private async Task<ReturnVisitRecord> ExecuteVisitAsync(string procedure, string idName, Guid id,
        Guid account, Guid owner, DateOnly date, DateTimeOffset now, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        var mustClose = connection.State != ConnectionState.Open;
        try
        {
            if (mustClose) await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = procedure;
            command.CommandType = CommandType.StoredProcedure;
            foreach (var (name, type, value) in new (string, DbType, object)[] {
                (idName, DbType.Guid, id), ("@UserAccountId", DbType.Guid, account),
                ("@CustomerId", DbType.Guid, owner), ("@VisitDate", DbType.Date, date.ToDateTime(TimeOnly.MinValue)),
                ("@NowUtc", DbType.DateTime2, now.UtcDateTime) })
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name; parameter.DbType = type; parameter.Value = value;
                command.Parameters.Add(parameter);
            }
            await using var reader = await PostgreSqlRoutine.ExecuteReaderAsync(command, ct);
            if (!await reader.ReadAsync(ct)) throw new InvalidOperationException("RETURN procedure returned no Visit.");
            return new(reader.GetGuid(reader.GetOrdinal("VisitId")), reader.GetGuid(reader.GetOrdinal("EntityId")),
                reader.IsDBNull(reader.GetOrdinal("EmployeeId")) ? null : reader.GetGuid(reader.GetOrdinal("EmployeeId")),
                reader.GetString(reader.GetOrdinal("VisitType")), DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("VisitDate"))),
                reader.IsDBNull(reader.GetOrdinal("ActualReturnDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ActualReturnDate")),
                reader.GetString(reader.GetOrdinal("Status")));
        }
        catch (PostgresException ex) when (PostgreSqlErrors.Number(ex) is >= 51301 and <= 51308)
        {
            var code = PostgreSqlErrors.Number(ex) switch
            {
                51301 => "RESOURCE_NOT_FOUND",
                51302 => "ACCOUNT_INACTIVE",
                51303 => "FORBIDDEN",
                51304 => "CONTRACT_NOT_ACTIVE",
                51305 => "RETURN_VISIT_PENDING",
                51306 => "VISIT_DATE_IN_PAST",
                51307 => "VISIT_INVALID_STATUS",
                _ => "VALIDATION_ERROR"
            };
            throw new StoredProcedureBusinessException(code, code);
        }
        finally { if (mustClose && connection.State == ConnectionState.Open) await connection.CloseAsync(); }
    }

    public async Task<ContractReturnRecord?> GetSummaryAsync(Guid id, BillingReadScope scope, CancellationToken ct)
    {
        // Hold the root while reading nested snapshots, preventing a mixed pre/post-finalize summary.
        await using var transaction = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct) : null;
        var contract = await db.Contracts.AsNoTracking().SingleOrDefaultAsync(x => x.ContractId == id
            && (!scope.CustomerId.HasValue || x.CustomerId == scope.CustomerId)
            && (!scope.FacilityId.HasValue || x.FacilityId == scope.FacilityId), ct);
        if (contract is null) return null;

        // Owner delegated the presentation choice: unfinished first, then most recently completed.
        // UUID only breaks equal timestamps; it is never treated as a creation timestamp.
        var selection = await (from i in db.Inspections.AsNoTracking()
                               join v in db.Visits.AsNoTracking() on i.VisitId equals (Guid?)v.VisitId into linked
                               from v in linked.DefaultIfEmpty()
                               where i.ContractId == id && i.StorageUnitId == contract.StorageUnitId
                                   && (i.VisitId == null || (v != null && v.VisitType == "RETURN" && v.EntityId == id))
                               orderby (i.Status != "COMPLETED") descending, i.CompletedAt descending,
                                   v.ActualReturnDate descending, v.VisitDate descending, i.InspectionId
                               select new { Inspection = i, Visit = v }).FirstOrDefaultAsync(ct);
        ReturnVisitRecord? visit = null;
        ReturnInspectionRecord? inspection = null;
        IReadOnlyList<ReturnDamageRecord> damages = [];
        IReadOnlyList<ReturnFeeRecord> fees = [];
        if (selection is not null)
        {
            var row = selection.Inspection;
            inspection = new(row.InspectionId, row.Status, row.ConditionNote, row.CompletedAt);
            if (selection.Visit is { } linkedVisit) visit = Map(linkedVisit);
            damages = await db.DamageRecords.AsNoTracking().Where(x => x.InspectionId == row.InspectionId)
                .OrderBy(x => x.CreatedAt).ThenBy(x => x.DamageRecordId)
                .Select(x => new ReturnDamageRecord(x.DamageRecordId, x.InspectionId, x.DamageTypeId,
                    x.DamageAmount, x.Note, x.Status, x.CreatedAt)).ToListAsync(ct);
            fees = await db.ExtraFees.AsNoTracking().Where(x => x.InspectionId == row.InspectionId)
                .OrderBy(x => x.CreatedAt).ThenBy(x => x.ExtraFeeId)
                .Select(x => new ReturnFeeRecord(x.ExtraFeeId, x.InspectionId, x.ExtraFeeTypeId,
                    x.Amount, x.Reason, x.CreatedAt)).ToListAsync(ct);
        }
        else
        {
            var scheduled = await db.Visits.AsNoTracking().Where(x => x.EntityId == id && x.VisitType == "RETURN")
                .OrderByDescending(x => x.Status == "SCHEDULED" || x.Status == "CHECKED_IN")
                .ThenByDescending(x => x.ActualReturnDate).ThenByDescending(x => x.VisitDate).ThenBy(x => x.VisitId)
                .FirstOrDefaultAsync(ct);
            if (scheduled is not null) visit = Map(scheduled);
        }
        var settlement = await db.DepositSettlements.AsNoTracking().Where(x => x.ContractId == id)
            .Select(x => new SavedReturnSettlementRecord(x.DepositSettlementId, x.TotalDeduction,
                x.RefundAmount, x.AdditionalAmountDue, x.Status)).SingleOrDefaultAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return new(id, contract.Status, visit, inspection, damages, fees, settlement);
    }
    private static ReturnVisitRecord Map(Persistence.Entities.Visit v) =>
        new(v.VisitId, v.EntityId, v.EmployeeId, v.VisitType, v.VisitDate, v.ActualReturnDate, v.Status);
}
