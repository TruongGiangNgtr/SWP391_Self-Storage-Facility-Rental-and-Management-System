using Frms.DataAccess.Repositories.Models;

namespace Frms.DataAccess.Repositories.Interfaces;

public interface IBillingRepository
{
    Task<InvoicePageRecord> ListInvoicesAsync(BillingReadScope scope, long skip, int take, CancellationToken cancellationToken);
    Task<InvoiceReadRecord?> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken);
    Task<BillingContractRecord?> GetContractAsync(Guid contractId, CancellationToken cancellationToken);
    Task<ContractBillingRecord?> GetContractBillingAsync(Guid contractId, BillingReadScope scope, CancellationToken cancellationToken);
}
