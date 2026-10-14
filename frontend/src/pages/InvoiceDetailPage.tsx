import { Link, useParams } from 'react-router-dom'
import { presentApiError } from '../api/apiErrorPresentation'
import { billingApi } from '../api/billingApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { CustomerStatusBadge as StatusBadge } from '../components/CustomerStatusBadge'
import { CustomerInvoiceContext } from '../components/CustomerInvoiceContext'
import { usePublicRead } from '../features/catalog/usePublicCatalog'
import { formatMoney, formatUtcDateTime } from '../utils/formatters'

const readInvoice = (invoiceId: string) => billingApi.getInvoice(invoiceId)

export function InvoiceDetailPage() {
  const { invoiceId = '' } = useParams()
  // Reuse the keyed reader with authenticated billingApi, not a public request.
  const { data: response, loading, error, refresh } = usePublicRead(invoiceId, readInvoice, true, presentApiError)
  const invoice = response?.data

  return (
    <main className="page-container flow-page narrow-page">
      <section className="page-heading page-heading-actions">
        <div>
          <p className="eyebrow">Payment</p>
          <h1>Invoice Details</h1>
        </div>
        <Link className="button button-secondary" to="/customer/invoices">
          My Invoices
        </Link>
      </section>

      <ApiErrorAlert error={error} />
      <button className="button button-secondary" type="button" disabled={loading} onClick={refresh}>Refresh Invoice</button>
      {loading ? (
        <LoadingState />
      ) : (
        invoice && (
          <section className="panel stack">
            <div className="card-heading-row">
              <h2>{invoice.invoiceType === 'DEPOSIT' ? 'Deposit Invoice' : 'Rental Invoice'}</h2>
              <StatusBadge status={invoice.status} />
            </div>
            <CustomerInvoiceContext invoice={invoice} />
            <dl className="detail-list">
              <div>
                <dt>Base Amount</dt>
                <dd>{formatMoney(invoice.baseAmount)}</dd>
              </div>
              <div>
                <dt>Discount</dt>
                <dd>{formatMoney(invoice.discountAmount)}</dd>
              </div>
              <div>
                <dt>Amount Due</dt>
                <dd>{formatMoney(invoice.amountDue)}</dd>
              </div>
              <div>
                <dt>Billing Month</dt>
                <dd>{invoice.billingMonth ?? 'Not Applicable'}</dd>
              </div>
              <div>
                <dt>Payment Deadline</dt>
                <dd>{formatUtcDateTime(invoice.dueDate)}</dd>
              </div>
              <div>
                <dt>Paid At</dt>
                <dd>{invoice.paidAt ? formatUtcDateTime(invoice.paidAt) : 'Not Recorded'}</dd>
              </div>
            </dl>
          </section>
        )
      )}
    </main>
  )
}
