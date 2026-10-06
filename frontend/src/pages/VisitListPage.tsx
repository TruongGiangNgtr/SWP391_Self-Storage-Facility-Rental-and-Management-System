import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { presentApiError } from '../api/apiErrorPresentation'
import type { ApiErrorPresentation } from '../api/apiErrorPresentation'
import type { Pagination } from '../api/api.types'
import { visitApi } from '../api/visitApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState, PaginationControls } from '../components/PageStates'
import { StatusBadge } from '../components/StatusBadge'
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
  const [page, setPage] = useState(1)
  const [visits, setVisits] = useState<VisitDetail[]>([])
  const [pagination, setPagination] = useState(EMPTY_PAGINATION)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<ApiErrorPresentation | null>(null)

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
  }, [page])

  return (
    <main className="page-container flow-page">
      <section className="page-heading">
        <nav className="flow-breadcrumb" aria-label="Breadcrumb"><Link to="/customer">Customer Portal</Link><span>/</span><span>Visits</span></nav>
        <p className="eyebrow">Visits</p>
        <h1>My Visits</h1>
        <p className="muted">Review your facility visits and update eligible scheduled visits.</p>
      </section>

      <ApiErrorAlert error={error} />
      {loading ? (
        <LoadingState />
      ) : error ? null : visits.length === 0 ? (
        <EmptyState message="You do not have any visits yet." />
      ) : (
        <div className="reservation-list">
          {visits.map((visit) => (
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
