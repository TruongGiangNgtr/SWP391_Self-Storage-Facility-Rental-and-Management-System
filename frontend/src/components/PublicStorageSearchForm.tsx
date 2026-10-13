import { useId, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { ApiErrorAlert } from './ApiErrorAlert'
import { initialPublicFilters, publicSearchQuery, validatePublicFilters } from '../features/catalog/publicSearch'
import { usePublicSizes } from '../features/catalog/usePublicCatalog'
import type { FacilitySummary, PublicCatalogFilters } from '../features/catalog/catalog.types'
import { presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { getNextBusinessMonth } from '../features/reservations/reservationValidation'

export function PublicStorageSearchForm({ initialValues, suggestions = [], pinnedFacility, onSearch, onDirtyChange, embedded = false }: {
  initialValues?: PublicCatalogFilters; suggestions?: readonly FacilitySummary[]; pinnedFacility?: FacilitySummary
  onSearch?(filters: PublicCatalogFilters): void; onDirtyChange?(dirty: boolean): void; embedded?: boolean
}) {
  const id = useId()
  const navigate = useNavigate()
  const [initial] = useState(() => initialValues ?? initialPublicFilters())
  const [form, setForm] = useState<PublicCatalogFilters>(initial)
  const [error, setError] = useState<ApiErrorPresentation | null>(null)
  const [sizePage, setSizePage] = useState(1)
  const sizes = usePublicSizes(form.mode, sizePage)
  // Paging is explicit, bounded and truthful; no invented Small/Medium size enums.
  const sizeOptions = [...new Set([...(sizes.data?.data.map(item => item.size) ?? []), ...(form.size ? [form.size] : [])])]

  function change(patch: Partial<PublicCatalogFilters>) {
    const next = { ...form, ...patch }
    setForm(next); setError(null)
    onDirtyChange?.(publicSearchQuery(next) !== publicSearchQuery(initial))
  }
  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const candidate = { ...form, location: form.location.trim(), size: form.size.trim(), facilityId: pinnedFacility?.facilityId ?? form.facilityId }
    const invalid = validatePublicFilters(candidate, true)
    if (invalid) { setError(presentValidationError(invalid)); return }
    if (onSearch) onSearch(candidate)
    else navigate(`/facilities?${publicSearchQuery(candidate)}`)
  }
  const field = (name: string) => `${id}-${name}`
  return <section className={`panel public-search-panel ${embedded ? 'public-search-embedded' : ''}`} aria-label="Storage search">
    {embedded && <div className="public-search-intro"><h2>Find Your Next Storage</h2><p className="muted">Choose a location, storage type, size and rental period.</p></div>}
    <form className="form-grid public-search-form" onSubmit={submit} noValidate>
      <div className="form-field public-location-field"><label htmlFor={field('location')}>Location / Facility</label><div className="public-location"><input id={field('location')} type="search" list={field('locations')} placeholder="City, area or facility name" value={pinnedFacility?.name ?? form.location} readOnly={Boolean(pinnedFacility)} onChange={event => change({ location: event.target.value, facilityId: '' })} /><img src="/figma/gwp-01/imgImage.svg" alt="" aria-hidden="true" /></div><datalist id={field('locations')}>{suggestions.map(item => <option key={item.facilityId} value={item.name}>{item.address}</option>)}</datalist></div>
      <div className="form-field"><label htmlFor={field('mode')}>Storage Type</label><div className="public-select"><select id={field('mode')} value={form.mode} onChange={event => { setSizePage(1); change({ mode: event.target.value as PublicCatalogFilters['mode'], size: '', unitTypeId: '' }) }}><option value="">All storage types</option><option value="PRIVATE">Private Storage</option><option value="PUBLIC">Public Storage</option></select><img src="/figma/gwp-01/imgImage.svg" alt="" aria-hidden="true" /></div></div>
      <div className="form-field"><label htmlFor={field('size')}>Storage Size</label><div className="public-select"><select id={field('size')} value={form.size} onChange={event => change({ size: event.target.value, unitTypeId: '' })}><option value="">All sizes</option>{sizeOptions.map(size => <option key={size} value={size}>{size}</option>)}</select><img src="/figma/gwp-01/imgImage.svg" alt="" aria-hidden="true" /></div></div>
      <div className="form-field"><label htmlFor={field('start')}>Check-in Month</label><div className="public-month"><input id={field('start')} type="month" min={getNextBusinessMonth()} value={form.startMonth} onChange={event => change({ startMonth: event.target.value })} /><img src="/figma/gwp-01/imgContainer.svg" alt="" aria-hidden="true" /></div></div>
      <div className="form-field"><label htmlFor={field('end')}>Check-out Month</label><div className="public-month"><input id={field('end')} type="month" min={form.startMonth || getNextBusinessMonth()} value={form.endMonth} onChange={event => change({ endMonth: event.target.value })} /><img src="/figma/gwp-01/imgContainer1.svg" alt="" aria-hidden="true" /></div></div>
      <button className="button public-search-submit" type="submit"><img src="/figma/gwp-01/imgImage1.svg" alt="" aria-hidden="true" />Find Storage</button>
      <ApiErrorAlert error={error} />
    </form>
    {sizes.loading && <p className="public-field-note" role="status">Loading size options… You can still search all sizes.</p>}
    {sizes.error && <div className="public-options-notice"><p className="muted">Size options could not be loaded. You can search all sizes or retry.</p><ApiErrorAlert error={sizes.error} /><button className="button button-secondary" type="button" onClick={sizes.refresh}>Retry Size Options</button></div>}
    {sizes.data && sizes.data.pagination.totalPages > 1 && <div className="action-row public-size-pages"><small className="muted">Size options page {sizePage}/{sizes.data.pagination.totalPages}</small><button className="button button-secondary" type="button" disabled={sizes.loading || sizePage <= 1} onClick={() => setSizePage(page => page - 1)}>Previous Sizes</button><button className="button button-secondary" type="button" disabled={sizes.loading || sizePage >= sizes.data.pagination.totalPages} onClick={() => setSizePage(page => page + 1)}>More Sizes</button></div>}
    <p className="public-field-note">Availability is checked for every month in your selected period. Your specific storage unit is chosen at handover.</p>
  </section>
}
