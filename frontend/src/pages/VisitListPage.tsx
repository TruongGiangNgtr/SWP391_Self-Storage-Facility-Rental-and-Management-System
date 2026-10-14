import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { presentApiError } from '../api/apiErrorPresentation'
import type { ApiErrorPresentation } from '../api/apiErrorPresentation'
import type { Pagination } from '../api/api.types'
import { visitApi } from '../api/visitApi'
import { CustomerListToolbar } from '../components/CustomerListToolbar'
import { useCustomerListFilter } from '../features/customer-list/useCustomerListFilter'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState, PaginationControls } from '../components/PageStates'
import { CustomerStatusBadge as StatusBadge } from '../components/CustomerStatusBadge'
import { FlowIcon } from '../components/FlowIcon'
import type { VisitDetail } from '../features/visits/visit.types'

const EMPTY_PAGINATION: Pagination = {
  page: 1,
  pageSize: 20,
  totalItems: 0,
  totalPages: 0,
}

const VISIT_NAMES: Record<VisitDetail['visitType'], string> = {
  RESERVATION: 'Handover',
  ACCESS: 'Access',
  RETURN: 'Return',
}

export function VisitListPage() {
  const filter = useCustomerListFilter()
  const [page, setPage] = useState(1)
  const [visits, setVisits] = useState<VisitDetail[]>([])
  const [pagination, setPagination] = useState(EMPTY_PAGINATION)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<ApiErrorPresentation | null>(null)
  const [retry, setRetry] = useState(0)

  useEffect(() => {
    let active = true

    visitApi
      .list(page)
      .then((response) => {
        if (active) {
          setVisits(response.data)
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
        <p className="eyebrow">Visits</p>
        <h1>My Visits</h1>
        <p className="muted">Review your facility visits and update eligible scheduled visits.</p>
        <div className="action-row">
          <Link className="button" to="/customer/contracts">Schedule Access Visit</Link>
          <button className="button button-secondary" type="button" disabled={loading} onClick={() => { setLoading(true); setError(null); setRetry((value) => value + 1) }}>Refresh Visits</button>
        </div>
      </section>

      <ApiErrorAlert error={error} />
      {loading ? (
        <LoadingState />
      ) : error ? null : visits.length === 0 ? (
        <EmptyState message="You do not have any visits yet." />
      ) : (
        <><CustomerListToolbar {...filter.controls} statuses={["SCHEDULED","CHECKED_IN","CHECKED_OUT","CANCELLED"]} visibleCount={visits.filter(visit => filter.matches(`${visit.visitId} ${VISIT_NAMES[visit.visitType]} ${visit.visitDate}`, visit.status)).length} /><div className="reservation-list">
          {visits.filter(visit => filter.matches(`${visit.visitId} ${VISIT_NAMES[visit.visitType]} ${visit.visitDate}`, visit.status)).map((visit) => (
            <article className={`reservation-card reservation-card-${visit.status === 'CANCELLED' ? 'cancelled' : 'confirmed'}`} key={visit.visitId}>
              <div className="card-heading-row">
                <h2>{VISIT_NAMES[visit.visitType]}</h2>
                <StatusBadge status={visit.status} />
              </div>
              <p className="flow-facility-address"><FlowIcon name="calendar" />Visit Date: {visit.visitDate}</p>
              <div className="action-row"><Link className="button button-secondary" to={visit.visitId}>
                View Details
              </Link></div>
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
