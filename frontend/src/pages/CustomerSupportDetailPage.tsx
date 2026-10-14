import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { cancelSupportTicket, getSupportTicket } from '../api/supportTicketApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { CustomerStatusBadge as StatusBadge } from '../components/CustomerStatusBadge'
import { customerLabel } from '../utils/customerPresentation'
import { CustomerFieldError } from '../components/CustomerFieldError'
import { canCancelSupportTicket, isSupportTicket } from '../features/customer-support/supportValidation'
import type { SupportTicketDetail } from '../features/manager-support/models/supportTicket'
import { formatUtcDateTime } from '../utils/formatters'

export function CustomerSupportDetailPage() {
  const { ticketId = '' } = useParams()
  const [retry, setRetry] = useState(0)
  const [reason, setReason] = useState('')
  const [reasonError, setReasonError] = useState<string | null>(null)
  const [working, setWorking] = useState(false)
  const [actionError, setActionError] = useState<ApiErrorPresentation | null>(null)
  const [snapshot, setSnapshot] = useState<{ key: string; ticket: SupportTicketDetail | null; error: ApiErrorPresentation | null } | null>(null)
  const key = `${ticketId}:${retry}`
  const loading = snapshot?.key !== key
  const ticket = loading ? null : snapshot.ticket
  const readError = loading ? null : snapshot.error
  const busy = useRef(false)
  const mounted = useRef(false)
  useEffect(() => { mounted.current = true; return () => { mounted.current = false } }, [])
  useEffect(() => {
    let active = true
    getSupportTicket(ticketId).then(({ data }) => {
      if (!active) return
      if (!isSupportTicket(data) || data.supportTicketId !== ticketId) setSnapshot({ key, ticket: null, error: presentValidationError('The server returned an unexpected ticket. Refresh before continuing.') })
      else setSnapshot({ key, ticket: data, error: null })
    }).catch((error: unknown) => { if (active) setSnapshot({ key, ticket: null, error: presentApiError(error) }) })
    return () => { active = false }
  }, [ticketId, key])

  async function cancel(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (busy.current || loading || !ticket || !canCancelSupportTicket(ticket)) return
    if (!reason.trim()) { setReasonError('Enter a reason for cancelling this ticket.'); return }
    setReasonError(null)
    busy.current = true; setWorking(true); setActionError(null)
    try {
      const { data } = await cancelSupportTicket(ticketId, { reason: reason.trim() })
      if (!mounted.current) return
      if (!isSupportTicket(data) || data.supportTicketId !== ticketId || data.contractId !== ticket.contractId || data.status !== 'CANCELLED') {
        setActionError(presentValidationError('The server returned an unexpected cancellation result. Review the refreshed ticket before retrying.'))
      } else setReason('')
      setRetry(value => value + 1)
    } catch (error) {
      if (!mounted.current) return
      setActionError(presentApiError(error)); setRetry(value => value + 1)
    } finally { busy.current = false; if (mounted.current) setWorking(false) }
  }

  return <main className="page-container flow-page narrow-page">
    <section className="page-heading page-heading-actions"><div><p className="eyebrow">Customer Support</p><h1>Support Ticket Details</h1></div><Link className="button button-secondary" to="/customer/support-tickets">My Support Tickets</Link></section>
    <ApiErrorAlert error={actionError} /><ApiErrorAlert error={readError} />
    <button className="button button-secondary" type="button" disabled={loading || working} onClick={() => { setActionError(null); setRetry(value => value + 1) }}>Refresh Ticket</button>
    {loading ? <LoadingState label="Loading your support ticket..." /> : ticket && <section className="panel stack">
      <div className="card-heading-row"><h2>{customerLabel(ticket.category)}</h2><StatusBadge status={ticket.status} /></div>
      <dl className="detail-list"><div><dt>Related Contract</dt><dd><Link to={`/customer/contracts/${encodeURIComponent(ticket.contractId)}`}>View Contract</Link></dd></div><div><dt>Description</dt><dd>{ticket.description}</dd></div><div><dt>Staff Assignment</dt><dd>{ticket.assignedEmployeeId ? 'Assigned' : 'Not Assigned'}</dd></div><div><dt>Staff Result</dt><dd>{ticket.resultNote ?? 'No result recorded yet.'}</dd></div><div><dt>Created (GMT+7)</dt><dd>{formatUtcDateTime(ticket.createdAt)}</dd></div><div><dt>Completed (GMT+7)</dt><dd>{ticket.completedAt ? formatUtcDateTime(ticket.completedAt) : 'Not Completed'}</dd></div></dl>
      <Link className="button button-secondary" to={`/customer/contracts/${encodeURIComponent(ticket.contractId)}`}>View Contract</Link>
      {canCancelSupportTicket(ticket) ? <form className="form-grid" onSubmit={cancel} noValidate>
        <h2>Cancel Ticket</h2><div className="form-field"><label htmlFor="supportCancelReason">Cancellation Reason</label><textarea id="supportCancelReason" value={reason} disabled={working} aria-invalid={Boolean(reasonError)} aria-describedby={reasonError ? 'supportCancelReason-error' : undefined} onChange={event => { setReason(event.target.value); setReasonError(null) }} /><CustomerFieldError id="supportCancelReason-error" message={reasonError} /></div>
        <p className="muted">Only eligible OPEN or IN_PROGRESS tickets can be cancelled. The backend checks the current state and ownership.</p>
        <button className="button button-danger" type="submit" disabled={working}>{working ? 'Cancelling...' : 'Cancel Support Ticket'}</button>
      </form> : <p className="notice">This ticket is {ticket.status}. It can no longer be cancelled.</p>}
    </section>}
    {actionError && <p className="notice">Review the refreshed ticket before retrying. No cancellation request is automatically replayed.</p>}
  </main>
}
