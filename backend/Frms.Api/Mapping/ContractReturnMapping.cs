using Frms.Api.DTOs.Responses;
using Frms.Business.Models.Results;

namespace Frms.Api.Mapping;

internal static class ContractReturnMapping
{
    public static VisitDetailResponse ToVisitResponse(this VisitResult x) => new(x.VisitId, x.EntityId, x.VisitType,
        x.VisitDate, x.ActualReturnDate.HasValue ? DateOnly.FromDateTime(x.ActualReturnDate.Value) : null, x.Status, x.EmployeeId);
    public static ContractReturnSummaryResponse ToResponse(this ContractReturnSummaryResult x) => new(
        x.ContractId, x.ContractStatus, x.ReturnVisit?.ToVisitResponse(),
        x.Inspection is null ? null : new(x.Inspection.InspectionId, x.Inspection.Status, x.Inspection.ConditionNote, x.Inspection.CompletedAt),
        x.Damages.Select(d => new ReturnDamageDetailResponse(d.DamageRecordId, d.InspectionId, d.DamageTypeId,
            d.DamageAmount, d.Note, d.Status, d.CreatedAt)).ToArray(),
        x.ExtraFees.Select(f => new ReturnFeeDetailResponse(f.ExtraFeeId, f.InspectionId, f.ExtraFeeTypeId,
            f.Amount, f.Reason, f.CreatedAt)).ToArray(),
        x.Settlement is null ? null : new(x.Settlement.DepositSettlementId, x.Settlement.TotalDeduction,
            x.Settlement.RefundAmount, x.Settlement.AdditionalAmountDue, x.Settlement.Status));
}
