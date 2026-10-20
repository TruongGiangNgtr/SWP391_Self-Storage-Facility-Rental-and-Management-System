import { Link } from 'react-router-dom'
import { ArrowRight, CircleHelp, FileText, Info, MapPin, ReceiptText, ShieldCheck, Wallet } from 'lucide-react'
import { billingApi } from '../../api/billingApi'
import { contractApi } from '../../api/contractApi'
import { presentValidationError } from '../../api/apiErrorPresentation'
import { ApiErrorAlert } from '../../components/ApiErrorAlert'
import { CustomerStatusBadge } from '../../components/CustomerStatusBadge'
import { EmptyState, LoadingState } from '../../components/PageStates'
import type { ContractListItem } from '../contracts/contractListPresentation'
import { useContractCardData } from '../contracts/useContractCardData'
import { useOverviewDetail } from '../customer-overview/useOverviewCollection'
import { isBillingSummary, isReturnSummary } from '../fee-return-tracking/trackingValidation'
import { useReservationFacilities } from '../reservations/useReservationUnitTypes'
import { reservationPeriod } from '../reservations/reservationListPresentation'
import { contractTitle, contractUnit } from '../../utils/customerPresentation'
import { formatMoney, formatUtcDateTime } from '../../utils/formatters'
import { isBillingInvoice } from './useBillingCollection'

export function DepositContractPanel({ item }: { item: ContractListItem }) {
  const details = useContractCardData([item])
  const contract = details.reads[item.contractId]?.contract
  const reservation = details.reads[item.contractId]?.reservation
  const facility = useReservationFacilities([item.facilityId])
  const billingRead = useOverviewDetail(item.contractId, 0, contractApi.getBilling, isBillingSummary)
  const returnRead = useOverviewDetail(item.contractId, 0, contractApi.getReturnSummary, isReturnSummary)
  const invoiceRead = useOverviewDetail(reservation?.depositInvoice.invoiceId ?? '', 0, billingApi.getInvoice, isBillingInvoice)
  const invoiceMatches = !invoiceRead.data || (invoiceRead.data.invoiceType === 'DEPOSIT' && invoiceRead.data.entityId === reservation?.reservationId)
  const returnMatches = !returnRead.data || returnRead.data.contractStatus === item.status
  const original = reservation?.depositAmount
  const invoice = invoiceMatches ? invoiceRead.data : null
  const returned = returnMatches ? returnRead.data : null
  const settlement = returned?.settlement
  const billing = billingRead.data
  const charges = [
    ...(billing?.lateFees ?? []).map(fee => ({ id: fee.lateFeeId, title: 'Late Fee', description: `${fee.overdueDays} overdue days`, amount: fee.amount, date: fee.calculatedAt, status: null, link: `/customer/invoices/${fee.invoiceId}` })),
    ...(returned?.damages ?? []).map(damage => ({ id: damage.damageRecordId, title: 'Damage Charge', description: damage.note || 'Inspection damage record', amount: damage.damageAmount, date: damage.createdAt, status: damage.status, link: null })),
    ...(returned?.extraFees ?? []).map(fee => ({ id: fee.extraFeeId, title: 'Extra Fee', description: fee.reason, amount: fee.amount, date: fee.createdAt, status: null, link: null })),
  ].sort((a, b) => Date.parse(b.date) - Date.parse(a.date))
  const partial = billingRead.loading || returnRead.loading || Boolean(billingRead.error || returnRead.error) || !returnMatches
  return <>
    <section className="billing-context" aria-label="Selected contract">
      <div><h2>{contractTitle(contract ?? item)}</h2><p className="muted"><MapPin size={14} aria-hidden="true" />{facility.reads[item.facilityId]?.data?.name ?? (facility.loading ? 'Loading facility…' : 'Facility name unavailable')} · Unit: {contractUnit(contract ?? item)}</p><p className="muted">{reservationPeriod(item.startMonth, item.endMonth)}</p></div>
      <div className="billing-context-actions"><CustomerStatusBadge status={item.status} /><Link className="button button-secondary" to={`/customer/contracts/${item.contractId}`}>View Contract<ArrowRight size={15} aria-hidden="true" /></Link></div>
    </section>
    <ApiErrorAlert error={details.error ?? facility.error} />
    {details.loading && <LoadingState label="Loading original deposit…" />}
    <section className="panel billing-overview" aria-labelledby="deposit-overview-heading">
      <div className="billing-section-heading"><h2 id="deposit-overview-heading">Deposit Overview</h2><ShieldCheck size={20} aria-hidden="true" /></div>
      <dl className="billing-metrics">
        <div><dt><Wallet size={18} aria-hidden="true" />Original Deposit</dt><dd>{original === undefined ? 'Not available' : formatMoney(original)}</dd><p>Original reservation snapshot</p></div>
        <div className="billing-metric-deduction"><dt><ReceiptText size={18} aria-hidden="true" />Recorded Deductions</dt><dd>{settlement ? formatMoney(settlement.totalDeduction) : returnRead.loading ? 'Loading…' : returnRead.error || !returnMatches ? 'Not available' : 'Not recorded yet'}</dd><p>{settlement ? 'From the recorded settlement' : 'No deduction total is inferred'}</p></div>
        <div className="billing-metric-refund"><dt><ShieldCheck size={18} aria-hidden="true" />Recorded Refund</dt><dd>{settlement ? formatMoney(settlement.refundAmount) : returnRead.loading ? 'Loading…' : returnRead.error || !returnMatches ? 'Not available' : 'Not recorded yet'}</dd><p>{settlement?.status === 'FINALIZED' ? 'Finalized settlement amount' : settlement ? 'Pending settlement · not final' : 'Confirmed through return settlement'}</p></div>
      </dl>
      <div className="billing-info"><Info size={18} aria-hidden="true" /><div>{settlement ? <><CustomerStatusBadge status={settlement.status} /><p>Additional amount due: <strong>{formatMoney(settlement.additionalAmountDue)}</strong>. {settlement.status === 'PENDING' ? 'These settlement values are not final. ' : ''}A recorded refund does not confirm that a bank transfer has been made.</p></> : <p>Deposit, fee and settlement amounts come from system records. Your original deposit is not a calculated current balance; a refundable amount is shown only when a settlement exists.</p>}</div></div>
      <ApiErrorAlert error={returnRead.error ?? (!returnMatches ? presentValidationError('Contract status has changed. Refresh records before reviewing the settlement.') : null)} />
    </section>
    <div className="billing-columns">
      <div className="billing-stack">
        <section className="panel billing-records" aria-labelledby="deposit-activity-heading">
          <div className="billing-section-heading"><div><h2 id="deposit-activity-heading">Deposit Activity</h2><p className="muted">The original deposit invoice and its recorded status.</p></div><FileText size={20} aria-hidden="true" /></div>
          <ApiErrorAlert error={invoiceRead.error ?? (!invoiceMatches ? presentValidationError('The deposit invoice does not match this reservation. Please refresh.') : null)} />
          {details.loading || invoiceRead.loading ? <LoadingState label="Loading deposit invoice…" /> : invoice ? <article className="billing-deposit-record"><div className="billing-record-title"><span className="billing-icon"><Wallet size={20} aria-hidden="true" /></span><div><h3>Reservation Deposit</h3><small>{invoice.paidAt ? `Paid: ${formatUtcDateTime(invoice.paidAt)}` : `Payment deadline: ${formatUtcDateTime(invoice.dueDate)}`}</small></div><CustomerStatusBadge status={invoice.status} /></div><div className="billing-record-bottom"><strong>{formatMoney(invoice.amountDue)}</strong><Link className="billing-text-link" to={`/customer/invoices/${invoice.invoiceId}`}>View Deposit Invoice<ArrowRight size={15} aria-hidden="true" /></Link></div></article> : <p className="muted">The deposit invoice could not be loaded. Refresh records to try again.</p>}
        </section>
        <section className="panel billing-records" aria-labelledby="recorded-charges-heading">
          <div className="billing-section-heading"><div><h2 id="recorded-charges-heading">Recorded Charges</h2><p className="muted">Fee and inspection records, not a list of deposit deductions.</p></div><span className="billing-count">{charges.length} loaded</span></div>
          <ApiErrorAlert error={billingRead.error} />
          {partial && <p className="muted" role="status">{billingRead.loading || returnRead.loading ? 'Loading fee and return records…' : 'Some records are unavailable. Refresh to review the complete summary.'}</p>}
          {!partial && !charges.length && <EmptyState message="No late fees, damage records or extra fees have been recorded." />}
          {charges.map(charge => <article className="billing-charge" key={charge.id}>
            <div className="billing-record-title"><span className="billing-icon"><ReceiptText size={20} aria-hidden="true" /></span><div><h3>{charge.title}</h3><small>Recorded: {formatUtcDateTime(charge.date)}</small></div>{charge.status && <CustomerStatusBadge status={charge.status} />}</div>
            <div className="billing-record-bottom"><p>{charge.description}</p><strong>{formatMoney(charge.amount)}</strong></div>
            {charge.status === 'PENDING' && <p className="billing-charge-note">Awaiting a Manager decision. Not an approved deduction.</p>}{charge.status === 'REJECTED' && <p className="billing-charge-note">Rejected damage record. Not an approved deduction.</p>}
            {charge.link && <Link className="billing-text-link" to={charge.link}>View Related Invoice<ArrowRight size={15} aria-hidden="true" /></Link>}
          </article>)}
          <Link className="billing-text-link" to={`/customer/contracts/${item.contractId}/fees-return`}>View Fees &amp; Return Tracking<ArrowRight size={15} aria-hidden="true" /></Link>
        </section>
      </div>
      <aside className="billing-stack" aria-label="Deposit guidance">
        <section className="panel billing-guidance"><h2>How Your Deposit Works</h2><ol><li>Your original deposit is captured with your reservation.</li><li>Recorded fees and damage decisions are reviewed during return settlement.</li><li>The system records deductions, a refund amount or an additional amount due when settling the return.</li></ol><div className="billing-info"><Info size={18} aria-hidden="true" /><p>Pending damage records are not final charges. A refund amount is not confirmation of a completed payout.</p></div></section>
        <section className="panel billing-help"><CircleHelp size={24} aria-hidden="true" /><h2>Need help with a charge?</h2><p className="muted">Contact the facility team to ask about your recorded fees or settlement.</p><Link className="button button-secondary" to="/customer/support-tickets/new">Create Support Ticket<ArrowRight size={15} aria-hidden="true" /></Link></section>
      </aside>
    </div>
  </>
}
