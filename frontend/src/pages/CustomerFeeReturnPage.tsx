import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { contractApi } from '../api/contractApi'
import { CustomerTabs } from '../components/CustomerTabs'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState } from '../components/PageStates'
import { StatusBadge } from '../components/StatusBadge'
import type { ContractBillingSummary, ContractReturnSummary } from '../features/fee-return-tracking/tracking.types'
import { isBillingSummary, isReturnSummary } from '../features/fee-return-tracking/trackingValidation'
import { useAccessContract } from '../features/visits/useAccessContract'
import { formatMoney, formatUtcDateTime } from '../utils/formatters'

interface ReadSnapshot<T> { key: string; data: T | null; error: ApiErrorPresentation | null }

export function CustomerFeeReturnPage() {
  const { contractId = '' } = useParams()
  const [section, setSection] = useState(0)
  const contractReader = useAccessContract(contractId)
  const contract = contractReader.contract
  const [retry, setRetry] = useState(0)
  const key = `${contractId}:${retry}`
  const [billingSnapshot, setBilling] = useState<ReadSnapshot<ContractBillingSummary> | null>(null)
  const [returnSnapshot, setReturn] = useState<ReadSnapshot<ContractReturnSummary> | null>(null)
  const billingLoading = billingSnapshot?.key !== key
  const returnLoading = returnSnapshot?.key !== key
  const billing = billingLoading ? null : billingSnapshot.data
  const returnSummary = returnLoading ? null : returnSnapshot.data
  const busy = billingLoading || returnLoading || contractReader.loading

  useEffect(() => {
    let active = true
    contractApi.getBilling(contractId).then(({ data }) => {
      if (!active) return
      setBilling(isBillingSummary(data, contractId)
        ? { key, data, error: null }
        : { key, data: null, error: presentValidationError('Billing details could not be verified for this contract. Refresh or contact support.') })
    }).catch((error: unknown) => { if (active) setBilling({ key, data: null, error: presentApiError(error) }) })
    contractApi.getReturnSummary(contractId).then(({ data }) => {
      if (!active) return
      setReturn(isReturnSummary(data, contractId)
        ? { key, data, error: null }
        : { key, data: null, error: presentValidationError('Return details could not be verified for this contract. Refresh before relying on the amounts.') })
    }).catch((error: unknown) => { if (active) setReturn({ key, data: null, error: presentApiError(error) }) })
    return () => { active = false }
  }, [contractId, key])

  return <main className="page-container flow-page customer-tracking-page">
    <section className="page-heading page-heading-actions"><div><p className="eyebrow">Customer Portal</p><h1>Fees &amp; Return Tracking</h1><p className="muted">Review billing, inspection and recorded settlement amounts for this contract.</p></div><Link className="button button-secondary" to={`/customer/contracts/${encodeURIComponent(contractId)}`}>View Contract</Link><Link className="button button-secondary" to="/customer/contracts">Switch Contract</Link></section>
    <ApiErrorAlert error={contractReader.error} />
    <button className="button button-secondary" disabled={busy} type="button" onClick={() => { contractReader.refresh(); setRetry(value => value + 1) }}>Refresh Tracking</button>
    {contractReader.loading ? <LoadingState label="Loading your contract..." /> : contract && <>
      <section className="panel stack"><div className="card-heading-row"><h2>Contract</h2><StatusBadge status={contract.status} /></div><p>{contractId}</p><p className="notice">Your first month was paid offline at handover and has no rental invoice. Later monthly invoices are shown below.</p></section>
      <CustomerTabs labels={['Billing & Overdue', 'Return & Settlement']} selected={section} onChange={setSection}>{({ tabId, panelId }) => <>
      <section className="panel stack" role="tabpanel" id={panelId(0)} aria-labelledby={tabId(0)} hidden={section !== 0} tabIndex={0}>
        <h2>Billing &amp; Overdue</h2>
        <ApiErrorAlert error={billingLoading ? null : billingSnapshot.error} />
        {billingLoading ? <LoadingState label="Loading billing summary..." /> : billing && <>
          {billing.invoices.length === 0 ? <EmptyState message="No invoices have been returned for this Contract." /> : billing.invoices.map(invoice => <article className="reservation-card" key={invoice.invoiceId}>
            <div className="card-heading-row"><h3>{invoice.invoiceType === 'RENTAL_FEE' ? 'Rental Invoice' : 'Deposit Invoice'}</h3><StatusBadge status={invoice.status} /></div>
            <dl className="detail-list compact"><div><dt>Billing Month</dt><dd>{invoice.billingMonth ?? 'Not Applicable'}</dd></div><div><dt>Amount Due</dt><dd>{formatMoney(invoice.amountDue)}</dd></div><div><dt>Due (GMT+7)</dt><dd>{formatUtcDateTime(invoice.dueDate)}</dd></div><div><dt>Paid (GMT+7)</dt><dd>{invoice.paidAt ? formatUtcDateTime(invoice.paidAt) : 'Not Recorded'}</dd></div></dl>
            <Link className="button button-secondary" to={`/customer/invoices/${encodeURIComponent(invoice.invoiceId)}`}>View Invoice</Link>
          </article>)}
          <h3>Late Fee Records</h3>
          {billing.lateFees.length === 0 ? <EmptyState message="No late-fee records have been returned." /> : billing.lateFees.map(fee => <article className="reservation-card" key={fee.lateFeeId}><dl className="detail-list compact"><div><dt>Invoice ID</dt><dd>{fee.invoiceId}</dd></div><div><dt>Overdue Days</dt><dd>{fee.overdueDays}</dd></div><div><dt>Recorded Late Fee</dt><dd>{formatMoney(fee.amount)}</dd></div><div><dt>Calculated (GMT+7)</dt><dd>{formatUtcDateTime(fee.calculatedAt)}</dd></div></dl></article>)}
          <p className="muted">These are recorded fee amounts. A fee record alone is not a payment request; applicable deductions are shown in settlement.</p>
        </>}
      </section>
      <section className="panel stack" role="tabpanel" id={panelId(1)} aria-labelledby={tabId(1)} hidden={section !== 1} tabIndex={0}>
        <h2>Return, Inspection &amp; Settlement</h2>
        <ApiErrorAlert error={returnLoading ? null : returnSnapshot.error} />
        {returnLoading ? <LoadingState label="Loading return summary..." /> : returnSummary && <>
          <div className="card-heading-row"><h3>Contract Status Reported by Return Summary</h3><StatusBadge status={returnSummary.contractStatus} /></div>
          {returnSummary.contractStatus !== contract.status && <p className="notice">The separate Contract and return-summary reads differ. Refresh to review the current server state.</p>}
          <h3>Return Visit</h3>
          {returnSummary.returnVisit ? <>
            <StatusBadge status={returnSummary.returnVisit.status} />
            <dl className="detail-list compact"><div><dt>Scheduled Return Date (GMT+7)</dt><dd>{returnSummary.returnVisit.visitDate}</dd></div><div><dt>Actual Return Date (GMT+7)</dt><dd>{returnSummary.returnVisit.actualReturnDate ?? 'Not Recorded'}</dd></div></dl>
            <Link className="button button-secondary" to={`/customer/visits/${encodeURIComponent(returnSummary.returnVisit.visitId)}`}>View Return Visit</Link>
            {['SCHEDULED', 'CHECKED_IN'].includes(returnSummary.returnVisit.status) && <p className="notice">A pending RETURN Visit blocks new ACCESS Visits and renewal. Scheduling is not proof of completed return.</p>}
          </> : <EmptyState message="No return visit has been reported." />}
          <h3>Inspection Progress</h3>
          {returnSummary.inspection ? <><StatusBadge status={returnSummary.inspection.status} /><p>{returnSummary.inspection.conditionNote ?? 'No inspection note recorded.'}</p><p>Completed (GMT+7): {returnSummary.inspection.completedAt ? formatUtcDateTime(returnSummary.inspection.completedAt) : 'Not Completed'}</p></> : <EmptyState message="No inspection has been reported." />}
          <p className="muted">After inspection, the facility still needs to finalize your return and settlement before your contract is closed.</p>
          <h3>Damage Records</h3>
          {returnSummary.damages.length === 0 ? <EmptyState message="No Damage records have been returned." /> : returnSummary.damages.map(damage => <article className="reservation-card" key={damage.damageRecordId}><div className="card-heading-row"><h4>{damage.note ?? 'Damage Record'}</h4><StatusBadge status={damage.status} /></div><p>Recorded Damage Amount: {formatMoney(damage.damageAmount)}</p><p className="muted">{damage.status === 'APPROVED' ? 'Approved for consideration in server-calculated settlement.' : damage.status === 'PENDING' ? 'Awaiting Manager decision; not an approved deduction.' : 'Rejected; not an approved deduction.'}</p></article>)}
          {returnSummary.damages.some(damage => damage.status === 'PENDING') && <p className="notice">Your return cannot be finalized until the Facility Manager has resolved the pending damage decisions.</p>}
          <h3>Extra Fee Records</h3>
          {returnSummary.extraFees.length === 0 ? <EmptyState message="No Extra Fee records have been returned." /> : returnSummary.extraFees.map(fee => <article className="reservation-card" key={fee.extraFeeId}><h4>{fee.reason}</h4><p>Recorded Extra Fee: {formatMoney(fee.amount)}</p></article>)}
          <h3>Deposit Settlement</h3>
          {returnSummary.settlement ? <><StatusBadge status={returnSummary.settlement.status} /><dl className="detail-list"><div><dt>Total Deduction</dt><dd>{formatMoney(returnSummary.settlement.totalDeduction)}</dd></div><div><dt>Refund Amount</dt><dd>{formatMoney(returnSummary.settlement.refundAmount)}</dd></div><div><dt>Additional Amount Due</dt><dd>{formatMoney(returnSummary.settlement.additionalAmountDue)}</dd></div></dl><p className="notice">These are recorded settlement amounts, not confirmation that a refund transfer or debt collection has occurred. This page performs no financial transaction.</p></> : <EmptyState message="Settlement has not been recorded yet. Refund and additional amounts due are not yet known." />}
        </>}
      </section>
      </>}</CustomerTabs>
    </>}
  </main>
}
