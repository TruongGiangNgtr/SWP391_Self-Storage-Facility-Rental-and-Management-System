import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { CalendarCheck2, CalendarDays, CheckCircle2, Info, MapPin, RefreshCw, Send, TriangleAlert, Undo2, Warehouse } from 'lucide-react'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { ApiRequestError } from '../api/httpClient'
import { visitApi } from '../api/visitApi'
import { CustomerActionFrame } from '../components/CustomerActionFrame'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { CalendarDateField } from '../components/CalendarDateField'
import { LoadingState } from '../components/PageStates'
import { CustomerStatusBadge as StatusBadge } from '../components/CustomerStatusBadge'
import { contractTitle, contractUnit } from '../utils/customerPresentation'
import { isUuid } from '../features/handover/handoverValidation'
import { isCalendarDate } from '../features/visits/accessValidation'
import { useAccessContract } from '../features/visits/useAccessContract'
import { useReservationFacilities } from '../features/reservations/useReservationUnitTypes'
import '../styles/customer-return-request.css'
import '../styles/renewal-return-flow.css'

export function ReturnVisitCreatePage() {
  const { contractId = '' } = useParams()
  const { contract, loading, error, refresh } = useAccessContract(contractId)
  const facilities = useReservationFacilities(contract ? [contract.facilityId] : [])
  const facility = contract ? facilities.reads[contract.facilityId]?.data : null
  const [visitDate, setVisitDate] = useState('')
  const [dateError, setDateError] = useState<string | null>(null)
  const [working, setWorking] = useState(false)
  const [actionError, setActionError] = useState<ApiErrorPresentation | null>(null)
  const submitBusy = useRef(false)
  const active = useRef(false)
  const navigate = useNavigate()

  useEffect(() => {
    active.current = true
    return () => { active.current = false }
  }, [])

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (submitBusy.current || loading || !contract || contract.status !== 'ACTIVE') return
    if (!isCalendarDate(visitDate)) {
      setDateError('Please select a valid return visit date.')
      return
    }

    submitBusy.current = true
    setDateError(null)
    setWorking(true)
    setActionError(null)
    try {
      const { data: visit } = await visitApi.createReturn(contractId, { visitDate })
      if (!active.current) return
      if (!visit || !isUuid(visit.visitId) || visit.entityId !== contractId ||
        visit.visitType !== 'RETURN' || visit.status !== 'SCHEDULED' ||
        visit.visitDate !== visitDate || visit.actualReturnDate !== null || visit.employeeId !== null) {
        setActionError(presentValidationError('We could not confirm your return visit. Check My Visits before submitting again, as your visit may already have been scheduled.'))
        refresh()
        return
      }
      navigate(`/customer/visits/${encodeURIComponent(visit.visitId)}`)
    } catch (caughtError) {
      if (!active.current) return
      setActionError(presentApiError(caughtError))
      if (caughtError instanceof ApiRequestError && caughtError.status === 409) refresh()
    } finally {
      submitBusy.current = false
      if (active.current) setWorking(false)
    }
  }

  return <CustomerActionFrame contractId={contractId} title="Request Storage Return" category="" subtitle="Choose your requested return date. The facility records the physical return, then completes inspection and settlement." className="customer-return-request completion-flow" headingIcon={<Undo2 size={24} />} busy={working}>
    {close => <>
      <ApiErrorAlert error={actionError ?? error} />
      <div className="return-request-toolbar"><button className="return-request-text-button" type="button" disabled={loading || working} onClick={() => { setActionError(null); facilities.retry(); refresh() }}><RefreshCw size={14} aria-hidden="true" />{loading ? 'Refreshing…' : 'Refresh Contract'}</button></div>
      {loading ? <LoadingState label="Loading your contract..." /> : contract && <>
        <section className="return-request-context" aria-label="Return contract">
          <dl className="return-request-facts">
            <div><dt>Contract</dt><dd className="return-request-contract-title"><h2>{contractTitle(contract)}</h2><StatusBadge status={contract.status} /></dd></div>
            <div><dt>Current Storage Unit</dt><dd><Warehouse size={15} aria-hidden="true" />{contractUnit(contract)}</dd></div>
            <div><dt>Facility Location</dt><dd><MapPin size={15} aria-hidden="true" />{facility?.name ?? (facilities.loading ? 'Loading facility…' : 'Facility unavailable')}</dd></div>
            <div><dt>Contract End Month</dt><dd><CalendarDays size={15} aria-hidden="true" />{contract.endMonth}</dd></div>
          </dl>
          <ApiErrorAlert error={facilities.error} />
        </section>
        {contract.status === 'ACTIVE' ? <form id="return-request-form" className="return-request-form" aria-label="Schedule return visit" onSubmit={handleCreate} noValidate>
          <section className="return-request-date" aria-label="Choose requested return date"><CalendarDateField id="returnVisitDate" label="Requested Return Date (GMT+7)" showCalendar={false} value={visitDate} disabled={working} error={dateError} onChange={value => { setVisitDate(value); setDateError(null) }} /><p className="return-request-helper">Choose the day you plan to return your storage at the facility. Your requested date and eligibility are checked when you submit.</p></section>
          <section className="return-request-policy" aria-labelledby="return-policy-heading"><div className="return-request-section-heading"><h2 id="return-policy-heading">Return Policy Overview</h2><span>Based on actual return</span></div><div className="return-request-policy-grid">
            <article className="return-policy-normal"><h3><CalendarCheck2 size={18} aria-hidden="true" />Normal Return</h3><p>Any refundable deposit is confirmed after inspection and settlement.</p><p className="return-policy-detail">Applicable late fees, extra fees and approved damage charges are considered in the final settlement.</p></article>
            <article className="return-policy-early"><h3><TriangleAlert size={18} aria-hidden="true" />Early Return</h3><p>An early return has no deposit refund. The final amount due is determined through settlement.</p><p className="return-policy-detail">The actual return recorded by the facility determines classification, not this requested date.</p></article>
          </div></section>
          <div className="return-request-notice"><Info size={20} aria-hidden="true" /><div><h2>A visit, not a contract cancellation</h2><p>Scheduling this visit does not end your contract or confirm a refund. While a return visit is pending, new access visits and contract renewal are blocked.</p></div></div>
          <p className="return-request-outcome"><CheckCircle2 size={17} aria-hidden="true" /><span>Your final return outcome is confirmed after staff records the actual return, inspection is completed and settlement is finalized.</span></p>
        </form> : <p className="notice">You can only schedule a return visit for an active contract. Open My Contracts to check its latest status.</p>}
      </>}
      {actionError && <p className="return-request-helper" role="note">If the request may already have been processed, check My Visits before retrying to avoid scheduling a duplicate return visit. This page never automatically resubmits your request.</p>}
      <footer className="return-request-actions"><div><button className="button button-secondary" type="button" disabled={working} onClick={close}>Cancel</button>{contract?.status === 'ACTIVE' && !loading && <button className="button" type="submit" form="return-request-form" disabled={working}>{working ? 'Submitting…' : 'Submit Return Request'}<Send size={16} aria-hidden="true" /></button>}</div><p>Submitting a request does not end your contract immediately.</p><Link className="return-request-text-link" to="/customer/visits" aria-disabled={working || undefined} onClick={event => { if (working) event.preventDefault() }}>View My Visits</Link></footer>
    </>}
  </CustomerActionFrame>
}
