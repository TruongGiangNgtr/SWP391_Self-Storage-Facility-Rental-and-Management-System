import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { listSupportTickets } from '../api/supportTicketApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState, PaginationControls } from '../components/PageStates'
import { StatusBadge } from '../components/StatusBadge'
import { isSupportPagination, isSupportTicket } from '../features/customer-support/supportValidation'
import type { CollectionEnvelope, SupportTicketDetail } from '../features/manager-support/models/supportTicket'
import { formatUtcDateTime } from '../utils/formatters'

export function CustomerSupportListPage() {
  const [page, setPage] = useState(1)
  const [retry, setRetry] = useState(0)
  const key = `${page}:${retry}`
  const [snapshot, setSnapshot] = useState<{ key: string; response: CollectionEnvelope<SupportTicketDetail> | null; error: ApiErrorPresentation | null } | null>(null)
  const loading = snapshot?.key !== key
  const response = loading ? null : snapshot.response
  const error = loading ? null : snapshot.error

  useEffect(() => {
    let active = true
    listSupportTickets({ page, pageSize: 20 }).then(response => {
      if (!active) return
      if (!Array.isArray(response.data) || !response.data.every(isSupportTicket) || !isSupportPagination(response.pagination)) {
        setSnapshot({ key, response: null, error: presentValidationError('The server returned an unexpected support-ticket list. Refresh before continuing.') })
      } else setSnapshot({ key, response, error: null })
    }).catch((error: unknown) => { if (active) setSnapshot({ key, response: null, error: presentApiError(error) }) })
    return () => { active = false }
  }, [page, key])

  return <main className="page-container flow-page">
    <section className="page-heading page-heading-actions">
      <div><p className="eyebrow">Customer Support</p><h1>My Support Tickets</h1><p className="muted">Track issues related to your contracts and read the Staff's result when available.</p></div>
      <Link className="button" to="/customer/support-tickets/new">Create Support Ticket</Link>
    </section>
    <button className="button button-secondary" disabled={loading} type="button" onClick={() => setRetry(value => value + 1)}>Refresh Tickets</button>
    <ApiErrorAlert error={error} />
    {loading ? <LoadingState label="Loading your support tickets..." /> : response && <>
      {response.data.length === 0 ? <EmptyState message="You do not have any support tickets yet." /> : <div className="reservation-list">
        {response.data.map(ticket => <article className="reservation-card" key={ticket.supportTicketId}>
          <div className="card-heading-row"><h2>{ticket.category.replaceAll('_', ' ')}</h2><StatusBadge status={ticket.status} /></div>
          <p>{ticket.description}</p>
          <dl className="detail-list compact"><div><dt>Contract ID</dt><dd>{ticket.contractId}</dd></div><div><dt>Created (GMT+7)</dt><dd>{formatUtcDateTime(ticket.createdAt)}</dd></div></dl>
          <Link className="button button-secondary" to={`/customer/support-tickets/${encodeURIComponent(ticket.supportTicketId)}`}>View Ticket</Link>
        </article>)}
      </div>}
      <PaginationControls pagination={response.pagination} disabled={loading} onPageChange={setPage} />
    </>}
  </main>
}
