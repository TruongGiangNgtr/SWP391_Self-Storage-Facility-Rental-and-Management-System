import { type FormEvent, useEffect, useMemo, useRef, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { presentApiError, presentValidationError } from '../api/apiErrorPresentation'
import type { ApiErrorPresentation } from '../api/apiErrorPresentation'
import { catalogApi } from '../api/catalogApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { FlowIcon } from '../components/FlowIcon'
import { EmptyState, LoadingState } from '../components/PageStates'
import type {
  FacilitySummary,
  UnitMode,
  UnitTypeAvailability,
} from '../features/catalog/catalog.types'
import {
  getNextBusinessMonth,
  validateMonthRange,
} from '../features/reservations/reservationValidation'
import { formatMoney } from '../utils/formatters'

interface SearchForm {
  facilityId: string
  mode: UnitMode | ''
  size: string
  startMonth: string
  endMonth: string
}

const INITIAL_MONTH = getNextBusinessMonth()

export function StorageSearchPage() {
  const location = useLocation()
  const searchVersion = useRef(0)
  const [facilities, setFacilities] = useState<FacilitySummary[]>([])
  const [catalog, setCatalog] = useState<UnitTypeAvailability[]>([])
  const [results, setResults] = useState<UnitTypeAvailability[] | null>(null)
  const [form, setForm] = useState<SearchForm>({
    facilityId: '',
    mode: '',
    size: '',
    startMonth: INITIAL_MONTH,
    endMonth: INITIAL_MONTH,
  })
  const [loadingFacilities, setLoadingFacilities] = useState(true)
  const [loadingCatalog, setLoadingCatalog] = useState(false)
  const [searching, setSearching] = useState(false)
  const [error, setError] = useState<ApiErrorPresentation | null>(null)

  useEffect(() => {
    let active = true

    catalogApi
      .listAllFacilities()
      .then((items) => {
        if (active) {
          setFacilities(items)
        }
      })
      .catch((requestError: unknown) => {
        if (active) {
          setError(presentApiError(requestError))
        }
      })
      .finally(() => {
        if (active) {
          setLoadingFacilities(false)
        }
      })

    return () => {
      active = false
    }
  }, [])

  useEffect(() => {
    if (!form.facilityId) {
      return
    }

    let active = true

    catalogApi
      .listAllFacilityUnitTypes(form.facilityId)
      .then((items) => {
        if (active) {
          setCatalog(items)
        }
      })
      .catch((requestError: unknown) => {
        if (active) {
          setCatalog([])
          setError(presentApiError(requestError))
        }
      })
      .finally(() => {
        if (active) {
          setLoadingCatalog(false)
        }
      })

    return () => {
      active = false
    }
  }, [form.facilityId])

  const availableModes = useMemo(
    () => [...new Set(catalog.map((item) => item.mode))],
    [catalog],
  )
  const availableSizes = useMemo(
    () => [
      ...new Set(
        catalog
          .filter((item) => !form.mode || item.mode === form.mode)
          .map((item) => item.size),
      ),
    ],
    [catalog, form.mode],
  )
  const selectedFacility = facilities.find(
    (facility) => facility.facilityId === form.facilityId,
  )

  function invalidateSearch() {
    searchVersion.current += 1
    setResults(null)
    setSearching(false)
    setError(null)
  }

  function updateForm<K extends keyof SearchForm>(field: K, value: SearchForm[K]) {
    setForm((current) => ({ ...current, [field]: value }))
    invalidateSearch()
  }

  async function handleSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)

    if (!form.facilityId || !form.mode || !form.size) {
      setError(
        presentValidationError(
          'Please select a location, unit type and unit size before searching.',
        ),
      )
      return
    }

    const monthError = validateMonthRange(form.startMonth, form.endMonth)
    if (monthError) {
      setError(presentValidationError(monthError))
      return
    }

    const version = ++searchVersion.current
    setResults(null)
    setSearching(true)
    try {
      const items = await catalogApi.listAllFacilityUnitTypes(form.facilityId, {
        startMonth: form.startMonth,
        endMonth: form.endMonth,
      })
      if (version !== searchVersion.current) {
        return
      }
      setResults(
        items.filter(
          (item) =>
            item.mode === form.mode &&
            item.size === form.size &&
            (item.availableCapacity ?? 0) > 0,
        ),
      )
    } catch (requestError) {
      if (version === searchVersion.current) {
        setResults(null)
        setError(presentApiError(requestError))
      }
    } finally {
      if (version === searchVersion.current) {
        setSearching(false)
      }
    }
  }

  return (
    <main className="page-container flow-page">
      <section className="page-heading">
        <nav className="flow-breadcrumb" aria-label="Breadcrumb">
          <Link to="/customer">Customer Portal</Link><span>/</span><span>Find Storage</span>
        </nav>
        <p className="eyebrow">Find and Reserve Storage</p>
        <h1>Find Storage</h1>
        <p className="muted">
          Select a location, unit type and rental period to find available storage.
        </p>
      </section>

      <section className="panel flow-search-panel">
        {loadingFacilities ? (
          <LoadingState label="Loading facilities..." />
        ) : (
          <form className="form-grid flow-search-form" onSubmit={handleSearch}>
            <div className="form-field form-field-wide">
              <label htmlFor="facility">Location / Facility</label>
              <select
                id="facility"
                value={form.facilityId}
                onChange={(event) => {
                  const facilityId = event.target.value
                  setForm((current) => ({
                    ...current,
                    facilityId,
                    mode: '',
                    size: '',
                  }))
                  setCatalog([])
                  invalidateSearch()
                  setLoadingCatalog(Boolean(facilityId))
                }}
              >
                <option value="">Select an active facility</option>
                {facilities.map((facility) => (
                  <option key={facility.facilityId} value={facility.facilityId}>
                    {facility.name} — {facility.address}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-field">
              <label htmlFor="mode">Unit Type</label>
              <select
                id="mode"
                disabled={!form.facilityId || loadingCatalog}
                value={form.mode}
                onChange={(event) => {
                  setForm((current) => ({
                    ...current,
                    mode: event.target.value as UnitMode | '',
                    size: '',
                  }))
                  invalidateSearch()
                }}
              >
                <option value="">Select a unit type</option>
                {availableModes.map((mode) => (
                  <option key={mode} value={mode}>
                    {mode === 'PRIVATE' ? 'Private Storage' : 'Public Storage'}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-field">
              <label htmlFor="size"><FlowIcon name="size" /> Unit Size</label>
              <select
                id="size"
                disabled={!form.facilityId || loadingCatalog}
                value={form.size}
                onChange={(event) => updateForm('size', event.target.value)}
              >
                <option value="">Select a unit size</option>
                {availableSizes.map((size) => (
                  <option key={size} value={size}>
                    {size}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-field">
              <label htmlFor="startMonth">Check-in Month</label>
              <input
                id="startMonth"
                min={INITIAL_MONTH}
                type="month"
                value={form.startMonth}
                onChange={(event) => updateForm('startMonth', event.target.value)}
              />
            </div>

            <div className="form-field">
              <label htmlFor="endMonth">Check-out Month</label>
              <input
                id="endMonth"
                min={form.startMonth || INITIAL_MONTH}
                type="month"
                value={form.endMonth}
                onChange={(event) => updateForm('endMonth', event.target.value)}
              />
            </div>

            <ApiErrorAlert error={error} />
            <button
              className="button"
              disabled={searching || loadingCatalog || facilities.length === 0}
              type="submit"
            >
              {searching ? 'Checking availability...' : 'Search'}
            </button>
          </form>
        )}
        {!loadingFacilities && !error && facilities.length === 0 && (
          <EmptyState message="There are currently no active facilities available for reservations." />
        )}
      </section>

      {results === null && !searching && !error && !loadingFacilities && facilities.length > 0 && (
        <EmptyState message="Select your search criteria and click Search to view available storage." />
      )}

      {results && (
        <section className="results-section" aria-live="polite">
          <div className="section-heading">
            <div>
              <p className="eyebrow">Available Storage</p>
              <h2>Storage Search Results</h2>
            </div>
            <span className="result-count">{results.length} {results.length === 1 ? 'option' : 'options'}</span>
          </div>

          {results.length === 0 ? (
            <EmptyState message="No matching storage is available for these months. Try a different unit size, type or rental period." />
          ) : (
            <section className="flow-facility">
              <div className="flow-facility-heading">
                <div className="facility-thumbnail" aria-hidden="true" />
                <div>
                  <p className="eyebrow">Storage Facility</p>
                  <h2>{selectedFacility?.name}</h2>
                  <p className="flow-facility-address"><FlowIcon name="location" />{selectedFacility?.address}</p>
                  {selectedFacility?.description && <p className="muted">{selectedFacility.description}</p>}
                </div>
              </div>
            <div className="flow-unit-list">
              {results.map((unitType) => {
                const params = new URLSearchParams({
                  facilityId: form.facilityId,
                  unitTypeId: unitType.unitTypeId,
                  startMonth: form.startMonth,
                  endMonth: form.endMonth,
                })

                return (
                  <article className="flow-unit-card" key={unitType.unitTypeId}>
                    <div>
                      <div className="unit-card-heading">
                        <span className="unit-icon-tile"><FlowIcon name="unit" /></span>
                        <h3>{unitType.name}</h3>
                        <span className="unit-size">{unitType.size}</span>
                      </div>
                      <p className="muted">{unitType.mode === 'PRIVATE' ? 'Private Storage' : 'Public Storage'}</p>
                      {unitType.description && <p className="muted">{unitType.description}</p>}
                      <span className="unit-capacity">Available Capacity: <strong>{unitType.availableCapacity}</strong></span>
                    </div>
                    <div className="unit-price">
                      <small>Monthly Rent</small>
                      <strong>{formatMoney(unitType.rentalPrice)}</strong>
                      <span>per month</span>
                      <Link className="button" to={`/customer/reservations/new?${params}`} state={{ backgroundLocation: location }}>
                        Create Reservation
                      </Link>
                    </div>
                  </article>
                )
              })}
            </div>
            </section>
          )}
        </section>
      )}
      <div className="notice"><FlowIcon name="info" />Reservations hold capacity for a unit type and rental period. Your physical storage unit will be selected at handover.</div>
    </main>
  )
}
