import { Link, useParams, useSearchParams } from 'react-router-dom'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { StatusBadge } from '../components/StatusBadge'
import { useStaffAccessVisit } from '../features/visits/useStaffAccessVisit'
import { formatMonthRange } from '../utils/formatters'
import '../styles/staff.css'

export function StaffAccessVisitPage() {
  const { contractId = '', visitId = '' } = useParams()
  const [searchParams] = useSearchParams()
  const requestedPage = Number(searchParams.get('workPage') ?? 1)
  const workPage = Number.isSafeInteger(requestedPage) && requestedPage > 0 ? requestedPage : 1
  const { context, loading, action, error, refresh, process } = useStaffAccessVisit(contractId, visitId, workPage)
  const busy = loading || action !== null

  return (
    <main className="page-container flow-page narrow-page staff-flow">
      <section className="page-heading page-heading-actions">
        <div><p className="eyebrow">Facility Staff</p><h1>Access Visit Processing</h1></div>
        <Link className="button button-secondary" to="/staff">Work List</Link>
      </section>
      <ApiErrorAlert error={error} />
      <button className="button button-secondary" type="button" disabled={busy} onClick={() => refresh()}>Refresh Details</button>
      {loading ? <LoadingState label="Loading access visit and contract..." /> : context && <>
        <section className="panel stack" aria-label="Access visit and customer">
          <div className="card-heading-row"><h2>Access Visit</h2><StatusBadge status={context.visit.status} /></div>
          {context.workItem ? <p><strong>{context.workItem.customer.fullName}</strong><br /><span className="muted">{context.workItem.customer.phoneNumber}</span></p>
            : <p className="notice">Customer identification is not in this work-list page. Return to Work List to verify the arriving customer; no customer details are inferred from URL IDs.</p>}
          <dl className="detail-list">
            <div><dt>Visit ID</dt><dd>{context.visit.visitId}</dd></div>
            <div><dt>Visit Date (GMT+7)</dt><dd>{context.visit.visitDate}</dd></div>
            <div><dt>Handling Staff ID</dt><dd>{context.visit.employeeId ?? 'Not Recorded'}</dd></div>
          </dl>
          {context.visit.status === 'SCHEDULED' && context.contract.status === 'ACTIVE' && <>
            <p className="muted">Verify the arriving customer. The backend validates your current Facility assignment, visit date and lifecycle.</p>
            <button className="button" type="button" disabled={busy} onClick={() => void process('check-in')}>{action === 'check-in' ? 'Checking In...' : 'Check In Customer'}</button>
          </>}
          {context.visit.status === 'SCHEDULED' && context.contract.status !== 'ACTIVE' && <p className="notice">This contract is not ACTIVE. Check-in is unavailable; refresh the work list.</p>}
          {context.visit.status === 'CHECKED_IN' && <>
            <p className="muted">Check out this visit after the customer has finished accessing the rented unit. The backend validates the command.</p>
            <button className="button" type="button" disabled={busy} onClick={() => void process('check-out')}>{action === 'check-out' ? 'Checking Out...' : 'Check Out Customer'}</button>
          </>}
          {(context.visit.status === 'CHECKED_OUT' || context.visit.status === 'CANCELLED') && <p className="notice">This visit is {context.visit.status}. No further check-in or check-out action is available.</p>}
        </section>
        <section className="panel stack" aria-label="Access contract details">
          <div className="card-heading-row"><h2>Related Contract</h2><StatusBadge status={context.contract.status} /></div>
          <dl className="detail-list">
            <div><dt>Contract ID</dt><dd>{context.contract.contractId}</dd></div>
            <div><dt>Facility ID</dt><dd>{context.contract.facilityId}</dd></div>
            <div><dt>Storage Unit ID</dt><dd>{context.contract.storageUnitId}</dd></div>
            <div><dt>Rental Period</dt><dd>{formatMonthRange(context.contract.startMonth, context.contract.endMonth)}</dd></div>
          </dl>
          <p className="muted">Access processing changes the Visit only. It does not perform handover, return finalization or unit reassignment.</p>
        </section>
      </>}
    </main>
  )
}
