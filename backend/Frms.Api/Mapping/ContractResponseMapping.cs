using Frms.Api.DTOs.Responses;
using Frms.Business.Models.Results;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Api.Mapping;

internal static class ContractResponseMapping
{
    public static ContractListResponse ToResponse(this ContractPageRecord result) => new(
        result.Items.Select(x => new ContractSummaryResponse(
            x.ContractId, x.FacilityId, x.StorageUnitId, x.UnitCode,
            x.UnitTypeId, x.UnitTypeName, x.UnitTypeMode,
            x.StartMonth.ToString("yyyy-MM"), x.EndMonth.ToString("yyyy-MM"), x.Status)
            { CustomerId = x.CustomerId }).ToArray(),
        new ContractPaginationResponse(result.Page, result.PageSize,
            result.TotalItems, result.TotalPages));

    public static ContractDetailResponse ToResponse(this ContractDetailRecord result) => new(
        result.ContractId, result.ReservationId, result.FacilityId,
        result.PolicyId, result.DiscountId,
        result.StartMonth.ToString("yyyy-MM"), result.EndMonth.ToString("yyyy-MM"),
        result.Status,
        new ContractStorageUnitResponse(
            result.StorageUnitId, result.UnitCode, result.LocationInfo,
            result.StorageUnitStatus, result.UnitTypeId, result.UnitTypeName,
            result.UnitTypeMode, result.UnitTypeSize),
        result.Extensions.Select(x => new ContractExtensionResponse(
            x.ContractExtensionId, x.OldEndMonth.ToString("yyyy-MM"),
            x.NewEndMonth.ToString("yyyy-MM"), x.AppliedMonthlyPrice,
            new DateTimeOffset(DateTime.SpecifyKind(x.CreatedAt, DateTimeKind.Utc)))).ToArray(),
        result.Visits.Select(x => new ContractVisitResponse(
            x.VisitId, x.VisitType, x.VisitDate,
            x.ActualReturnDate.HasValue ? DateOnly.FromDateTime(x.ActualReturnDate.Value) : null,
            x.Status, x.EmployeeId)).ToArray(),
        result.RentalInvoices.Select(x => new ContractInvoiceStatusResponse(
            x.InvoiceId, x.BillingMonth?.ToString("yyyy-MM"),
            x.AmountDue, x.Status)).ToArray())
        { CustomerId = result.CustomerId, StorageUnitId = result.StorageUnitId };

    public static RenewContractResponse ToResponse(this RenewedContractRecord result) => new(
        result.ContractId, result.OldEndMonth.ToString("yyyy-MM"),
        result.NewEndMonth.ToString("yyyy-MM"), result.AppliedMonthlyPrice, result.Status);

    public static FinalizeReturnResponse ToResponse(this FinalizedReturnResult result) => new(
        result.ContractId, result.ContractStatus, result.StorageUnitStatus,
        new DepositSettlementResponse(
            result.Settlement.DepositSettlementId, result.Settlement.TotalDeduction,
            result.Settlement.RefundAmount, result.Settlement.AdditionalAmountDue,
            result.Settlement.Status));
}
