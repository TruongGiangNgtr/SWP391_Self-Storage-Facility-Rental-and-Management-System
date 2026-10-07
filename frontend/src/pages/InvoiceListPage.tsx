import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { presentApiError } from '../api/apiErrorPresentation'
import type { ApiErrorPresentation } from '../api/apiErrorPresentation'
import type { Pagination } from '../api/api.types'
import { billingApi } from '../api/billingApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState, PaginationControls } from '../components/PageStates'
import { StatusBadge } from '../components/StatusBadge'
import type { InvoiceDetail } from '../features/billing/billing.types'
import { formatMoney, formatUtcDateTime } from '../utils/formatters'

const EMPTY_PAGINATION: Pagination = {
  page: 1,
  pageSize: 20,
  totalItems: 0,
  totalPages: 0,
}

export function InvoiceListPage() {
  const [page, setPage] = useState(1)
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
  }, [page])

  return (
    <main className="page-container flow-page">
      <section className="page-heading">
        <p className="eyebrow">Payment</p>
        <h1>My Invoices</h1>
      </section>

      <ApiErrorAlert error={error} />
      {loading ? (
        <LoadingState />
      ) : error ? null : invoices.length === 0 ? (
        <EmptyState message="You do not have any invoices yet." />
      ) : (
        <div className="card-grid list-grid">
          {invoices.map((invoice) => (
            <article className="summary-card" key={invoice.invoiceId}>
              <div className="card-heading-row">
                <h2>{invoice.invoiceType === 'DEPOSIT' ? 'Deposit' : 'Rental Fee'}</h2>
                <StatusBadge status={invoice.status} />
              </div>
              <strong className="amount">{formatMoney(invoice.amountDue)}</strong>
              <p className="muted">Payment Deadline: {formatUtcDateTime(invoice.dueDate)}</p>
              <Link className="button button-secondary" to={invoice.invoiceId}>
                View Details
              </Link>
            </article>
          ))}
        </div>
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
