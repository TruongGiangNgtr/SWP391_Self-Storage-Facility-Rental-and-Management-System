import { useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { StatusBadge } from '../components/StatusBadge'
import { getExtensionStartMonth, isRentalMonth } from '../features/renewal/renewalValidation'
import { useContractRenewal } from '../features/renewal/useContractRenewal'
import { formatMoney, formatMonthRange } from '../utils/formatters'

export function ContractRenewalPage() {
  const { contractId = '' } = useParams()
  const { contract, loading, readError, actionError, working, result, lastAttemptEndMonth, refreshContract, renew } = useContractRenewal(contractId)
  const [newEndMonth, setNewEndMonth] = useState('')
  const extensionStartMonth = contract ? getExtensionStartMonth(contract.endMonth) : null
  const busy = loading || working
  const targetReached = !result && contract && lastAttemptEndMonth && isRentalMonth(contract.endMonth) && contract.endMonth >= lastAttemptEndMonth

  async function handleRenew(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    await renew(newEndMonth)
  }

  return (
    <main className="page-container flow-page narrow-page">
      <section className="page-heading page-heading-actions">
        <div><p className="eyebrow">Contract Renewal</p><h1>Renew Contract</h1><p className="muted">Extend your existing rental continuously from its current end month.</p></div>
        <Link className="button button-secondary" to={`/customer/contracts/${encodeURIComponent(contractId)}`}>View Contract</Link>
      </section>
      <ApiErrorAlert error={actionError} />
      <ApiErrorAlert error={readError} />
      <button className="button button-secondary" type="button" disabled={busy} onClick={refreshContract}>Refresh Contract</button>

      {result && <section className="panel stack" aria-label="Renewal result">
        <div className="card-heading-row"><h2>Renewal Confirmed by API</h2><StatusBadge status={result.status} /></div>
        <dl className="detail-list">
          <div><dt>Contract ID</dt><dd>{result.contractId}</dd></div>
          <div><dt>Previous End Month</dt><dd>{result.oldEndMonth}</dd></div>
          <div><dt>New End Month</dt><dd>{result.newEndMonth}</dd></div>
          <div><dt>Extension Period</dt><dd>{formatMonthRange(getExtensionStartMonth(result.oldEndMonth)!, result.newEndMonth)}</dd></div>
          <div><dt>Applied Monthly Price</dt><dd>{formatMoney(result.appliedMonthlyPrice)}</dd></div>
        </dl>
        <p className="notice">These values were returned by the renewal API. The applied monthly price is the backend's snapshot for this extension, not a frontend quote or an invoice amount.</p>
        <p className="muted">This is the same Contract and Storage Unit. Renewal does not create a separate renewal-payment flow or restart first-month offline collection.</p>
      </section>}

      {loading ? <LoadingState label="Refreshing contract details..." /> : contract && <>
        <section className="panel stack" aria-label="Current contract">
          <div className="card-heading-row"><h2>Current Contract</h2><StatusBadge status={contract.status} /></div>
          <dl className="detail-list">
            <div><dt>Contract ID</dt><dd>{contract.contractId}</dd></div>
            <div><dt>Storage Unit ID</dt><dd>{contract.storageUnitId}</dd></div>
            <div><dt>Original Start Month</dt><dd>{contract.startMonth}</dd></div>
            <div><dt>Current End Month</dt><dd>{contract.endMonth}</dd></div>
          </dl>
          <p className="muted">Read from the backend. Contract Discount and the original rental snapshots are not changed by this form.</p>
        </section>
        {result && contract.endMonth < result.newEndMonth && <p className="notice">The refreshed Contract has not yet reflected the confirmed end month. Refresh or contact support; do not submit the same renewal again.</p>}
        {targetReached && <p className="notice">The Contract now reaches or exceeds the end month from your last attempt. That does not identify which request updated it. Review the Contract before choosing any further extension; do not repeat the same target.</p>}
        {!result && contract.status === 'ACTIVE' && extensionStartMonth && <section className="panel stack" aria-label="Renewal form">
          <h2>Choose Extension Period</h2>
          <form className="form-grid" onSubmit={handleRenew} noValidate>
            <div className="form-field">
              <label htmlFor="newEndMonth">New End Month</label>
              <input id="newEndMonth" type="month" min={extensionStartMonth} value={newEndMonth} disabled={busy} onChange={(event) => setNewEndMonth(event.target.value)} />
            </div>
            <dl className="detail-list compact">
              <div><dt>Extension Starts</dt><dd>{extensionStartMonth}</dd></div>
              <div><dt>Requested Extension</dt><dd>{isRentalMonth(newEndMonth) && newEndMonth >= extensionStartMonth ? formatMonthRange(extensionStartMonth, newEndMonth) : 'Select a later end month'}</dd></div>
            </dl>
            <p className="notice">The backend checks that the Contract is ACTIVE, no RETURN Visit is pending, and capacity is available for every extension month before committing renewal.</p>
            <p className="muted">The applied monthly price is captured from the current UnitType price at renewal. It is displayed only after the backend returns the result. This page does not confirm return status or available capacity.</p>
            <button className="button" type="submit" disabled={busy || Boolean(targetReached && newEndMonth <= contract.endMonth)}>{working ? 'Renewing...' : 'Confirm Renewal'}</button>
          </form>
        </section>}
        {!result && contract.status !== 'ACTIVE' && <p className="notice">Only an ACTIVE contract can be renewed. This contract is {contract.status}.</p>}
        {!result && contract.status === 'ACTIVE' && !extensionStartMonth && <p className="notice">A valid extension start month cannot be derived from this Contract. Refresh or contact support.</p>}
      </>}
      {actionError && <p className="muted">The request may already have been processed if the response was interrupted. Review the refreshed Contract before retrying. This page never automatically resubmits a renewal.</p>}
      <Link className="button button-secondary" to="/customer/contracts">My Contracts</Link>
    </main>
  )
}
