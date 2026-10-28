import { FlowDialog } from '../components/FlowDialog'
import { type FormEvent, useCallback, useEffect, useRef, useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { ArrowLeft, ArrowRight, CalendarClock, CalendarDays, CircleX, ClipboardCheck, DoorOpen, Info, PackageCheck, RefreshCw, Undo2, UserRound } from 'lucide-react'
import { presentApiError } from '../api/apiErrorPresentation'
import type { ApiErrorPresentation } from '../api/apiErrorPresentation'
import { visitApi } from '../api/visitApi'
import { ApiRequestError } from '../api/httpClient'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { CustomerStatusBadge as StatusBadge } from '../components/CustomerStatusBadge'
import { CalendarDateField } from '../components/CalendarDateField'
import { CustomerFieldError } from '../components/CustomerFieldError'
import type { VisitDetail } from '../features/visits/visit.types'
import { VisitJourney } from '../components/AccessVisitJourney'
import '../styles/customer-visit-details.css'

const VISIT_NAMES: Record<VisitDetail['visitType'], string> = {
  RESERVATION: 'Handover',
  ACCESS: 'Access',
  RETURN: 'Return',
}

export function VisitDetailPage() {
  const { visitId = '' } = useParams()
  const [searchParams] = useSearchParams()
  const [visit, setVisit] = useState<VisitDetail | null>(null)
  const [newVisitDate, setNewVisitDate] = useState('')
  const [cancelReason, setCancelReason] = useState('')
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  // List shortcuts only open the existing form. Eligibility is checked after VIS-004;
  // no command is sent until the customer submits it explicitly.
  const [mode, setMode] = useState<'reschedule' | 'cancel' | null>(() => {
    const action = searchParams.get('action')
    return action === 'reschedule' || action === 'cancel' ? action : null
  })
  const [loading, setLoading] = useState(true)
  const [working, setWorking] = useState(false)
  const [error, setError] = useState<ApiErrorPresentation | null>(null)
  const actionBusy = useRef(false)

  const loadVisit = useCallback(async (preserveDraft = false) => {
    try {
      const response = await visitApi.get(visitId)
      setVisit(response.data)
      if (!preserveDraft) setNewVisitDate(response.data.visitDate)
    } catch (requestError) {
      setVisit(null)
      setError(presentApiError(requestError))
    } finally {
      setLoading(false)
    }
  }, [visitId])

  useEffect(() => {
    let active = true
    visitApi
      .get(visitId)
      .then((response) => {
        if (active) {
          setVisit(response.data)
          setNewVisitDate(response.data.visitDate)
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
  }, [visitId])

  async function handleReschedule(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (loading || actionBusy.current || visit?.status !== 'SCHEDULED') return
    if (!newVisitDate) {
      setFieldErrors({ date: 'Please select a new visit date.' })
      return
    }

    actionBusy.current = true
    setFieldErrors({})
    setWorking(true)
    setError(null)
    try {
      await visitApi.reschedule(visitId, { visitDate: newVisitDate })
      setMode(null)
      await loadVisit()
    } catch (requestError) {
      setError(presentApiError(requestError))
      if (requestError instanceof ApiRequestError && requestError.status === 409) {
        await loadVisit(true)
      }
    } finally {
      actionBusy.current = false
      setWorking(false)
    }
  }

  async function handleCancel(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (loading || actionBusy.current || visit?.status !== 'SCHEDULED') return
    if (!cancelReason.trim()) {
      setFieldErrors({ reason: 'Please enter a reason for cancelling the visit.' })
      return
    }

    actionBusy.current = true
    setWorking(true)
    setError(null)
    try {
      await visitApi.cancel(visitId, { reason: cancelReason.trim() })
      setMode(null)
      setCancelReason('')
      await loadVisit()
    } catch (requestError) {
      setError(presentApiError(requestError))
      if (requestError instanceof ApiRequestError && requestError.status === 409) {
        await loadVisit(true)
      }
    } finally {
      actionBusy.current = false
      setWorking(false)
    }
  }

  const VisitIcon = visit?.visitType === 'RESERVATION' ? PackageCheck : visit?.visitType === 'RETURN' ? Undo2 : DoorOpen
  return (
    <main className="page-container flow-page customer-visit-details">
      <div className="visit-detail-navigation"><Link className="visit-detail-text-link" to="/customer/visits"><ArrowLeft size={16} aria-hidden="true" />Back to My Visits</Link><button className="visit-detail-text-button" type="button" disabled={loading || working} onClick={() => {
        setLoading(true); setError(null); setMode(null); void loadVisit()
      }}><RefreshCw size={14} aria-hidden="true" />{loading ? 'Refreshing…' : 'Refresh Visit'}</button></div>
      <header className="page-heading visit-detail-heading"><h1>Visit Details</h1><p className="muted">Review your facility visit and manage its scheduled date or cancellation.</p></header>
      <ApiErrorAlert error={error} />
      {loading && !visit ? <LoadingState label="Loading visit..." /> : visit && <>
        {(visit.visitType === 'ACCESS' || visit.visitType === 'RESERVATION') && <VisitJourney status={visit.status} visitType={visit.visitType} />}
        <section className={`panel visit-detail-card visit-detail-${visit.status.toLowerCase().replaceAll('_', '-')}`} aria-label="Visit information">
          <header className="visit-detail-card-heading"><span className="visit-detail-icon"><VisitIcon size={25} aria-hidden="true" /></span><div><h2>{VISIT_NAMES[visit.visitType]}</h2><p>Facility visit</p></div><StatusBadge status={visit.status} /></header>
          <dl className={`visit-detail-facts${visit.visitType === 'RETURN' ? ' visit-detail-return-facts' : ''}`}>
            <div className="visit-detail-date"><span className="visit-detail-fact-icon"><CalendarDays size={22} aria-hidden="true" /></span><div><dt>Visit Date <span>(GMT+7)</span></dt><dd>{visit.visitDate}</dd></div></div>
            {visit.visitType === 'RETURN' && <div><span className="visit-detail-fact-icon"><ClipboardCheck size={22} aria-hidden="true" /></span><div><dt>Actual Return Date</dt><dd>{visit.actualReturnDate ?? 'Not Available'}</dd></div></div>}
            <div><span className="visit-detail-fact-icon"><UserRound size={22} aria-hidden="true" /></span><div><dt>Handling Staff</dt><dd>{visit.employeeId ? 'Recorded by the facility' : 'Not Recorded'}</dd></div></div>
          </dl>

          {visit.visitType === 'ACCESS' && <>
            <p className="visit-detail-notice"><Info size={18} aria-hidden="true" /><span>{visit.status === 'SCHEDULED' ? 'Your access visit is scheduled. Staff will check you in and out at the facility.'
              : visit.status === 'CHECKED_IN' ? 'You are checked in. Staff will check you out after your visit. This visit can no longer be rescheduled or cancelled.'
              : visit.status === 'CHECKED_OUT' ? 'This access visit is complete. Your rental contract is managed separately.' : 'This access visit has been cancelled.'}</span></p>
          </>}

          {visit.visitType === 'RESERVATION' && (
            <>
              {(visit.status === 'SCHEDULED' || visit.status === 'CHECKED_IN') && (
                <p className="visit-detail-notice"><Info size={18} aria-hidden="true" /><span>{visit.status === 'CHECKED_IN'
                    ? 'You are checked in. Staff will complete handover after receiving the first month offline and using the storage unit selected by the Manager.'
                    : 'Bring identification and pay the first month offline at the facility. Review your reservation for the locked first-month rental amount.'}</span></p>
              )}
            </>
          )}

          {(visit.visitType !== 'RETURN' || (visit.status === 'SCHEDULED' && mode !== 'cancel')) && <footer className="visit-detail-actions">
            {visit.visitType === 'ACCESS' && <Link className="visit-detail-text-link" to={`/customer/contracts/${encodeURIComponent(visit.entityId)}`}>View Contract<ArrowRight size={15} aria-hidden="true" /></Link>}
            {visit.visitType === 'RESERVATION' && <Link className="visit-detail-text-link" to={`/customer/reservations/${visit.entityId}`}>View Reservation<ArrowRight size={15} aria-hidden="true" /></Link>}
            {visit.status === 'SCHEDULED' && mode !== 'cancel' && <div><button className="button" disabled={loading || working} onClick={() => setMode('reschedule')} type="button"><CalendarClock size={16} aria-hidden="true" />Reschedule</button><button className="button button-danger" disabled={loading || working} onClick={() => setMode('cancel')} type="button"><CircleX size={16} aria-hidden="true" />Cancel Visit</button></div>}
          </footer>}
        </section>

          {visit.status === 'SCHEDULED' && mode === 'reschedule' && (
            <FlowDialog className="visit-reschedule-dialog" title="Reschedule Visit" subtitle="Choose a new date for this visit." category="" busy={working} trapFocus onClose={() => setMode(null)}><ApiErrorAlert error={error} /><form className="form-grid" onSubmit={handleReschedule}>
              <CalendarDateField id="newVisitDate" label="New Visit Date" value={newVisitDate} disabled={loading || working} error={fieldErrors.date} onChange={value => { setNewVisitDate(value); setFieldErrors({}) }} />
              <p className="visit-detail-edit-hint"><Info size={16} aria-hidden="true" />The new date must follow the policy for this visit.</p>
              <div className="action-row">
                <button className="button button-secondary" disabled={working} onClick={() => setMode(null)} type="button">
                  Close
                </button>
                <button className="button" disabled={working} type="submit">
                  {working ? 'Updating...' : 'Save New Date'}
                </button>
              </div>
            </form></FlowDialog>
          )}

          {visit.status === 'SCHEDULED' && mode === 'cancel' && (
            <section className="panel visit-detail-edit visit-detail-cancel" aria-labelledby="visit-cancel-heading"><div className="visit-detail-edit-heading"><span className="visit-detail-icon"><CircleX size={23} aria-hidden="true" /></span><h2 id="visit-cancel-heading">Cancel Visit</h2></div><form className="form-grid" onSubmit={handleCancel}>
              <div className="form-field">
                <label htmlFor="visitCancelReason">Cancellation Reason</label>
                <textarea
                  id="visitCancelReason"
                  rows={3}
                  value={cancelReason}
                  disabled={loading || working}
                  aria-invalid={Boolean(fieldErrors.reason)}
                  aria-describedby={fieldErrors.reason ? 'visitCancelReason-error' : undefined}
                  onChange={(event) => { setCancelReason(event.target.value); setFieldErrors({}) }}
                />
                <CustomerFieldError id="visitCancelReason-error" message={fieldErrors.reason} />
              </div>
              <div className="action-row">
                <button className="button button-secondary" disabled={working} onClick={() => setMode(null)} type="button">
                  Close
                </button>
                <button className="button button-danger" disabled={working} type="submit">
                  {working ? 'Cancelling...' : 'Confirm Visit Cancellation'}
                </button>
              </div>
            </form></section>
          )}
      </>}
    </main>
  )
}
