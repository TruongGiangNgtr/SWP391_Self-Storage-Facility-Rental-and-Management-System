import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { presentApiError } from '../api/apiErrorPresentation'
import type { ApiErrorPresentation } from '../api/apiErrorPresentation'
import type { Pagination } from '../api/api.types'
import { billingApi } from '../api/billingApi'
import { CustomerListToolbar } from '../components/CustomerListToolbar'
import { useCustomerListFilter } from '../features/customer-list/useCustomerListFilter'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState, PaginationControls } from '../components/PageStates'
import { CustomerStatusBadge as StatusBadge } from '../components/CustomerStatusBadge'
import { CustomerInvoiceContext } from '../components/CustomerInvoiceContext'
import type { InvoiceDetail } from '../features/billing/billing.types'
import { formatMoney, formatUtcDateTime } from '../utils/formatters'

const EMPTY_PAGINATION: Pagination = {
  page: 1,
  pageSize: 20,
  totalItems: 0,
  totalPages: 0,
}

export function InvoiceListPage() {
  const filter = useCustomerListFilter()
  const [page, setPage] = useState(1)
  const [retry, setRetry] = useState(0)
  const [invoices, setInvoices] = useState<InvoiceDetail[]>([])
  const [pagination, setPagination] = useState(EMPTY_PAGINATION)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<ApiErrorPresentation | null>(null)

  useEffect(() => {
    let active = true

    billingApi
      .listInvoices(page)
      .then((response) => {
        if (active) {
          setInvoices(response.data)
          setPagination(response.pagination)
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
  }, [page, retry])

  return (
    <main className="page-container flow-page">
      <section className="page-heading">
        <p className="eyebrow">Payment</p>
        <h1>Deposit &amp; Invoices</h1>
        <p className="muted">Review deposit and rental invoices, payment deadlines and recorded status.</p>
      </section>

      <ApiErrorAlert error={error} />
      <button className="button button-secondary" type="button" disabled={loading} onClick={() => { setLoading(true); setError(null); setRetry(value => value + 1) }}>Refresh Invoices</button>
      {loading ? (
        <LoadingState />
      ) : error ? null : invoices.length === 0 ? (
        <EmptyState message="You do not have any invoices yet." />
      ) : (
        <><CustomerListToolbar {...filter.controls} statuses={["UNPAID","PAID","OVERDUE","CANCELLED"]} visibleCount={invoices.filter(invoice => filter.matches(`${invoice.invoiceId} ${invoice.invoiceType} ${invoice.billingMonth ?? ''}`, invoice.status)).length} /><div className="card-grid list-grid">
          {invoices.filter(invoice => filter.matches(`${invoice.invoiceId} ${invoice.invoiceType} ${invoice.billingMonth ?? ''}`, invoice.status)).map((invoice) => (
            <article className="summary-card" key={invoice.invoiceId}>
              <div className="card-heading-row">
                <h2>{invoice.invoiceType === 'DEPOSIT' ? 'Deposit' : 'Rental Fee'}</h2>
                <StatusBadge status={invoice.status} />
              </div>
              <CustomerInvoiceContext invoice={invoice} />
              <strong className="amount">{formatMoney(invoice.amountDue)}</strong>
              <p className="muted">Payment Deadline: {formatUtcDateTime(invoice.dueDate)}</p>
              <Link className="button button-secondary" to={invoice.invoiceId}>
                View Details
              </Link>
            </article>
          ))}
        </div></>
      )}

      <PaginationControls
        disabled={loading}
        pagination={pagination}
        onPageChange={(nextPage) => {
          setLoading(true)
          setError(null)
          setPage(nextPage)
        }}
      />
    </main>
  )
}
