import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import type { ApiCollectionResponse } from '../api/api.types'
import { presentApiError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { visitApi } from '../api/visitApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState, PaginationControls } from '../components/PageStates'
import { StatusBadge } from '../components/StatusBadge'
import { isReservationWorkItem, type StaffWorkItem } from '../features/handover/handover.types'
import { getCurrentBusinessDate } from '../utils/formatters'
import '../styles/staff.css'

export function StaffWorkItemsPage() {
  const [date, setDate] = useState(getCurrentBusinessDate)
  const [page, setPage] = useState(1)
  const [retry, setRetry] = useState(0)
  const [snapshot, setSnapshot] = useState<{ key: string; response: ApiCollectionResponse<StaffWorkItem> | null; error: ApiErrorPresentation | null } | null>(null)
  const key = `${date}:${page}:${retry}`
  const loading = snapshot?.key !== key
  const response = loading ? null : snapshot.response
  const error = loading ? null : snapshot.error

  useEffect(() => {
    let active = true
    visitApi.listStaffWorkItems({ date, page })
      .then((response) => { if (active) setSnapshot({ key, response, error: null }) })
      .catch((caughtError: unknown) => { if (active) setSnapshot({ key, response: null, error: presentApiError(caughtError) }) })
    return () => { active = false }
  }, [date, page, key])

  return (
    <main className="page-container flow-page staff-flow">
      <section className="page-heading">
        <p className="eyebrow">Facility Staff</p>
        <h1>Daily Work List</h1>
        <p className="muted">Review arrivals at your facility. Open a reservation visit to check in the customer and process handover.</p>
      </section>
      <section className="panel action-row" aria-label="Work list filters">
        <div className="form-field">
          <label htmlFor="workDate">Visit Date (GMT+7)</label>
          <input id="workDate" type="date" value={date} onChange={(event) => {
            if (event.target.value) { setDate(event.target.value); setPage(1) }
          }} />
        </div>
        <button className="button button-secondary" type="button" disabled={loading} onClick={() => setRetry((value) => value + 1)}>Refresh Work List</button>
      </section>
      <ApiErrorAlert error={error} />
      {loading ? <LoadingState label="Loading facility work items..." /> : error ? null : response && (
        <>
          <p className="muted">{response.pagination.totalItems} work items for {date}. Access and return processing will be implemented in their respective flows.</p>
          {response.data.length === 0 ? <EmptyState message="No work items for this date." /> : (
            <div className="card-grid list-grid">
              {response.data.map((item) => (
                <article className="summary-card" key={`${item.workType}-${item.referenceId}`}>
                  <div className="card-heading-row">
                    <h2>{item.workType === 'RESERVATION_VISIT' ? 'Reservation Visit' : item.workType.replaceAll('_', ' ')}</h2>
                    <StatusBadge status={item.status} />
                  </div>
                  {isReservationWorkItem(item) && <p><strong>{item.customer.fullName}</strong><br /><span className="muted">{item.customer.phoneNumber}</span></p>}
                  <dl className="detail-list compact staff-work-detail">
                    <div><dt>Scheduled Date</dt><dd>{item.scheduledDate}</dd></div>
                    <div><dt>Reference ID</dt><dd>{item.referenceId}</dd></div>
                  </dl>
                  {isReservationWorkItem(item) ? (
                    <Link className="button button-secondary" to={`/staff/reservations/${encodeURIComponent(item.entityId)}/visits/${encodeURIComponent(item.referenceId)}?workPage=${page}`}>
                      {item.status === 'SCHEDULED' ? 'Open Check-in' : item.status === 'CHECKED_IN' ? 'Open Handover' : 'View Visit'}
                    </Link>
                  ) : <p className="muted">{item.workType === 'RESERVATION_VISIT' ? 'The server has not supplied the reservation/customer context required to open this visit.' : 'Read-only in this flow.'}</p>}
                </article>
              ))}
            </div>
          )}
          <PaginationControls pagination={response.pagination} disabled={loading} onPageChange={setPage} />
        </>
      )}
    </main>
  )
}
