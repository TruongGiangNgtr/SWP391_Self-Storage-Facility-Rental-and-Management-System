using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Implementations;

internal sealed class InspectionService(
    IInspectionRepository repository,
    ICurrentUserContext currentUser,
    IFacilityAuthorizationService facilityAuthorizationService) : IInspectionService
{
    public async Task<DamageDecisionRecord> DecideDamageAsync(
        Guid damageRecordId, string decision, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated)
            throw new BusinessException("UNAUTHORIZED", "Authentication is required.", 401);
        if (currentUser.Role != "FACILITY_MANAGER")
            throw new BusinessException("FORBIDDEN", "Facility Manager role is required.", 403);
        if (decision is not ("APPROVED" or "REJECTED"))
            throw new BusinessException("VALIDATION_ERROR", "Decision must be APPROVED or REJECTED.", 400);

        var assignedFacilityId = await facilityAuthorizationService
            .GetAssignedFacilityIdAsync(cancellationToken);
        var damageFacilityId = await repository.GetDamageFacilityIdAsync(
            damageRecordId, cancellationToken);
        if (!damageFacilityId.HasValue)
            throw new BusinessException("RESOURCE_NOT_FOUND", "DamageRecord was not found.", 404);
        if (damageFacilityId.Value != assignedFacilityId)
            throw new BusinessException("FORBIDDEN", "DamageRecord belongs to a different facility.", 403);

        var managerEmployeeId = await repository.GetEmployeeIdByUserAccountIdAsync(
            currentUser.UserAccountId, cancellationToken);
        if (!managerEmployeeId.HasValue)
            throw new BusinessException("FORBIDDEN", "Facility Manager employee profile is unavailable.", 403);

        try
        {
            return await repository.DecideDamageAsync(
                damageRecordId, managerEmployeeId.Value, decision, cancellationToken);
        }
        catch (Frms.DataAccess.StoredProcedures.StoredProcedureBusinessException ex)
        {
            throw ex.Code switch
            {
                "DAMAGE_INVALID_STATUS" => new BusinessException(ex.Code,
                    "DamageRecord is not pending.", 409),
                "DAMAGE_RECORD_NOT_FOUND" => new BusinessException("RESOURCE_NOT_FOUND",
                    "DamageRecord was not found.", 404),
                "FACILITY_MANAGER_SCOPE_INVALID" => new BusinessException("FORBIDDEN",
                    "Manager does not have access to this facility.", 403),
                _ => new BusinessException("INTERNAL_ERROR", "Unexpected stored procedure error.", 500)
            };
        }
    }

    private void EnsureFacilityRole()
    {
        if (!currentUser.IsAuthenticated)
            throw new BusinessException("UNAUTHORIZED", "Authentication is required.", 401);
        if (currentUser.Role is not ("FACILITY_STAFF" or "FACILITY_MANAGER"))
            throw new BusinessException("FORBIDDEN", "You are not authorized to access inspections.", 403);
    }

    public async Task<(IReadOnlyList<InspectionSummaryRecord> Items, int TotalItems)> ListAccessibleAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        EnsureFacilityRole();
        if (page < 1 || pageSize < 1 || pageSize > 100)
            throw new BusinessException("VALIDATION_ERROR", "page must be >= 1 and pageSize must be between 1 and 100.", 400);
        var facilityId = await facilityAuthorizationService.GetAssignedFacilityIdAsync(cancellationToken);
        var count = await repository.CountByFacilityAsync(facilityId, cancellationToken);
        var rows = await repository.ListByFacilityAsync(facilityId, (page - 1) * pageSize, pageSize, cancellationToken);
        return (rows, count);
    }

    public async Task<InspectionMonitoringDetailRecord> GetAccessibleAsync(
        Guid inspectionId, CancellationToken cancellationToken = default)
    {
        EnsureFacilityRole();
        var detail = await repository.GetDetailAsync(inspectionId, cancellationToken)
            ?? throw new BusinessException("RESOURCE_NOT_FOUND", "Inspection was not found.", 404);
        await facilityAuthorizationService.EnsureSameFacilityAsync(
            detail.Inspection.FacilityId, cancellationToken);
        return detail;
    }

    public Task<IReadOnlyList<DamageTypeRecord>> ListActiveDamageTypesAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureFacilityRole();
        return repository.ListActiveDamageTypesAsync(cancellationToken);
    }
}
