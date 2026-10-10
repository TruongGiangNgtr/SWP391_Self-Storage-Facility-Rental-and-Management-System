using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.StoredProcedures;

namespace Frms.Business.Services.Implementations;

internal sealed class ReturnProcessingService(
    IReturnProcessingRepository repository,
    ICurrentUserContext currentUser,
    IFacilityAuthorizationService facilityAuthorization) : IReturnProcessingService
{
    private async Task<Guid> StaffAsync(Guid facilityId, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            throw new BusinessException("UNAUTHORIZED", "Authentication required.", 401);
        if (currentUser.Role != "FACILITY_STAFF")
            throw new BusinessException("FORBIDDEN", "Facility Staff required.", 403);
        await facilityAuthorization.EnsureSameFacilityAsync(facilityId, ct);
        var id = await repository.GetEmployeeIdAsync(currentUser.UserAccountId, facilityId, ct);
        if (id is null)
            throw new BusinessException("FORBIDDEN", "Staff Facility assignment not found.", 403);
        return id.Value;
    }

    public async Task<ConfirmedReturnResult> ConfirmAsync(
        Guid visitId, DateOnly actualReturnDate, CancellationToken ct = default)
    {
        if (visitId == Guid.Empty || actualReturnDate == default)
            throw new BusinessException("VALIDATION_ERROR", "Visit and actualReturnDate are required.", 400);
        var visit = await repository.GetVisitAsync(visitId, ct);
        if (visit is null) throw new BusinessException("RESOURCE_NOT_FOUND", "Visit not found.", 404);
        if (visit.VisitType != "RETURN")
            throw new BusinessException("VISIT_ENTITY_MISMATCH", "Visit must be RETURN.", 409);
        var contract = await repository.GetContractAsync(visit.EntityId, ct);
        if (contract is null) throw new BusinessException("RESOURCE_NOT_FOUND", "Contract not found.", 404);
        var staff = await StaffAsync(contract.FacilityId, ct);
        if (visit.Status != "CHECKED_IN" || contract.Status != "ACTIVE" || visit.ActualReturnDate != null)
            throw new BusinessException("VISIT_INVALID_STATUS", "Return cannot be confirmed in this state.", 409);
        try
        {
            var r = await repository.ConfirmAsync(visitId, actualReturnDate, staff, ct);
            return new(r.VisitId, r.ActualReturnDate, r.InspectionId,
                r.InspectionStatus, r.StorageUnitStatus, r.ReturnClassification);
        }
        catch (StoredProcedureBusinessException ex) { throw Error(ex.Code); }
    }

    public async Task<FinalizedReturnResult> FinalizeAsync(
        Guid contractId, string storageUnitStatus, CancellationToken ct = default)
    {
        if (contractId == Guid.Empty || storageUnitStatus is not ("AVAILABLE" or "MAINTENANCE"))
            throw new BusinessException("VALIDATION_ERROR", "storageUnitStatus must be AVAILABLE or MAINTENANCE.", 400);
        var contract = await repository.GetContractAsync(contractId, ct);
        if (contract is null) throw new BusinessException("RESOURCE_NOT_FOUND", "Contract not found.", 404);
        var staff = await StaffAsync(contract.FacilityId, ct);
        if (contract.Status != "ACTIVE")
            throw new BusinessException("RETURN_ALREADY_FINALIZED", "Contract is not active.", 409);
        try
        {
            var r = await repository.FinalizeAsync(contractId, storageUnitStatus, staff, ct);
            return new(r.ContractId, r.ContractStatus, r.StorageUnitStatus,
                new SettlementResult(r.DepositSettlementId, r.TotalDeduction,
                    r.RefundAmount, r.AdditionalAmountDue, r.Status));
        }
        catch (StoredProcedureBusinessException ex) { throw Error(ex.Code); }
    }

    private static BusinessException Error(string code) => new(code,
        "Return operation failed: " + code,
        code is "FORBIDDEN" ? 403 : code is "RESOURCE_NOT_FOUND" ? 404 : 409);
}
