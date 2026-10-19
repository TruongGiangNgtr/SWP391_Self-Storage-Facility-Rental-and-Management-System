import { useEffect, useRef, useState, type KeyboardEvent } from 'react'
import type { FacilitySummary } from '../features/catalog/catalog.types'
import { usePublicFacilities } from '../features/catalog/usePublicCatalog'
import { ApiErrorAlert } from './ApiErrorAlert'

interface PublicLocationFieldProps {
  id: string
  value: string
  pinnedFacility?: FacilitySummary
  onChange(value: string): void
}

// Location suggestions are a public catalogue read, not requested-period
// availability. The submitted search still performs the authoritative filters.
export function PublicLocationField({ id, value, pinnedFacility, onChange }: PublicLocationFieldProps) {
  const input = useRef<HTMLInputElement>(null)
  const [open, setOpen] = useState(false)
  const [activeIndex, setActiveIndex] = useState(-1)
  const [lookup, setLookup] = useState({ location: value.trim(), page: 1 })
  const waitingForInput = lookup.location !== value.trim()
  const facilities = usePublicFacilities(
    { location: lookup.location, page: lookup.page, pageSize: 20, sortBy: 'name', sortDirection: 'asc' },
    open && !pinnedFacility && !waitingForInput,
  )
  const options = facilities.data?.data ?? []
  const loading = waitingForInput || facilities.loading
  const listId = `${id}-options`
  const activeOption = options[activeIndex]

  useEffect(() => {
    if (lookup.location === value.trim()) return
    const timer = window.setTimeout(() => setLookup({ location: value.trim(), page: 1 }), 250)
    return () => window.clearTimeout(timer)
  }, [value, lookup.location])

  useEffect(() => {
    if (open && activeOption) document.getElementById(`${listId}-${activeOption.facilityId}`)?.scrollIntoView({ block: 'nearest' })
  }, [open, activeOption, listId])

  function close() {
    setOpen(false)
    setActiveIndex(-1)
  }

  function select(facility: FacilitySummary) {
    onChange(facility.name)
    close()
    input.current?.focus()
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (pinnedFacility) return
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault()
      setOpen(true)
      setActiveIndex(index => event.key === 'ArrowDown'
        ? Math.min(open ? index + 1 : 0, options.length - 1)
        : open ? Math.max(index - 1, 0) : options.length - 1)
    } else if (open && event.key === 'Enter' && activeOption) {
      event.preventDefault()
      select(activeOption)
    } else if (open && event.key === 'Escape') {
      event.preventDefault()
      event.stopPropagation()
      close()
    } else if (open && options.length && (event.key === 'Home' || event.key === 'End')) {
      event.preventDefault()
      setActiveIndex(event.key === 'Home' ? 0 : options.length - 1)
    }
  }

  function changePage(page: number) {
    setActiveIndex(-1)
    setLookup(current => ({ ...current, page }))
    input.current?.focus()
  }

  return <div className="form-field public-location-field"
    onBlur={event => { if (!event.currentTarget.contains(event.relatedTarget)) close() }}>
    <label id={`${id}-label`} htmlFor={id}>Location / Facility</label>
    <div className="public-location">
      <input ref={input} id={id} type="text" autoComplete="off"
        role={pinnedFacility ? undefined : 'combobox'}
        aria-autocomplete={pinnedFacility ? undefined : 'list'}
        aria-expanded={pinnedFacility ? undefined : open}
        aria-controls={open ? listId : undefined}
        aria-activedescendant={open && activeOption ? `${listId}-${activeOption.facilityId}` : undefined}
        placeholder="City, area or facility name" value={pinnedFacility?.name ?? value}
        readOnly={Boolean(pinnedFacility)} onKeyDown={handleKeyDown}
        onClick={() => { if (!pinnedFacility) setOpen(true) }}
        onChange={event => { onChange(event.target.value); setActiveIndex(-1); setOpen(true) }} />
      {!pinnedFacility && <button className="public-location-toggle" type="button"
        aria-label={open ? 'Close location suggestions' : 'Open location suggestions'}
        aria-expanded={open} aria-controls={open ? listId : undefined}
        onClick={() => {
          if (open) close()
          else { setOpen(true); input.current?.focus() }
        }}>
        <img src="/figma/gwp-01/imgImage.svg" alt="" aria-hidden="true" />
      </button>}
      {open && !pinnedFacility && <div className="public-location-suggestions">
        <div id={listId} role="listbox" aria-labelledby={`${id}-label`} aria-busy={loading}>
          {!loading && options.map((facility, index) => <div key={facility.facilityId}
            id={`${listId}-${facility.facilityId}`} role="option" aria-selected={index === activeIndex}
            className="public-location-option" onMouseDown={event => event.preventDefault()}
            onClick={() => select(facility)}>
            <strong>{facility.name}</strong><span>{facility.address}</span>
          </div>)}
        </div>
        {loading && <p className="public-location-message" role="status">Loading locations…</p>}
        {!loading && facilities.error && <div className="public-location-message">
          <p>Locations could not be loaded. You can still type a location and search.</p>
          <ApiErrorAlert error={facilities.error} />
          <button className="button button-secondary" type="button" onClick={facilities.refresh}>Retry Locations</button>
        </div>}
        {!loading && facilities.data && options.length === 0 && <p className="public-location-message" role="status">
          No matching locations. You can still type a city, area or facility name.
        </p>}
        {!loading && facilities.data && facilities.data.pagination.totalPages > 1 && <div className="public-location-pages">
          <small>Page {lookup.page} of {facilities.data.pagination.totalPages}</small>
          <button className="button button-secondary" type="button" disabled={lookup.page <= 1}
            onClick={() => changePage(lookup.page - 1)}>Previous Locations</button>
          <button className="button button-secondary" type="button" disabled={lookup.page >= facilities.data.pagination.totalPages}
            onClick={() => changePage(lookup.page + 1)}>More Locations</button>
        </div>}
        <p className="public-location-hint">Location suggestions only. Select Find Storage to check availability.</p>
      </div>}
    </div>
  </div>
}
