import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ArrowLeft, CalendarDays, DoorOpen, Info, RefreshCw } from 'lucide-react'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { ApiRequestError } from '../api/httpClient'
import { visitApi } from '../api/visitApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { CalendarDateField } from '../components/CalendarDateField'
import { LoadingState } from '../components/PageStates'
import { CustomerStatusBadge as StatusBadge } from '../components/CustomerStatusBadge'
import { contractTitle, contractUnit } from '../utils/customerPresentation'
import { isUuid } from '../features/handover/handoverValidation'
import { isCalendarDate } from '../features/visits/accessValidation'
import { useAccessContract } from '../features/visits/useAccessContract'
import { formatMonthRange } from '../utils/formatters'
import '../styles/guest-customer-remaining.css'

export function AccessVisitCreatePage() {
  const { contractId = '' } = useParams()
  const { contract, loading, error, refresh } = useAccessContract(contractId)
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
      setDateError('Please select a valid visit date.')
      return
    }
    submitBusy.current = true
    setDateError(null)
    setWorking(true)
    setActionError(null)
    try {
      const { data: visit } = await visitApi.createAccess(contractId, { visitDate })
      if (!active.current) return
      if (!visit || !isUuid(visit.visitId) || visit.visitType !== 'ACCESS' || visit.entityId !== contractId ||
        visit.status !== 'SCHEDULED' || visit.visitDate !== visitDate || visit.actualReturnDate !== null) {
        setActionError(presentValidationError('The server returned an unexpected access visit. Check My Visits before submitting again.'))
        refresh()
        return
      }
      navigate(`/customer/visits/${encodeURIComponent(visit.visitId)}`)
    } catch (error) {
      if (!active.current) return
      setActionError(presentApiError(error))
      if (error instanceof ApiRequestError && error.status === 409) refresh()
    } finally {
      submitBusy.current = false
      if (active.current) setWorking(false)
    }
  }

  return (
    <main className="page-container flow-page narrow-page customer-access-visit">
      <nav className="remaining-page-navigation" aria-label="Access visit navigation">
        <Link to={`/customer/contracts/${encodeURIComponent(contractId)}`}><ArrowLeft size={16} aria-hidden="true" />View Contract</Link>
        <button type="button" disabled={loading || working} onClick={() => { setActionError(null); refresh() }}><RefreshCw size={15} aria-hidden="true" />Refresh Contract</button>
      </nav>
      <section className="page-heading page-heading-actions">
        <div><p className="eyebrow">Access Visit</p><h1>Schedule Access Visit</h1><p className="muted">Choose a date to visit your rented storage unit.</p></div>
      </section>
      <ApiErrorAlert error={actionError ?? error} />
      {loading ? <LoadingState label="Loading your contract..." /> : contract && <>
        <section className="panel stack access-context" aria-label="Access contract">
          <div className="remaining-card-heading"><span className="remaining-icon"><DoorOpen size={22} aria-hidden="true" /></span><h2>Selected Contract</h2><StatusBadge status={contract.status} /></div>
          <dl className="remaining-facts access-contract-facts">
            <div><dt>Unit Type</dt><dd>{contractTitle(contract)}</dd></div>
            <div><dt>Storage Unit</dt><dd>{contractUnit(contract)}</dd></div>
            <div><dt>Rental Period</dt><dd>{formatMonthRange(contract.startMonth, contract.endMonth)}</dd></div>
          </dl>
        </section>
        {contract.status === 'ACTIVE' ? <section className="panel stack access-schedule" aria-label="Schedule access visit">
          <div className="remaining-card-heading"><span className="remaining-icon"><CalendarDays size={22} aria-hidden="true" /></span><h2>Visit Date</h2></div>
          <form className="access-visit-form" onSubmit={handleCreate}>
            <CalendarDateField id="accessVisitDate" label="Access Visit Date (GMT+7)" value={visitDate} disabled={working} error={dateError} onChange={value => { setVisitDate(value); setDateError(null) }} />
            <div className="remaining-info"><Info size={18} aria-hidden="true" /><p>You cannot schedule a new access visit while a return visit is pending. Your eligibility is checked when you submit.</p></div>
            <p className="remaining-helper">Choose a date to visit your rented unit. This does not create a new reservation or change your rental period.</p>
            <div className="remaining-actions"><Link className="button button-secondary" to="/customer/visits">My Visits</Link><button className="button" type="submit" disabled={working}><CalendarDays size={16} aria-hidden="true" />{working ? 'Scheduling...' : 'Confirm Access Visit'}</button></div>
          </form>
        </section> : <p className="notice">This contract is not ACTIVE. You cannot schedule an access visit.</p>}
      </>}
      {actionError && <p className="muted">If the request may already have been processed, check My Visits before retrying to avoid scheduling a duplicate visit.</p>}
      {(!contract || contract.status !== 'ACTIVE') && <Link className="button button-secondary access-back" to="/customer/visits">My Visits</Link>}
    </main>
  )
}
