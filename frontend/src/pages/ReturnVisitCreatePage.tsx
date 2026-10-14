import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
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
import { formatMonthRange } from '../utils/formatters'

export function ReturnVisitCreatePage() {
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
    <CustomerActionFrame contractId={contractId} title="Schedule Return Visit" category="Storage Return" subtitle="Choose a date to return your rented storage unit at the facility." busy={working}>

      <ApiErrorAlert error={actionError ?? error} />
      <button className="button button-secondary" type="button" disabled={loading || working} onClick={() => { setActionError(null); refresh() }}>Refresh Contract</button>
      {loading ? <LoadingState label="Loading your contract..." /> : contract && <>
        <section className="panel stack" aria-label="Return contract">
          <div className="card-heading-row"><h2>Selected Contract</h2><StatusBadge status={contract.status} /></div>
          <dl className="detail-list compact">
            <div><dt>Unit Type</dt><dd>{contractTitle(contract)}</dd></div>
            <div><dt>Storage Unit</dt><dd>{contractUnit(contract)}</dd></div>
            <div><dt>Rental Period</dt><dd>{formatMonthRange(contract.startMonth, contract.endMonth)}</dd></div>
          </dl>
        </section>
        {contract.status === 'ACTIVE' ? <section className="panel stack" aria-label="Schedule return visit">
          <h2>Return Visit Date</h2>
          <form className="form-grid" onSubmit={handleCreate} noValidate>
            <CalendarDateField id="returnVisitDate" label="Return Visit Date (GMT+7)" value={visitDate} disabled={working} error={dateError} onChange={value => { setVisitDate(value); setDateError(null) }} />
            <p className="notice">While a return visit is pending, you cannot schedule new access visits or renew this contract.</p>
            <p className="muted">Your selected date and return eligibility will be checked when you submit.</p>
            <p className="muted">Scheduling a visit does not end your contract. The facility records your actual return date, then completes inspection and settlement.</p>
            <button className="button" type="submit" disabled={working}>{working ? 'Scheduling...' : 'Confirm Return Visit'}</button>
          </form>
        </section> : <p className="notice">This contract is not ACTIVE. You cannot schedule a return visit.</p>}
      </>}
      {actionError && <p className="muted">If the request may already have been processed, check My Visits before retrying to avoid scheduling a duplicate return visit.</p>}
      <Link className="button button-secondary" to="/customer/visits">My Visits</Link>
    </CustomerActionFrame>
  )
}
