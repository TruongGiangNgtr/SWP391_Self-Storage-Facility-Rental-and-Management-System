import { useState, type ReactNode } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { ArrowRight, ArrowUpRight, Building2, CalendarDays, CheckCircle2, Headset, Info, KeyRound, MapPin, MessageSquare, Package, RefreshCw, WalletCards } from 'lucide-react'
import { contractApi } from '../api/contractApi'
import { reservationApi } from '../api/reservationApi'
import { visitApi } from '../api/visitApi'
import { listSupportTickets } from '../api/supportTicketApi'
import { useAuth } from '../auth/auth.context'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState } from '../components/PageStates'
import { CustomerStatusBadge as StatusBadge } from '../components/CustomerStatusBadge'
import { FacilityImage } from '../components/FacilityImage'
import { CustomerNotificationList } from '../components/CustomerNotificationList'
import { useNotifications } from '../features/customer-notifications/useNotifications'
import { contractTitle, contractUnit, customerLabel } from '../utils/customerPresentation'
import { useOverviewCollection, useOverviewDetail } from '../features/customer-overview/useOverviewCollection'
import { useOverviewContracts } from '../features/customer-overview/useOverviewContracts'
import { useContractCardData } from '../features/contracts/useContractCardData'
import { isOverviewReservation, isOverviewVisit, overviewDate } from '../features/customer-overview/overviewValidation'
import { isSupportTicket } from '../features/customer-support/supportValidation'
import { isReturnSummary } from '../features/fee-return-tracking/trackingValidation'
import { usePublicFacility, usePublicRead } from '../features/catalog/usePublicCatalog'
import { publicCatalogApi } from '../api/publicCatalogApi'
import { formatMoney, formatMonthRange, getCurrentBusinessDate } from '../utils/formatters'
import { StorageSearchPage } from './StorageSearchPage'
import '../styles/customer-overview.css'
import '../styles/customer-overview-editorial.css'

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
  const contracts = useOverviewContracts(revision)
  const notifications = useNotifications(1, 3, true, revision)
  const visits = useOverviewCollection(readVisits, revision, isOverviewVisit)
  const tickets = useOverviewCollection(readTickets, revision, isSupportTicket)
  const activeContracts = contracts.response?.data.filter(item => item.status === 'ACTIVE') ?? []
  const activeContract = activeContracts.find(item => item.contractId === selectedContractId) ?? activeContracts[0]
  const contractDetails = useContractCardData(activeContract ? [activeContract] : [])
  const selectedDetails = activeContract ? contractDetails.reads[activeContract.contractId] : undefined
  const displayContract = selectedDetails?.contract ?? activeContract
  const upcomingVisits = visits.response?.data.filter(item => item.visitType === 'RESERVATION' && item.status === 'SCHEDULED' && item.visitDate >= getCurrentBusinessDate()).sort((a, b) => a.visitDate.localeCompare(b.visitDate)) ?? []
  const upcomingVisit = upcomingVisits[0]
  const openTickets = tickets.response?.data.filter(item => item.status === 'OPEN' || item.status === 'IN_PROGRESS') ?? []
  const openTicket = [...openTickets].sort((a, b) => b.createdAt.localeCompare(a.createdAt))[0]
  const rental = { data: selectedDetails?.reservation ?? null, loading: contractDetails.loading, error: contractDetails.error }
  const handover = useOverviewDetail(upcomingVisit?.entityId ?? '', revision, reservationApi.get, isOverviewReservation)
  const returnSummary = useOverviewDetail(activeContract?.contractId ?? '', revision, contractApi.getReturnSummary, isReturnSummary)
  // Never mix separately loaded resources into another contract/reservation's preview.
  const rentalData = rental.data
  const handoverData = handover.data?.reservationVisit?.visitId === upcomingVisit?.visitId &&
    handover.data?.reservationVisit?.visitDate === upcomingVisit?.visitDate && handover.data?.reservationVisit?.status === 'SCHEDULED' &&
    handover.data?.status === 'CONFIRMED' ? handover.data : null
  const facility = usePublicFacility(activeContract?.facilityId ?? '', Boolean(activeContract))
  const handoverFacility = usePublicFacility(handoverData?.facilityId ?? '', Boolean(handoverData))
  const handoverType = usePublicRead(handoverData?.unitTypeId ?? '', readUnitType, Boolean(handoverData))
  const deposit = activeContract ? rentalData : handoverData
  const depositLoading = contracts.loading || (activeContract ? rental.loading : visits.loading || handover.loading)
  const settlement = returnSummary.data?.settlement
  const busy = contracts.loading || visits.loading || tickets.loading || rental.loading || handover.loading || returnSummary.loading || notifications.loading
  const contractPath = activeContract ? `/customer/contracts/${encodeURIComponent(activeContract.contractId)}` : '/customer/contracts'
  const trackingPath = activeContract ? `${contractPath}/fees-return` : handoverData ? `/customer/reservations/${encodeURIComponent(handoverData.reservationId)}` : '/customer/invoices'
  const totalContracts = contracts.response?.pagination.totalItems ?? 0
  const loadedContracts = contracts.response?.data.length ?? 0
  const hasMoreContracts = totalContracts > loadedContracts

  const totalVisits = visits.response?.pagination.totalItems ?? 0
  const loadedVisits = visits.response?.data.length ?? 0
  const hasMoreVisits = totalVisits > loadedVisits

  const totalTickets = tickets.response?.pagination.totalItems ?? 0
  const loadedTickets = tickets.response?.data.length ?? 0
  const hasMoreTickets = totalTickets > loadedTickets

  const contractKpiValue = contracts.loading ? '…' : !contracts.response ? '—' : hasMoreContracts ? `${activeContracts.length} (first ${loadedContracts})` : String(activeContracts.length)
  const contractKpiHelper = contracts.loading ? 'Loading contracts…' : contracts.error ? 'Currently unavailable' : hasMoreContracts ? `${activeContracts.length} active in first ${loadedContracts} · ${totalContracts} total contracts` : activeContracts.length === 1 ? '1 active contract' : `${activeContracts.length} active contracts`
  const contractKpiTag = hasMoreContracts ? 'First 20 preview' : 'Active'

  const visitKpiValue = visits.loading ? '…' : !visits.response ? '—' : hasMoreVisits ? `${upcomingVisits.length} (first ${loadedVisits})` : String(upcomingVisits.length)
  const visitKpiHelper = visits.loading ? 'Loading check-ins…' : visits.error ? 'Currently unavailable' : hasMoreVisits ? (upcomingVisit ? `${overviewDate(upcomingVisit.visitDate)} · in first ${loadedVisits}` : `No upcoming check-ins in first ${loadedVisits} · ${totalVisits} total visits`) : upcomingVisit ? overviewDate(upcomingVisit.visitDate) : 'No upcoming handovers'
  const visitKpiTag = hasMoreVisits ? 'First 20 preview' : 'Scheduled'

  const ticketKpiValue = tickets.loading ? '…' : !tickets.response ? '—' : hasMoreTickets ? `${openTickets.length} (first ${loadedTickets})` : String(openTickets.length)
  const ticketKpiHelper = tickets.loading ? 'Loading tickets…' : tickets.error ? 'Currently unavailable' : hasMoreTickets ? `${openTickets.length} open in first ${loadedTickets} · ${totalTickets} total tickets` : openTickets.length === 1 ? '1 open or in progress ticket' : `${openTickets.length} open or in progress tickets`
  const ticketKpiTag = hasMoreTickets ? 'First 20 preview' : 'Open'

  const scopeNote = 'Counts cover the first 20 records returned by each list, not account-wide totals.'
  const metrics = [
    { title: 'Active Contracts', value: contractKpiValue, helper: contractKpiHelper, to: '/customer/contracts', icon: Building2, tag: contractKpiTag, tone: 'green' },
    { title: 'Upcoming Check-in', value: visitKpiValue, helper: visitKpiHelper, to: '/customer/visits', icon: CalendarDays, tag: visitKpiTag, tone: 'sky' },
    { title: 'Security Deposit', value: depositLoading ? '…' : deposit ? formatMoney(deposit.depositAmount) : '—', helper: activeContract ? 'Original deposit · selected contract' : 'Original deposit · upcoming reservation', to: trackingPath, icon: WalletCards, tag: 'Original deposit', tone: 'blue' },
    { title: 'Open Support Tickets', value: ticketKpiValue, helper: ticketKpiHelper, to: '/customer/support-tickets', icon: MessageSquare, tag: ticketKpiTag, tone: 'slate' },
  ]

  return <main className="page-container flow-page customer-overview">
    <section className="page-heading overview-heading"><div><h1>Welcome back{user?.fullName && <>, <span className="overview-customer-name">{user.fullName}</span></>}</h1><p className="overview-intro-label">Your space. All in one place.</p><p className="muted">Manage your storage, reservations, deposits and support requests in one place.</p></div><button className="customer-icon-button overview-refresh" type="button" disabled={busy} aria-label="Refresh Overview" title="Refresh Overview" onClick={() => { contractDetails.retry(); setRevision(value => value + 1) }}><RefreshCw size={18} aria-hidden="true" /><span>Refresh</span></button></section>
    <div className="overview-content-grid">
      <OverviewSection title="Active Contracts" className="overview-contracts" aside={<Link className="button button-secondary overview-section-link" to="/customer/contracts">View All <ArrowUpRight size={15} aria-hidden="true" /></Link>}>
        <div className="panel overview-contract-card"><ApiErrorAlert error={contracts.error} />
          {contracts.loading ? <LoadingState label="Loading contracts…" /> : contracts.response && !activeContract ? <><EmptyState message="No active contracts in your loaded results. Find storage or view all contracts." /><Link className="button" to="/facilities">Find Your Storage <ArrowRight size={16} aria-hidden="true" /></Link></> : activeContract && <>
            {activeContracts.length > 1 && <div className="form-field overview-contract-picker"><label htmlFor="overview-contract">Viewing Contract</label><select id="overview-contract" value={activeContract.contractId} onChange={event => setSelectedContractId(event.target.value)}>{activeContracts.map(item => <option key={item.contractId} value={item.contractId}>{contractTitle(item)} · {contractUnit(item)} · {formatMonthRange(item.startMonth, item.endMonth)}</option>)}</select></div>}
            <div className="overview-rental-identity"><header className="overview-card-header"><span className="overview-icon"><Building2 size={24} aria-hidden="true" /></span><div><h3>{facility.data?.data.name ?? contractTitle(activeContract)}</h3><p>{facility.data?.data.address ?? (facility.loading ? 'Loading facility…' : 'Facility details unavailable')}</p></div><StatusBadge status={activeContract.status} /></header>{facility.data && <FacilityImage facility={facility.data.data} className="overview-rental-photo" eager />}</div><ApiErrorAlert error={facility.error} />
            <div className="overview-unit"><span className="overview-icon"><Package size={21} aria-hidden="true" /></span><div><span className="overview-label">Storage Specification</span><strong>{contractTitle(displayContract ?? activeContract)}{displayContract?.storageUnit?.unitTypeSize ? ` · ${displayContract.storageUnit.unitTypeSize}` : ''}</strong></div><span className="overview-unit-code"><KeyRound size={14} aria-hidden="true" />{contractUnit(displayContract ?? activeContract)}</span></div>
            <ApiErrorAlert error={rental.error} /><dl className="overview-contract-facts"><div><dt>Storage Unit</dt><dd>{contractUnit(activeContract)}</dd></div><div><dt>Rental Period</dt><dd>{formatMonthRange(activeContract.startMonth, activeContract.endMonth)}</dd></div><div><dt>Initial Monthly Rent</dt><dd>{rental.loading ? 'Loading…' : rentalData ? `${formatMoney(rentalData.lockedRentalPrice)}/month` : 'Not available'}</dd></div><div><dt>Original Deposit</dt><dd>{rental.loading ? 'Loading…' : rentalData ? formatMoney(rentalData.depositAmount) : 'Not available'}</dd></div></dl>
            <div className="overview-card-actions"><Link className="button" to={contractPath}>View Contract <ArrowRight size={16} aria-hidden="true" /></Link><div className="overview-text-actions"><Link to={`${contractPath}/renew`} state={{ backgroundLocation: location }}>Request Extension</Link><Link to={`${contractPath}/return-visits/new`} state={{ backgroundLocation: location }}>Return Storage</Link></div></div>
          </>}
        </div>
      </OverviewSection>
      <section className="overview-metrics" aria-label="Storage overview">
        {metrics.map(item => <Link className="panel overview-metric" to={item.to} key={item.title} title={item.title === 'Security Deposit' ? item.helper : scopeNote}><div className="overview-metric-top"><span className={`overview-icon overview-icon-${item.tone}`}><item.icon size={21} aria-hidden="true" /></span><span className={`overview-tag overview-tag-${item.tone}`}>{item.tag}</span></div><div><h2>{item.title}</h2><strong className={item.title === 'Security Deposit' ? 'overview-money' : ''}>{item.value}</strong><p>{item.helper}</p></div></Link>)}
        <p className="overview-metrics-note">{scopeNote}</p>
      </section>
      <OverviewSection title="Upcoming Check-in" className="overview-checkin" aside={<span className="overview-section-meta">{upcomingVisit ? overviewDate(upcomingVisit.visitDate) : 'Handover visits'}</span>}>
        <div className="panel overview-checkin-card"><ApiErrorAlert error={visits.error} />
          {visits.loading ? <LoadingState label="Loading handover visits…" /> : visits.response && !upcomingVisit ? <><EmptyState message="No upcoming handover visits in your loaded results." /><Link className="button button-secondary" to="/customer/visits">View All Visits</Link></> : upcomingVisit && <>
            <div className="overview-visit-date"><CalendarDays size={24} aria-hidden="true" /><div><span>Next handover</span><time dateTime={upcomingVisit.visitDate}>{overviewDate(upcomingVisit.visitDate)}</time></div></div>
            <header className="overview-card-header"><span className="overview-icon overview-icon-sky"><MapPin size={24} aria-hidden="true" /></span><div><h3>{handoverFacility.data?.data.name ?? 'Scheduled Handover'}</h3><p>{handoverType.data?.data.name ?? (handover.loading ? 'Loading reservation…' : 'Your reservation visit')}{handoverType.data ? ` · ${handoverType.data.data.size}` : ''}</p></div><StatusBadge status={upcomingVisit.status} /></header>
            <ApiErrorAlert error={handover.error} /><ApiErrorAlert error={handoverFacility.error} /><ApiErrorAlert error={handoverType.error} />
            {handover.data && !handoverData && <p className="notice">The reservation and visit details could not be matched. Open your visit to review its current state.</p>}
            <dl className="overview-checkin-facts"><div><dt>Visit Type</dt><dd>Reservation Handover</dd></div><div><dt>Rental Period</dt><dd>{handoverData ? formatMonthRange(handoverData.startMonth, handoverData.endMonth) : handover.loading ? 'Loading…' : 'Not available'}</dd></div></dl>
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
      <OverviewSection title="Notifications" className="overview-notifications" aside={<Link className="button button-secondary overview-section-link" to="/customer/notifications">View All <ArrowUpRight size={15} aria-hidden="true" /></Link>}>
        <div className="panel overview-notification-card"><ApiErrorAlert error={notifications.error} />
          {notifications.loading ? <LoadingState label="Loading notifications…" /> : notifications.response && (notifications.response.data.length ? <><CustomerNotificationList items={notifications.response.data} compact /><p className="overview-notification-note">Latest {notifications.response.data.length} {notifications.response.data.length === 1 ? 'notification' : 'notifications'}. Open View All for all updates.</p></> : <EmptyState message="No notifications yet. Updates about your storage activity will appear here once recorded." />)}
          {notifications.error && <button className="button button-secondary overview-notification-retry" type="button" onClick={notifications.refresh}>Retry Notifications</button>}
        </div>
      </OverviewSection>
    </div>
    <section className="panel overview-support" aria-label="Open Support Ticket"><span className="overview-icon"><Headset size={24} aria-hidden="true" /></span><div className="overview-support-content"><div className="overview-support-heading"><h2>Open Support Ticket</h2>{openTicket && <StatusBadge status={openTicket.status} />}</div><ApiErrorAlert error={tickets.error} />{tickets.loading ? <p role="status" className="muted">Loading support tickets…</p> : tickets.response && <p className="muted">{openTicket ? `${customerLabel(openTicket.category)} · Submitted ${new Intl.DateTimeFormat('en-GB', { timeZone: 'Asia/Ho_Chi_Minh', day: 'numeric', month: 'short', year: 'numeric' }).format(new Date(openTicket.createdAt))}` : 'No open support tickets in your loaded results.'}</p>}</div><div className="overview-support-actions">{openTicket && <Link className="button button-secondary" to={`/customer/support-tickets/${encodeURIComponent(openTicket.supportTicketId)}`}>View Ticket</Link>}<Link className="overview-section-link" to="/customer/support-tickets/new">Create Support Request</Link></div></section>
    <details className="overview-coverage"><summary>About these overview figures</summary><p>{scopeNote} The preview shows one selected contract and the earliest loaded handover. Security Deposit shows that contract's original deposit, not an account-wide or estimated refundable balance. Open the full lists to review other pages.</p></details>
    <details className="support-overview-search"><summary>Find storage for another rental</summary><StorageSearchPage embedded /></details>
  </main>
}
