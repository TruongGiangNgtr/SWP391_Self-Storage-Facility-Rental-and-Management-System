using Frms.Api.Authorization;
using Frms.Api.DTOs.Requests;
using Frms.Api.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Frms.Business.Models.Commands;
using Frms.Business.Services.Interfaces;

namespace Frms.Api.Controllers;

[Route("api/v1/contracts")]
public sealed class ContractsController(
    IRenewalService renewalService,
    IContractService contractService,
    IReturnProcessingService returnService)
    : ScaffoldControllerBase
{
    /// <summary>CON-001: List own Contracts.</summary>
    [Authorize(
    Roles =
        RoleNames.Customer + "," +
        RoleNames.FacilityManager + "," +
        RoleNames.BusinessOperationsManager)]
    [HttpGet]
    [ProducesResponseType(
        typeof(ContractListResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ContractListResponse>> ListContracts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result =
            await contractService.ListAccessibleAsync(
                page,
                pageSize,
                cancellationToken);

        var response =
            new ContractListResponse(
                result.Items
                    .Select(x => new ContractSummaryResponse(
                        x.ContractId,
                        x.FacilityId,
                        x.StorageUnitId,
                        x.UnitCode,
                        x.UnitTypeId,
                        x.UnitTypeName,
                        x.UnitTypeMode,
                        x.StartMonth.ToString("yyyy-MM"),
                        x.EndMonth.ToString("yyyy-MM"),
                        x.Status))
                    .ToArray(),
                new ContractPaginationResponse(
                    result.Page,
                    result.PageSize,
                    result.TotalItems,
                    result.TotalPages));

        return Ok(response);
    }

    /// <summary>CON-002: Get own Contract detail.</summary>
    [Authorize(
    Roles =
        RoleNames.Customer + "," +
        RoleNames.FacilityStaff + "," +
        RoleNames.FacilityManager + "," +
        RoleNames.BusinessOperationsManager)]
    [HttpGet("{contractId:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<ContractDetailResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ContractDetailResponse>>> GetContract(
        Guid contractId,
        CancellationToken cancellationToken)
    {
        var result =
            await contractService.GetAccessibleAsync(
                contractId,
                cancellationToken);

        var response =
            new ContractDetailResponse(
                result.ContractId,
                result.ReservationId,
                result.FacilityId,
                result.PolicyId,
                result.DiscountId,
                result.StartMonth.ToString("yyyy-MM"),
                result.EndMonth.ToString("yyyy-MM"),
                result.Status,
                new ContractStorageUnitResponse(
                    result.StorageUnitId,
                    result.UnitCode,
                    result.LocationInfo,
                    result.StorageUnitStatus,
                    result.UnitTypeId,
                    result.UnitTypeName,
                    result.UnitTypeMode,
                    result.UnitTypeSize),
                result.Extensions
                    .Select(x => new ContractExtensionResponse(
                        x.ContractExtensionId,
                        x.OldEndMonth.ToString("yyyy-MM"),
                        x.NewEndMonth.ToString("yyyy-MM"),
                        x.AppliedMonthlyPrice,
                        new DateTimeOffset(
                            DateTime.SpecifyKind(
                                x.CreatedAt,
                                DateTimeKind.Utc))))
                    .ToArray(),
                result.Visits
                    .Select(x => new ContractVisitResponse(
                        x.VisitId,
                        x.VisitType,
                        x.VisitDate,
                        x.ActualReturnDate.HasValue
                            ? DateOnly.FromDateTime(
                                x.ActualReturnDate.Value)
                            : null,
                        x.Status,
                        x.EmployeeId))
                    .ToArray(),
                result.RentalInvoices
                    .Select(x => new ContractInvoiceStatusResponse(
                        x.InvoiceId,
                        x.BillingMonth?.ToString("yyyy-MM"),
                        x.AmountDue,
                        x.Status))
                    .ToArray());

        return Ok(
            new ApiResponse<ContractDetailResponse>(
                response,
                "Contract retrieved."));
    }

    /// <summary>CON-003: Renew an active Contract.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpPost("{contractId:guid}/renew")]
    [ProducesResponseType(
        typeof(ApiResponse<RenewContractResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RenewContractResponse>>> RenewContract(
        Guid contractId,
        [FromBody] RenewContractRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await renewalService.RenewAsync(
                new RenewContractCommand(
                    contractId,
                    request.NewEndMonth),
                cancellationToken);

        var response =
            new RenewContractResponse(
                result.ContractId,
                result.OldEndMonth.ToString("yyyy-MM"),
                result.NewEndMonth.ToString("yyyy-MM"),
                result.AppliedMonthlyPrice,
                result.Status);

        return Ok(
            new ApiResponse<RenewContractResponse>(
                response,
                "Contract renewed."));
    }

    /// <summary>CON-004: Contract billing and overdue summary scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpGet("{contractId:guid}/billing")]
    public ActionResult<ApiErrorResponse> GetContractBilling(
        Guid contractId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("CON-004");

    /// <summary>CON-005: Contract return, inspection and settlement summary scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpGet("{contractId:guid}/return-summary")]
    public ActionResult<ApiErrorResponse> GetReturnSummary(
        Guid contractId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("CON-005");

    /// <summary>VIS-002: Create a RETURN Visit scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpPost("{contractId:guid}/return-visits")]
    [ProducesResponseType(typeof(ApiResponse<VisitDetailResponse>), StatusCodes.Status201Created)]
    public ActionResult<ApiErrorResponse> CreateReturnVisit(
        Guid contractId,
        [FromBody] CreateReturnVisitRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("VIS-002");

    /// <summary>INS-008: Atomic settlement, terminal Contract and Unit release.</summary>
    [Authorize(Roles = RoleNames.FacilityStaff)]
    [HttpPost("{contractId:guid}/finalize-return")]
    [ProducesResponseType(typeof(ApiResponse<FinalizeReturnResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<FinalizeReturnResponse>>> FinalizeReturn(
        Guid contractId, [FromBody] FinalizeReturnRequest request,
        CancellationToken cancellationToken)
    {
        var r = await returnService.FinalizeAsync(contractId, request.StorageUnitStatus,
            cancellationToken);
        return Ok(new ApiResponse<FinalizeReturnResponse>(new(
            r.ContractId, r.ContractStatus, r.StorageUnitStatus,
            new DepositSettlementResponse(r.Settlement.DepositSettlementId,
                r.Settlement.TotalDeduction, r.Settlement.RefundAmount,
                r.Settlement.AdditionalAmountDue, r.Settlement.Status))));
    }

}
