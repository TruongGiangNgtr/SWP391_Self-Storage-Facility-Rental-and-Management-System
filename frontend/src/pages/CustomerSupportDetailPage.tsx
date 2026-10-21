import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ArrowLeft, ArrowRight, ClipboardCheck, Info, MessageSquareText, RefreshCw, Ticket, XCircle } from 'lucide-react'
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
import '../styles/customer-support.css'
import '../styles/customer-support-pages.css'

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

  return <main className="page-container flow-page customer-support customer-support-pages support-detail-page">
    <div className="support-page-navigation">
      <Link className="support-text-link" to="/customer/support-tickets"><ArrowLeft size={16} aria-hidden="true" />My Support Tickets</Link>
      <button className="support-text-button" type="button" disabled={loading || working} onClick={() => { setActionError(null); setRetry(value => value + 1) }}><RefreshCw size={14} aria-hidden="true" />Refresh Ticket</button>
    </div>
    <header className="support-page-heading"><div><p className="eyebrow">Customer Support</p><h1>Support Ticket Details</h1><p className="muted">Review your request and the recorded Staff result.</p></div></header>
    <ApiErrorAlert error={actionError} /><ApiErrorAlert error={readError} />
    {loading ? <LoadingState label="Loading your support ticket..." /> : ticket && <>
      <section className={`panel support-page-card support-detail-${ticket.status.toLowerCase().replaceAll('_', '-')}`} aria-label="Support ticket information">
        <header className="support-page-card-heading"><span className="support-page-icon"><Ticket size={24} aria-hidden="true" /></span><div><span className="support-detail-label">Contract Support</span><h2>{customerLabel(ticket.category)}</h2></div><StatusBadge status={ticket.status} /></header>
        <div className="support-detail-columns">
          <dl className="support-detail-messages">
            <div><dt><MessageSquareText size={18} aria-hidden="true" />Description</dt><dd>{ticket.description}</dd></div>
            <div className="support-detail-result"><dt><ClipboardCheck size={18} aria-hidden="true" />Staff Result</dt><dd>{ticket.resultNote ?? 'No result recorded yet.'}</dd></div>
          </dl>
          <dl className="support-detail-facts">
            <div><dt>Related Contract</dt><dd><Link className="support-text-link" to={`/customer/contracts/${encodeURIComponent(ticket.contractId)}`}>View Contract<ArrowRight size={14} aria-hidden="true" /></Link></dd></div>
            <div><dt>Staff Assignment</dt><dd>{ticket.assignedEmployeeId ? 'Assigned' : 'Not Assigned'}</dd></div>
            <div><dt>Created (GMT+7)</dt><dd>{formatUtcDateTime(ticket.createdAt)}</dd></div>
            <div><dt>Completed (GMT+7)</dt><dd>{ticket.completedAt ? formatUtcDateTime(ticket.completedAt) : 'Not Completed'}</dd></div>
          </dl>
        </div>
        <footer className="support-detail-footer"><Link className="button button-secondary" to={`/customer/contracts/${encodeURIComponent(ticket.contractId)}`}>View Contract<ArrowRight size={15} aria-hidden="true" /></Link></footer>
      </section>
      {canCancelSupportTicket(ticket) ? <section className="panel support-page-card support-cancel-card" aria-labelledby="support-cancel-heading">
        <div className="support-page-card-heading"><span className="support-page-icon"><XCircle size={24} aria-hidden="true" /></span><h2 id="support-cancel-heading">Cancel Ticket</h2></div>
        <form className="form-grid support-cancel-form" onSubmit={cancel} noValidate>
          <div className="form-field"><label htmlFor="supportCancelReason">Cancellation Reason</label><textarea id="supportCancelReason" rows={4} value={reason} disabled={working} aria-invalid={Boolean(reasonError)} aria-describedby={reasonError ? 'supportCancelReason-error' : undefined} onChange={event => { setReason(event.target.value); setReasonError(null) }} /><CustomerFieldError id="supportCancelReason-error" message={reasonError} /></div>
          <p className="support-page-note">Only eligible OPEN or IN_PROGRESS tickets can be cancelled. The backend checks the current state and ownership.</p>
          <div className="support-form-actions"><button className="button button-danger" type="submit" disabled={working}><XCircle size={16} aria-hidden="true" />{working ? 'Cancelling...' : 'Cancel Support Ticket'}</button></div>
        </form>
      </section> : <p className="support-page-notice"><Info size={17} aria-hidden="true" /><span>This ticket is {ticket.status}. It can no longer be cancelled.</span></p>}
    </>}
    {actionError && <p className="support-page-notice support-page-warning"><Info size={17} aria-hidden="true" /><span>Review the refreshed ticket before retrying. No cancellation request is automatically replayed.</span></p>}
  </main>
}
