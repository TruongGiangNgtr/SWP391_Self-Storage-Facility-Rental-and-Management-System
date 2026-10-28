import { Link, useLocation, useParams } from 'react-router-dom'
import { ArrowLeft, ArrowRight, Building2, CalendarDays, CalendarPlus, CircleHelp, CirclePlus, FileText, Info, MapPin, ReceiptText, RefreshCw, ShieldCheck, SlidersHorizontal, Undo2, Wallet, Warehouse } from 'lucide-react'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { CustomerStatusBadge as StatusBadge } from '../components/CustomerStatusBadge'
import { contractTitle, contractUnit, customerLabel } from '../utils/customerPresentation'
import { useCustomerContractDetails } from '../features/contracts/useCustomerContractDetails'
import { reservationDuration, reservationPeriod } from '../features/reservations/reservationListPresentation'
import { formatMoney } from '../utils/formatters'
import '../styles/customer-contract-details.css'

export function CustomerContractDetailPage() {
  const location = useLocation()
  const { contractId = '' } = useParams()
  const { contract, loading, error, refresh, reservation, originalLoading, originalError, facility, facilityLoading, facilityError } = useCustomerContractDetails(contractId)
  const active = contract?.status === 'ACTIVE'
  const detailsPath = `/customer/contracts/${encodeURIComponent(contractId)}`
  const unavailable = originalLoading ? 'Loading…' : 'Not available'
  return (
    <main className="page-container flow-page customer-contract-details">
      <div className="contract-detail-navigation"><Link className="contract-detail-text-link" to="/customer/contracts"><ArrowLeft size={16} aria-hidden="true" />Back to My Contracts</Link><button className="contract-detail-text-button" type="button" disabled={loading} onClick={refresh}><RefreshCw size={14} aria-hidden="true" />{loading ? 'Refreshing…' : 'Refresh Contract'}</button></div>
      <header className="contract-detail-heading"><div><h1>{contract ? contractTitle(contract) : 'Contract Details'}</h1>{contract && <StatusBadge status={contract.status} />}</div><p className="muted">Review your storage details, rental terms and contract management options.</p></header>
      <ApiErrorAlert error={error} />
      {loading ? <LoadingState label="Loading your contract..." /> : contract && <>
        <section className="rental-contract-summary" aria-label="Rental at a glance">
          <dl><div><dt>Storage Unit</dt><dd>{contractUnit(contract)}</dd></div><div><dt>Facility</dt><dd>{facility?.name ?? (facilityLoading ? 'Loading…' : 'Not available')}</dd></div><div><dt>Rental Period</dt><dd>{reservationPeriod(contract.startMonth, contract.endMonth)}</dd></div></dl>
          {active && <div className="contract-detail-access"><div><p className="muted">An access visit does not create a reservation or change your rental period.</p></div><Link className="button" to={`${detailsPath}/access-visits/new`}><CirclePlus size={16} aria-hidden="true" />Create Access Visit</Link></div>}
        </section>
        <section className={`contract-detail-banner contract-detail-banner-${contract.status.toLowerCase()}`} aria-label="Recorded contract status"><ShieldCheck size={23} aria-hidden="true" /><div><h2>Your storage contract is {customerLabel(contract.status).toLowerCase()}</h2><p>{active ? <>The recorded rental period is <strong>{reservationPeriod(contract.startMonth, contract.endMonth)}</strong>. Extension, return and access-visit eligibility are checked against the current rules when you submit.</> : <>This is a historical contract. Review its rental terms, invoices and return settlement below. New access visits, extensions and return requests are not available for this contract.</>}</p></div></section>
        <div className="contract-detail-columns">
          <div className="contract-detail-stack">
            <section className="panel contract-detail-card" aria-labelledby="contract-overview-heading"><div className="contract-detail-card-heading"><FileText size={20} aria-hidden="true" /><h2 id="contract-overview-heading">Contract Overview</h2></div>
              <dl className="contract-detail-facts"><div><dt>Storage Type</dt><dd>{contractTitle(contract)}</dd></div><div><dt>Original Reservation</dt><dd><Link className="contract-detail-text-link" to={`/customer/reservations/${encodeURIComponent(contract.reservationId)}`}>View Reservation<ArrowRight size={14} aria-hidden="true" /></Link></dd></div><div><dt>Contract Status</dt><dd><StatusBadge status={contract.status} /></dd></div><div><dt>Rental Duration</dt><dd>{reservationDuration(contract.startMonth, contract.endMonth)}</dd></div><div><dt>Start Month</dt><dd>{contract.startMonth}</dd></div><div><dt>Current End Month</dt><dd>{contract.endMonth}</dd></div></dl>
            </section>
            <section className="panel contract-detail-card" aria-labelledby="manage-contract-heading"><div className="contract-detail-card-heading"><SlidersHorizontal size={20} aria-hidden="true" /><h2 id="manage-contract-heading">Manage Contract</h2></div>
              {active ? <><p className="muted">Extend your rental period or arrange a storage return, subject to the current contract and visit rules.</p><div className="contract-detail-actions"><Link className="button button-secondary" to={`${detailsPath}/renew`} state={{ backgroundLocation: location }}><CalendarPlus size={17} aria-hidden="true" />Request Extension</Link><Link className="button button-secondary contract-detail-soft-button" to={`${detailsPath}/return-visits/new`} state={{ backgroundLocation: location }}><Undo2 size={17} aria-hidden="true" />Request Return</Link></div><p className="contract-detail-helper">Opening a form does not change your contract. A scheduled return does not end it immediately.</p></> : <p className="muted">This contract is {customerLabel(contract.status).toLowerCase()}. Its management actions are no longer available. You can still review the related visits and recorded financial outcomes.</p>}
              <Link className="contract-detail-text-link" to="/customer/visits"><CalendarDays size={15} aria-hidden="true" />My Visits<ArrowRight size={14} aria-hidden="true" /></Link>
            </section>
            <section className="panel contract-detail-card" aria-labelledby="rental-terms-heading"><div className="contract-detail-card-heading"><Wallet size={20} aria-hidden="true" /><h2 id="rental-terms-heading">Rental Terms</h2></div>
              <ApiErrorAlert error={originalError} />
              <dl className="contract-detail-facts"><div><dt>Original Monthly Rent</dt><dd className="contract-detail-rent">{reservation ? <>{formatMoney(reservation.lockedRentalPrice)}<small>/ month</small></> : unavailable}</dd></div><div><dt>Original Security Deposit</dt><dd>{reservation ? formatMoney(reservation.depositAmount) : unavailable}</dd></div><div><dt>Deposit Invoice Status</dt><dd>{reservation ? <StatusBadge status={reservation.depositInvoice.status} /> : unavailable}</dd></div><div><dt>Agreement Status</dt><dd>{customerLabel(contract.status)}</dd></div></dl>
              <div className="contract-detail-info"><Info size={17} aria-hidden="true" /><p>These prices are the original reservation snapshots. Renewal pricing may differ; use the recorded invoices for later billed amounts. The original deposit is not a current balance or a refund confirmation.</p></div>
              <Link className="contract-detail-text-link" to={`${detailsPath}/fees-return`}>View Billing &amp; Return Tracking<ArrowRight size={14} aria-hidden="true" /></Link>
            </section>

          </div>
          <aside className="contract-detail-stack" aria-label="Storage and deposit details">
            <section className="panel contract-detail-card" aria-labelledby="rented-storage-heading"><div className="contract-detail-card-heading"><Warehouse size={20} aria-hidden="true" /><h2 id="rented-storage-heading">Rented Storage</h2></div><div className="contract-detail-unit"><span>Physical Unit</span><strong>{contractUnit(contract)}</strong></div>
              <ApiErrorAlert error={facilityError} />
              <dl className="contract-detail-storage-facts"><div><dt><Building2 size={16} aria-hidden="true" />Facility</dt><dd>{facility?.name ?? (facilityLoading ? 'Loading…' : 'Not available')}</dd></div><div><dt><MapPin size={16} aria-hidden="true" />Facility Location</dt><dd>{facility?.address || (facilityLoading ? 'Loading…' : 'Not available')}</dd></div><div><dt>Storage Type / Size</dt><dd>{contract.storageUnit ? `${contract.storageUnit.unitTypeMode === 'PRIVATE' ? 'Private' : 'Public'} · ${contract.storageUnit.unitTypeSize}` : 'Not available'}</dd></div><div><dt>Unit Location</dt><dd>{contract.storageUnit?.locationInfo || 'Not recorded'}</dd></div></dl>
              {facility && <Link className="contract-detail-text-link" to={`/facilities/${encodeURIComponent(contract.facilityId)}`}>View Facility<ArrowRight size={14} aria-hidden="true" /></Link>}
            </section>
            <section className="panel contract-detail-card" aria-labelledby="contract-deposit-heading"><div className="contract-detail-card-heading"><ReceiptText size={20} aria-hidden="true" /><h2 id="contract-deposit-heading">Deposit &amp; Charges</h2></div><div className="contract-detail-deposit"><span>Original Security Deposit</span><strong>{reservation ? formatMoney(reservation.depositAmount) : unavailable}</strong>{reservation && <StatusBadge status={reservation.depositInvoice.status} />}</div><p className="muted">Review your deposit invoice and recorded return charges. A refundable amount is confirmed through settlement, not calculated here.</p>{reservation && <Link className="contract-detail-text-link" to={`/customer/invoices/${encodeURIComponent(reservation.depositInvoice.invoiceId)}`}>View Deposit Invoice<ArrowRight size={14} aria-hidden="true" /></Link>}<Link className="contract-detail-text-link" to="/customer/invoices?tab=invoices">View Invoices<ArrowRight size={14} aria-hidden="true" /></Link></section>
            <section className="panel contract-detail-card contract-detail-help"><CircleHelp size={25} aria-hidden="true" /><h2>Need help with your contract?</h2><p className="muted">Create a support ticket for this contract to ask the facility team for help.</p><Link className="button button-secondary" to={`/customer/support-tickets/new?contractId=${encodeURIComponent(contract.contractId)}`}>Create Support Ticket<ArrowRight size={15} aria-hidden="true" /></Link></section>
          </aside>
        </div>
      </>}
    </main>
  )
}
