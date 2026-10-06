import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { presentApiError } from '../api/apiErrorPresentation'
import type { ApiErrorPresentation } from '../api/apiErrorPresentation'
import { billingApi } from '../api/billingApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { StatusBadge } from '../components/StatusBadge'
import type { InvoiceDetail } from '../features/billing/billing.types'
import { formatMoney, formatUtcDateTime } from '../utils/formatters'

export function InvoiceDetailPage() {
  const { invoiceId = '' } = useParams()
  const [invoice, setInvoice] = useState<InvoiceDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<ApiErrorPresentation | null>(null)

  useEffect(() => {
    let active = true
    billingApi
      .getInvoice(invoiceId)
      .then((response) => {
        if (active) {
          setInvoice(response.data)
        }
      })
      .catch((requestError: unknown) => {
        if (active) {
          setError(presentApiError(requestError))
        }
      })
      .finally(() => {
        if (active) {
          setLoading(false)
        }
      })

    return () => {
      active = false
    }
  }, [invoiceId])

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
      {loading ? (
        <LoadingState />
      ) : (
        invoice && (
          <section className="panel stack">
            <div className="card-heading-row">
              <h2>{invoice.invoiceType === 'DEPOSIT' ? 'Deposit Invoice' : 'Rental Invoice'}</h2>
              <StatusBadge status={invoice.status} />
            </div>
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
                <dd>{invoice.paidAt ? formatUtcDateTime(invoice.paidAt) : 'Not Paid'}</dd>
              </div>
            </dl>
            {invoice.invoiceType === 'DEPOSIT' && (
              <Link className="button" to={`/customer/reservations/${invoice.entityId}`}>
                Open Reservation
              </Link>
            )}
          </section>
        )
      )}
    </main>
  )
}
