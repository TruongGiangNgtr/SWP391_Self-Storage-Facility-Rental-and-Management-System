import { useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { CustomerActionFrame } from '../components/CustomerActionFrame'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { CustomerStatusBadge as StatusBadge } from '../components/CustomerStatusBadge'
import { contractTitle, contractUnit } from '../utils/customerPresentation'
import { getExtensionStartMonth, isRentalMonth, validateRenewalMonth } from '../features/renewal/renewalValidation'
import { CustomerFieldError } from '../components/CustomerFieldError'
import { useContractRenewal } from '../features/renewal/useContractRenewal'
import { formatMoney, formatMonthRange } from '../utils/formatters'

export function ContractRenewalPage() {
  const { contractId = '' } = useParams()
  const { contract, loading, readError, actionError, working, result, lastAttemptEndMonth, refreshContract, renew } = useContractRenewal(contractId)
  const [newEndMonth, setNewEndMonth] = useState('')
  const [monthError, setMonthError] = useState<string | null>(null)
  const extensionStartMonth = contract ? getExtensionStartMonth(contract.endMonth) : null
  const busy = loading || working
  const targetReached = !result && contract && lastAttemptEndMonth && isRentalMonth(contract.endMonth) && contract.endMonth >= lastAttemptEndMonth

  async function handleRenew(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!contract) return
    const invalid = validateRenewalMonth(contract.endMonth, newEndMonth)
    setMonthError(invalid)
    if (invalid) return
    await renew(newEndMonth)
  }

  return (
    <CustomerActionFrame contractId={contractId} title="Renew Contract" category="Contract Renewal" subtitle="Extend your rental from the month after its current end date." busy={working}>

      <ApiErrorAlert error={actionError} />
      <ApiErrorAlert error={readError} />
      <button className="button button-secondary" type="button" disabled={busy} onClick={refreshContract}>Refresh Contract</button>

      {result && <section className="panel stack" aria-label="Renewal result">
        <div className="card-heading-row"><h2>Renewal Confirmed</h2><StatusBadge status={result.status} /></div>
        <dl className="detail-list">
          <div><dt>Previous End Month</dt><dd>{result.oldEndMonth}</dd></div>
          <div><dt>New End Month</dt><dd>{result.newEndMonth}</dd></div>
          <div><dt>Extension Period</dt><dd>{formatMonthRange(getExtensionStartMonth(result.oldEndMonth)!, result.newEndMonth)}</dd></div>
          <div><dt>Applied Monthly Price</dt><dd>{formatMoney(result.appliedMonthlyPrice)}</dd></div>
        </dl>
        <p className="notice">Your extension is confirmed at the monthly price shown here. Review your invoices separately for payment amounts.</p>
        <p className="muted">Your existing contract and storage unit remain the same. There is no separate upfront renewal payment.</p>
      </section>}

      {loading ? <LoadingState label="Refreshing contract details..." /> : contract && <>
        <section className="panel stack" aria-label="Current contract">
          <div className="card-heading-row"><h2>Current Contract</h2><StatusBadge status={contract.status} /></div>
          <dl className="detail-list">
            <div><dt>Unit Type</dt><dd>{contractTitle(contract)}</dd></div>
            <div><dt>Storage Unit</dt><dd>{contractUnit(contract)}</dd></div>
            <div><dt>Original Start Month</dt><dd>{contract.startMonth}</dd></div>
            <div><dt>Current End Month</dt><dd>{contract.endMonth}</dd></div>
          </dl>
          <p className="muted">Your existing rental terms are unchanged for the original rental period.</p>
        </section>
        {result && contract.endMonth < result.newEndMonth && <p className="notice">The refreshed Contract has not yet reflected the confirmed end month. Refresh or contact support; do not submit the same renewal again.</p>}
        {targetReached && <p className="notice">The Contract now reaches or exceeds the end month from your last attempt. That does not identify which request updated it. Review the Contract before choosing any further extension; do not repeat the same target.</p>}
        {!result && contract.status === 'ACTIVE' && extensionStartMonth && <section className="panel stack" aria-label="Renewal form">
          <h2>Choose Extension Period</h2>
          <form className="form-grid" onSubmit={handleRenew} noValidate>
            <div className="form-field">
              <label htmlFor="newEndMonth">New End Month</label>
              <input id="newEndMonth" type="month" min={extensionStartMonth} value={newEndMonth} disabled={busy} aria-invalid={Boolean(monthError)} aria-describedby={monthError ? 'newEndMonth-error' : undefined} onChange={(event) => { setNewEndMonth(event.target.value); setMonthError(null) }} />
              <CustomerFieldError id="newEndMonth-error" message={monthError} />
            </div>
            <dl className="detail-list compact">
              <div><dt>Extension Starts</dt><dd>{extensionStartMonth}</dd></div>
              <div><dt>Requested Extension</dt><dd>{isRentalMonth(newEndMonth) && newEndMonth >= extensionStartMonth ? formatMonthRange(extensionStartMonth, newEndMonth) : 'Select a later end month'}</dd></div>
            </dl>
            <p className="notice">Renewal requires an active contract, no pending return visit and availability throughout the extension period.</p>
            <p className="muted">The price for your extension is confirmed after submission, using the current storage-type price. Availability and eligibility are checked when you submit.</p>
            <button className="button" type="submit" disabled={busy || Boolean(targetReached && newEndMonth <= contract.endMonth)}>{working ? 'Renewing...' : 'Confirm Renewal'}</button>
          </form>
        </section>}
        {!result && contract.status !== 'ACTIVE' && <p className="notice">Only an ACTIVE contract can be renewed. This contract is {contract.status}.</p>}
        {!result && contract.status === 'ACTIVE' && !extensionStartMonth && <p className="notice">A valid extension start month cannot be derived from this Contract. Refresh or contact support.</p>}
      </>}
      {actionError && <p className="muted">The request may already have been processed if the response was interrupted. Review the refreshed Contract before retrying. This page never automatically resubmits a renewal.</p>}
      <Link className="button button-secondary" to="/customer/contracts">My Contracts</Link>
    </CustomerActionFrame>
  )
}
