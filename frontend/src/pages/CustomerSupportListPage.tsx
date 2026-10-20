import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { CheckCircle2, CircleHelp, Clock3, Info, MessageSquareText, Plus, RefreshCw, Search, Ticket, XCircle } from 'lucide-react'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { listSupportTickets } from '../api/supportTicketApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState, PaginationControls } from '../components/PageStates'
import { CustomerStatusBadge as StatusBadge } from '../components/CustomerStatusBadge'
import { customerLabel } from '../utils/customerPresentation'
import { isSupportTicketListResponse } from '../features/customer-support/supportValidation'
import { SUPPORT_TICKET_STATUSES, type CollectionEnvelope, type SupportTicketDetail } from '../features/manager-support/models/supportTicket'
import { formatUtcDateTime } from '../utils/formatters'
import '../styles/customer-support.css'

function SupportTicketCard({ ticket }: { ticket: SupportTicketDetail }) {
  const ResultIcon = ticket.resultNote ? MessageSquareText : ticket.status === 'COMPLETED' ? CheckCircle2 : ticket.status === 'CANCELLED' ? XCircle : ticket.status === 'IN_PROGRESS' ? Clock3 : Info
  return <article className={`panel support-ticket-card support-ticket-${ticket.status.toLowerCase().replaceAll('_', '-')}`}>
    <header className="support-ticket-heading"><div><span className="support-ticket-label"><Ticket size={15} aria-hidden="true" />Contract Support</span><h2>{customerLabel(ticket.category)}</h2></div><StatusBadge status={ticket.status} /></header>
    <p className="support-ticket-description">{ticket.description}</p>
    <div className="support-ticket-result"><ResultIcon size={18} aria-hidden="true" /><div><strong>{ticket.resultNote ? 'Staff Result' : 'Recorded Status'}</strong><p>{ticket.resultNote || (ticket.status === 'CANCELLED' ? 'This ticket has been cancelled.' : ticket.status === 'COMPLETED' ? 'This ticket is completed. No Staff result is available in this record.' : ticket.assignedEmployeeId ? 'Staff is assigned. No result has been recorded yet.' : 'No Staff result has been recorded yet.')}</p></div></div>
    <footer className="support-ticket-footer"><div><p>Submitted on: <time dateTime={ticket.createdAt}>{formatUtcDateTime(ticket.createdAt)}</time></p>{ticket.completedAt && <p>Completed on: <time dateTime={ticket.completedAt}>{formatUtcDateTime(ticket.completedAt)}</time></p>}<Link className="support-text-link" to={`/customer/contracts/${encodeURIComponent(ticket.contractId)}`}>View Related Contract</Link></div><Link className="button button-secondary" to={`/customer/support-tickets/${encodeURIComponent(ticket.supportTicketId)}`}>View Ticket</Link></footer>
  </article>
}

export function CustomerSupportListPage() {
  const [params, setParams] = useSearchParams()
  const rawPage = Number(params.get('page') ?? 1)
  const page = Number.isSafeInteger(rawPage) && rawPage > 0 ? rawPage : 1
  const status = SUPPORT_TICKET_STATUSES.find(value => value === params.get('status')) ?? ''
  // A support description can be sensitive: keep the search text in memory, not the URL/storage.
  const [query, setQuery] = useState('')
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
      if (!isSupportTicketListResponse(response, page)) {
        setSnapshot({ key, response: null, error: presentValidationError('The server returned an unexpected support-ticket list. Refresh before continuing.') })
      } else setSnapshot({ key, response, error: null })
    }).catch((error: unknown) => { if (active) setSnapshot({ key, response: null, error: presentApiError(error) }) })
    return () => { active = false }
  }, [page, key])

  const tickets = response?.data ?? []
  const visible = tickets.filter(ticket => (!status || ticket.status === status) &&
    `${customerLabel(ticket.category)} ${ticket.description}`.toLocaleLowerCase('en').includes(query.trim().toLocaleLowerCase('en')))
  const filtered = Boolean(status || query.trim())
  function change(values: Record<string, string>) {
    const next = new URLSearchParams(params)
    Object.entries(values).forEach(([name, value]) => { if (value) next.set(name, value); else next.delete(name) })
    setParams(next)
  }
  function clearFilters() { setQuery(''); change({ status: '' }) }
  return <main className="page-container flow-page customer-support">
    <header className="support-page-heading">
      <div><p className="eyebrow">Customer Portal</p><h1>Support Tickets</h1><p className="muted">Track your submitted support requests and recorded Staff responses.</p></div>
      <div className="support-heading-actions">{response && <span className="support-total">{response.pagination.totalItems} {response.pagination.totalItems === 1 ? 'ticket' : 'tickets'} total</span>}<Link className="button" to="/customer/support-tickets/new"><Plus size={17} aria-hidden="true" />Create Ticket</Link></div>
    </header>
    <section className="support-info-banner" aria-label="About ticket updates"><span className="support-banner-icon"><MessageSquareText size={21} aria-hidden="true" /></span><div><h2>Your ticket updates, in one place</h2><p>Open a ticket to review its status and Staff result. Support requests relate to your contracts and do not automatically change a rental or payment.</p></div></section>
    <section className="panel support-filter" aria-label="Filter tickets on this page">
      <div className="form-field"><label htmlFor="support-search">Search tickets on this page</label><div className="support-search"><Search size={17} aria-hidden="true" /><input id="support-search" type="search" placeholder="Search by category or description" value={query} onChange={event => setQuery(event.target.value)} /></div></div>
      <div className="support-status-filters" role="group" aria-label="Filter by ticket status">{['', ...SUPPORT_TICKET_STATUSES].map(value => <button key={value} className="support-status-filter" type="button" aria-pressed={status === value} onClick={() => change({ status: value })}>{value ? customerLabel(value) : 'All'}</button>)}</div>
    </section>
    <div className="support-list-tools"><p className="muted" aria-live="polite">{loading ? 'Loading your support tickets…' : error ? 'Ticket records could not be loaded.' : `${visible.length} matching ${visible.length === 1 ? 'ticket' : 'tickets'} of ${tickets.length} loaded on this page. Filters do not search other pages.`}</p><div>{filtered && <button className="support-text-button" type="button" onClick={clearFilters}>Clear Filters</button>}<button className="support-text-button" disabled={loading} type="button" onClick={() => setRetry(value => value + 1)}><RefreshCw size={14} aria-hidden="true" />{loading ? 'Refreshing…' : 'Refresh Tickets'}</button></div></div>
    <ApiErrorAlert error={error} />
    {loading ? <LoadingState label="Loading your support tickets..." /> : response && <>
      {!tickets.length ? <section className="panel support-empty"><CircleHelp size={28} aria-hidden="true" /><EmptyState message={response.pagination.totalItems === 0 ? 'You do not have any support tickets yet. Create a ticket when you need help with one of your contracts.' : 'There are no support tickets on this page. Review another page to find your requests.'} />{page > 1 && <button className="button button-secondary" type="button" onClick={() => change({ page: '' })}>Go to First Page</button>}</section> : !visible.length ? <section className="panel support-empty"><Search size={28} aria-hidden="true" /><EmptyState message="No tickets on this page match your filters. Clear filters or review another page." /><button className="button button-secondary" type="button" onClick={clearFilters}>Clear Filters</button></section> : <section className="support-ticket-list" aria-label="Your support tickets">{visible.map(ticket => <SupportTicketCard key={ticket.supportTicketId} ticket={ticket} />)}</section>}
      <PaginationControls pagination={response.pagination} disabled={loading} onPageChange={next => change({ page: String(next) })} />
    </>}
  </main>
}
