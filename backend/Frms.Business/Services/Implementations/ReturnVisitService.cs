using Frms.Business.Abstractions.Security;
using Frms.Business.Abstractions.Time;
using Frms.Business.Exceptions;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Frms.DataAccess.StoredProcedures;

namespace Frms.Business.Services.Implementations;

internal sealed class ReturnVisitService(IReturnVisitRepository repository, IVisitRepository visits,
    ICurrentUserContext currentUser, IAuthenticationService authentication, IClock clock) : IReturnVisitService
{
    public async Task<VisitResult> CreateAsync(Guid contractId, DateOnly visitDate, CancellationToken ct)
    {
        var actor = await CustomerAsync(ct);
        var contract = await OwnedContractAsync(contractId, actor.CustomerId!.Value, ct);
        EnsureActive(contract);
        ValidateDate(visitDate);
        try
        {
            return Map(await repository.CreateAsync(actor.UserAccountId, actor.CustomerId.Value,
            contractId, visitDate, clock.UtcNow, ct));
        }
        catch (StoredProcedureBusinessException ex) { throw Error(ex.Code); }
    }

    public async Task<VisitResult> RescheduleAsync(Guid visitId, DateOnly visitDate, CancellationToken ct)
    {
        var actor = await CustomerAsync(ct);
        var visit = await ScheduledOwnedAsync(actor.CustomerId!.Value, visitId, ct);
        EnsureActive(await OwnedContractAsync(visit.EntityId, actor.CustomerId.Value, ct));
        ValidateDate(visitDate);
        try
        {
            return Map(await repository.RescheduleAsync(actor.UserAccountId, actor.CustomerId.Value,
            visitId, visitDate, clock.UtcNow, ct));
        }
        catch (StoredProcedureBusinessException ex) { throw Error(ex.Code); }
    }

    public async Task<VisitResult> CancelAsync(Guid visitId, string reason, CancellationToken ct)
    {
        var actor = await CustomerAsync(ct);
        await ScheduledOwnedAsync(actor.CustomerId!.Value, visitId, ct);
        if (string.IsNullOrWhiteSpace(reason)) throw Error("VALIDATION_ERROR");
        // Existing authoritative usp_CancelVisit atomically rejects a concurrent check-in.
        // Contract ownership is immutable; cancellation does not create a return event.
        try { return ServiceResultProjection.Map(await visits.CancelAsync(actor.CustomerId.Value, visitId, reason.Trim(), ct)); }
        catch (StoredProcedureBusinessException ex) { throw Error(ex.Code); }
    }

    public async Task<ContractReturnSummaryResult> GetSummaryAsync(Guid contractId, CancellationToken ct)
    {
        var actor = await ActorAsync(ct);
        var scope = actor.Role switch
        {
            "CUSTOMER" when actor.CustomerId.HasValue => new BillingReadScope(actor.CustomerId, null),
            "FACILITY_STAFF" or "FACILITY_MANAGER" when actor.EmployeeId.HasValue && actor.FacilityId.HasValue
                => new BillingReadScope(null, actor.FacilityId),
            "BUSINESS_OPERATIONS_MANAGER" when actor.EmployeeId.HasValue => new BillingReadScope(null, null),
            _ => throw Error("FORBIDDEN")
        };
        var contract = await repository.GetContractAsync(contractId, ct) ?? throw Error("RESOURCE_NOT_FOUND");
        if (scope.CustomerId.HasValue && scope.CustomerId != contract.CustomerId) throw Error("RESOURCE_NOT_FOUND");
        if (scope.FacilityId.HasValue && scope.FacilityId != contract.FacilityId) throw Error("FORBIDDEN");
        var row = await repository.GetSummaryAsync(contractId, scope, ct) ?? throw Error("RESOURCE_NOT_FOUND");
        return new(row.ContractId, row.ContractStatus, row.ReturnVisit is null ? null : Map(row.ReturnVisit),
            row.Inspection is null ? null : new(row.Inspection.InspectionId, row.Inspection.Status,
                row.Inspection.ConditionNote, row.Inspection.CompletedAt.HasValue ? Utc(row.Inspection.CompletedAt.Value) : null),
            row.Damages.Select(x => new ReturnDamageDetailResult(x.DamageRecordId, x.InspectionId, x.DamageTypeId,
                x.DamageAmount, x.Note, x.Status, Utc(x.CreatedAt))).ToArray(),
            row.ExtraFees.Select(x => new ReturnFeeDetailResult(x.ExtraFeeId, x.InspectionId, x.ExtraFeeTypeId,
                x.Amount, x.Reason, Utc(x.CreatedAt))).ToArray(),
            row.Settlement is null ? null : new(row.Settlement.DepositSettlementId, row.Settlement.TotalDeduction,
                row.Settlement.RefundAmount, row.Settlement.AdditionalAmountDue, row.Settlement.Status));
    }

    private async Task<CurrentAccountResult> ActorAsync(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated) throw Error("UNAUTHORIZED");
        var actor = await authentication.GetCurrentAccountAsync(currentUser.UserAccountId, ct);
        if (actor.Status != "ACTIVE") throw Error("ACCOUNT_INACTIVE");
        return actor;
    }
    private async Task<CurrentAccountResult> CustomerAsync(CancellationToken ct)
    {
        var actor = await ActorAsync(ct);
        if (actor.Role != "CUSTOMER" || !actor.CustomerId.HasValue) throw Error("FORBIDDEN");
        return actor;
    }
    private async Task<ReturnContractRecord> OwnedContractAsync(Guid id, Guid owner, CancellationToken ct)
    {
        var contract = await repository.GetContractAsync(id, ct);
        if (contract is null || contract.CustomerId != owner) throw Error("RESOURCE_NOT_FOUND");
        return contract;
    }
    private async Task<ReturnVisitRecord> ScheduledOwnedAsync(Guid owner, Guid id, CancellationToken ct)
    {
        var visit = await repository.GetOwnedVisitAsync(owner, id, ct) ?? throw Error("RESOURCE_NOT_FOUND");
        if (visit.VisitType != "RETURN") throw Error("VISIT_ENTITY_MISMATCH");
        if (visit.Status != "SCHEDULED" || visit.ActualReturnDate.HasValue) throw Error("VISIT_INVALID_STATUS");
        return visit;
    }
    private static void EnsureActive(ReturnContractRecord contract)
    {
        if (contract.Status != "ACTIVE") throw Error("CONTRACT_NOT_ACTIVE");
    }
    private void ValidateDate(DateOnly date)
    {
        if (date == default) throw Error("VALIDATION_ERROR");
        if (date < DateOnly.FromDateTime(clock.ToBusinessTime(clock.UtcNow).DateTime)) throw Error("VISIT_DATE_IN_PAST");
    }
    private static VisitResult Map(ReturnVisitRecord x) => new(x.VisitId, x.EntityId, x.EmployeeId,
        x.VisitType, x.VisitDate, x.ActualReturnDate, x.Status);
    private static DateTimeOffset Utc(DateTime x) => new(DateTime.SpecifyKind(x, DateTimeKind.Utc));
    private static BusinessException Error(string code) => new(code, code switch
    {
        "RETURN_VISIT_PENDING" => "A pending RETURN Visit already exists.",
        "VISIT_DATE_IN_PAST" => "Visit date cannot be before today in the business time zone.",
        "CONTRACT_NOT_ACTIVE" => "RETURN Visit requires an active Contract.",
        "VISIT_INVALID_STATUS" => "Only a scheduled RETURN Visit may be changed.",
        "RESOURCE_NOT_FOUND" => "The requested resource was not found.",
        "ACCOUNT_INACTIVE" => "The account is inactive.",
        "UNAUTHORIZED" => "Authentication is required.",
        "FORBIDDEN" => "Access to this operation is denied.",
        _ => "Return visit request is invalid."
    }, code switch
    {
        "UNAUTHORIZED" => 401,
        "ACCOUNT_INACTIVE" or "FORBIDDEN" => 403,
        "RESOURCE_NOT_FOUND" => 404,
        "VALIDATION_ERROR" or "VISIT_DATE_IN_PAST" => 400,
        _ => 409
    });
}
