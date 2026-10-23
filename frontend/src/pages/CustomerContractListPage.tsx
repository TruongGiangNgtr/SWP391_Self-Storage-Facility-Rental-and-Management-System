import { useEffect, useState, type FormEvent, type ReactNode } from 'react'
import { Link, useLocation, useSearchParams } from 'react-router-dom'
import { ArrowRight, CalendarDays, CalendarPlus, CircleHelp, CirclePlus, FileCheck2, FileText, History, Info, Link2, MapPin, RefreshCw, Search, ShieldCheck, SlidersHorizontal, Undo2, Wallet, type LucideIcon } from 'lucide-react'
import type { ApiCollectionResponse } from '../api/api.types'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { contractApi } from '../api/contractApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { CustomerTabs } from '../components/CustomerTabs'
import { EmptyState, LoadingState, PaginationControls } from '../components/PageStates'
import { CustomerStatusBadge as StatusBadge } from '../components/CustomerStatusBadge'
import { CONTRACT_TABS, isContractListResponse, type ContractFilters, type ContractListItem } from '../features/contracts/contractListPresentation'
import { useContractCardData } from '../features/contracts/useContractCardData'
import { useReservationFacilities } from '../features/reservations/useReservationUnitTypes'
import { reservationDuration, reservationPeriod } from '../features/reservations/reservationListPresentation'
import { contractTitle, contractUnit, customerLabel } from '../utils/customerPresentation'
import { formatMoney } from '../utils/formatters'
import '../styles/customer-contracts.css'

function ContractFilterForm({ initial, facilities, onApply }: { initial: ContractFilters; facilities: { id: string; name: string }[]; onApply(filters: ContractFilters): void }) {
  const [draft, setDraft] = useState(initial)
  function submit(event: FormEvent<HTMLFormElement>) { event.preventDefault(); onApply({ ...draft, query: draft.query.trim() }) }
  return <form className="panel contracts-filter-form" aria-label="Filter contracts on this page" onSubmit={submit}>
    <div className="form-field"><label htmlFor="contract-search">Search this page</label><div className="contracts-search"><Search size={18} aria-hidden="true" /><input id="contract-search" type="search" value={draft.query} placeholder="Facility, storage type, unit or period" onChange={event => setDraft(current => ({ ...current, query: event.target.value }))} /></div></div>
    <div className="form-field"><label htmlFor="contract-facility">Facility</label><select id="contract-facility" value={draft.facility} onChange={event => setDraft(current => ({ ...current, facility: event.target.value }))}><option value="">All Facilities</option>{facilities.map(facility => <option key={facility.id} value={facility.id}>{facility.name}</option>)}</select></div>
    <div className="form-field"><label htmlFor="contract-status">Status</label><select id="contract-status" value={draft.status} onChange={event => setDraft(current => ({ ...current, status: event.target.value as ContractFilters['status'] }))}><option value="">All Statuses</option><option value="ACTIVE">Active</option><option value="COMPLETED">Completed</option><option value="TERMINATED">Terminated</option></select></div>
    <button className="button" type="submit"><SlidersHorizontal size={16} aria-hidden="true" />Apply Filters</button>
  </form>
}

function ContractFact({ label, icon: Icon, children, footer }: { label: string; icon: LucideIcon; children: ReactNode; footer: ReactNode }) {
  return <div><dt>{label}<Icon size={17} aria-hidden="true" /></dt><dd>{children}</dd><div className="contracts-fact-footer">{footer}</div></div>
}

export function CustomerContractListPage() {
  const location = useLocation()
  const [params, setParams] = useSearchParams()
  const rawPage = Number(params.get('page') ?? '1')
  const page = Number.isSafeInteger(rawPage) && rawPage > 0 ? rawPage : 1
  const selectedTab = params.get('tab') === 'history' ? 1 : 0
  const filters: ContractFilters = { query: params.get('q') ?? '', facility: params.get('facility') ?? '', status: (['ACTIVE', 'COMPLETED', 'TERMINATED'] as const).find(status => status === params.get('status')) ?? '' }
  const [revision, setRevision] = useState(0)
  const [selectedContractId, setSelectedContractId] = useState('')
  const key = `${page}:${revision}`
  const [snapshot, setSnapshot] = useState<{ key: string; response: ApiCollectionResponse<ContractListItem> | null; error: ApiErrorPresentation | null } | null>(null)
  const loading = snapshot?.key !== key
  const response = loading ? null : snapshot?.response
  const error = loading ? null : snapshot?.error ?? null
  const contracts = response?.data ?? []
  const facilities = useReservationFacilities(contracts.map(contract => contract.facilityId))
  const details = useContractCardData(contracts)
  useEffect(() => {
    let active = true
    contractApi.list(page).then(result => {
      if (active) setSnapshot(isContractListResponse(result, page) ? { key, response: result, error: null }
        : { key, response: null, error: presentValidationError('We could not load your contracts. Refresh the page to check your latest rentals.') })
    }).catch((caught: unknown) => { if (active) setSnapshot({ key, response: null, error: presentApiError(caught) }) })
    return () => { active = false }
  }, [page, key])
  function applyFilters(next: ContractFilters, nextTab = selectedTab) {
    const query = new URLSearchParams()
    if (page > 1) query.set('page', String(page))
    if (nextTab === 1) query.set('tab', 'history')
    if (next.query) query.set('q', next.query)
    if (next.facility) query.set('facility', next.facility)
    if (next.status) query.set('status', next.status)
    setParams(query)
  }
  const clearFilters = () => applyFilters({ query: '', facility: '', status: '' })
  const refresh = () => { setRevision(value => value + 1); facilities.retry(); details.retry() }
  const filterPending = Boolean(filters.query.trim()) && (facilities.loading || details.loading)
  const visible = contracts.filter(contract => {
    const facility = facilities.reads[contract.facilityId]?.data
    const detail = details.reads[contract.contractId]?.contract ?? contract
    const text = `${contractTitle(detail)} ${contractUnit(detail)} ${facility?.name ?? ''} ${facility?.address ?? ''} ${contract.startMonth} ${contract.endMonth} ${reservationPeriod(contract.startMonth, contract.endMonth)}`.toLowerCase()
    return (!filters.facility || contract.facilityId === filters.facility) && (!filters.status || contract.status === filters.status) && text.includes(filters.query.trim().toLowerCase())
  })
  const activeContracts = visible.filter(contract => contract.status === 'ACTIVE')
  const historyContracts = visible.filter(contract => contract.status !== 'ACTIVE')
  const facilityOptions = [...new Set(contracts.map(contract => contract.facilityId))].map(id => ({ id, name: facilities.reads[id]?.data?.name ?? 'Facility name unavailable' }))
  const chargeContracts = selectedTab === 1 ? historyContracts : visible
  const chargeContract = chargeContracts.find(contract => contract.contractId === selectedContractId) ?? chargeContracts.find(contract => contract.status === 'ACTIVE') ?? chargeContracts[0]
  const chargeReservation = chargeContract && details.reads[chargeContract.contractId]?.reservation

  function renderCard(contract: ContractListItem) {
    const read = details.reads[contract.contractId]
    const original = read?.reservation
    const display = read?.contract ?? contract
    const facility = facilities.reads[contract.facilityId]?.data
    const detailPath = `/customer/contracts/${encodeURIComponent(contract.contractId)}`
    const historical = contract.status !== 'ACTIVE'
    const Icon = historical ? History : FileText
    return <article className={`panel contracts-card contracts-card-${contract.status.toLowerCase()}`} key={contract.contractId} aria-label={`${contractTitle(display)} · ${customerLabel(contract.status)}`}>
      <header className="contracts-card-heading"><span className="contracts-icon"><Icon size={25} aria-hidden="true" /></span><div className="contracts-card-title"><div><h2>{contractTitle(display)}</h2><StatusBadge status={contract.status} /></div><div className="contracts-card-meta"><span><MapPin size={14} aria-hidden="true" />{facility?.name ?? (facilities.reads[contract.facilityId]?.error ? 'Facility unavailable' : 'Loading facility…')}</span>{read?.contract ? <Link to={`/customer/reservations/${encodeURIComponent(read.contract.reservationId)}`}><Link2 size={14} aria-hidden="true" />Original Reservation</Link> : <span>{read ? 'Reservation details unavailable' : 'Loading original reservation…'}</span>}</div></div><Link className="button button-secondary contracts-detail-link" to={detailPath}>{historical ? 'View Details' : 'View Contract'}<ArrowRight size={14} aria-hidden="true" /></Link></header>
      <dl className="contracts-facts">
        <ContractFact label="Contract Period" icon={CalendarDays} footer={<span>Rental duration: <strong>{reservationDuration(contract.startMonth, contract.endMonth)}</strong></span>}>{reservationPeriod(contract.startMonth, contract.endMonth)}</ContractFact>
        <ContractFact label="Initial Monthly Rent" icon={Wallet} footer={<span>Original reservation rate. Renewal pricing may differ.</span>}>{original ? <>{formatMoney(original.lockedRentalPrice)}<small>/ month</small></> : <span className="contracts-unavailable">{read ? 'Not available' : 'Loading…'}</span>}</ContractFact>
        <ContractFact label="Original Security Deposit" icon={ShieldCheck} footer={original ? <><span>Deposit invoice</span><StatusBadge status={original.depositInvoice.status} /></> : <span>Original reservation snapshot</span>}>{original ? formatMoney(original.depositAmount) : <span className="contracts-unavailable">{read ? 'Not available' : 'Loading…'}</span>}</ContractFact>
        <ContractFact label="Agreement Status" icon={FileCheck2} footer={<span>Storage unit: <strong>{contractUnit(display)}</strong></span>}><span className="contracts-agreement-status">{customerLabel(contract.status)}</span></ContractFact>
      </dl>
      {!historical && <div className="contracts-callout"><span className="contracts-callout-icon"><Info size={21} aria-hidden="true" /></span><p>Your contract is active for <strong>{reservationPeriod(contract.startMonth, contract.endMonth)}</strong>. You can request an extension or schedule a return, subject to the current contract and visit rules.</p></div>}
      {historical ? <div className="contracts-history-footer"><p>Review invoices and return settlement for the recorded outcome. The original deposit is not a current balance or refund confirmation.</p><Link className="contracts-text-link" to={`${detailPath}/fees-return`}>Fees &amp; Return Tracking<ArrowRight size={14} aria-hidden="true" /></Link></div> : <div className="contracts-actions"><div className="contracts-secondary-actions">{read?.contract?.status === 'ACTIVE' ? <><Link className="button button-secondary contracts-soft-button" to={`${detailPath}/renew`} state={{ backgroundLocation: location }}><CalendarPlus size={17} aria-hidden="true" />Request Extension</Link><Link className="button button-secondary contracts-soft-button" to={`${detailPath}/return-visits/new`} state={{ backgroundLocation: location }}><Undo2 size={17} aria-hidden="true" />Request Return</Link></> : <span className="muted">{read ? 'Refresh or open the contract to verify available actions.' : 'Loading available contract actions…'}</span>}</div><div className="contracts-primary-actions"><Link className="contracts-text-link contracts-support-link" to="/customer/support-tickets/new" title="Choose the relevant contract when creating your ticket"><CircleHelp size={15} aria-hidden="true" />Need Help? Create Ticket</Link>{read?.contract?.status === 'ACTIVE' && <Link className="button" to={`${detailPath}/access-visits/new`}><CirclePlus size={17} aria-hidden="true" />Create Access Visit</Link>}</div></div>}
    </article>
  }
  function renderHistory() {
    return <section className="contracts-history"><div className="contracts-section-heading"><div><h2>Contract History</h2><p className="muted">Your completed and terminated rental contracts.</p></div><span>{historyContracts.length} on this page</span></div>{historyContracts.length ? <div className="contracts-card-list">{historyContracts.map(renderCard)}</div> : <EmptyState message="No contract history on this page matches your filters." />}</section>
  }
  function renderPanel(historyOnly: boolean) {
    if (loading || filterPending) return <LoadingState label={loading ? 'Loading your contracts…' : 'Loading storage details for your search…'} />
    if (error) return <button className="button button-secondary" type="button" onClick={refresh}>Retry Contracts</button>
    if (!contracts.length) return <div className="panel stack"><EmptyState message="You do not have any contracts on this page. A contract is created after handover is completed." /><Link className="button button-secondary" to="/customer/storage-search">Find Storage</Link></div>
    if (!visible.length) return <div className="panel stack"><EmptyState message="No contracts on this page match your filters." /><button className="button button-secondary" type="button" onClick={clearFilters}>Clear Filters</button></div>
    if (historyOnly) return renderHistory()
    return <div className="contracts-sections"><section aria-label="Active contracts on this page" className="contracts-card-list">{activeContracts.length ? activeContracts.map(renderCard) : <EmptyState message="No active contracts on this page match your filters." />}</section>{historyContracts.length > 0 && renderHistory()}</div>
  }
  return <main className="page-container flow-page customer-contracts">
    <section className="page-heading contracts-heading"><div><p className="eyebrow contracts-eyebrow">Contract Management</p><h1>My Contracts</h1><p className="muted">Manage your active contracts, view rental terms and review contract history.</p></div><Link className="button button-secondary contracts-soft-button" to="/customer/support-tickets"><CircleHelp size={17} aria-hidden="true" />Contact Support</Link></section>
    <ApiErrorAlert error={error} />
    {(facilities.error || details.error) && <div className="panel stack"><p className="muted">Some related details could not be loaded. Missing amounts are not shown as zero. Refresh before relying on a contract whose details have changed.</p><ApiErrorAlert error={details.error ?? facilities.error} /><button className="button button-secondary" type="button" disabled={details.loading || facilities.loading} onClick={() => { details.retry(); facilities.retry() }}>Retry Contract Details</button></div>}
    <ContractFilterForm key={JSON.stringify(filters)} initial={filters} facilities={facilityOptions} onApply={next => applyFilters(next, next.status === 'COMPLETED' || next.status === 'TERMINATED' ? 1 : next.status === 'ACTIVE' ? 0 : selectedTab)} />
    <div className="contracts-list-summary"><p className="muted" role="status">{loading ? 'Loading this page…' : error ? 'Contract list unavailable.' : filterPending ? 'Loading storage details for your search…' : `${visible.length} matching contracts on this page. Counts and filters cover this page only.`}</p><div>{(filters.query || filters.status || filters.facility) && <button className="contracts-text-button" type="button" onClick={clearFilters}>Clear Filters</button>}<button className="contracts-text-button" type="button" disabled={loading} onClick={refresh}><RefreshCw size={14} aria-hidden="true" />Refresh Contracts</button></div></div>
    <div className="contracts-tabs"><CustomerTabs labels={CONTRACT_TABS} counts={loading || error || filterPending ? undefined : [activeContracts.length, historyContracts.length]} ariaLabel="Contract sections" selected={selectedTab} onChange={index => applyFilters({ ...filters, status: '' }, index)}>{({ tabId, panelId }) => CONTRACT_TABS.map((label, index) => <div key={label} role="tabpanel" id={panelId(index)} aria-labelledby={tabId(index)} hidden={selectedTab !== index} tabIndex={0}>{selectedTab === index && renderPanel(index === 1)}</div>)}</CustomerTabs></div>
    {response && <PaginationControls pagination={response.pagination} disabled={loading} onPageChange={next => setParams(current => { const query = new URLSearchParams(current); if (next === 1) query.delete('page'); else query.set('page', String(next)); return query })} />}
    {!loading && !error && !filterPending && chargeContract && <section className="panel contracts-charges"><span className="contracts-icon"><Wallet size={24} aria-hidden="true" /></span><div className="contracts-charges-content"><h2>Deposit &amp; Charges</h2><p className="muted">Review invoices, fees and return settlement for the selected contract.</p><div className="form-field"><label htmlFor="charges-contract">Contract on this page</label><select id="charges-contract" value={chargeContract.contractId} onChange={event => setSelectedContractId(event.target.value)}>{chargeContracts.map(contract => <option key={contract.contractId} value={contract.contractId}>{contractTitle(details.reads[contract.contractId]?.contract ?? contract)} · {contractUnit(details.reads[contract.contractId]?.contract ?? contract)} · {reservationPeriod(contract.startMonth, contract.endMonth)} · {customerLabel(contract.status)}</option>)}</select></div>{chargeReservation && <small>Original deposit: {formatMoney(chargeReservation.depositAmount)}. See settlement for any refund or deduction.</small>}</div><Link className="button" to={`/customer/contracts/${encodeURIComponent(chargeContract.contractId)}/fees-return`}>View Deposit &amp; Charges<ArrowRight size={16} aria-hidden="true" /></Link></section>}
  </main>
}
