import { useState, type ReactNode } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { ArrowRight, ArrowUpRight, Bell, Building2, CalendarDays, CheckCircle2, ChevronRight, Headset, Info, KeyRound, MapPin, MessageSquare, Package, RefreshCw, WalletCards } from 'lucide-react'
import { contractApi } from '../api/contractApi'
import { reservationApi } from '../api/reservationApi'
import { visitApi } from '../api/visitApi'
import { listSupportTickets } from '../api/supportTicketApi'
import { useAuth } from '../auth/auth.context'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState } from '../components/PageStates'
import { CustomerStatusBadge as StatusBadge } from '../components/CustomerStatusBadge'
import { contractTitle, contractUnit, customerLabel } from '../utils/customerPresentation'
import { useOverviewCollection, useOverviewDetail } from '../features/customer-overview/useOverviewCollection'
import { isOverviewContract, isOverviewReservation, isOverviewVisit, overviewDate } from '../features/customer-overview/overviewValidation'
import { isSupportTicket } from '../features/customer-support/supportValidation'
import { isReturnSummary } from '../features/fee-return-tracking/trackingValidation'
import { usePublicFacility, usePublicRead } from '../features/catalog/usePublicCatalog'
import { publicCatalogApi } from '../api/publicCatalogApi'
import { formatMoney, formatMonthRange, getCurrentBusinessDate } from '../utils/formatters'
import { StorageSearchPage } from './StorageSearchPage'
import '../styles/customer-overview.css'

const readContracts = () => contractApi.list(1, 20)
const readVisits = () => visitApi.list(1, 20)
const readTickets = () => listSupportTickets({ page: 1, pageSize: 20 })
const readUnitType = (id: string, signal: AbortSignal) => publicCatalogApi.getUnitType(id, signal)

function OverviewSection({ title, aside, children, className = '' }: { title: string; aside?: ReactNode; children: ReactNode; className?: string }) {
  return <section className={`overview-section ${className}`} aria-label={title}><div className="overview-section-heading"><h2>{title}</h2>{aside}</div>{children}</section>
}

export function CustomerPortalPage() {
  const { user } = useAuth()
  const location = useLocation()
  const [revision, setRevision] = useState(0)
  const [selectedContractId, setSelectedContractId] = useState('')
  const contracts = useOverviewCollection(readContracts, revision, isOverviewContract)
  const visits = useOverviewCollection(readVisits, revision, isOverviewVisit)
  const tickets = useOverviewCollection(readTickets, revision, isSupportTicket)
  const activeContracts = contracts.response?.data.filter(item => item.status === 'ACTIVE') ?? []
  const activeContract = activeContracts.find(item => item.contractId === selectedContractId) ?? activeContracts[0]
  const upcomingVisits = visits.response?.data.filter(item => item.visitType === 'RESERVATION' && item.status === 'SCHEDULED' && item.visitDate >= getCurrentBusinessDate()).sort((a, b) => a.visitDate.localeCompare(b.visitDate)) ?? []
  const upcomingVisit = upcomingVisits[0]
  const openTickets = tickets.response?.data.filter(item => item.status === 'OPEN' || item.status === 'IN_PROGRESS') ?? []
  const openTicket = [...openTickets].sort((a, b) => b.createdAt.localeCompare(a.createdAt))[0]
  const rental = useOverviewDetail(activeContract?.reservationId ?? '', revision, reservationApi.get, isOverviewReservation)
  const handover = useOverviewDetail(upcomingVisit?.entityId ?? '', revision, reservationApi.get, isOverviewReservation)
  const returnSummary = useOverviewDetail(activeContract?.contractId ?? '', revision, contractApi.getReturnSummary, isReturnSummary)
  // Never mix separately loaded resources into another contract/reservation's preview.
  const rentalData = rental.data?.facilityId === activeContract?.facilityId ? rental.data : null
  const handoverData = handover.data?.reservationVisit?.visitId === upcomingVisit?.visitId &&
    handover.data?.reservationVisit?.visitDate === upcomingVisit?.visitDate && handover.data?.reservationVisit?.status === 'SCHEDULED' &&
    handover.data?.status === 'CONFIRMED' ? handover.data : null
  const facility = usePublicFacility(activeContract?.facilityId ?? '', Boolean(activeContract))
  const handoverFacility = usePublicFacility(handoverData?.facilityId ?? '', Boolean(handoverData))
  const handoverType = usePublicRead(handoverData?.unitTypeId ?? '', readUnitType, Boolean(handoverData))
  const deposit = activeContract ? rentalData : handoverData
  const depositLoading = contracts.loading || (activeContract ? rental.loading : visits.loading || handover.loading)
  const settlement = returnSummary.data?.settlement
  const busy = contracts.loading || visits.loading || tickets.loading || rental.loading || handover.loading || returnSummary.loading
  const contractPath = activeContract ? `/customer/contracts/${encodeURIComponent(activeContract.contractId)}` : '/customer/contracts'
  const trackingPath = activeContract ? `${contractPath}/fees-return` : handoverData ? `/customer/reservations/${encodeURIComponent(handoverData.reservationId)}` : '/customer/invoices'
  const scopeNote = 'Counts cover the first 20 records returned by each list, not account-wide totals.'
  const metrics = [
    { title: 'Active Contracts', value: contracts.loading ? '…' : contracts.response ? String(activeContracts.length) : '—', helper: contracts.loading ? 'Loading contracts…' : contracts.error ? 'Currently unavailable' : 'In your loaded contracts', to: '/customer/contracts', icon: Building2, tag: 'Active', tone: 'green' },
    { title: 'Upcoming Check-in', value: visits.loading ? '…' : visits.response ? String(upcomingVisits.length) : '—', helper: upcomingVisit ? overviewDate(upcomingVisit.visitDate) : visits.error ? 'Currently unavailable' : 'No upcoming handovers loaded', to: '/customer/visits', icon: CalendarDays, tag: 'Scheduled', tone: 'sky' },
    { title: 'Security Deposit', value: depositLoading ? '…' : deposit ? formatMoney(deposit.depositAmount) : '—', helper: activeContract ? 'Original deposit · selected contract' : 'Original deposit · upcoming reservation', to: trackingPath, icon: WalletCards, tag: 'Original deposit', tone: 'blue' },
    { title: 'Open Support Tickets', value: tickets.loading ? '…' : tickets.response ? String(openTickets.length) : '—', helper: tickets.error ? 'Currently unavailable' : 'Open or in progress · loaded tickets', to: '/customer/support-tickets', icon: MessageSquare, tag: 'Open', tone: 'slate' },
  ]

  return <main className="page-container flow-page customer-overview">
    <section className="page-heading overview-heading"><div><p className="eyebrow">Customer Portal</p><h1>Welcome back{user?.fullName ? `, ${user.fullName}` : ''}</h1><p className="muted">Manage your storage, reservations, deposits and support requests in one place.</p></div><button className="customer-icon-button" type="button" disabled={busy} aria-label="Refresh Overview" title="Refresh Overview" onClick={() => setRevision(value => value + 1)}><RefreshCw size={18} aria-hidden="true" /></button></section>
    <StorageSearchPage embedded />
    <section className="overview-metrics" aria-label="Storage overview">
      {metrics.map(item => <Link className="panel overview-metric" to={item.to} key={item.title} title={item.title === 'Security Deposit' ? item.helper : scopeNote}><div className="overview-metric-top"><span className={`overview-icon overview-icon-${item.tone}`}><item.icon size={21} aria-hidden="true" /></span><span className={`overview-tag overview-tag-${item.tone}`}>{item.tag}</span></div><div><h2>{item.title}</h2><strong className={item.title === 'Security Deposit' ? 'overview-money' : ''}>{item.value}</strong><p>{item.helper}</p></div></Link>)}
    </section>
    <div className="overview-content-grid">
      <OverviewSection title="Active Contracts" className="overview-contracts" aside={<Link className="overview-section-link" to="/customer/contracts">View All <ChevronRight size={14} aria-hidden="true" /></Link>}>
        <div className="panel overview-contract-card"><ApiErrorAlert error={contracts.error} />
          {contracts.loading ? <LoadingState label="Loading contracts…" /> : contracts.response && !activeContract ? <EmptyState message="No active contracts in your loaded results. Find storage or view all contracts." /> : activeContract && <>
            {activeContracts.length > 1 && <div className="form-field overview-contract-picker"><label htmlFor="overview-contract">Viewing Contract</label><select id="overview-contract" value={activeContract.contractId} onChange={event => setSelectedContractId(event.target.value)}>{activeContracts.map(item => <option key={item.contractId} value={item.contractId}>{contractTitle(item)} · {contractUnit(item)} · {formatMonthRange(item.startMonth, item.endMonth)}</option>)}</select></div>}
            <header className="overview-card-header"><span className="overview-icon"><Building2 size={24} aria-hidden="true" /></span><div><h3>{facility.data?.data.name ?? contractTitle(activeContract)}</h3><p>{facility.data?.data.address ?? (facility.loading ? 'Loading facility…' : 'Facility details unavailable')}</p></div><StatusBadge status={activeContract.status} /></header><ApiErrorAlert error={facility.error} />
            <div className="overview-unit"><span className="overview-icon"><Package size={21} aria-hidden="true" /></span><div><span className="overview-label">Storage Specification</span><strong>{contractTitle(activeContract)}{activeContract.storageUnit?.unitTypeSize ? ` · ${activeContract.storageUnit.unitTypeSize}` : ''}</strong></div><span className="overview-unit-code"><KeyRound size={14} aria-hidden="true" />{contractUnit(activeContract)}</span></div>
            <ApiErrorAlert error={rental.error} /><dl className="overview-contract-facts"><div><dt>Storage Unit</dt><dd>{contractUnit(activeContract)}</dd></div><div><dt>Rental Period</dt><dd>{formatMonthRange(activeContract.startMonth, activeContract.endMonth)}</dd></div><div><dt>Initial Monthly Rent</dt><dd>{rental.loading ? 'Loading…' : rentalData ? `${formatMoney(rentalData.lockedRentalPrice)}/month` : 'Not available'}</dd></div><div><dt>Original Deposit</dt><dd>{rental.loading ? 'Loading…' : rentalData ? formatMoney(rentalData.depositAmount) : 'Not available'}</dd></div></dl>
            <div className="overview-card-actions"><Link className="button" to={contractPath}>View Contract <ArrowRight size={16} aria-hidden="true" /></Link><div className="overview-text-actions"><Link to={`${contractPath}/renew`} state={{ backgroundLocation: location }}>Request Extension</Link><Link to={`${contractPath}/return-visits/new`} state={{ backgroundLocation: location }}>Return Storage</Link></div></div>
          </>}
        </div>
      </OverviewSection>
      <OverviewSection title="Notifications" className="overview-notifications" aside={<Link className="overview-section-link" to="/customer/notifications">View All <ChevronRight size={14} aria-hidden="true" /></Link>}>
        <div className="panel overview-unavailable"><span className="overview-icon"><Bell size={24} aria-hidden="true" /></span><h3>Notifications are not available yet</h3><p className="muted">This feature is not available yet. Please try again later.</p><p className="muted">You can review your visits, reservations and support tickets in the Customer Portal.</p><Link className="overview-section-link" to="/customer/notifications">Open Notifications <ArrowRight size={16} aria-hidden="true" /></Link></div>
      </OverviewSection>
      <OverviewSection title="Upcoming Check-in" className="overview-checkin" aside={<span className="overview-section-meta">{upcomingVisit ? overviewDate(upcomingVisit.visitDate) : 'Handover visits'}</span>}>
        <div className="panel overview-checkin-card"><ApiErrorAlert error={visits.error} />
          {visits.loading ? <LoadingState label="Loading handover visits…" /> : visits.response && !upcomingVisit ? <><EmptyState message="No upcoming handover visits in your loaded results." /><Link className="button button-secondary" to="/customer/visits">View All Visits</Link></> : upcomingVisit && <>
            <header className="overview-card-header"><span className="overview-icon overview-icon-sky"><MapPin size={24} aria-hidden="true" /></span><div><h3>{handoverFacility.data?.data.name ?? 'Scheduled Handover'}</h3><p>{handoverType.data?.data.name ?? (handover.loading ? 'Loading reservation…' : 'Your reservation visit')}{handoverType.data ? ` · ${handoverType.data.data.size}` : ''}</p></div><StatusBadge status={upcomingVisit.status} /></header>
            <ApiErrorAlert error={handover.error} /><ApiErrorAlert error={handoverFacility.error} /><ApiErrorAlert error={handoverType.error} />
            {handover.data && !handoverData && <p className="notice">The reservation and visit details could not be matched. Open your visit to review its current state.</p>}
            <dl className="overview-checkin-facts"><div><dt>Visit Type</dt><dd>Reservation Handover</dd></div><div><dt>Visit Date</dt><dd>{overviewDate(upcomingVisit.visitDate)}</dd></div><div><dt>Rental Period</dt><dd>{handoverData ? formatMonthRange(handoverData.startMonth, handoverData.endMonth) : handover.loading ? 'Loading…' : 'Not available'}</dd></div></dl>
            <div className="overview-checklist">{handoverData && <span className={`overview-tag overview-tag-${handoverData.depositInvoice.status === 'PAID' ? 'green' : 'slate'}`}><CheckCircle2 size={14} aria-hidden="true" />Deposit {customerLabel(handoverData.depositInvoice.status)}</span>}<span className="overview-tag overview-tag-slate"><Info size={14} aria-hidden="true" />Unit selected at handover</span></div>
            <div className="overview-card-actions"><Link className="button button-secondary" to={handoverData ? `/customer/reservations/${encodeURIComponent(handoverData.reservationId)}` : `/customer/visits/${encodeURIComponent(upcomingVisit.visitId)}`}>{handoverData ? 'View Reservation' : 'View Visit'} <ArrowUpRight size={15} aria-hidden="true" /></Link></div>
          </>}
        </div>
      </OverviewSection>
      <OverviewSection title="Deposit & Charges" className="overview-deposit" aside={<span className="overview-section-meta">{activeContract ? 'Selected contract' : 'Upcoming reservation'}</span>}>
        <div className="panel overview-deposit-card"><ApiErrorAlert error={activeContract ? rental.error : handover.error} /><ApiErrorAlert error={returnSummary.error} />
          <p className="overview-deposit-context muted">{activeContract ? `${contractTitle(activeContract)} · ${contractUnit(activeContract)}` : handoverData ? 'Deposit for your next handover' : 'Choose a contract or schedule a handover to view related deposit details.'}</p>
          <dl className="overview-deposit-facts"><div><dt>Original Deposit</dt><dd>{depositLoading ? 'Loading…' : deposit ? formatMoney(deposit.depositAmount) : 'Not available'}</dd></div><div><dt>Recorded Deductions</dt><dd>{returnSummary.loading ? 'Loading…' : settlement ? formatMoney(settlement.totalDeduction) : 'Not available'}</dd></div><div className="overview-refund"><dt>{settlement?.status === 'FINALIZED' ? 'Final Refund Amount' : settlement ? 'Pending Refund Amount' : 'Refund Amount'}</dt><dd>{returnSummary.loading ? 'Loading…' : settlement ? formatMoney(settlement.refundAmount) : 'Not available yet'}</dd></div></dl>
          <p className="muted overview-deposit-note">Original deposit is not your refundable balance. Deductions and refund amounts appear only when returned by the settlement service; final amounts depend on return and inspection.</p>
          <Link className="button button-secondary" to={trackingPath}>{activeContract ? 'View Deposit & Charges' : handoverData ? 'View Reservation Deposit' : 'View Deposit & Invoices'}</Link>
        </div>
      </OverviewSection>
    </div>
    <section className="panel overview-support" aria-label="Open Support Ticket"><span className="overview-icon"><Headset size={24} aria-hidden="true" /></span><div className="overview-support-content"><div className="overview-support-heading"><h2>Open Support Ticket</h2>{openTicket && <StatusBadge status={openTicket.status} />}</div><ApiErrorAlert error={tickets.error} />{tickets.loading ? <p role="status" className="muted">Loading support tickets…</p> : tickets.response && <p className="muted">{openTicket ? `${customerLabel(openTicket.category)} · Submitted ${new Intl.DateTimeFormat('en-GB', { timeZone: 'Asia/Ho_Chi_Minh', day: 'numeric', month: 'short', year: 'numeric' }).format(new Date(openTicket.createdAt))}` : 'No open support tickets in your loaded results.'}</p>}</div><div className="overview-support-actions">{openTicket && <Link className="button button-secondary" to={`/customer/support-tickets/${encodeURIComponent(openTicket.supportTicketId)}`}>View Ticket</Link>}<Link className="overview-section-link" to="/customer/support-tickets/new">Create Support Request</Link></div></section>
    <details className="overview-coverage"><summary>About these overview figures</summary><p>{scopeNote} The preview shows one selected contract and the earliest loaded handover. Security Deposit shows that contract's original deposit, not an account-wide or estimated refundable balance. Open the full lists to review other pages.</p></details>
  </main>
}
