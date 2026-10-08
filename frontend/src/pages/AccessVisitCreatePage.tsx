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

export function AccessVisitCreatePage() {
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
      setActionError(presentValidationError('Please select a valid visit date.'))
      return
    }
    submitBusy.current = true
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
    <main className="page-container flow-page narrow-page">
      <section className="page-heading page-heading-actions">
        <div><p className="eyebrow">Access Visit</p><h1>Schedule Access Visit</h1></div>
        <Link className="button button-secondary" to={`/customer/contracts/${encodeURIComponent(contractId)}`}>View Contract</Link>
      </section>
      <ApiErrorAlert error={actionError ?? error} />
      <button className="button button-secondary" type="button" disabled={loading || working} onClick={() => { setActionError(null); refresh() }}>Refresh Contract</button>
      {loading ? <LoadingState label="Loading your contract..." /> : contract && <>
        <section className="panel stack" aria-label="Access contract">
          <div className="card-heading-row"><h2>Selected Contract</h2><StatusBadge status={contract.status} /></div>
          <dl className="detail-list compact">
            <div><dt>Contract ID</dt><dd>{contract.contractId}</dd></div>
            <div><dt>Storage Unit ID</dt><dd>{contract.storageUnitId}</dd></div>
            <div><dt>Rental Period</dt><dd>{formatMonthRange(contract.startMonth, contract.endMonth)}</dd></div>
          </dl>
        </section>
        {contract.status === 'ACTIVE' ? <section className="panel stack" aria-label="Schedule access visit">
          <h2>Visit Date</h2>
          <form className="form-grid" onSubmit={handleCreate}>
            <CalendarDateField id="accessVisitDate" label="Access Visit Date (GMT+7)" value={visitDate} disabled={working} onChange={setVisitDate} />
            <p className="notice">A pending return visit for this contract blocks new access visits. The backend checks the current contract and return state when you submit.</p>
            <p className="muted">This page does not confirm that there is no pending return visit. Date eligibility is checked by the backend; no handover-specific day window is applied here.</p>
            <button className="button" type="submit" disabled={working}>{working ? 'Scheduling...' : 'Confirm Access Visit'}</button>
          </form>
        </section> : <p className="notice">This contract is not ACTIVE. You cannot schedule an access visit.</p>}
      </>}
      {actionError && <p className="muted">If the request may already have been processed, check My Visits before retrying to avoid scheduling a duplicate visit.</p>}
      <Link className="button button-secondary" to="/customer/visits">My Visits</Link>
    </main>
  )
}
