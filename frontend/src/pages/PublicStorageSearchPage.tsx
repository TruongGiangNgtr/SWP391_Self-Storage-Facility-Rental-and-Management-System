import { useId, useMemo, useState } from 'react'
import { ArrowUpRight, Search } from 'lucide-react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { PublicStorageSearchForm } from '../components/PublicStorageSearchForm'
import { BookingJourney } from '../components/BookingJourney'
import { FacilityImage } from '../components/FacilityImage'
import { PublicUnitTypeList } from '../components/PublicUnitTypeList'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState, PaginationControls } from '../components/PageStates'
import { initialPublicFilters, publicSearchQuery, readPublicSearch } from '../features/catalog/publicSearch'
import { usePublicFacilities } from '../features/catalog/usePublicCatalog'
import { presentValidationError } from '../api/apiErrorPresentation'
import type { FacilitySummary, PublicCatalogFilters } from '../features/catalog/catalog.types'

function FacilitySearchGroup({ facility, filters, search, dirty, initiallyOpen }: { facility: FacilitySummary; filters: PublicCatalogFilters; search: string; dirty: boolean; initiallyOpen: boolean }) {
  const [open, setOpen] = useState(initiallyOpen)
  const id = useId()
  return <section className="flow-facility public-result-facility">
    <div className="flow-facility-heading"><FacilityImage facility={facility} className="facility-thumbnail" /><div className="public-facility-heading-text"><h2>{facility.name}</h2><p className="flow-facility-address"><img src="/figma/gwp-01/imgIcon12.svg" alt="" aria-hidden="true" />{facility.address}</p>{facility.description && <p className="muted">{facility.description}</p>}<div className="action-row"><Link className="public-text-link" to={`/facilities/${encodeURIComponent(facility.facilityId)}?${publicSearchQuery(filters)}`}>View Facility Details <ArrowUpRight size={14} aria-hidden="true" /></Link><button className="button button-secondary" type="button" aria-expanded={open} aria-controls={id} onClick={() => setOpen(value => !value)}>{open ? 'Hide Storage Options' : 'View Storage Options'}</button></div></div></div>
    <div id={id}>{open && <PublicUnitTypeList key={`${facility.facilityId}:${search}`} facilityId={facility.facilityId} facilityName={facility.name} filters={filters} disabled={dirty} />}</div>
  </section>
}

function PublicSearchResults() {
  const { search } = useLocation()
  const navigate = useNavigate()
  const query = useMemo(() => readPublicSearch(search), [search])
  const [dirty, setDirty] = useState(false)
  const { filters } = query
  const hasPeriod = Boolean(filters.startMonth && filters.endMonth)
  // A valid submitted period (also carried by direct/back URLs) starts a search.
  // General catalogue reads for the location/size controls remain independent.
  const results = usePublicFacilities({ ...filters, page: query.page, pageSize: query.pageSize, sortBy: 'name', sortDirection: query.sortDirection }, hasPeriod && !query.error)
  const inputValues = filters.startMonth || filters.endMonth ? filters : { ...filters, startMonth: initialPublicFilters().startMonth, endMonth: initialPublicFilters().endMonth }
  const url = (extra: { page?: number; sortDirection?: 'asc' | 'desc' } = {}) => `/facilities?${publicSearchQuery(filters, { pageSize: query.pageSize, page: query.page, sortDirection: query.sortDirection, ...extra })}`
  return <main className="page-container flow-page public-search-page booking-search">
    <header className="booking-search-heading">
      <div><nav className="flow-breadcrumb" aria-label="Breadcrumb"><Link to="/">Home</Link><span aria-hidden="true">/</span><span aria-current="page">Find Storage</span></nav><h1>Find Storage</h1><p>A little more room for what comes next. Choose your rental months, compare available spaces and review your selection.</p></div>
      <Link className="booking-guide-link" to="/size-guide">Not sure which size? <ArrowUpRight size={16} aria-hidden="true" /></Link>
    </header>
    <BookingJourney step={0} />
    <div className="booking-search-layout">
      <aside className="booking-filter-rail" aria-label="Search preferences">
        <PublicStorageSearchForm initialValues={inputValues} embedded onDirtyChange={setDirty} onSearch={next => navigate(`/facilities?${publicSearchQuery(next)}`)} />
        <div className="booking-filter-help"><p>A space that fits starts with the right size.</p><Link className="booking-guide-link" to="/size-guide">Explore the size guide <ArrowUpRight size={14} aria-hidden="true" /></Link></div>
      </aside>
      <section className="booking-search-results" aria-label="Storage search results">
    {dirty && hasPeriod && <p className="notice">Filters changed. Select Find Storage to update these results before reserving.</p>}
    {!hasPeriod && !query.error && <section className="panel public-search-start booking-search-start" aria-labelledby="search-start-heading"><Search className="booking-start-icon" size={30} strokeWidth={1.5} aria-hidden="true" /><div><h2 id="search-start-heading">Your next space starts here.</h2><p>Choose your rental months and select Find Storage to see matching facilities. Your results will reflect availability for the full period.</p><ol><li>Choose a location and storage preferences.</li><li>Compare available spaces and monthly rent.</li><li>Review your selection before reserving.</li></ol></div></section>}
    {hasPeriod && !query.error && <section className="public-results-toolbar" aria-label="Search results controls"><div><h2>Storage Search Results</h2>{results.data && <span className="result-count">{results.data.pagination.totalItems} matching {results.data.pagination.totalItems === 1 ? 'facility' : 'facilities'}</span>}</div><div className="form-field"><label htmlFor="facility-sort">Sort Facilities</label><select id="facility-sort" value={query.sortDirection} onChange={event => navigate(url({ page: 1, sortDirection: event.target.value as 'asc' | 'desc' }))}><option value="asc">Name A–Z</option><option value="desc">Name Z–A</option></select></div></section>}
    <ApiErrorAlert error={query.error ? presentValidationError(query.error) : results.error} />
    {!query.error && results.error && <button className="button button-secondary" type="button" onClick={results.refresh}>Retry Search</button>}
    {results.loading ? <LoadingState label="Finding matching facilities…" /> : results.data && <>
      {results.data.data.length === 0 ? <EmptyState message="No facilities match this search. Try a different location, storage size or rental period." /> : results.data.data.map((facility, index) => <FacilitySearchGroup key={facility.facilityId} facility={facility} filters={filters} search={search} dirty={dirty} initiallyOpen={index === 0} />)}
      <PaginationControls pagination={results.data.pagination} disabled={results.loading} onPageChange={page => navigate(url({ page }))} />
    </>}
    <section className="notice public-booking-note"><div><strong>How reservations work</strong><p>Choose a storage type and period, then sign in to create a reservation. Pay the deposit and select a handover date; the specific unit is chosen at handover.</p></div><Link to="/how-it-works">Learn How It Works →</Link></section>
      </section>
    </div>
  </main>
}
export function PublicStorageSearchPage() {
  const { search } = useLocation()
  return <PublicSearchResults key={search} />
}
