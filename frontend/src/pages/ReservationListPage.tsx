import { useEffect, useState, type FormEvent } from 'react'
import { Link, useLocation, useSearchParams } from 'react-router-dom'
import { ArrowRight, CalendarCheck2, CalendarDays, CheckCircle2, CircleHelp, Clock3, CreditCard, Info, MapPin, Plus, RefreshCw, RotateCcw, Search, SlidersHorizontal, XCircle } from 'lucide-react'
import type { ApiCollectionResponse } from '../api/api.types'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { reservationApi } from '../api/reservationApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { CustomerTabs } from '../components/CustomerTabs'
import { EmptyState, LoadingState, PaginationControls } from '../components/PageStates'
import { CustomerStatusBadge as StatusBadge } from '../components/CustomerStatusBadge'
import { getReservationStatusLabel, RESERVATION_FILTER_LABELS } from '../features/reservations/reservationPresentation'
import { useReservationFacilities, useReservationUnitTypes } from '../features/reservations/useReservationUnitTypes'
import { isReservationListResponse, reservationDuration, reservationNotice, reservationPeriod, RESERVATION_LIST_STATUSES, RESERVATION_LIST_TABS } from '../features/reservations/reservationListPresentation'
import type { ReservationDetail, ReservationStatus } from '../features/reservations/reservation.types'
import { initialPublicFilters, publicSearchQuery } from '../features/catalog/publicSearch'
import { overviewDate } from '../features/customer-overview/overviewValidation'
import { formatMoney, formatUtcDateTime } from '../utils/formatters'
import '../styles/customer-reservations.css'

interface ReservationFilters { query: string; status: ReservationStatus | ''; unitType: string }
function ReservationFilterForm({ initial, types, onApply }: { initial: ReservationFilters; types: { id: string; name: string }[]; onApply(value: ReservationFilters): void }) {
  const [draft, setDraft] = useState(initial)
  function submit(event: FormEvent<HTMLFormElement>) { event.preventDefault(); onApply({ ...draft, query: draft.query.trim() }) }
  return <form className="panel reservations-filter-form" aria-label="Filter reservations on this page" onSubmit={submit}>
    <div className="form-field"><label htmlFor="reservation-search">Search this page</label><div className="reservations-search"><Search size={18} aria-hidden="true" /><input id="reservation-search" type="search" value={draft.query} placeholder="Search by facility, unit type or period" onChange={event => setDraft(current => ({ ...current, query: event.target.value }))} /></div></div>
    <div className="form-field"><label htmlFor="reservation-unit-filter">Unit Type</label><select id="reservation-unit-filter" value={draft.unitType} onChange={event => setDraft(current => ({ ...current, unitType: event.target.value }))}><option value="">All Unit Types</option>{types.map(type => <option key={type.id} value={type.id}>{type.name}</option>)}</select></div>
    <div className="form-field"><label htmlFor="reservation-status-filter">Status</label><select id="reservation-status-filter" value={draft.status} onChange={event => setDraft(current => ({ ...current, status: event.target.value as ReservationFilters['status'] }))}><option value="">All Statuses</option>{RESERVATION_LIST_STATUSES.filter(Boolean).map(status => <option value={status} key={status}>{RESERVATION_FILTER_LABELS[status as ReservationStatus]}</option>)}</select></div>
    <button className="button button-secondary" type="submit"><SlidersHorizontal size={16} aria-hidden="true" />Apply Filters</button>
  </form>
}

export function ReservationListPage() {
  const location = useLocation()
  const [params, setParams] = useSearchParams()
  const rawPage = Number(params.get('page') ?? '1')
  const page = Number.isSafeInteger(rawPage) && rawPage > 0 ? rawPage : 1
  const status = (RESERVATION_LIST_STATUSES.find(item => item === params.get('status')) ?? '')
  const filters: ReservationFilters = { query: params.get('q') ?? '', status, unitType: params.get('unitType') ?? '' }
  const [revision, setRevision] = useState(0)
  const key = `${page}:${revision}`
  const [snapshot, setSnapshot] = useState<{ key: string; response: ApiCollectionResponse<ReservationDetail> | null; error: ApiErrorPresentation | null } | null>(null)
  const loading = snapshot?.key !== key
  const response = !loading ? snapshot?.response : null
  const error = !loading ? snapshot?.error ?? null : null
  const reservations = response?.data ?? []
  const unitTypes = useReservationUnitTypes(reservations.map(item => item.unitTypeId))
  const facilities = useReservationFacilities(reservations.map(item => item.facilityId))
  useEffect(() => {
    let active = true
    reservationApi.list(page).then(result => {
      if (active) setSnapshot(isReservationListResponse(result, page)
        ? { key, response: result, error: null }
        : { key, response: null, error: presentValidationError('We could not load your reservations. Refresh the page to check your latest bookings.') })
    }).catch((caught: unknown) => { if (active) setSnapshot({ key, response: null, error: presentApiError(caught) }) })
    return () => { active = false }
  }, [key, page])

  function applyFilters(next: ReservationFilters) {
    const query = new URLSearchParams()
    if (page > 1) query.set('page', String(page))
    if (next.query) query.set('q', next.query)
    if (next.status) query.set('status', next.status)
    if (next.unitType) query.set('unitType', next.unitType)
    setParams(query)
  }
  const matched = reservations.filter(item => {
    const unitType = unitTypes.reads[item.unitTypeId]?.data
    const facility = facilities.reads[item.facilityId]?.data
    const text = `${unitType?.name ?? ''} ${facility?.name ?? ''} ${facility?.address ?? ''} ${item.startMonth} ${item.endMonth} ${reservationPeriod(item.startMonth, item.endMonth)}`.toLowerCase()
    return (!filters.unitType || filters.unitType === item.unitTypeId) && text.includes(filters.query.trim().toLowerCase())
  })
  const counts = RESERVATION_LIST_STATUSES.map(value => matched.filter(item => !value || item.status === value).length)
  const visible = matched.filter(item => !status || item.status === status)
  const selectedTab = RESERVATION_LIST_STATUSES.indexOf(status)
  const catalogPending = Boolean(filters.query.trim()) && (unitTypes.loading || facilities.loading)
  const types = [...new Set(reservations.map(item => item.unitTypeId))].map(id => ({ id, name: unitTypes.reads[id]?.data?.name ?? 'Unit type unavailable' }))
  const refresh = () => { setRevision(value => value + 1); unitTypes.retry(); facilities.retry() }

  function cards() {
    if (loading) return <LoadingState label="Loading your reservations…" />
    if (error) return <button className="button button-secondary" type="button" onClick={refresh}>Retry Reservations</button>
    if (catalogPending) return <LoadingState label="Loading storage details for your search…" />
    if (!reservations.length) return <EmptyState message="You do not have any reservations on this page. Find storage to start a new reservation." />
    if (!visible.length) return <div className="panel stack"><EmptyState message="No reservations on this page match your filters." /><button className="button button-secondary" type="button" onClick={() => applyFilters({ query: '', status: '', unitType: '' })}>Clear Filters</button></div>
    return <div className="reservations-card-list">{visible.map(item => {
      const type = unitTypes.reads[item.unitTypeId]?.data
      const facility = facilities.reads[item.facilityId]?.data
      const detailPath = `/customer/reservations/${encodeURIComponent(item.reservationId)}`
      const completed = item.status === 'COMPLETED', cancelled = item.status === 'CANCELLED'
      const invoice = item.depositInvoice, visit = item.reservationVisit
      const NoticeIcon = completed ? CheckCircle2 : cancelled ? XCircle : item.status === 'CONFIRMED' ? CalendarCheck2 : Info
      return <article className={`panel reservations-card reservations-card-${item.status.toLowerCase().replaceAll('_', '-')}`} key={item.reservationId} aria-label={`${type?.name ?? 'Storage Reservation'} · ${getReservationStatusLabel(item)}`}>
        <header className="reservations-card-heading"><div className="reservations-card-title"><h2>{type?.name ?? (unitTypes.reads[item.unitTypeId]?.error ? 'Storage type unavailable' : 'Loading storage type…')}</h2><span className="reservations-facility"><MapPin size={15} aria-hidden="true" />{facility?.name ?? (facilities.reads[item.facilityId]?.error ? 'Facility unavailable' : 'Loading facility…')}</span><span className="reservations-created">Created {formatUtcDateTime(item.createdAt)}</span></div><StatusBadge status={item.status} label={getReservationStatusLabel(item)} /></header>
        <dl className="reservations-facts"><div><dt>Unit Type</dt><dd>{type?.name ?? 'Not available'}</dd><small>{[type?.mode === 'PRIVATE' ? 'Private Storage' : type?.mode === 'PUBLIC' ? 'Public Storage' : '', type?.size].filter(Boolean).join(' · ') || 'Storage details unavailable'}</small></div><div><dt>{cancelled ? 'Intended Period' : 'Rental Period'}</dt><dd>{reservationPeriod(item.startMonth, item.endMonth)}</dd><small>{reservationDuration(item.startMonth, item.endMonth)}</small></div>
          {item.status === 'CONFIRMED' ? <><div><dt>Deposit Invoice</dt><dd>{formatMoney(invoice.amountDue)}</dd><StatusBadge status={invoice.status} /></div><div><dt>Handover Visit</dt><dd>{visit ? overviewDate(visit.visitDate) : 'Not available'}</dd>{visit ? <StatusBadge status={visit.status} /> : <small>Review reservation details</small>}</div></> : <><div><dt>Locked Monthly Rent</dt><dd>{formatMoney(item.lockedRentalPrice)}</dd><small>Per month · reservation snapshot</small></div><div><dt>Deposit Invoice</dt><dd>{formatMoney(invoice.amountDue)}</dd><StatusBadge status={invoice.status} /></div></>}
        </dl>
        <div className="reservations-callout"><NoticeIcon size={21} aria-hidden="true" /><div>{completed && <strong>Handover Complete</strong>}<p>{reservationNotice(item)}</p></div></div>
        <footer className="reservations-card-actions"><div className="reservations-secondary-actions"><Link className="reservations-text-link" to={detailPath}>View Details <ArrowRight size={14} aria-hidden="true" /></Link>{(item.status === 'PENDING_DEPOSIT' || item.status === 'CONFIRMED') && <Link className="reservations-text-link reservations-cancel-link" to={detailPath} title="Open reservation details to review and confirm cancellation"><XCircle size={15} aria-hidden="true" />Review / Cancel</Link>}{item.status === 'PENDING_DEPOSIT' && invoice.status === 'UNPAID' && <span className="reservations-deadline"><Clock3 size={14} aria-hidden="true" />Due {formatUtcDateTime(invoice.dueDate)}</span>}</div>
          <div className="reservations-primary-actions">
            {item.status === 'PENDING_DEPOSIT' && invoice.status === 'UNPAID' && <Link className="button" to={detailPath} title="Review this reservation before starting payment"><CreditCard size={17} aria-hidden="true" />Pay Deposit · {formatMoney(invoice.amountDue)}</Link>}
            {item.status === 'PENDING_DEPOSIT' && invoice.status === 'PAID' && <Link className="button" to={`${detailPath}/confirm`} state={{ backgroundLocation: location }}><CalendarDays size={17} aria-hidden="true" />Schedule Handover</Link>}
            {item.status === 'PENDING_DEPOSIT' && (invoice.status === 'OVERDUE' || invoice.status === 'CANCELLED') && <Link className="button button-secondary" to={`/customer/invoices/${encodeURIComponent(invoice.invoiceId)}`}>View Deposit Invoice</Link>}
            {item.status === 'CONFIRMED' && visit && <Link className="button" to={`/customer/visits/${encodeURIComponent(visit.visitId)}`}><CalendarDays size={17} aria-hidden="true" />{visit.status === 'SCHEDULED' ? 'Manage Handover Date' : 'View Handover Visit'}</Link>}
            {completed && <Link className="button" to="/customer/contracts">View My Contracts <ArrowRight size={16} aria-hidden="true" /></Link>}
            {cancelled && <Link className="button button-secondary" to={`/facilities?${publicSearchQuery({ ...initialPublicFilters(), facilityId: item.facilityId, unitTypeId: item.unitTypeId })}`}><RotateCcw size={16} aria-hidden="true" />Reserve Again</Link>}
          </div>
        </footer>
      </article>
    })}</div>
  }

  return <main className="page-container flow-page customer-reservations">
    <section className="page-heading reservations-heading"><div><p className="eyebrow reservations-eyebrow"><CalendarDays size={13} aria-hidden="true" />Reservation Management</p><h1>My Reservations</h1><p className="muted">Track your reservations, settle deposit invoices and manage your handover visits.</p></div><div className="reservations-heading-actions"><button className="button button-secondary" type="button" disabled={loading} onClick={refresh} aria-label="Refresh Reservations" title="Refresh Reservations"><RefreshCw size={16} aria-hidden="true" /></button><Link className="button reservations-new-button" to="/customer/storage-search"><Plus size={16} aria-hidden="true" />New Storage Reservation</Link></div></section>
    <ApiErrorAlert error={error} />
    {(unitTypes.error || facilities.error) && <div className="panel stack"><p className="muted">Some storage names could not be loaded. Your reservation amounts and statuses still come from the reservation service.</p><ApiErrorAlert error={unitTypes.error} /><ApiErrorAlert error={facilities.error} /><button className="button button-secondary" type="button" onClick={() => { unitTypes.retry(); facilities.retry() }} disabled={unitTypes.loading || facilities.loading}>Retry Storage Details</button></div>}
    <div className="reservations-tabs"><CustomerTabs labels={RESERVATION_LIST_TABS} counts={loading || error || catalogPending ? undefined : counts} ariaLabel="Reservation status" selected={selectedTab} onChange={index => applyFilters({ ...filters, status: RESERVATION_LIST_STATUSES[index] })}>{({ tabId, panelId }) => <>
      <ReservationFilterForm key={JSON.stringify(filters)} initial={filters} types={types} onApply={applyFilters} />
      <div className="reservations-filter-summary"><p className="muted" role="status">{loading ? 'Loading this page…' : error ? 'Reservation list unavailable.' : catalogPending ? 'Loading storage details for your search…' : `${visible.length} matching reservations on this page. Counts and filters cover this page only.`}</p>{(filters.query || filters.status || filters.unitType) && <button className="reservations-text-button" type="button" onClick={() => applyFilters({ query: '', status: '', unitType: '' })}>Clear Filters</button>}</div>
      {RESERVATION_LIST_TABS.map((label, index) => <div key={label} role="tabpanel" id={panelId(index)} aria-labelledby={tabId(index)} hidden={selectedTab !== index} tabIndex={0}>{selectedTab === index && cards()}</div>)}
    </>}</CustomerTabs></div>
    {response && <PaginationControls pagination={response.pagination} disabled={loading} onPageChange={next => setParams(current => { const query = new URLSearchParams(current); if (next === 1) query.delete('page'); else query.set('page', String(next)); return query })} />}
    <section className="panel reservations-help"><span className="reservations-help-icon"><CircleHelp size={26} aria-hidden="true" /></span><div><h2>Need Assistance with Your Reservation?</h2><p className="muted">Questions about storage sizes, deposit invoices or handover visits?</p></div><div className="reservations-help-actions"><Link className="button button-secondary" to="/help">Read FAQs</Link><Link className="button button-secondary" to="/customer/support-tickets">Open Support Tickets</Link></div></section>
  </main>
}
