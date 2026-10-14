using Frms.Api.DTOs.Responses;
using Frms.Business.Models.Results;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Api.Mapping;

internal static class InspectionResponseMapping
{
    public static InspectionSummaryResponse ToSummary(this InspectionResult result) => new(
        result.InspectionId, result.ContractId, result.StorageUnitId,
        result.VisitId, result.EmployeeId, result.Status,
        result.ConditionNote, result.CompletedAt);

    public static InspectionMonitoringSummaryResponse ToSummary(this InspectionSummaryRecord result) => new(
        result.InspectionId, result.ContractId, result.StorageUnitId,
        result.VisitId, result.EmployeeId, result.FacilityId,
        result.Status, result.ConditionNote, result.CompletedAt);

    public static InspectionMonitoringDetailResponse ToMonitoringDetail(this InspectionDetailRecord result)
    {
        var inspection = result.Inspection;
        return new InspectionMonitoringDetailResponse(
            inspection.InspectionId, inspection.ContractId, inspection.StorageUnitId,
            inspection.VisitId, inspection.EmployeeId, inspection.Status,
            inspection.ConditionNote,
            result.Damages.Select(x => new InspectionMonitoringDamageResponse(
                x.DamageRecordId, x.DamageTypeId, x.DamageTypeName,
                x.DamageAmount, x.Note, x.Status, x.CreatedAt)).ToArray(),
            result.ExtraFees.Select(x => new InspectionMonitoringExtraFeeResponse(
                x.ExtraFeeId, x.ExtraFeeTypeId, x.Amount,
                x.Reason, x.CreatedAt)).ToArray(),
            result.Evidence.Select(x => new InspectionEvidenceMetadataResponse(
                x.InspectionEvidenceId, x.EvidenceType, x.CreatedAt)).ToArray(),
            inspection.CompletedAt);
    }

    public static DamageTypeResponse ToResponse(this DamageTypeRecord result) => new(
        result.DamageTypeId, result.Name, result.DefaultAmount, result.Status);
}
