using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;
using Frms.DataAccess.StoredProcedures;

namespace Frms.Business.Services.Implementations;

internal sealed class InspectionWorkflowService(
    IInspectionWorkflowRepository repository,
    ICurrentUserContext currentUser,
    IFacilityAuthorizationService facilityAuthorization)
    : IInspectionWorkflowService
{
    private async Task<Guid> FacilityAsync(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            throw new BusinessException("UNAUTHORIZED", "Authentication required.", 401);
        if (currentUser.Role is not ("FACILITY_STAFF" or "FACILITY_MANAGER"))
            throw new BusinessException("FORBIDDEN", "Facility Staff/Manager required.", 403);
        return await facilityAuthorization.GetAssignedFacilityIdAsync(ct);
    }

    private async Task<(Guid Facility, Guid Staff)> StaffAsync(CancellationToken ct)
    {
        var f = await FacilityAsync(ct);
        if (currentUser.Role != "FACILITY_STAFF")
            throw new BusinessException("FORBIDDEN", "Facility Staff required.", 403);
        var id = await repository.GetEmployeeIdAsync(currentUser.UserAccountId, f, ct);
        if (id is null) throw new BusinessException("FORBIDDEN", "Staff assignment is missing.", 403);
        return (f, id.Value);
    }

    private async Task CheckScopeAsync(Guid inspectionId, Guid facilityId, CancellationToken ct)
    {
        var target = await repository.GetInspectionFacilityIdAsync(inspectionId, ct);
        if (target is null) throw new BusinessException("RESOURCE_NOT_FOUND", "Inspection not found.", 404);
        if (target.Value != facilityId)
            throw new BusinessException("FORBIDDEN", "Inspection belongs to another Facility.", 403);
    }

    private static InspectionResult Map(InspectionRecord i) =>
        new(i.InspectionId, i.ContractId, i.StorageUnitId, i.VisitId,
            i.EmployeeId, i.Status, i.ConditionNote, i.CompletedAt);

    public async Task<InspectionPageResult> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            throw new BusinessException("INVALID_PAGINATION", "Invalid page or pageSize.", 400);
        var facility = await FacilityAsync(ct);
        var (items, total) = await repository.ListAsync(
            facility, (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue), pageSize, ct);
        return new(items.Select(Map).ToArray(), page, pageSize, total);
    }

    public async Task<InspectionDetailsResult> GetAsync(Guid inspectionId, CancellationToken ct = default)
    {
        var facility = await FacilityAsync(ct);
        await CheckScopeAsync(inspectionId, facility, ct);
        var detail = await repository.GetDetailAsync(inspectionId, facility, ct)
            ?? throw new BusinessException("RESOURCE_NOT_FOUND", "Inspection not found.", 404);
        return new(Map(detail.Inspection),
            detail.Damages.Select(d => new InspectionDamageResult(d.DamageRecordId,
                d.DamageTypeId, d.DamageAmount, d.Note, d.Status)).ToArray(),
            detail.ExtraFees.Select(f => new InspectionFeeResult(f.ExtraFeeId,
                f.ExtraFeeTypeId, f.Amount, f.Reason)).ToArray(),
            detail.Evidence.Select(e => new InspectionEvidenceResult(
                e.InspectionEvidenceId, e.EvidenceType, e.CreatedAt)).ToArray());
    }

    private async Task<Guid> ClaimedStaffAsync(Guid inspectionId, CancellationToken ct)
    {
        var (facility, staff) = await StaffAsync(ct);
        await CheckScopeAsync(inspectionId, facility, ct);
        var detail = await repository.GetDetailAsync(inspectionId, facility, ct);
        if (detail?.Inspection.Status != "IN_PROGRESS" || detail.Inspection.EmployeeId != staff)
            throw new BusinessException("INSPECTION_INVALID_STATUS", "Claim this Inspection before editing.", 409);
        return staff;
    }

    public async Task<InspectionResult> ClaimAsync(Guid inspectionId, CancellationToken ct = default)
    {
        var (facility, staff) = await StaffAsync(ct);
        await CheckScopeAsync(inspectionId, facility, ct);
        try { return Map(await repository.ClaimAsync(inspectionId, staff, ct)); }
        catch (StoredProcedureBusinessException ex) { throw Error(ex.Code); }
    }

    public async Task<InspectionDamageResult> RecordDamageAsync(Guid id, Guid typeId,
        decimal amount, string? note, CancellationToken ct = default)
    {
        if (typeId == Guid.Empty || amount < 0) throw new BusinessException("VALIDATION_ERROR", "Invalid damage input.", 400);
        var staff = await ClaimedStaffAsync(id, ct);
        try
        {
            var r = await repository.RecordDamageAsync(id, typeId, amount, note, staff, ct);
            return new(r.DamageRecordId, r.DamageTypeId, r.DamageAmount, r.Note, r.Status);
        }
        catch (StoredProcedureBusinessException ex) { throw Error(ex.Code); }
    }

    public async Task<InspectionFeeResult> RecordExtraFeeAsync(Guid id, Guid typeId,
        decimal amount, string reason, CancellationToken ct = default)
    {
        if (typeId == Guid.Empty || amount < 0 || string.IsNullOrWhiteSpace(reason))
            throw new BusinessException("VALIDATION_ERROR", "Invalid extra fee input.", 400);
        var staff = await ClaimedStaffAsync(id, ct);
        try
        {
            var r = await repository.RecordExtraFeeAsync(id, typeId, amount, reason.Trim(), staff, ct);
            return new(r.ExtraFeeId, r.ExtraFeeTypeId, r.Amount, r.Reason);
        }
        catch (StoredProcedureBusinessException ex) { throw Error(ex.Code); }
    }

    public async Task<InspectionEvidenceResult> AddEvidenceAsync(Guid id, byte[] data,
        string type, CancellationToken ct = default)
    {
        if (data.Length == 0 || type is not ("IMAGE" or "VIDEO" or "DOCUMENT"))
            throw new BusinessException("VALIDATION_ERROR", "Invalid evidence payload.", 400);
        var staff = await ClaimedStaffAsync(id, ct);
        try
        {
            var r = await repository.AddEvidenceAsync(id, data, type, staff, ct);
            return new(r.InspectionEvidenceId, r.EvidenceType, r.CreatedAt);
        }
        catch (StoredProcedureBusinessException ex) { throw Error(ex.Code); }
    }

    public async Task<InspectionResult> CompleteAsync(Guid id, string note, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(note))
            throw new BusinessException("VALIDATION_ERROR", "conditionNote is required.", 400);
        var staff = await ClaimedStaffAsync(id, ct);
        try { return Map(await repository.CompleteAsync(id, note.Trim(), staff, ct)); }
        catch (StoredProcedureBusinessException ex) { throw Error(ex.Code); }
    }

    public async Task<IReadOnlyList<DamageTypeResult>> ListDamageTypesAsync(CancellationToken ct = default)
    {
        await FacilityAsync(ct);
        var rows = await repository.GetDamageTypesAsync(ct);
        return rows.Select(x => new DamageTypeResult(x.Id, x.Name, x.DefaultAmount, x.Status)).ToArray();
    }

    private static BusinessException Error(string code) => new(code,
        "Inspection operation failed: " + code,
        code == "FORBIDDEN" ? 403 : code == "RESOURCE_NOT_FOUND" ? 404 : 409);
}
