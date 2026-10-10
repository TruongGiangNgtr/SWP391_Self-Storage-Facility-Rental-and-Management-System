using Frms.DataAccess.Persistence;
using Frms.DataAccess.Persistence.Entities;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class ReturnProcessingRepository(
    FrmsDbContext db, ReturnSqlExecutor sql) : IReturnProcessingRepository
{
    public Task<Visit?> GetVisitAsync(Guid id, CancellationToken ct) =>
        db.Visits.AsNoTracking().SingleOrDefaultAsync(x => x.VisitId == id, ct);

    public Task<Contract?> GetContractAsync(Guid id, CancellationToken ct) =>
        db.Contracts.AsNoTracking().SingleOrDefaultAsync(x => x.ContractId == id, ct);

    public Task<Guid?> GetEmployeeIdAsync(Guid accountId, Guid facilityId, CancellationToken ct) =>
        db.Employees.AsNoTracking()
          .Where(x => x.UserAccountId == accountId && x.FacilityId == facilityId)
          .Select(x => (Guid?)x.EmployeeId)
          .SingleOrDefaultAsync(ct);

    public async Task<ActualReturnRecord> ConfirmAsync(
        Guid visitId, DateOnly date, Guid staffId, CancellationToken ct)
    {
        // Signature must be checked against deployed SQL. See README.md.
        // Visit.EntityId references ContractId for RETURN visits.
        var contractId = await db.Visits
            .AsNoTracking()
            .Where(x =>
                x.VisitId == visitId &&
                x.VisitType == "RETURN")
            .Select(x => x.EntityId)
            .SingleAsync(ct);

        // Match the deployed SQL stored procedure signature.
        await sql.ExecuteAsync(
            "usp_ConfirmActualReturn",
            ct,
            ReturnSqlExecutor.GuidParam(
                "@ContractId",
                contractId),
            ReturnSqlExecutor.GuidParam(
                "@EmployeeId",
                staffId),
            ReturnSqlExecutor.GuidParam(
                "@VisitId",
                visitId),
            (
                "@ActualReturnDate",
                System.Data.DbType.DateTime2,
                date.ToDateTime(TimeOnly.MinValue)
            )
        );

        var visit = await db.Visits.AsNoTracking()
            .SingleAsync(x => x.VisitId == visitId, ct);
        var contract = await db.Contracts.AsNoTracking()
            .SingleAsync(x => x.ContractId == visit.EntityId, ct);
        var unit = await db.StorageUnits.AsNoTracking()
            .SingleAsync(x => x.StorageUnitId == contract.StorageUnitId, ct);
        var inspection = await db.Inspections.AsNoTracking()
            .Where(x => x.VisitId == visitId)
            .OrderByDescending(x => x.CompletedAt)
            .ThenBy(x => x.InspectionId)
            .FirstAsync(ct);

        var contractEnd = new DateOnly(contract.EndMonth.Year, contract.EndMonth.Month,
            DateTime.DaysInMonth(contract.EndMonth.Year, contract.EndMonth.Month));
        return new ActualReturnRecord(visitId, date, inspection.InspectionId,
            inspection.Status, unit.Status, date < contractEnd ? "EARLY" : "NORMAL");
    }

    public async Task<ReturnSettlementRecord> FinalizeAsync(
        Guid contractId, string status, Guid staffId, CancellationToken ct)
    {
        // Must be a *single atomic SQL transaction*, not sequential EF updates.
        await sql.ExecuteAsync("usp_FinalizeReturn", ct,
            ReturnSqlExecutor.GuidParam("@ContractId", contractId),
            ReturnSqlExecutor.TextParam("@StorageUnitStatus", status));

        var contract = await db.Contracts.AsNoTracking()
            .SingleAsync(x => x.ContractId == contractId, ct);
        var unit = await db.StorageUnits.AsNoTracking()
            .SingleAsync(x => x.StorageUnitId == contract.StorageUnitId, ct);
        var settlement = await db.DepositSettlements.AsNoTracking()
            .SingleAsync(x => x.ContractId == contractId, ct);
        return new ReturnSettlementRecord(contractId, contract.Status, unit.Status,
            settlement.DepositSettlementId, settlement.TotalDeduction,
            settlement.RefundAmount, settlement.AdditionalAmountDue, settlement.Status);
    }
}
