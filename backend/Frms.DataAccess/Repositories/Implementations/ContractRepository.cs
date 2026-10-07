using Frms.DataAccess.Persistence;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Repositories.Implementations;

internal sealed class ContractRepository(
    FrmsDbContext dbContext)
    : IContractRepository
{
    public Task<Guid?> GetCustomerIdByUserAccountIdAsync(
        Guid userAccountId,
        CancellationToken cancellationToken)
    {
        return dbContext.Customers
            .AsNoTracking()
            .Where(x => x.UserAccountId == userAccountId)
            .Select(x => (Guid?)x.CustomerId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<int> CountByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return dbContext.Contracts
            .AsNoTracking()
            .CountAsync(
                x => x.CustomerId == customerId,
                cancellationToken);
    }

    public Task<IReadOnlyList<ContractSummaryRecord>> ListByCustomerAsync(
        Guid customerId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        return ListAsync(
            customerId,
            facilityId: null,
            skip,
            take,
            cancellationToken);
    }

    public Task<ContractDetailRecord?> GetDetailAsync(
    Guid customerId,
    Guid contractId,
    CancellationToken cancellationToken) {
        return GetDetailCoreAsync(
            contractId,
            customerId,
            cancellationToken);
    }

    public Task<ContractDetailRecord?> GetDetailByIdAsync(
        Guid contractId,
        CancellationToken cancellationToken) {
        return GetDetailCoreAsync(
            contractId,
            customerId: null,
            cancellationToken);
    }

    private async Task<ContractDetailRecord?> GetDetailCoreAsync(
    Guid contractId,
    Guid? customerId,
    CancellationToken cancellationToken) {
        var contract = await (
                from c in dbContext.Contracts.AsNoTracking()
                join unit in dbContext.StorageUnits.AsNoTracking()
                    on c.StorageUnitId equals unit.StorageUnitId
                join unitType in dbContext.UnitTypes.AsNoTracking()
                    on unit.UnitTypeId equals unitType.UnitTypeId
                where c.ContractId == contractId
                      && (!customerId.HasValue ||
                          c.CustomerId == customerId.Value)
                select new {
                    Contract = c,
                    Unit = unit,
                    UnitType = unitType
                })
            .SingleOrDefaultAsync(cancellationToken);

        if (contract is null) {
            return null;
        }

        var extensions = await dbContext.ContractExtensions
            .AsNoTracking()
            .Where(x => x.ContractId == contractId)
            .OrderBy(x => x.OldEndMonth)
            .Select(x => new ContractExtensionRecord(
                x.ContractExtensionId,
                x.OldEndMonth,
                x.NewEndMonth,
                x.AppliedMonthlyPrice,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        var visits = await dbContext.Visits
            .AsNoTracking()
            .Where(x =>
                (x.EntityId == contractId &&
                 (x.VisitType == "ACCESS" ||
                  x.VisitType == "RETURN"))
                ||
                (x.EntityId == contract.Contract.ReservationId &&
                 x.VisitType == "RESERVATION"))
            .OrderByDescending(x => x.VisitDate)
            .ThenByDescending(x => x.VisitId)
            .Select(x => new ContractVisitRecord(
                x.VisitId,
                x.VisitType,
                x.VisitDate,
                x.ActualReturnDate,
                x.Status,
                x.EmployeeId))
            .ToListAsync(cancellationToken);

        var invoices = await dbContext.Invoices
            .AsNoTracking()
            .Where(x =>
                x.EntityId == contractId &&
                x.InvoiceType == "RENTAL_FEE")
            .OrderByDescending(x => x.BillingMonth)
            .Select(x => new ContractInvoiceRecord(
                x.InvoiceId,
                x.BillingMonth,
                x.AmountDue,
                x.Status))
            .ToListAsync(cancellationToken);

        return new ContractDetailRecord(
            contract.Contract.ContractId,
            contract.Contract.ReservationId,
            contract.Contract.FacilityId,
            contract.Contract.StorageUnitId,
            contract.Contract.PolicyId,
            contract.Contract.DiscountId,
            contract.Contract.StartMonth,
            contract.Contract.EndMonth,
            contract.Contract.Status,
            contract.Unit.UnitCode,
            contract.Unit.LocationInfo,
            contract.Unit.Status,
            contract.UnitType.UnitTypeId,
            contract.UnitType.Name,
            contract.UnitType.Mode,
            contract.UnitType.Size,
            extensions,
            visits,
            invoices);
    }

    public Task<int> CountByFacilityAsync(
    Guid facilityId,
    CancellationToken cancellationToken) {
        return dbContext.Contracts
            .AsNoTracking()
            .CountAsync(
                x => x.FacilityId == facilityId,
                cancellationToken);
    }

    public Task<int> CountAllAsync(
        CancellationToken cancellationToken) {
        return dbContext.Contracts
            .AsNoTracking()
            .CountAsync(cancellationToken);
    }

    public Task<IReadOnlyList<ContractSummaryRecord>> ListByFacilityAsync(
        Guid facilityId,
        int skip,
        int take,
        CancellationToken cancellationToken) {
        return ListAsync(
            customerId: null,
            facilityId,
            skip,
            take,
            cancellationToken);
    }

    public Task<IReadOnlyList<ContractSummaryRecord>> ListAllAsync(
        int skip,
        int take,
        CancellationToken cancellationToken) {
        return ListAsync(
            customerId: null,
            facilityId: null,
            skip,
            take,
            cancellationToken);
    }

    private async Task<IReadOnlyList<ContractSummaryRecord>> ListAsync(
        Guid? customerId,
        Guid? facilityId,
        int skip,
        int take,
        CancellationToken cancellationToken) {
        var contracts =
            dbContext.Contracts.AsNoTracking();

        var scoped = contracts.AsQueryable();

        if (customerId.HasValue) {
            scoped = scoped.Where(
                x => x.CustomerId == customerId.Value);
        }

        if (facilityId.HasValue) {
            scoped = scoped.Where(
                x => x.FacilityId == facilityId.Value);
        }

        return await (
                from contract in scoped
                join unit in dbContext.StorageUnits.AsNoTracking()
                    on contract.StorageUnitId equals unit.StorageUnitId
                join unitType in dbContext.UnitTypes.AsNoTracking()
                    on unit.UnitTypeId equals unitType.UnitTypeId
                orderby contract.Status == "ACTIVE" descending,
                    contract.EndMonth descending,
                    contract.ContractId
                select new ContractSummaryRecord(
                    contract.ContractId,
                    contract.FacilityId,
                    contract.StorageUnitId,
                    unit.UnitCode,
                    unitType.UnitTypeId,
                    unitType.Name,
                    unitType.Mode,
                    contract.StartMonth,
                    contract.EndMonth,
                    contract.Status))
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
