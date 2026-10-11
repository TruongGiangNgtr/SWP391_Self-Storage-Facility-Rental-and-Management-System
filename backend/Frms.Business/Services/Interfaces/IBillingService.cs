using Frms.Business.Models.Results;

namespace Frms.Business.Services.Interfaces;

public interface IBillingService
{
    Task<InvoicePageResult> ListInvoicesAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<InvoiceResult> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken);
    Task<ContractBillingResult> GetContractBillingAsync(Guid contractId, CancellationToken cancellationToken);
}
