import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import type { ApiCollectionResponse } from '../api/api.types'
import { presentApiError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { contractApi } from '../api/contractApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState, PaginationControls } from '../components/PageStates'
import { StatusBadge } from '../components/StatusBadge'
import type { HandoverContractDetail as ContractDetail } from '../features/handover/contract.types'
import { formatMonthRange } from '../utils/formatters'

export function CustomerContractListPage() {
  const [page, setPage] = useState(1)
  const [retry, setRetry] = useState(0)
  const [snapshot, setSnapshot] = useState<{ key: string; response: ApiCollectionResponse<ContractDetail> | null; error: ApiErrorPresentation | null } | null>(null)
  const key = `${page}:${retry}`
  const loading = snapshot?.key !== key
  const response = loading ? null : snapshot.response
  const error = loading ? null : snapshot.error

  useEffect(() => {
    let active = true
    contractApi.list(page)
      .then((response) => { if (active) setSnapshot({ key, response, error: null }) })
      .catch((error: unknown) => { if (active) setSnapshot({ key, response: null, error: presentApiError(error) }) })
    return () => { active = false }
  }, [page, key])

  return (
    <main className="page-container flow-page">
      <section className="page-heading page-heading-actions">
        <div><p className="eyebrow">Customer Portal</p><h1>My Contracts</h1><p className="muted">Choose an active contract to schedule a visit to your rented storage unit.</p></div>
        <button className="button button-secondary" type="button" disabled={loading} onClick={() => setRetry((value) => value + 1)}>Refresh Contracts</button>
      </section>
      <ApiErrorAlert error={error} />
      {loading ? <LoadingState label="Loading your contracts..." /> : error ? null : response && (
        <>
          {response.data.length === 0 ? <EmptyState message="You do not have any contracts yet." /> : <div className="reservation-list">
            {response.data.map((contract) => <article className="reservation-card" key={contract.contractId}>
              <div className="card-heading-row"><h2>Storage Contract</h2><StatusBadge status={contract.status} /></div>
              <dl className="detail-list compact">
                <div><dt>Contract ID</dt><dd>{contract.contractId}</dd></div>
                <div><dt>Storage Unit ID</dt><dd>{contract.storageUnitId}</dd></div>
                <div><dt>Rental Period</dt><dd>{formatMonthRange(contract.startMonth, contract.endMonth)}</dd></div>
              </dl>
              <div className="action-row">
                <Link className="button button-secondary" to={`/customer/contracts/${encodeURIComponent(contract.contractId)}`}>View Contract</Link>
                {contract.status === 'ACTIVE' && <Link className="button" to={`/customer/contracts/${encodeURIComponent(contract.contractId)}/access-visits/new`}>Schedule Access Visit</Link>}
              </div>
            </article>)}
          </div>}
          <PaginationControls pagination={response.pagination} disabled={loading} onPageChange={setPage} />
        </>
      )}
    </main>
  )
}
