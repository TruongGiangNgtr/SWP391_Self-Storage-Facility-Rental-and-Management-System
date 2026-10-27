import { useId, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { ArrowRight, Box, CalendarDays, ChevronDown, ChevronUp, Info, Ruler, Search, SlidersHorizontal } from 'lucide-react'
import { ApiErrorAlert } from './ApiErrorAlert'
import { initialPublicFilters, publicSearchQuery, validatePublicFilters } from '../features/catalog/publicSearch'
import { usePublicSizes } from '../features/catalog/usePublicCatalog'
import type { FacilitySummary, PublicCatalogFilters } from '../features/catalog/catalog.types'
import { presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { getNextBusinessMonth } from '../features/reservations/reservationValidation'
import { reservationDuration, reservationPeriod } from '../features/reservations/reservationListPresentation'
import { PublicLocationField } from './PublicLocationField'
import { MonthPicker } from './MonthPicker'

export function PublicStorageSearchForm({ initialValues, pinnedFacility, onSearch, onDirtyChange, embedded = false, collapsible = false }: {
  initialValues?: PublicCatalogFilters; pinnedFacility?: FacilitySummary
  onSearch?(filters: PublicCatalogFilters): void; onDirtyChange?(dirty: boolean): void; embedded?: boolean; collapsible?: boolean
}) {
  const id = useId()
  const navigate = useNavigate()
  const [initial] = useState(() => initialValues ?? initialPublicFilters())
  const [form, setForm] = useState<PublicCatalogFilters>(initial)
  const [error, setError] = useState<ApiErrorPresentation | null>(null)
  const [sizePage, setSizePage] = useState(1)
  const [expanded, setExpanded] = useState(!collapsible)
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
  const showForm = !collapsible || expanded
  const validPeriod = !validatePublicFilters(form, true)
  const appliedPeriod = collapsible && !validatePublicFilters(initial, true) ? reservationPeriod(initial.startMonth, initial.endMonth) : ''
  return <section className={`panel public-search-panel ${embedded ? 'public-search-embedded' : ''} ${showForm ? '' : 'public-search-collapsed'}`} aria-label="Storage search">
    <div className="public-search-header">
      <span className="public-search-mark"><Search size={22} aria-hidden="true" /></span>
      <div className="public-search-intro">
        <h2>{collapsible ? 'Your search' : embedded ? 'Find your storage' : 'Make space for your next move'}</h2>
        {collapsible ? <p className="public-applied-filters"><strong>{pinnedFacility?.name || initial.location || 'All locations'}</strong><span>{appliedPeriod}</span><span>{initial.mode === 'PRIVATE' ? 'Private Storage' : initial.mode === 'PUBLIC' ? 'Public Storage' : 'All storage types'} · {initial.size || 'All sizes'}</span></p>
          : <p className="muted">Choose your space and rental months. We’ll find the matching options.</p>}
      </div>
      {collapsible && <button className="button button-secondary public-edit-search" type="button" aria-expanded={showForm} aria-controls={field('body')} onClick={() => setExpanded(value => !value)}>
        {showForm ? <ChevronUp size={16} aria-hidden="true" /> : <SlidersHorizontal size={16} aria-hidden="true" />}{showForm ? 'Hide Search' : 'Edit Search'}
      </button>}
    </div>
    <div id={field('body')} className="public-search-body" hidden={!showForm}>
    <form className="form-grid public-search-form" onSubmit={submit} noValidate>
      <div className="public-storage-choices" role="group" aria-label="Storage preferences">
        <PublicLocationField id={field('location')} value={form.location} pinnedFacility={pinnedFacility}
          onChange={location => change({ location, facilityId: '' })} />
        <div className="form-field"><label htmlFor={field('mode')}>Storage Type</label><div className="public-select"><Box className="public-input-icon" size={18} aria-hidden="true" /><select id={field('mode')} value={form.mode} onChange={event => { setSizePage(1); change({ mode: event.target.value as PublicCatalogFilters['mode'], size: '', unitTypeId: '' }) }}><option value="">All storage types</option><option value="PRIVATE">Private Storage</option><option value="PUBLIC">Public Storage</option></select><ChevronDown className="public-select-chevron" size={16} aria-hidden="true" /></div></div>
        <div className="form-field"><label htmlFor={field('size')}>Storage Size</label><div className="public-select"><Ruler className="public-input-icon" size={18} aria-hidden="true" /><select id={field('size')} value={form.size} onChange={event => change({ size: event.target.value, unitTypeId: '' })}><option value="">All sizes</option>{sizeOptions.map(size => <option key={size} value={size}>{size}</option>)}</select><ChevronDown className="public-select-chevron" size={16} aria-hidden="true" /></div></div>
      </div>
      <div className="public-search-bottom">
        <section className="public-rental-period" aria-labelledby={field('period-title')}>
          <div className="public-rental-heading"><CalendarDays size={17} aria-hidden="true" /><h3 id={field('period-title')}>Rental period</h3>
            <details className="public-rental-help"><summary aria-label="About rental months"><Info size={16} aria-hidden="true" /></summary><p id={field('period-help')}>Choose whole calendar months. Check-in must be next month or later. Check-out can be the same month as check-in or later.</p></details>
          </div>
          <div className="public-month-range">
            <div className="form-field public-check-in-field"><label htmlFor={field('start')}>Check-in Month</label><MonthPicker id={field('start')} label="Check-in Month" min={getNextBusinessMonth()} value={form.startMonth} describedBy={field('period-help')} onChange={startMonth => change({ startMonth })} />
              {validPeriod && <span className="public-rental-duration" role="status">{reservationDuration(form.startMonth, form.endMonth)}</span>}
            </div>
            <ArrowRight className="public-month-arrow" size={18} aria-hidden="true" />
            <div className="form-field"><label htmlFor={field('end')}>Check-out Month</label><MonthPicker id={field('end')} label="Check-out Month" min={form.startMonth || getNextBusinessMonth()} value={form.endMonth} describedBy={field('period-help')} onChange={endMonth => change({ endMonth })} /></div>
          </div>
        </section>
        <div className="public-search-action"><button className="button public-search-submit" type="submit"><Search size={18} aria-hidden="true" />Find Storage</button></div>
      </div>
      <ApiErrorAlert error={error} />
    </form>
    {sizes.loading && <p className="public-field-note" role="status">Loading size options… You can still search all sizes.</p>}
    {sizes.error && <div className="public-options-notice"><p className="muted">Size options could not be loaded. You can search all sizes or retry.</p><ApiErrorAlert error={sizes.error} /><button className="button button-secondary" type="button" onClick={sizes.refresh}>Retry Size Options</button></div>}
    {sizes.data && sizes.data.pagination.totalPages > 1 && <div className="action-row public-size-pages"><small className="muted">Size options page {sizePage}/{sizes.data.pagination.totalPages}</small><button className="button button-secondary" type="button" disabled={sizes.loading || sizePage <= 1} onClick={() => setSizePage(page => page - 1)}>Previous Sizes</button><button className="button button-secondary" type="button" disabled={sizes.loading || sizePage >= sizes.data.pagination.totalPages} onClick={() => setSizePage(page => page + 1)}>More Sizes</button></div>}
    <p className="public-field-note public-search-footnote"><Info size={14} aria-hidden="true" /><span>Availability is checked for your full rental period. Your specific unit is chosen at handover.</span></p>
    </div>
  </section>
}
