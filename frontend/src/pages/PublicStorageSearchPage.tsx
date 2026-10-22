import { useId, useMemo, useState } from 'react'
import { Search } from 'lucide-react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { PublicStorageSearchForm } from '../components/PublicStorageSearchForm'
import { PublicUnitTypeList } from '../components/PublicUnitTypeList'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState, PaginationControls } from '../components/PageStates'
import { initialPublicFilters, publicSearchQuery, readPublicSearch } from '../features/catalog/publicSearch'
import { usePublicFacilities } from '../features/catalog/usePublicCatalog'
import { presentValidationError } from '../api/apiErrorPresentation'
import { formatMonthRange } from '../utils/formatters'
import type { FacilitySummary, PublicCatalogFilters } from '../features/catalog/catalog.types'

function FacilitySearchGroup({ facility, filters, search, dirty, initiallyOpen }: { facility: FacilitySummary; filters: PublicCatalogFilters; search: string; dirty: boolean; initiallyOpen: boolean }) {
  const [open, setOpen] = useState(initiallyOpen)
  const id = useId()
  return <section className="flow-facility public-result-facility">
    <div className="flow-facility-heading"><div className="facility-thumbnail" aria-hidden="true" /><div className="public-facility-heading-text"><p className="eyebrow">Storage Facility</p><h2>{facility.name}</h2><p className="flow-facility-address"><img src="/figma/gwp-01/imgIcon12.svg" alt="" aria-hidden="true" />{facility.address}</p>{facility.description && <p className="muted">{facility.description}</p>}<div className="action-row"><Link className="public-text-link" to={`/facilities/${encodeURIComponent(facility.facilityId)}?${publicSearchQuery(filters)}`}>View Facility Details →</Link><button className="button button-secondary" type="button" aria-expanded={open} aria-controls={id} onClick={() => setOpen(value => !value)}>{open ? 'Hide Storage Options' : 'View Storage Options'}</button></div></div></div>
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
  return <main className="page-container flow-page public-search-page">
    <section className="page-heading"><nav className="flow-breadcrumb" aria-label="Breadcrumb"><Link to="/">Home</Link><span aria-hidden="true">/</span><span aria-current="page">Find Storage</span></nav><p className="eyebrow">Find Your Space</p><h1>Find Storage</h1><p className="muted">Compare storage types and choose a space that fits your belongings.</p></section>
    <PublicStorageSearchForm initialValues={inputValues} onDirtyChange={setDirty} onSearch={next => navigate(`/facilities?${publicSearchQuery(next)}`)} />
    {dirty && hasPeriod && <p className="notice">Filters changed. Select Find Storage to update these results before reserving.</p>}
    {!hasPeriod && !query.error && <section className="panel public-search-start" aria-labelledby="search-start-heading"><span className="public-icon-tile"><Search size={24} aria-hidden="true" /></span><div><h2 id="search-start-heading">Find storage for your rental period</h2><p>Review your rental months, add any location, type or size preferences, then select Find Storage. Matching facilities will appear here after you search.</p></div></section>}
    {hasPeriod && !query.error && <section className="panel public-search-summary" aria-label="Applied search"><div><strong>{filters.location || 'All locations'}</strong><p className="muted">{formatMonthRange(filters.startMonth, filters.endMonth)} · {filters.mode === 'PRIVATE' ? 'Private Storage' : filters.mode === 'PUBLIC' ? 'Public Storage' : 'All storage types'} · {filters.size || 'All sizes'}</p></div><div className="form-field"><label htmlFor="facility-sort">Sort Facilities</label><select id="facility-sort" value={query.sortDirection} onChange={event => navigate(url({ page: 1, sortDirection: event.target.value as 'asc' | 'desc' }))}><option value="asc">Name A–Z</option><option value="desc">Name Z–A</option></select></div></section>}
    <ApiErrorAlert error={query.error ? presentValidationError(query.error) : results.error} />
    {!query.error && results.error && <button className="button button-secondary" type="button" onClick={results.refresh}>Retry Search</button>}
    {results.loading ? <LoadingState label="Finding matching facilities…" /> : results.data && <>
      <div className="section-heading"><h2>Storage Search Results</h2><span className="result-count">{results.data.pagination.totalItems} matching {results.data.pagination.totalItems === 1 ? 'facility' : 'facilities'}</span></div>
      {results.data.data.length === 0 ? <EmptyState message="No facilities match this search. Try a different location, storage size or rental period." /> : results.data.data.map((facility, index) => <FacilitySearchGroup key={facility.facilityId} facility={facility} filters={filters} search={search} dirty={dirty} initiallyOpen={index === 0} />)}
      <PaginationControls pagination={results.data.pagination} disabled={results.loading} onPageChange={page => navigate(url({ page }))} />
    </>}
    <section className="notice public-booking-note"><div><strong>How reservations work</strong><p>Choose a storage type and period, then sign in to create a reservation. Pay the deposit and select a handover date; the specific unit is chosen at handover.</p></div><Link to="/how-it-works">Learn How It Works →</Link></section>
  </main>
}
export function PublicStorageSearchPage() {
  const { search } = useLocation()
  return <PublicSearchResults key={search} />
}
