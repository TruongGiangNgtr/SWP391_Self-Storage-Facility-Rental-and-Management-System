import { useState, type FormEvent } from 'react'
import { useParams } from 'react-router-dom'
import { CalendarDays, CalendarSync, CheckCircle2, Info, RefreshCw, Zap } from 'lucide-react'
import { CustomerActionFrame } from '../components/CustomerActionFrame'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { CustomerStatusBadge as StatusBadge } from '../components/CustomerStatusBadge'
import { contractTitle, contractUnit, customerLabel } from '../utils/customerPresentation'
import { getExtensionStartMonth, isRentalMonth, validateRenewalMonth } from '../features/renewal/renewalValidation'
import { reservationDuration, reservationPeriod } from '../features/reservations/reservationListPresentation'
import { CustomerFieldError } from '../components/CustomerFieldError'
import { useContractRenewal } from '../features/renewal/useContractRenewal'
import { formatMoney, formatMonthRange } from '../utils/formatters'
import '../styles/customer-renewal.css'
import '../styles/renewal-return-flow.css'

export function ContractRenewalPage() {
  const { contractId = '' } = useParams()
  const { contract, loading, readError, actionError, working, result, lastAttemptEndMonth, refreshContract, renew } = useContractRenewal(contractId)
  const [newEndMonth, setNewEndMonth] = useState('')
  const [monthError, setMonthError] = useState<string | null>(null)
  const extensionStartMonth = contract ? getExtensionStartMonth(contract.endMonth) : null
  const validSelection = Boolean(extensionStartMonth && isRentalMonth(newEndMonth) && newEndMonth >= extensionStartMonth)
  const busy = loading || working
  const targetReached = !result && contract && lastAttemptEndMonth && isRentalMonth(contract.endMonth) && contract.endMonth >= lastAttemptEndMonth
  const canRenew = !result && contract?.status === 'ACTIVE' && Boolean(extensionStartMonth)

  async function handleRenew(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!contract || !canRenew) return
    const invalid = validateRenewalMonth(contract.endMonth, newEndMonth)
    setMonthError(invalid)
    if (invalid) return
    await renew(newEndMonth)
  }

  return <CustomerActionFrame contractId={contractId} title="Extend Contract Duration" category="" subtitle="Choose a new check-out month. Your extension and monthly price are confirmed when the request succeeds." className="customer-renewal completion-flow contracts-editorial-dialog" headingIcon={<CalendarSync size={24} />} busy={working}>
    {close => <>
      <ApiErrorAlert error={actionError} />
      <ApiErrorAlert error={readError} />
      <div className="renewal-toolbar"><button className="renewal-refresh" type="button" disabled={busy} onClick={refreshContract}><RefreshCw size={14} aria-hidden="true" />{loading ? 'Refreshing…' : 'Refresh Contract'}</button></div>

      {result && <section className="renewal-confirmation" aria-label="Renewal result" role="status">
        <div className="renewal-section-heading"><CheckCircle2 size={20} aria-hidden="true" /><h2>Extension Confirmed</h2><StatusBadge status={result.status} /></div>
        <dl className="renewal-facts"><div><dt>Previous End Month</dt><dd>{result.oldEndMonth}</dd></div><div><dt>New End Month</dt><dd>{result.newEndMonth}</dd></div><div><dt>Extension Period</dt><dd>{formatMonthRange(getExtensionStartMonth(result.oldEndMonth)!, result.newEndMonth)}</dd></div><div><dt>Applied Monthly Price</dt><dd>{formatMoney(result.appliedMonthlyPrice)}<small>/ month</small></dd></div></dl>
        <p>Your extension is confirmed at the monthly price shown here. Review your invoices separately for payment amounts.</p>
      </section>}

      {loading ? <LoadingState label="Refreshing contract details..." /> : contract && <>
        <section className="renewal-context" aria-label="Current contract">
          <div className="renewal-context-heading"><div><span className="renewal-label">Current Contract</span><h2>{contractTitle(contract)}</h2></div><StatusBadge status={contract.status} /></div>
          <dl className="renewal-facts"><div><dt>Storage Unit</dt><dd>{contractUnit(contract)}</dd></div><div><dt>Current End Month</dt><dd>{contract.endMonth}</dd></div></dl>
        </section>
        {result && contract.endMonth < result.newEndMonth && <p className="notice">The refreshed Contract has not yet reflected the confirmed end month. Refresh or contact support; do not submit the same renewal again.</p>}
        {targetReached && <p className="notice">The Contract now reaches or exceeds the end month from your last attempt. That does not identify which request updated it. Review the Contract before choosing any further extension; do not repeat the same target.</p>}
        {canRenew && <form id="contract-renewal-form" className="renewal-form" aria-label="Renewal form" onSubmit={handleRenew} noValidate>
          <section className="renewal-selection" aria-label="Choose extension period">
            <div className="form-field"><label htmlFor="newEndMonth"><CalendarDays size={17} aria-hidden="true" />Select New Check-out Month</label><input id="newEndMonth" type="month" min={extensionStartMonth!} value={newEndMonth} disabled={busy} aria-invalid={Boolean(monthError)} aria-describedby={`newEndMonth-help${monthError ? ' newEndMonth-error' : ''}`} onChange={event => { setNewEndMonth(event.target.value); setMonthError(null) }} /><CustomerFieldError id="newEndMonth-error" message={monthError} /></div>
            <p id="newEndMonth-help" className="renewal-helper">Select a month after {contract.endMonth}. Your extension starts at {extensionStartMonth}.</p>
            <dl className="renewal-facts"><div><dt>Current End Month</dt><dd>{contract.endMonth}</dd></div><div><dt>New Check-out Month{validSelection && <span className="renewal-duration">+{reservationDuration(extensionStartMonth!, newEndMonth)}</span>}</dt><dd className="renewal-new-month">{validSelection ? newEndMonth : 'Choose a month'}</dd></div></dl>
          </section>
          <section className="renewal-summary" aria-label="Extension summary"><h2>Extension Summary</h2><dl><div><dt>Extension Period</dt><dd>{validSelection ? reservationPeriod(extensionStartMonth!, newEndMonth) : 'Select a new check-out month'}</dd></div><div><dt>Extension Duration</dt><dd>{validSelection ? reservationDuration(extensionStartMonth!, newEndMonth) : 'Not selected'}</dd></div><div><dt>Monthly Price</dt><dd>Confirmed after submission</dd></div></dl><p><Info size={16} aria-hidden="true" /><span>The server checks availability for every extension month and applies the current storage-type price. No separate upfront renewal payment is required.</span></p></section>
          <div className="renewal-continuity"><Zap size={18} aria-hidden="true" /><p><strong>Continuous rental:</strong> The extension starts immediately after your current rental period. A successful renewal updates your contract's end month. An active contract with no pending return visit is required.</p></div>
        </form>}
        {!result && contract.status !== 'ACTIVE' && <p className="notice">Only an ACTIVE contract can be renewed. This contract is {customerLabel(contract.status).toLowerCase()}.</p>}
        {!result && contract.status === 'ACTIVE' && !extensionStartMonth && <p className="notice">A valid extension start month cannot be derived from this Contract. Refresh or contact support.</p>}
      </>}
      {actionError && <p className="renewal-helper" role="note">The request may already have been processed if the response was interrupted. Review the refreshed Contract before retrying. This page never automatically resubmits a renewal.</p>}
      <footer className="renewal-actions"><button className="button button-secondary" type="button" disabled={working} onClick={close}>{result ? 'Close' : 'Cancel'}</button>{canRenew && <button className="button" type="submit" form="contract-renewal-form" disabled={busy || Boolean(targetReached && contract && newEndMonth <= contract.endMonth)}><CalendarSync size={17} aria-hidden="true" />{working ? 'Extending…' : 'Confirm Extension'}</button>}</footer>
    </>}
  </CustomerActionFrame>
}
