using System.Data;
using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class InspectionWorkflowRepository(
    FrmsDbContext db, ReturnSqlExecutor sql) : IInspectionWorkflowRepository
{
    public Task<Guid?> GetEmployeeIdAsync(Guid userAccountId, Guid facilityId, CancellationToken ct)
        => db.Employees.AsNoTracking()
            .Where(e => e.UserAccountId == userAccountId && e.FacilityId == facilityId)
            .Select(e => (Guid?)e.EmployeeId).SingleOrDefaultAsync(ct);

    public Task<Guid?> GetInspectionFacilityIdAsync(Guid inspectionId, CancellationToken ct)
        => (from i in db.Inspections.AsNoTracking()
            join c in db.Contracts.AsNoTracking() on i.ContractId equals c.ContractId
            where i.InspectionId == inspectionId
            select (Guid?)c.FacilityId).SingleOrDefaultAsync(ct);

    private IQueryable<Frms.DataAccess.Persistence.Entities.Inspection> Scoped(Guid facilityId)
        => from i in db.Inspections.AsNoTracking()
           join c in db.Contracts.AsNoTracking() on i.ContractId equals c.ContractId
           where c.FacilityId == facilityId
           select i;

    private static InspectionRecord Map(Frms.DataAccess.Persistence.Entities.Inspection i)
        => new(i.InspectionId, i.ContractId, i.StorageUnitId, i.VisitId,
            i.EmployeeId, i.Status, i.ConditionNote, i.CompletedAt);

    public async Task<(IReadOnlyList<InspectionRecord> Items, int Total)> ListAsync(
        Guid facilityId, int skip, int take, CancellationToken ct)
    {
        var query = Scoped(facilityId);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.Status == "PENDING")
            .ThenBy(x => x.InspectionId).Skip(skip).Take(take).ToListAsync(ct);
        return (rows.Select(Map).ToArray(), total);
    }

    public async Task<InspectionWorkflowDetailRecord?> GetDetailAsync(
        Guid inspectionId, Guid facilityId, CancellationToken ct)
    {
        var item = await Scoped(facilityId)
            .SingleOrDefaultAsync(x => x.InspectionId == inspectionId, ct);
        if (item is null) return null;
        var damages = await db.DamageRecords.AsNoTracking()
            .Where(x => x.InspectionId == inspectionId)
            .Select(x => new DamageRecordItem(x.DamageRecordId, x.DamageTypeId,
                x.DamageAmount, x.Note, x.Status)).ToListAsync(ct);
        var fees = await db.ExtraFees.AsNoTracking()
            .Where(x => x.InspectionId == inspectionId)
            .Select(x => new ExtraFeeRecordItem(x.ExtraFeeId, x.ExtraFeeTypeId,
                x.Amount, x.Reason)).ToListAsync(ct);
        var evidence = await db.InspectionEvidence.AsNoTracking()
            .Where(x => x.InspectionId == inspectionId)
            .Select(x => new EvidenceRecordItem(x.InspectionEvidenceId,
                x.EvidenceType, x.CreatedAt)).ToListAsync(ct);
        return new InspectionWorkflowDetailRecord(Map(item), damages, fees, evidence);
    }

    public async Task<IReadOnlyList<(Guid Id, string Name, decimal? DefaultAmount, string Status)>> GetDamageTypesAsync(CancellationToken ct)
    {
        var data = await db.DamageTypes.AsNoTracking()
            .Where(x => x.Status == "ACTIVE").OrderBy(x => x.Name)
            .Select(x => new { Id = x.DamageTypeId, x.Name, x.DefaultAmount, x.Status })
            .ToListAsync(ct);
        return data.Select(x => (x.Id, x.Name, x.DefaultAmount, x.Status)).ToArray();
    }

    private async Task<InspectionRecord> ReadAsync(Guid id, CancellationToken ct)
        => Map(await db.Inspections.AsNoTracking()
            .SingleAsync(x => x.InspectionId == id, ct));

    public async Task<InspectionRecord> ClaimAsync(Guid id, Guid staffId, CancellationToken ct)
    {
        await sql.ExecuteAsync("usp_ClaimInspection", ct,
            ReturnSqlExecutor.GuidParam("@InspectionId", id),
            ReturnSqlExecutor.GuidParam("@EmployeeId", staffId));
        return await ReadAsync(id, ct);
    }

    public async Task<DamageRecordItem> RecordDamageAsync(
        Guid id, Guid typeId, decimal amount, string? note, Guid staffId, CancellationToken ct)
    {
        var before = await db.DamageRecords.AsNoTracking()
            .Where(x => x.InspectionId == id).Select(x => x.DamageRecordId).ToListAsync(ct);
        await sql.ExecuteAsync("usp_RecordDamage", ct,
            ReturnSqlExecutor.GuidParam("@InspectionId", id),
            ReturnSqlExecutor.GuidParam("@DamageTypeId", typeId),
            ReturnSqlExecutor.DecimalParam("@DamageAmount", amount),
            ReturnSqlExecutor.TextParam("@Note", note));
        var row = await db.DamageRecords.AsNoTracking()
            .Where(x => x.InspectionId == id && !before.Contains(x.DamageRecordId))
            .OrderByDescending(x => x.CreatedAt).FirstAsync(ct);
        return new(row.DamageRecordId, row.DamageTypeId, row.DamageAmount, row.Note, row.Status);
    }

    public async Task<ExtraFeeRecordItem> RecordExtraFeeAsync(
        Guid id, Guid typeId, decimal amount, string reason, Guid staffId, CancellationToken ct)
    {
        var before = await db.ExtraFees.AsNoTracking()
            .Where(x => x.InspectionId == id).Select(x => x.ExtraFeeId).ToListAsync(ct);
        await sql.ExecuteAsync("usp_RecordExtraFee", ct,
            ReturnSqlExecutor.GuidParam("@InspectionId", id),
            ReturnSqlExecutor.GuidParam("@ExtraFeeTypeId", typeId),
            ReturnSqlExecutor.DecimalParam("@Amount", amount),
            ReturnSqlExecutor.TextParam("@Reason", reason));
        var row = await db.ExtraFees.AsNoTracking()
            .Where(x => x.InspectionId == id && !before.Contains(x.ExtraFeeId))
            .OrderByDescending(x => x.CreatedAt).FirstAsync(ct);
        return new(row.ExtraFeeId, row.ExtraFeeTypeId, row.Amount, row.Reason);
    }

    public async Task<EvidenceRecordItem> AddEvidenceAsync(
        Guid id, byte[] bytes, string evidenceType, Guid staffId, CancellationToken ct)
    {
        var before = await db.InspectionEvidence.AsNoTracking()
            .Where(x => x.InspectionId == id).Select(x => x.InspectionEvidenceId).ToListAsync(ct);
        await sql.ExecuteAsync("usp_AddInspectionEvidence", ct,
            ReturnSqlExecutor.GuidParam("@InspectionId", id),
            ("@FileData", DbType.Binary, bytes),
            ReturnSqlExecutor.TextParam("@EvidenceType", evidenceType));
        var row = await db.InspectionEvidence.AsNoTracking()
            .Where(x => x.InspectionId == id && !before.Contains(x.InspectionEvidenceId))
            .OrderByDescending(x => x.CreatedAt).FirstAsync(ct);
        return new(row.InspectionEvidenceId, row.EvidenceType, row.CreatedAt);
    }

    public async Task<InspectionRecord?> CompleteAsync(
        Guid id, string note, Guid staffId, CancellationToken ct) {
        var completedAtUtc = DateTime.UtcNow;

        await using var transaction =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, ct);

        var updated = await db.Inspections
            .Where(i =>
                i.InspectionId == id &&
                i.Status == "IN_PROGRESS" &&
                i.EmployeeId == staffId &&
                db.Contracts.Any(c =>
                    c.ContractId == i.ContractId &&
                    c.Status == "ACTIVE" &&
                    c.StorageUnitId == i.StorageUnitId &&
                    db.StorageUnits.Any(u =>
                        u.StorageUnitId == i.StorageUnitId &&
                        u.FacilityId == c.FacilityId &&
                        u.Status == "INSPECTION") &&
                    db.Employees.Any(e =>
                        e.EmployeeId == staffId &&
                        e.FacilityId == c.FacilityId &&
                        db.UserAccounts.Any(a =>
                            a.UserAccountId == e.UserAccountId &&
                            a.Status == "ACTIVE"))))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(i => i.Status, "COMPLETED")
                .SetProperty(i => i.ConditionNote, note)
                .SetProperty(i => i.CompletedAt, completedAtUtc), ct);

        if (updated != 1)
            return null;

        var inspection = await ReadAsync(id, ct);

        var unitStillInspecting =
            await db.StorageUnits.AsNoTracking()
                .AnyAsync(u =>
                    u.StorageUnitId == inspection.StorageUnitId &&
                    u.Status == "INSPECTION", ct);

        if (!unitStillInspecting)
            throw new InvalidOperationException(
                "INS-007: StorageUnit must remain INSPECTION.");

        await transaction.CommitAsync(ct);
        return inspection;
    }
}
