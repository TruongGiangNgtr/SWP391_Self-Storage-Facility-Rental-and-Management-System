using System.Data;
using Frms.DataAccess.StoredProcedures;
using Microsoft.Data.SqlClient;
using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class InspectionRepository(FrmsDbContext dbContext) : IInspectionRepository
{
    public Task<Guid?> GetDamageFacilityIdAsync(Guid damageRecordId, CancellationToken cancellationToken) =>
        (from damage in dbContext.DamageRecords.AsNoTracking()
         join inspection in dbContext.Inspections.AsNoTracking()
             on damage.InspectionId equals inspection.InspectionId
         join contract in dbContext.Contracts.AsNoTracking()
             on inspection.ContractId equals contract.ContractId
         where damage.DamageRecordId == damageRecordId
         select (Guid?)contract.FacilityId)
        .SingleOrDefaultAsync(cancellationToken);

    public Task<Guid?> GetEmployeeIdByUserAccountIdAsync(Guid userAccountId,
        CancellationToken cancellationToken) =>
        dbContext.Employees.AsNoTracking()
            .Where(e => e.UserAccountId == userAccountId)
            .Select(e => (Guid?)e.EmployeeId)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<DamageDecisionRecord> DecideDamageAsync(
        Guid damageRecordId, Guid managerEmployeeId, string decision,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        try
        {
            if (shouldClose) await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.usp_DecideDamage";
            command.CommandType = CommandType.StoredProcedure;
            AddGuid(command, "@DamageRecordId", damageRecordId);
            AddGuid(command, "@ManagerEmployeeId", managerEmployeeId);
            var decisionParam = command.CreateParameter();
            decisionParam.ParameterName = "@Decision";
            decisionParam.DbType = DbType.AnsiString;
            decisionParam.Size = 50;
            decisionParam.Value = decision;
            command.Parameters.Add(decisionParam);

            // Procedure returns one row (DamageRecordId, Status) after committing.
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("usp_DecideDamage returned no decision row.");
            return new DamageDecisionRecord(reader.GetGuid(0), reader.GetString(1));
        }
        catch (SqlException ex) when (ex.Number is 51209 or 51210 or 51147)
        {
            var code = ex.Number switch
            {
                51209 => "DAMAGE_INVALID_STATUS",
                51210 => "DAMAGE_RECORD_NOT_FOUND",
                51147 => "FACILITY_MANAGER_SCOPE_INVALID",
                _ => throw new InvalidOperationException("Unexpected SQL error.")
            };
            throw new StoredProcedureBusinessException(code, code);
        }
        finally
        {
            if (shouldClose && connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }
    }

    private static void AddGuid(System.Data.Common.DbCommand command, string name, Guid value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = DbType.Guid;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    public Task<int> CountByFacilityAsync(Guid facilityId, CancellationToken cancellationToken) =>
        (from inspection in dbContext.Inspections.AsNoTracking()
         join contract in dbContext.Contracts.AsNoTracking()
             on inspection.ContractId equals contract.ContractId
         where contract.FacilityId == facilityId
         select inspection.InspectionId).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<InspectionSummaryRecord>> ListByFacilityAsync(
        Guid facilityId, int skip, int take, CancellationToken cancellationToken) =>
        await (from inspection in dbContext.Inspections.AsNoTracking()
               join contract in dbContext.Contracts.AsNoTracking()
                   on inspection.ContractId equals contract.ContractId
               where contract.FacilityId == facilityId
               orderby inspection.Status == "PENDING" descending,
                   inspection.Status == "IN_PROGRESS" descending,
                   inspection.InspectionId
               select new InspectionSummaryRecord(
                   inspection.InspectionId, inspection.ContractId, inspection.StorageUnitId,
                   inspection.VisitId, inspection.EmployeeId, contract.FacilityId,
                   inspection.Status, inspection.ConditionNote, inspection.CompletedAt))
              .Skip(skip).Take(take).ToListAsync(cancellationToken);

    public async Task<InspectionDetailRecord?> GetDetailAsync(
        Guid inspectionId, CancellationToken cancellationToken)
    {
        var summary = await (from inspection in dbContext.Inspections.AsNoTracking()
                             join contract in dbContext.Contracts.AsNoTracking()
                                 on inspection.ContractId equals contract.ContractId
                             where inspection.InspectionId == inspectionId
                             select new InspectionSummaryRecord(
                                 inspection.InspectionId, inspection.ContractId, inspection.StorageUnitId,
                                 inspection.VisitId, inspection.EmployeeId, contract.FacilityId,
                                 inspection.Status, inspection.ConditionNote, inspection.CompletedAt))
            .SingleOrDefaultAsync(cancellationToken);
        if (summary is null) return null;

        var damages = await (from damage in dbContext.DamageRecords.AsNoTracking()
                             join type in dbContext.DamageTypes.AsNoTracking()
                                 on damage.DamageTypeId equals type.DamageTypeId
                             where damage.InspectionId == inspectionId
                             orderby damage.CreatedAt, damage.DamageRecordId
                             select new InspectionDamageRecord(
                                 damage.DamageRecordId, damage.DamageTypeId, type.Name,
                                 damage.DamageAmount, damage.Note, damage.Status, damage.CreatedAt))
            .ToListAsync(cancellationToken);

        var extraFees = await dbContext.ExtraFees.AsNoTracking()
            .Where(x => x.InspectionId == inspectionId)
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.ExtraFeeId)
            .Select(x => new InspectionExtraFeeRecord(
                x.ExtraFeeId, x.ExtraFeeTypeId, x.Amount, x.Reason, x.CreatedAt))
            .ToListAsync(cancellationToken);

        // Do not expose stored FileData in a JSON detail response.
        var evidence = await dbContext.InspectionEvidence.AsNoTracking()
            .Where(x => x.InspectionId == inspectionId)
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.InspectionEvidenceId)
            .Select(x => new InspectionEvidenceRecord(
                x.InspectionEvidenceId, x.EvidenceType, x.CreatedAt))
            .ToListAsync(cancellationToken);

        return new InspectionDetailRecord(summary, damages, extraFees, evidence);
    }

    public async Task<IReadOnlyList<DamageTypeRecord>> ListActiveDamageTypesAsync(
        CancellationToken cancellationToken) =>
        await dbContext.DamageTypes.AsNoTracking()
            .Where(x => x.Status == "ACTIVE")
            .OrderBy(x => x.Name).ThenBy(x => x.DamageTypeId)
            .Select(x => new DamageTypeRecord(
                x.DamageTypeId, x.Name, x.DefaultAmount, x.Status))
            .ToListAsync(cancellationToken);
}
