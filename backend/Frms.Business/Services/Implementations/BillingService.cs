using Frms.Business.Abstractions.Security;
using Frms.Business.Exceptions;
using Frms.Business.Models.Results;
using Frms.Business.Services.Interfaces;
using Frms.DataAccess.Repositories.Interfaces;
using Frms.DataAccess.Repositories.Models;

namespace Frms.Business.Services.Implementations;

internal sealed class BillingService(
    IBillingRepository repository, ICurrentUserContext currentUser, IAuthenticationService authentication)
    : IBillingService
{
    public async Task<InvoicePageResult> ListInvoicesAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var scope = await GetScopeAsync(cancellationToken);
        if (page < 1 || pageSize is < 1 or > 100)
            throw new BusinessException("VALIDATION_ERROR", "page must be >= 1 and pageSize must be between 1 and 100.", 400);
        var result = await repository.ListInvoicesAsync(scope, ((long)page - 1) * pageSize, pageSize, cancellationToken);
        return new InvoicePageResult(result.Items.Select(ToResult).ToArray(), page, pageSize, result.TotalItems,
            (int)Math.Min(int.MaxValue, (result.TotalItems + pageSize - 1) / pageSize));
    }

    public async Task<InvoiceResult> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        var scope = await GetScopeAsync(cancellationToken);
        var invoice = await repository.GetInvoiceAsync(invoiceId, cancellationToken)
            ?? throw NotFound("Invoice");
        EnsureScope(scope, invoice.CustomerId, invoice.FacilityId, "Invoice");
        return ToResult(invoice);
    }

    public async Task<ContractBillingResult> GetContractBillingAsync(Guid contractId, CancellationToken cancellationToken)
    {
        var scope = await GetScopeAsync(cancellationToken);
        var contract = await repository.GetContractAsync(contractId, cancellationToken)
            ?? throw NotFound("Contract");
        EnsureScope(scope, contract.CustomerId, contract.FacilityId, "Contract");
        var result = await repository.GetContractBillingAsync(contractId, scope, cancellationToken)
            ?? throw NotFound("Contract");
        return new ContractBillingResult(result.ContractId, result.Invoices.Select(ToResult).ToArray(),
            result.LateFees.Select(x => new LateFeeResult(x.LateFeeId, x.InvoiceId, x.OverdueDays,
                x.Amount, Utc(x.CalculatedAt))).ToArray());
    }

    private async Task<BillingReadScope> GetScopeAsync(CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
            throw new BusinessException("UNAUTHORIZED", "Authentication is required.", 401);
        var account = await authentication.GetCurrentAccountAsync(currentUser.UserAccountId, cancellationToken);
        if (account.Status != "ACTIVE")
            throw new BusinessException("ACCOUNT_INACTIVE", "The account is inactive.", 403);
        return account.Role switch
        {
            "CUSTOMER" when account.CustomerId.HasValue => new(account.CustomerId, null),
            "FACILITY_STAFF" or "FACILITY_MANAGER" when account.EmployeeId.HasValue && account.FacilityId.HasValue
                => new(null, account.FacilityId),
            "BUSINESS_OPERATIONS_MANAGER" when account.EmployeeId.HasValue => new(null, null),
            _ => throw Forbidden()
        };
    }

    private static void EnsureScope(BillingReadScope scope, Guid customerId, Guid facilityId, string resource)
    {
        // Contract reads consistently use non-disclosing 404 for other Customers.
        if (scope.CustomerId.HasValue && scope.CustomerId != customerId) throw NotFound(resource);
        if (scope.FacilityId.HasValue && scope.FacilityId != facilityId) throw Forbidden();
    }

    private static InvoiceResult ToResult(InvoiceReadRecord x) => new(x.InvoiceId, x.EntityId, x.InvoiceType,
        x.BillingMonth, x.BaseAmount, x.DiscountId, x.DiscountAmount, x.AmountDue,
        Utc(x.DueDate), x.Status, x.PaidAt.HasValue ? Utc(x.PaidAt.Value) : null);
    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static BusinessException Forbidden() => new("FORBIDDEN", "Access to this resource is denied.", 403);
    private static BusinessException NotFound(string resource) => new("RESOURCE_NOT_FOUND", $"{resource} was not found.", 404);
}
