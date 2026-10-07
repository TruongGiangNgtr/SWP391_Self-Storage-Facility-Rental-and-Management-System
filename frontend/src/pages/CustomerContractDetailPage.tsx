import { Link, useParams } from 'react-router-dom'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { StatusBadge } from '../components/StatusBadge'
import { useAccessContract } from '../features/visits/useAccessContract'
import { formatMonthRange } from '../utils/formatters'

export function CustomerContractDetailPage() {
  const { contractId = '' } = useParams()
  const { contract, loading, error, refresh } = useAccessContract(contractId)
  return (
    <main className="page-container flow-page narrow-page">
      <section className="page-heading page-heading-actions">
        <div><p className="eyebrow">My Contracts</p><h1>Contract Details</h1></div>
        <Link className="button button-secondary" to="/customer/contracts">My Contracts</Link>
      </section>
      <ApiErrorAlert error={error} />
      <button className="button button-secondary" type="button" disabled={loading} onClick={refresh}>Refresh Contract</button>
      {loading ? <LoadingState label="Loading your contract..." /> : contract && <section className="panel stack">
        <div className="card-heading-row"><h2>Storage Contract</h2><StatusBadge status={contract.status} /></div>
        <dl className="detail-list">
          <div><dt>Contract ID</dt><dd>{contract.contractId}</dd></div>
          <div><dt>Facility ID</dt><dd>{contract.facilityId}</dd></div>
          <div><dt>Storage Unit ID</dt><dd>{contract.storageUnitId}</dd></div>
          <div><dt>Rental Period</dt><dd>{formatMonthRange(contract.startMonth, contract.endMonth)}</dd></div>
        </dl>
        <div className="action-row">
          {contract.status === 'ACTIVE' ? <Link className="button" to={`/customer/contracts/${encodeURIComponent(contractId)}/access-visits/new`}>Schedule Access Visit</Link>
            : <p className="notice">Access visits cannot be created for a contract that is not ACTIVE.</p>}
          {contract.status === 'ACTIVE' && <Link className="button button-secondary" to={`/customer/contracts/${encodeURIComponent(contractId)}/renew`}>Renew Contract</Link>}
          <Link className="button button-secondary" to="/customer/visits">My Visits</Link>
          <Link className="button button-secondary" to={`/customer/reservations/${encodeURIComponent(contract.reservationId)}`}>Original Reservation</Link>
        </div>
        <p className="muted">An access visit is a visit to your rented unit. It does not create a new reservation or change your rental period.</p>
      </section>}
    </main>
  )
}
