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
    IRenewalService renewalService)
    : ScaffoldControllerBase
{
    /// <summary>CON-001: List own Contracts scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpGet]
    public ActionResult<ApiErrorResponse> ListContracts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) => ScaffoldNotImplemented("CON-001");

    /// <summary>CON-002: Get own Contract detail scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpGet("{contractId:guid}")]
    public ActionResult<ApiErrorResponse> GetContract(
        Guid contractId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("CON-002");

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

    /// <summary>VIS-001: Create an ACCESS Visit scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpPost("{contractId:guid}/access-visits")]
    [ProducesResponseType(typeof(ApiResponse<VisitDetailResponse>), StatusCodes.Status201Created)]
    public ActionResult<ApiErrorResponse> CreateAccessVisit(
        Guid contractId,
        [FromBody] CreateAccessVisitRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("VIS-001");

    /// <summary>VIS-002: Create a RETURN Visit scaffold.</summary>
    [Authorize(Roles = RoleNames.Customer)]
    [HttpPost("{contractId:guid}/return-visits")]
    [ProducesResponseType(typeof(ApiResponse<VisitDetailResponse>), StatusCodes.Status201Created)]
    public ActionResult<ApiErrorResponse> CreateReturnVisit(
        Guid contractId,
        [FromBody] CreateReturnVisitRequest request,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("VIS-002");

    /// <summary>INS-008: Finalize return and settlement scaffold.</summary>
    [Authorize(Roles = RoleNames.FacilityStaff)]
    [HttpPost("{contractId:guid}/finalize-return")]
    [ProducesResponseType(typeof(ApiResponse<FinalizeReturnResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiErrorResponse> FinalizeReturn(
        Guid contractId,
        CancellationToken cancellationToken) => ScaffoldNotImplemented("INS-008");
}
