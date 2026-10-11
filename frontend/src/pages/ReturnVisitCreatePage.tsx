import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { ApiRequestError } from '../api/httpClient'
import { visitApi } from '../api/visitApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { CalendarDateField } from '../components/CalendarDateField'
import { LoadingState } from '../components/PageStates'
import { StatusBadge } from '../components/StatusBadge'
import { isUuid } from '../features/handover/handoverValidation'
import { isCalendarDate } from '../features/visits/accessValidation'
import { useAccessContract } from '../features/visits/useAccessContract'
import { formatMonthRange } from '../utils/formatters'

export function ReturnVisitCreatePage() {
  const { contractId = '' } = useParams()
  const { contract, loading, error, refresh } = useAccessContract(contractId)
  const [visitDate, setVisitDate] = useState('')
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
      setActionError(presentValidationError('Please select a valid return visit date.'))
      return
    }

    submitBusy.current = true
    setWorking(true)
    setActionError(null)
    try {
      const { data: visit } = await visitApi.createReturn(contractId, { visitDate })
      if (!active.current) return
      if (!visit || !isUuid(visit.visitId) || visit.entityId !== contractId ||
        visit.visitType !== 'RETURN' || visit.status !== 'SCHEDULED' ||
        visit.visitDate !== visitDate || visit.actualReturnDate !== null || visit.employeeId !== null) {
        setActionError(presentValidationError('The server returned an unexpected return visit. Check My Visits before submitting again.'))
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

  return (
    <main className="page-container flow-page narrow-page">
      <section className="page-heading page-heading-actions">
        <div><p className="eyebrow">Storage Return</p><h1>Schedule Return Visit</h1><p className="muted">Choose a date to visit the facility and return your rented storage unit.</p></div>
        <Link className="button button-secondary" to={`/customer/contracts/${encodeURIComponent(contractId)}`}>View Contract</Link>
      </section>
      <ApiErrorAlert error={actionError ?? error} />
      <button className="button button-secondary" type="button" disabled={loading || working} onClick={() => { setActionError(null); refresh() }}>Refresh Contract</button>
      {loading ? <LoadingState label="Loading your contract..." /> : contract && <>
        <section className="panel stack" aria-label="Return contract">
          <div className="card-heading-row"><h2>Selected Contract</h2><StatusBadge status={contract.status} /></div>
          <dl className="detail-list compact">
            <div><dt>Contract ID</dt><dd>{contract.contractId}</dd></div>
            <div><dt>Rental Period</dt><dd>{formatMonthRange(contract.startMonth, contract.endMonth)}</dd></div>
          </dl>
        </section>
        {contract.status === 'ACTIVE' ? <section className="panel stack" aria-label="Schedule return visit">
          <h2>Return Visit Date</h2>
          <form className="form-grid" onSubmit={handleCreate} noValidate>
            <CalendarDateField id="returnVisitDate" label="Return Visit Date (GMT+7)" value={visitDate} disabled={working} onChange={setVisitDate} />
            <p className="notice">A RETURN Visit that is SCHEDULED or CHECKED_IN blocks new access visits and contract renewal.</p>
            <p className="muted">The backend validates the date, Contract ownership and current return state. This page does not confirm that no return visit is already pending.</p>
            <p className="muted">Scheduling does not end your Contract or release the Storage Unit. Staff records the actual return date, and return finalization follows inspection and settlement.</p>
            <button className="button" type="submit" disabled={working}>{working ? 'Scheduling...' : 'Confirm Return Visit'}</button>
          </form>
        </section> : <p className="notice">This contract is not ACTIVE. You cannot schedule a return visit.</p>}
      </>}
      {actionError && <p className="muted">If the request may already have been processed, check My Visits before retrying to avoid scheduling a duplicate return visit.</p>}
      <Link className="button button-secondary" to="/customer/visits">My Visits</Link>
    </main>
  )
}
