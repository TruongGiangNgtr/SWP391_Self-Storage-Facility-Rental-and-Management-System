import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ArrowRight, CalendarClock, ClipboardCheck, FileText, Info, ReceiptText, RefreshCw, ShieldAlert, Undo2, Wallet, Warehouse } from 'lucide-react'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { contractApi } from '../api/contractApi'
import { CustomerTabs } from '../components/CustomerTabs'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState } from '../components/PageStates'
import { CustomerStatusBadge as StatusBadge } from '../components/CustomerStatusBadge'
import { contractOption } from '../utils/customerPresentation'
import type { ContractBillingSummary, ContractReturnSummary } from '../features/fee-return-tracking/tracking.types'
import { isBillingSummary, isReturnSummary } from '../features/fee-return-tracking/trackingValidation'
import { useAccessContract } from '../features/visits/useAccessContract'
import { formatMoney, formatUtcDateTime } from '../utils/formatters'
import '../styles/customer-fee-return-tracking.css'
import '../styles/renewal-return-flow.css'

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
        : { key, data: null, error: presentValidationError('We could not load the billing details for this contract. Refresh the page or contact support for help.') })
    }).catch((error: unknown) => { if (active) setBilling({ key, data: null, error: presentApiError(error) }) })
    contractApi.getReturnSummary(contractId).then(({ data }) => {
      if (!active) return
      setReturn(isReturnSummary(data, contractId)
        ? { key, data, error: null }
        : { key, data: null, error: presentValidationError('We could not load the latest return and settlement details. Refresh the page before checking the amounts, or contact the facility for help.') })
    }).catch((error: unknown) => { if (active) setReturn({ key, data: null, error: presentApiError(error) }) })
    return () => { active = false }
  }, [contractId, key])

  return <main className="page-container flow-page customer-tracking-page completion-flow">
    <header className="tracking-page-heading">
      <div><h1>Fees &amp; Return Tracking</h1><p className="muted">Review billing, inspection and recorded settlement amounts for this contract.</p></div>
      <button className="button button-secondary" disabled={busy} type="button" onClick={() => { contractReader.refresh(); setRetry(value => value + 1) }}><RefreshCw size={16} aria-hidden="true" />Refresh Tracking</button>
    </header>
    <div className="tracking-navigation">
      <Link className="tracking-text-link" to={`/customer/contracts/${encodeURIComponent(contractId)}`}><FileText size={16} aria-hidden="true" />View Contract<ArrowRight size={14} aria-hidden="true" /></Link>
      <Link className="tracking-text-link" to="/customer/contracts">Switch Contract<ArrowRight size={14} aria-hidden="true" /></Link>
      <Link className="tracking-text-link" to="/customer/invoices?tab=invoices"><ReceiptText size={16} aria-hidden="true" />All Invoices<ArrowRight size={14} aria-hidden="true" /></Link>
    </div>
    <ApiErrorAlert error={contractReader.error} />
    {contractReader.loading ? <LoadingState label="Loading your contract..." /> : contract && <>
      <section className="panel tracking-contract">
        <div className="tracking-contract-heading"><span className="tracking-icon"><Warehouse size={22} aria-hidden="true" /></span><div><h2>Selected Contract</h2><p>{contractOption(contract)}</p></div><StatusBadge status={contract.status} /></div>
        <p className="tracking-notice"><Info size={17} aria-hidden="true" /><span>First-month rent is handled offline at handover and is not included in this invoice list. Review recorded invoices for later rental months below.</span></p>
      </section>
      <CustomerTabs labels={['Billing & Overdue', 'Return & Settlement']} selected={section} onChange={setSection}>{({ tabId, panelId }) => <>
      <section className="tracking-tab-panel" role="tabpanel" id={panelId(0)} aria-labelledby={tabId(0)} hidden={section !== 0} tabIndex={0}>
        <h2>Billing &amp; Overdue</h2>
        <ApiErrorAlert error={billingLoading ? null : billingSnapshot.error} />
        {billingLoading ? <LoadingState label="Loading billing summary..." /> : billing && <>
          <div className="tracking-columns">
            <section className="panel tracking-card tracking-invoices" aria-labelledby="tracking-invoices-heading">
              <div className="tracking-card-heading"><ReceiptText size={20} aria-hidden="true" /><h3 id="tracking-invoices-heading">Invoices</h3></div>
              {billing.invoices.length === 0 ? <EmptyState message="No invoices have been returned for this Contract." /> : billing.invoices.map(invoice => <article className="tracking-record" key={invoice.invoiceId}>
                <div className="tracking-record-heading"><h4>{invoice.invoiceType === 'RENTAL_FEE' ? 'Rental Invoice' : 'Deposit Invoice'}</h4><StatusBadge status={invoice.status} /></div>
                <dl className="tracking-facts"><div><dt>Billing Month</dt><dd>{invoice.billingMonth ?? 'Not Applicable'}</dd></div><div><dt>Amount Due</dt><dd>{formatMoney(invoice.amountDue)}</dd></div><div><dt>Due (GMT+7)</dt><dd>{formatUtcDateTime(invoice.dueDate)}</dd></div><div><dt>Paid (GMT+7)</dt><dd>{invoice.paidAt ? formatUtcDateTime(invoice.paidAt) : 'Not Recorded'}</dd></div></dl>
                <Link className="tracking-text-link tracking-record-link" to={`/customer/invoices/${encodeURIComponent(invoice.invoiceId)}`}>View Invoice<ArrowRight size={14} aria-hidden="true" /></Link>
              </article>)}
            </section>
            <section className="panel tracking-card tracking-late-fees" aria-labelledby="tracking-late-fees-heading">
              <div className="tracking-card-heading"><CalendarClock size={20} aria-hidden="true" /><h3 id="tracking-late-fees-heading">Late Fee Records</h3></div>
              {billing.lateFees.length === 0 ? <EmptyState message="No late-fee records have been returned." /> : billing.lateFees.map(fee => <article className="tracking-record" key={fee.lateFeeId}><dl className="tracking-facts">
                <div><dt>Related Invoice</dt><dd><Link className="tracking-text-link" to={`/customer/invoices/${encodeURIComponent(fee.invoiceId)}`}>View Invoice<ArrowRight size={14} aria-hidden="true" /></Link></dd></div>
                <div><dt>Overdue Days</dt><dd>{fee.overdueDays}</dd></div><div><dt>Recorded Late Fee</dt><dd className="tracking-amount-warning">{formatMoney(fee.amount)}</dd></div><div><dt>Calculated (GMT+7)</dt><dd>{formatUtcDateTime(fee.calculatedAt)}</dd></div>
              </dl></article>)}
              <p className="tracking-note">These are recorded fee amounts. A fee record alone is not a payment request; applicable deductions are shown in settlement.</p>
            </section>
          </div>
        </>}
      </section>
      <section className="tracking-tab-panel" role="tabpanel" id={panelId(1)} aria-labelledby={tabId(1)} hidden={section !== 1} tabIndex={0}>
        <h2>Return, Inspection &amp; Settlement</h2>
        <ApiErrorAlert error={returnLoading ? null : returnSnapshot.error} />
        {returnLoading ? <LoadingState label="Loading return summary..." /> : returnSummary && <>
          <section className="panel tracking-summary-status">
            <div className="tracking-record-heading"><h3>Contract Status</h3><StatusBadge status={returnSummary.contractStatus} /></div>
            {returnSummary.contractStatus !== contract.status && <p className="tracking-notice tracking-warning"><Info size={17} aria-hidden="true" /><span>The separate Contract and return-summary reads differ. Refresh to review the current server state.</span></p>}
          </section>
          <div className="tracking-columns">
            <div className="tracking-stack">
              <section className="panel tracking-card tracking-return-visit" aria-labelledby="tracking-return-visit-heading">
                <div className="tracking-card-heading"><Undo2 size={20} aria-hidden="true" /><h3 id="tracking-return-visit-heading">Return Visit</h3>{returnSummary.returnVisit && <StatusBadge status={returnSummary.returnVisit.status} />}</div>
                {returnSummary.returnVisit ? <>
                  <dl className="tracking-facts"><div><dt>Scheduled Return Date (GMT+7)</dt><dd>{returnSummary.returnVisit.visitDate}</dd></div><div><dt>Actual Return Date (GMT+7)</dt><dd>{returnSummary.returnVisit.actualReturnDate ?? 'Not Recorded'}</dd></div></dl>
                  <Link className="tracking-text-link" to={`/customer/visits/${encodeURIComponent(returnSummary.returnVisit.visitId)}`}>View Return Visit<ArrowRight size={14} aria-hidden="true" /></Link>
                  {['SCHEDULED', 'CHECKED_IN'].includes(returnSummary.returnVisit.status) && <p className="tracking-notice"><Info size={17} aria-hidden="true" /><span>A pending RETURN Visit blocks new ACCESS Visits and renewal. Scheduling is not proof of completed return.</span></p>}
                </> : <EmptyState message="No return visit has been reported." />}
              </section>
              <section className="panel tracking-card tracking-inspection" aria-labelledby="tracking-inspection-heading">
                <div className="tracking-card-heading"><ClipboardCheck size={20} aria-hidden="true" /><h3 id="tracking-inspection-heading">Inspection Progress</h3>{returnSummary.inspection && <StatusBadge status={returnSummary.inspection.status} />}</div>
                {returnSummary.inspection ? <>
                  <p className="tracking-condition-note">{returnSummary.inspection.conditionNote ?? 'No inspection note recorded.'}</p>
                  <p className="tracking-completed">Completed (GMT+7): <strong>{returnSummary.inspection.completedAt ? formatUtcDateTime(returnSummary.inspection.completedAt) : 'Not Completed'}</strong></p>
                </> : <EmptyState message="No inspection has been reported." />}
                <p className="tracking-note">Inspection completion alone does not finalize a return. Review the recorded contract and settlement status for the latest outcome.</p>
              </section>
              <section className="panel tracking-card tracking-damages" aria-labelledby="tracking-damages-heading">
                <div className="tracking-card-heading"><ShieldAlert size={20} aria-hidden="true" /><h3 id="tracking-damages-heading">Damage Records</h3></div>
                {returnSummary.damages.length === 0 ? <EmptyState message="No Damage records have been returned." /> : returnSummary.damages.map(damage => <article className="tracking-record" key={damage.damageRecordId}>
                  <div className="tracking-record-heading"><h4>{damage.note ?? 'Damage Record'}</h4><StatusBadge status={damage.status} /></div>
                  <p className="tracking-record-amount">Recorded Damage Amount: <strong>{formatMoney(damage.damageAmount)}</strong></p>
                  <p className="tracking-note">{damage.status === 'APPROVED' ? 'Approved for consideration in server-calculated settlement.' : damage.status === 'PENDING' ? 'Awaiting Manager decision; not an approved deduction.' : 'Rejected; not an approved deduction.'}</p>
                </article>)}
                {returnSummary.damages.some(damage => damage.status === 'PENDING') && <p className="tracking-notice tracking-warning"><Info size={17} aria-hidden="true" /><span>Your return cannot be finalized until the Facility Manager has resolved the pending damage decisions.</span></p>}
              </section>
              <section className="panel tracking-card tracking-extra-fees" aria-labelledby="tracking-extra-fees-heading">
                <div className="tracking-card-heading"><ReceiptText size={20} aria-hidden="true" /><h3 id="tracking-extra-fees-heading">Extra Fee Records</h3></div>
                {returnSummary.extraFees.length === 0 ? <EmptyState message="No Extra Fee records have been returned." /> : returnSummary.extraFees.map(fee => <article className="tracking-record" key={fee.extraFeeId}><h4>{fee.reason}</h4><p className="tracking-record-amount">Recorded Extra Fee: <strong>{formatMoney(fee.amount)}</strong></p></article>)}
              </section>
            </div>
            <section className="panel tracking-card tracking-settlement" aria-labelledby="tracking-settlement-heading">
              <div className="tracking-card-heading"><Wallet size={20} aria-hidden="true" /><h3 id="tracking-settlement-heading">Deposit Settlement</h3>{returnSummary.settlement && <StatusBadge status={returnSummary.settlement.status} />}</div>
              {returnSummary.settlement ? <>
                <dl className="tracking-settlement-metrics"><div className="tracking-deduction"><dt>Total Deduction</dt><dd>{formatMoney(returnSummary.settlement.totalDeduction)}</dd></div><div className="tracking-refund"><dt>Refund Amount</dt><dd>{formatMoney(returnSummary.settlement.refundAmount)}</dd></div><div className="tracking-additional"><dt>Additional Amount Due</dt><dd>{formatMoney(returnSummary.settlement.additionalAmountDue)}</dd></div></dl>
                <p className="tracking-notice"><Info size={17} aria-hidden="true" /><span>These are recorded settlement amounts, not confirmation that a refund transfer or debt collection has occurred. This page performs no financial transaction.</span></p>
              </> : <EmptyState message="Settlement has not been recorded yet. Refund and additional amounts due are not yet known." />}
            </section>
          </div>
        </>}
      </section>
      </>}</CustomerTabs>
    </>}
  </main>
}
