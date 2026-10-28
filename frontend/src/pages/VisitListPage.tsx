import { useEffect, useState, type FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { ArrowRight, BadgeCheck, CalendarCheck2, CalendarClock, CalendarDays, CalendarX2, CirclePlus, DoorOpen, FileText, Info, MapPin, RefreshCw, Search, ShieldCheck, XCircle } from 'lucide-react'
import type { ApiCollectionResponse } from '../api/api.types'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { visitApi } from '../api/visitApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { CustomerTabs } from '../components/CustomerTabs'
import { CustomerFieldError } from '../components/CustomerFieldError'
import { EmptyState, LoadingState, PaginationControls } from '../components/PageStates'
import { CustomerStatusBadge as StatusBadge } from '../components/CustomerStatusBadge'
import { overviewDate } from '../features/customer-overview/overviewValidation'
import { useReservationFacilities, useReservationUnitTypes } from '../features/reservations/useReservationUnitTypes'
import { useVisitContexts, visitContextKey } from '../features/visits/useVisitContexts'
import { isVisitListResponse, matchesVisitDate, visitFilterError, visitNotice, VISIT_DATE_RANGES, VISIT_NAMES, VISIT_STATUSES, VISIT_TABS, type VisitFilters } from '../features/visits/visitListPresentation'
import type { VisitDetail, VisitType } from '../features/visits/visit.types'
import { getCurrentBusinessDate } from '../utils/formatters'
import { RentalListFilters } from '../components/RentalListFilters'
import '../styles/customer-visits.css'

function VisitFilterForm({ initial, facilities, onApply }: { initial: VisitFilters; facilities: { id: string; name: string }[]; onApply(value: VisitFilters): void }) {
  const [draft, setDraft] = useState(initial)
  const [error, setError] = useState<string | null>(null)
  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const message = visitFilterError(draft)
    setError(message)
    if (!message) onApply({ ...draft, query: draft.query.trim() })
  }
  return <form className="panel visits-filter-form" aria-label="Filter visits on this page" onSubmit={submit}>
    <div className="form-field"><label htmlFor="visit-search">Search this page</label><div className="visits-search"><Search size={18} aria-hidden="true" /><input id="visit-search" type="search" placeholder="Facility, storage type or date" value={draft.query} onChange={event => setDraft(current => ({ ...current, query: event.target.value }))} /></div></div>
    <div className="form-field"><label htmlFor="visit-type">Visit Type</label><select id="visit-type" value={draft.type} onChange={event => setDraft(current => ({ ...current, type: event.target.value as VisitFilters['type'] }))}><option value="">All Visit Types</option>{Object.entries(VISIT_NAMES).map(([type, label]) => <option key={type} value={type}>{label}</option>)}</select></div>
    <div className="form-field"><label htmlFor="visit-facility">Facility</label><select id="visit-facility" value={draft.facility} onChange={event => setDraft(current => ({ ...current, facility: event.target.value }))}><option value="">All Facilities</option>{facilities.map(facility => <option key={facility.id} value={facility.id}>{facility.name}</option>)}</select></div>
    <div className="form-field"><label htmlFor="visit-date-range">Date Range</label><select id="visit-date-range" value={draft.range} onChange={event => { setDraft(current => ({ ...current, range: event.target.value as VisitFilters['range'] })); setError(null) }}><option value="">All Dates</option><option value="week">Upcoming 7 Days</option><option value="month">This Month</option><option value="past">Past 30 Days</option><option value="custom">Custom Range</option></select></div>
    <button className="button" type="submit">Apply Filters</button>
    {draft.range === 'custom' && <div className="visits-custom-dates"><div className="form-field"><label htmlFor="visit-from">From Date</label><input id="visit-from" type="date" value={draft.from} aria-invalid={Boolean(error)} aria-describedby={error ? 'visit-range-error' : undefined} onChange={event => { setDraft(current => ({ ...current, from: event.target.value })); setError(null) }} /></div><div className="form-field"><label htmlFor="visit-to">To Date</label><input id="visit-to" type="date" value={draft.to} aria-invalid={Boolean(error)} aria-describedby={error ? 'visit-range-error' : undefined} onChange={event => { setDraft(current => ({ ...current, to: event.target.value })); setError(null) }} /></div><CustomerFieldError id="visit-range-error" message={error} /></div>}
  </form>
}

export function VisitListPage() {
  const [params, setParams] = useSearchParams()
  const rawPage = Number(params.get('page') ?? '1')
  const page = Number.isSafeInteger(rawPage) && rawPage > 0 ? rawPage : 1
  const status = VISIT_STATUSES.find(value => value === params.get('status')) ?? ''
  const filters: VisitFilters = { query: params.get('q') ?? '', type: (Object.keys(VISIT_NAMES) as VisitType[]).find(value => value === params.get('type')) ?? '', facility: params.get('facility') ?? '', range: VISIT_DATE_RANGES.find(value => value === params.get('range')) ?? '', from: params.get('from') ?? '', to: params.get('to') ?? '' }
  const [revision, setRevision] = useState(0)
  const key = `${page}:${revision}`
  const [snapshot, setSnapshot] = useState<{ key: string; response: ApiCollectionResponse<VisitDetail> | null; error: ApiErrorPresentation | null } | null>(null)
  const loading = snapshot?.key !== key
  const response = !loading ? snapshot?.response : null
  const error = !loading ? snapshot?.error ?? null : null
  const visits = response?.data ?? []
  const contexts = useVisitContexts(visits)
  const contextData = Object.values(contexts.reads).flatMap(read => read.data ? [read.data] : [])
  const facilities = useReservationFacilities(contextData.map(data => data.facilityId))
  const unitTypes = useReservationUnitTypes(contextData.flatMap(data => data.unitTypeId ? [data.unitTypeId] : []))
  useEffect(() => {
    let active = true
    visitApi.list(page).then(result => {
      if (active) setSnapshot(isVisitListResponse(result, page) ? { key, response: result, error: null }
        : { key, response: null, error: presentValidationError('We could not load your visits. Refresh the page to check your latest appointments.') })
    }).catch((caught: unknown) => { if (active) setSnapshot({ key, response: null, error: presentApiError(caught) }) })
    return () => { active = false }
  }, [key, page])
  function applyFilters(next: VisitFilters, nextStatus = status) {
    const query = new URLSearchParams()
    if (page > 1) query.set('page', String(page))
    if (nextStatus) query.set('status', nextStatus)
    if (next.query) query.set('q', next.query)
    if (next.type) query.set('type', next.type)
    if (next.facility) query.set('facility', next.facility)
    if (next.range) query.set('range', next.range)
    if (next.range === 'custom') { query.set('from', next.from); query.set('to', next.to) }
    setParams(query)
  }
  const clearFilters = () => applyFilters({ query: '', type: '', facility: '', range: '', from: '', to: '' }, '')
  const retryContext = () => { contexts.retry(); facilities.retry(); unitTypes.retry() }
  const refresh = () => { setRevision(value => value + 1); retryContext() }
  const relatedLoading = contexts.loading || facilities.loading || unitTypes.loading
  const relatedError = contexts.error ?? facilities.error ?? unitTypes.error
  const filterPending = Boolean(filters.query.trim() || filters.facility) && relatedLoading
  const dateError = visitFilterError(filters)
  const today = getCurrentBusinessDate()
  const matched = visits.filter(visit => {
    const context = contexts.reads[visitContextKey(visit)]?.data
    const facility = context && facilities.reads[context.facilityId]?.data
    const typeName = context?.unitTypeId ? unitTypes.reads[context.unitTypeId]?.data?.name : context?.unitTypeName
    const text = `${VISIT_NAMES[visit.visitType]} ${visit.visitDate} ${overviewDate(visit.visitDate)} ${facility?.name ?? ''} ${facility?.address ?? ''} ${typeName ?? ''} ${context?.unitCode ?? ''}`.toLowerCase()
    return (!filters.type || visit.visitType === filters.type) && (!filters.facility || context?.facilityId === filters.facility) && text.includes(filters.query.trim().toLowerCase()) && matchesVisitDate(visit.visitDate, filters, today)
  })
  const counts = VISIT_STATUSES.map(value => matched.filter(visit => !value || visit.status === value).length)
  const visible = matched.filter(visit => !status || visit.status === status)
  const selected = VISIT_STATUSES.indexOf(status)
  const facilityOptions = [...new Set(contextData.map(data => data.facilityId))].map(id => ({ id, name: facilities.reads[id]?.data?.name ?? 'Facility name unavailable' }))
  function renderCards() {
    if (loading || filterPending) return <LoadingState label={loading ? 'Loading your visits…' : 'Loading storage details for your filters…'} />
    if (error) return <button className="button button-secondary" type="button" onClick={refresh}>Retry Visits</button>
    if (dateError) return <div className="panel stack"><p role="alert">{dateError}</p><button className="button button-secondary" type="button" onClick={clearFilters}>Clear Filters</button></div>
    if (!visits.length) return <div className="panel stack"><EmptyState message="You do not have any visits on this page." /><Link className="button button-secondary" to="/customer/contracts">View My Contracts</Link></div>
    if (!visible.length) return <div className="panel stack"><EmptyState message="No visits on this page match your filters." /><button className="button button-secondary" type="button" onClick={clearFilters}>Clear Filters</button></div>
    return <div className="visits-card-list">{visible.map(visit => {
      const contextRead = contexts.reads[visitContextKey(visit)], context = contextRead?.data
      const facility = context && facilities.reads[context.facilityId]?.data
      const typeName = context?.unitTypeId ? unitTypes.reads[context.unitTypeId]?.data?.name : context?.unitTypeName
      const detailPath = `/customer/visits/${encodeURIComponent(visit.visitId)}`
      const entityPath = `/customer/${visit.visitType === 'RESERVATION' ? 'reservations' : 'contracts'}/${encodeURIComponent(visit.entityId)}`
      const TypeIcon = visit.visitType === 'RESERVATION' ? BadgeCheck : visit.visitType === 'ACCESS' ? DoorOpen : CalendarCheck2
      const DateIcon = visit.status === 'CANCELLED' ? CalendarX2 : visit.status === 'CHECKED_OUT' ? CalendarCheck2 : CalendarClock
      const canBookAgain = visit.visitType === 'ACCESS' && (visit.status === 'CHECKED_OUT' || visit.status === 'CANCELLED') && context?.status === 'ACTIVE'
      return <article className={`panel visits-card visits-card-${visit.status.toLowerCase().replaceAll('_', '-')}`} key={visit.visitId} aria-label={`${VISIT_NAMES[visit.visitType]} · ${overviewDate(visit.visitDate)}`}>
        <header className="visits-card-heading"><span className="visits-icon visits-type-icon"><TypeIcon size={22} aria-hidden="true" /></span><div className="visits-card-title"><div className="visits-title-row"><h2>{VISIT_NAMES[visit.visitType]}</h2><span className={`visits-type-label visits-type-${visit.visitType.toLowerCase()}`}>{visit.visitType === 'RESERVATION' ? 'Reservation Visit' : VISIT_NAMES[visit.visitType]}</span><span className="visits-facility"><MapPin size={14} aria-hidden="true" />{facility?.name ?? (contextRead?.error || (context && facilities.reads[context.facilityId]?.error) ? 'Facility unavailable' : 'Loading facility…')}</span></div><p>{typeName ?? (contextRead?.error || (context?.unitTypeId && unitTypes.reads[context.unitTypeId]?.error) ? 'Storage type unavailable' : 'Loading storage type…')}{visit.visitType !== 'RESERVATION' && context?.unitCode && context.unitCode !== 'Not available' ? ` · Unit ${context.unitCode}` : ''}{visit.visitType === 'RESERVATION' ? ' · Unit selected at handover' : ''}</p></div><StatusBadge status={visit.status} /></header>
        <div className="visits-card-facts"><div className="visits-fact visits-related"><span className="visits-icon"><FileText size={21} aria-hidden="true" /></span><div><h3>{visit.visitType === 'RESERVATION' ? 'Related Reservation' : 'Related Contract'}</h3><Link to={entityPath}>{visit.visitType === 'RESERVATION' ? 'View Reservation' : 'View Contract'} <ArrowRight size={14} aria-hidden="true" /></Link>{context ? <StatusBadge status={context.status} /> : <small>{contextRead?.error ? 'Details unavailable · retry below' : 'Loading details…'}</small>}</div></div>
          <div className="visits-fact visits-date"><span className="visits-icon"><DateIcon size={21} aria-hidden="true" /></span><div><h3>{visit.status === 'CANCELLED' ? 'Scheduled Date · Cancelled' : visit.status === 'CHECKED_OUT' ? 'Visit Date · Checked Out' : visit.status === 'CHECKED_IN' ? 'Visit Date · Checked In' : `Scheduled ${visit.visitType === 'RESERVATION' ? 'Handover' : visit.visitType === 'RETURN' ? 'Return' : 'Access'} Date`}</h3><strong>{overviewDate(visit.visitDate)}</strong><small>{visit.visitType === 'RETURN' && visit.actualReturnDate ? `Actual return: ${overviewDate(visit.actualReturnDate)}` : 'Facility local date (GMT+7)'}</small></div></div></div>
        <footer className="visits-card-footer"><p><Info size={16} aria-hidden="true" />{visitNotice(visit)}</p><div className="visits-card-actions"><Link className="visits-text-link" to={detailPath}>View Details</Link>{visit.status === 'SCHEDULED' && <><Link className="button button-secondary" to={`${detailPath}?action=reschedule`}><CalendarDays size={16} aria-hidden="true" />Reschedule Date</Link><Link className="button button-danger" to={`${detailPath}?action=cancel`}><XCircle size={16} aria-hidden="true" />Cancel Visit</Link></>}{canBookAgain && <Link className={`button${visit.status === 'CHECKED_OUT' ? ' button-secondary' : ''}`} to={`/customer/contracts/${encodeURIComponent(visit.entityId)}/access-visits/new`}><CirclePlus size={16} aria-hidden="true" />Schedule Another Access</Link>}{visit.visitType === 'RETURN' && visit.status === 'CHECKED_OUT' && <Link className="button button-secondary" to={`/customer/contracts/${encodeURIComponent(visit.entityId)}/fees-return`}>View Return Tracking</Link>}</div></footer>
      </article>
    })}</div>
  }
  return <main className="page-container flow-page customer-visits">
    <section className="page-heading visits-heading"><div><h1>My Visits</h1><p className="muted">Schedule, manage and track your facility visits for handover, storage access and return.</p></div><div className="visits-heading-actions"><Link className="button" to="/customer/contracts"><CirclePlus size={18} aria-hidden="true" />Schedule Access Visit</Link></div></section>
    <ApiErrorAlert error={error} />
    {relatedError && <div className="panel stack"><p className="muted">Some related storage details could not be loaded. Visit dates and statuses still come from the visit service. Search and facility filters may be incomplete until you retry.</p><ApiErrorAlert error={relatedError} /><button className="button button-secondary" type="button" disabled={relatedLoading} onClick={retryContext}>Retry Storage Details</button></div>}
    <RentalListFilters><VisitFilterForm key={JSON.stringify(filters)} initial={filters} facilities={facilityOptions} onApply={applyFilters} /></RentalListFilters>
    <div className="visits-list-summary"><p className="muted" role="status">{loading ? 'Loading this page…' : error ? 'Visit list unavailable.' : filterPending ? 'Loading storage details for your filters…' : `${visible.length} matching visits on this page. Counts and filters cover this page only.`}</p><div>{(filters.query || filters.type || filters.facility || filters.range || status) && <button className="visits-text-button" type="button" onClick={clearFilters}>Clear Filters</button>}<button className="visits-text-button" type="button" disabled={loading} onClick={refresh}><RefreshCw size={14} aria-hidden="true" />Refresh Visits</button></div></div>
    <div className="visits-tabs"><CustomerTabs labels={VISIT_TABS} counts={loading || error || filterPending || dateError ? undefined : counts} ariaLabel="Visit status" selected={selected} onChange={index => applyFilters(filters, VISIT_STATUSES[index])}>{({ tabId, panelId }) => VISIT_TABS.map((label, index) => <div key={label} role="tabpanel" id={panelId(index)} aria-labelledby={tabId(index)} hidden={selected !== index} tabIndex={0}>{selected === index && renderCards()}</div>)}</CustomerTabs></div>
    {response && <PaginationControls pagination={response.pagination} disabled={loading} onPageChange={next => setParams(current => { const query = new URLSearchParams(current); if (next === 1) query.delete('page'); else query.set('page', String(next)); return query })} />}
    <section className="panel visits-guidelines" id="visit-guidelines" tabIndex={-1}><span className="visits-icon"><ShieldCheck size={24} aria-hidden="true" /></span><div><h2>Facility Visit Guidelines</h2><p className="muted">Only scheduled visits can be rescheduled or cancelled, subject to the visit policy. Access visits require an active contract and cannot be created while a return visit is pending.</p></div></section>
  </main>
}
