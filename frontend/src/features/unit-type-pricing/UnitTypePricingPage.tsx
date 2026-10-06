import {
  type SyntheticEvent,
  useEffect,
  useMemo,
  useState,
} from 'react'
import { businessApi } from '../../api/businessApi'
import { ApiRequestError } from '../../api/httpClient'
import type {
  UnitType,
  UnitTypeMode,
} from '../../models/unitType'
import { formatMoney } from '../../utils/formatters'

interface PriceFormErrors {
  rentalPrice?: string
}

export function UnitTypePricingPage() {
  const [unitTypes, setUnitTypes] = useState<UnitType[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [pageError, setPageError] = useState<string | null>(null)
  const [searchTerm, setSearchTerm] = useState('')
  const [modeFilter, setModeFilter] =
    useState<'ALL' | UnitTypeMode>('ALL')
  const [currentPage, setCurrentPage] = useState(1)
  const [editingUnitType, setEditingUnitType] =
    useState<UnitType | null>(null)
  const [rentalPrice, setRentalPrice] = useState('')
  const [priceErrors, setPriceErrors] =
    useState<PriceFormErrors>({})
  const [isSaving, setIsSaving] = useState(false)

  const pageSize = 5

  useEffect(() => {
    let cancelled = false

    async function loadUnitTypes() {
      setIsLoading(true)
      setPageError(null)

      try {
        const data =
          await businessApi.listAllUnitTypes()

        if (cancelled) {
          return
        }

        setUnitTypes(data)
      } catch (error) {
        if (cancelled) {
          return
        }

        setUnitTypes([])

        if (error instanceof ApiRequestError) {
          if (error.status === 401) {
            setPageError(
              'Unauthorized. Please sign in as Business Operations Manager.',
            )
            return
          }

          if (error.status === 403) {
            setPageError(
              'You do not have permission to access Unit Type Pricing.',
            )
            return
          }

          setPageError(error.message)
          return
        }

        setPageError('Unable to load unit types.')
      } finally {
        if (!cancelled) {
          setIsLoading(false)
        }
      }
    }

    void loadUnitTypes()

    return () => {
      cancelled = true
    }
  }, [])

  const totalUnitTypes = unitTypes.length

  const publicUnitTypes = unitTypes.filter(
    (unitType) => unitType.mode === 'PUBLIC',
  ).length

  const privateUnitTypes = unitTypes.filter(
    (unitType) => unitType.mode === 'PRIVATE',
  ).length

  const filteredUnitTypes = useMemo(() => {
    const normalizedSearch = searchTerm
      .trim()
      .toLowerCase()

    return unitTypes.filter((unitType) => {
      const matchesMode =
        modeFilter === 'ALL' ||
        unitType.mode === modeFilter

      const matchesSearch =
        normalizedSearch.length === 0 ||
        unitType.name
          .toLowerCase()
          .includes(normalizedSearch) ||
        unitType.size
          .toLowerCase()
          .includes(normalizedSearch) ||
        unitType.description
          ?.toLowerCase()
          .includes(normalizedSearch)

      return matchesMode && matchesSearch
    })
  }, [modeFilter, searchTerm, unitTypes])

  const totalPages = Math.max(
    1,
    Math.ceil(filteredUnitTypes.length / pageSize),
  )

  const paginatedUnitTypes = useMemo(() => {
    const startIndex =
      (currentPage - 1) * pageSize

    return filteredUnitTypes.slice(
      startIndex,
      startIndex + pageSize,
    )
  }, [currentPage, filteredUnitTypes])

  const firstVisibleItem =
    filteredUnitTypes.length === 0
      ? 0
      : (currentPage - 1) * pageSize + 1

  const lastVisibleItem = Math.min(
    currentPage * pageSize,
    filteredUnitTypes.length,
  )

  function openPriceModal(unitType: UnitType) {
    setEditingUnitType(unitType)
    setRentalPrice(String(unitType.rentalPrice))
    setPriceErrors({})
  }

  function closePriceModal() {
    if (isSaving) {
      return
    }

    setEditingUnitType(null)
    setRentalPrice('')
    setPriceErrors({})
  }

  async function handleUpdatePrice(
    event: SyntheticEvent<HTMLFormElement>,
  ) {
    event.preventDefault()

    if (!editingUnitType || isSaving) {
      return
    }

    const normalizedValue = rentalPrice.trim()
    const parsedPrice = Number(normalizedValue)
    const nextErrors: PriceFormErrors = {}

    if (normalizedValue.length === 0) {
      nextErrors.rentalPrice =
        'Rental price is required.'
    } else if (!Number.isFinite(parsedPrice)) {
      nextErrors.rentalPrice =
        'Rental price must be a valid number.'
    } else if (parsedPrice < 0) {
      nextErrors.rentalPrice =
        'Rental price must be greater than or equal to 0.'
    }

    if (Object.keys(nextErrors).length > 0) {
      setPriceErrors(nextErrors)
      return
    }

    setIsSaving(true)
    setPriceErrors({})

    try {
      const response =
        await businessApi.updateUnitTypePrice(
          editingUnitType.unitTypeId,
          { rentalPrice: parsedPrice },
        )

      setUnitTypes((current) =>
        current.map((unitType) =>
          unitType.unitTypeId ===
          editingUnitType.unitTypeId
            ? response.data
            : unitType,
        ),
      )

      setEditingUnitType(null)
      setRentalPrice('')
    } catch (error) {
      if (error instanceof ApiRequestError) {
        if (error.code === 'VALIDATION_ERROR') {
          const serverMessage =
            error.errors?.rentalPrice?.[0]

          setPriceErrors({
            rentalPrice:
              serverMessage ??
              'The rental price is invalid.',
          })
          return
        }

        window.alert(error.message)
        return
      }

      window.alert(
        'Unable to update unit type price.',
      )
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <section className="facility-page">
      <div className="facility-breadcrumb">
        Business Operations / Unit Types &amp; Pricing
      </div>

      <div className="facility-page-heading">
        <div>
          <span className="facility-eyebrow">
            UNIT TYPE PRICING
          </span>

          <h1>Unit Type Pricing</h1>

          <p>
            View global storage unit types and maintain
            the current monthly rental price.
          </p>
        </div>

        <div className="facility-heading-actions">
          <span className="facility-protection-note">
            Global Pricing · Historical Snapshots Protected
          </span>
        </div>
      </div>

      <div className="facility-summary-grid">
        <article className="facility-summary-card">
          <div>
            <span className="facility-summary-label">
              TOTAL UNIT TYPES
            </span>

            <strong>{totalUnitTypes}</strong>

            <p>Global UnitType master records</p>
          </div>

          <div className="facility-summary-icon">
            U
          </div>
        </article>

        <article className="facility-summary-card">
          <div>
            <span className="facility-summary-label">
              PUBLIC TYPES
            </span>

            <strong>{publicUnitTypes}</strong>

            <p className="facility-summary-success">
              Unit types with PUBLIC mode
            </p>
          </div>

          <div className="facility-summary-icon facility-summary-icon--active">
            P
          </div>
        </article>

        <article className="facility-summary-card">
          <div>
            <span className="facility-summary-label">
              PRIVATE TYPES
            </span>

            <strong>{privateUnitTypes}</strong>

            <p>Unit types with PRIVATE mode</p>
          </div>

          <div className="facility-summary-icon">
            P
          </div>
        </article>
      </div>

      <div className="facility-toolbar">
        <div className="facility-search-wrapper">
          <span className="facility-search-icon">
            ⌕
          </span>

          <input
            type="search"
            value={searchTerm}
            onChange={(event) => {
              setSearchTerm(event.target.value)
              setCurrentPage(1)
            }}
            placeholder="Search by unit type name, size or description..."
            className="facility-search-input"
          />
        </div>

        <div className="facility-status-filters">
          <button
            type="button"
            className={
              modeFilter === 'ALL'
                ? 'facility-filter-button facility-filter-button--active'
                : 'facility-filter-button'
            }
            onClick={() => {
              setModeFilter('ALL')
              setCurrentPage(1)
            }}
          >
            All
          </button>

          <button
            type="button"
            className={
              modeFilter === 'PUBLIC'
                ? 'facility-filter-button facility-filter-button--active'
                : 'facility-filter-button'
            }
            onClick={() => {
              setModeFilter('PUBLIC')
              setCurrentPage(1)
            }}
          >
            Public
          </button>

          <button
            type="button"
            className={
              modeFilter === 'PRIVATE'
                ? 'facility-filter-button facility-filter-button--active'
                : 'facility-filter-button'
            }
            onClick={() => {
              setModeFilter('PRIVATE')
              setCurrentPage(1)
            }}
          >
            Private
          </button>
        </div>

        <button
          type="button"
          className="facility-clear-button"
          onClick={() => {
            setSearchTerm('')
            setModeFilter('ALL')
            setCurrentPage(1)
          }}
        >
          Clear filters
        </button>
      </div>

      <div className="facility-table-card">
        <div className="facility-table-header">
          <div>
            <h2>Global Unit Type Pricing Registry</h2>

            <p>
              Current global monthly RentalPrice for each
              UnitType.
            </p>
          </div>

          <span>
            Showing {filteredUnitTypes.length} unit types
          </span>
        </div>

        <div className="facility-table-scroll">
          <table className="facility-table">
            <thead>
              <tr>
                <th>Unit Type</th>
                <th>Mode &amp; Size</th>
                <th>Current Rental Price</th>
                <th>Action</th>
              </tr>
            </thead>

            <tbody>
              {isLoading && (
                <tr>
                  <td
                    colSpan={4}
                    className="facility-loading-state"
                  >
                    <div className="facility-state-content">
                      <div className="facility-spinner" />

                      <strong>
                        Loading unit types...
                      </strong>

                      <span>
                        Retrieving the current global UnitType
                        catalogue.
                      </span>
                    </div>
                  </td>
                </tr>
              )}

              {pageError && (
                <tr>
                  <td
                    colSpan={4}
                    className="facility-error-state"
                  >
                    <div className="facility-state-content">
                      <div className="facility-error-icon">
                        !
                      </div>

                      <strong>
                        Unable to load unit types
                      </strong>

                      <span>{pageError}</span>
                    </div>
                  </td>
                </tr>
              )}

              {!isLoading &&
                !pageError &&
                paginatedUnitTypes.map((unitType) => (
                  <tr key={unitType.unitTypeId}>
                    <td>
                      <div className="facility-name-cell">
                        <strong>{unitType.name}</strong>

                        <span>
                          {unitType.description ??
                            'No description provided.'}
                        </span>
                      </div>
                    </td>

                    <td>
                      <div className="facility-location-cell">
                        <strong>{unitType.mode}</strong>
                        <span>{unitType.size}</span>
                      </div>
                    </td>

                    <td>
                      <div className="facility-name-cell">
                        <strong>
                          {formatMoney(
                            unitType.rentalPrice,
                          )}
                        </strong>
                        <span>
                          Current monthly global price
                        </span>
                      </div>
                    </td>

                    <td>
                      <div className="facility-actions">
                        <button
                          type="button"
                          className="facility-action-button facility-action-button--activate"
                          onClick={() =>
                            openPriceModal(unitType)
                          }
                        >
                          Update Price
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}

              {!isLoading &&
                !pageError &&
                unitTypes.length === 0 && (
                  <tr>
                    <td
                      colSpan={4}
                      className="facility-empty-state"
                    >
                      <div className="facility-state-content">
                        <strong>
                          No unit types available
                        </strong>

                        <span>
                          UnitType master rows must be provided
                          by approved deployment data preparation.
                        </span>
                      </div>
                    </td>
                  </tr>
                )}

              {!isLoading &&
                !pageError &&
                unitTypes.length > 0 &&
                filteredUnitTypes.length === 0 && (
                  <tr>
                    <td
                      colSpan={4}
                      className="facility-empty-state"
                    >
                      <div className="facility-state-content">
                        <strong>
                          No matching unit types
                        </strong>

                        <span>
                          Try another search term or clear the
                          current filters.
                        </span>

                        <button
                          type="button"
                          className="facility-secondary-button"
                          onClick={() => {
                            setSearchTerm('')
                            setModeFilter('ALL')
                            setCurrentPage(1)
                          }}
                        >
                          Clear Filters
                        </button>
                      </div>
                    </td>
                  </tr>
                )}
            </tbody>
          </table>
        </div>

        <div className="facility-table-footer">
          <span>Rows per page: {pageSize}</span>

          <div className="facility-pagination">
            <span>
              Showing {firstVisibleItem}–
              {lastVisibleItem} of{' '}
              {filteredUnitTypes.length} unit types
            </span>

            <button
              type="button"
              className="facility-page-button"
              disabled={currentPage === 1}
              onClick={() =>
                setCurrentPage((current) =>
                  Math.max(1, current - 1),
                )
              }
            >
              Previous
            </button>

            <span className="facility-page-number">
              {currentPage}
            </span>

            <button
              type="button"
              className="facility-page-button"
              disabled={
                currentPage === totalPages ||
                filteredUnitTypes.length === 0
              }
              onClick={() =>
                setCurrentPage((current) =>
                  Math.min(
                    totalPages,
                    current + 1,
                  ),
                )
              }
            >
              Next
            </button>
          </div>
        </div>
      </div>

      <div className="facility-governance-card">
        <div className="facility-governance-icon">
          §
        </div>

        <div>
          <h2>
            Global Pricing &amp; Snapshot Protection
          </h2>

          <p>
            Updating RentalPrice changes only the current
            global UnitType price. Historical Reservation,
            ContractExtension and issued Invoice price
            snapshots remain unchanged.
          </p>
        </div>
      </div>

      {editingUnitType && (
        <div
          className="facility-modal-backdrop"
          onMouseDown={closePriceModal}
        >
          <div
            className="facility-modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="unit-type-price-title"
            onMouseDown={(event) =>
              event.stopPropagation()
            }
          >
            <div className="facility-modal-header">
              <div>
                <span className="facility-eyebrow">
                  UNIT TYPE PRICING
                </span>

                <h2 id="unit-type-price-title">
                  Update Rental Price
                </h2>

                <p>
                  Update the current global monthly price
                  for {editingUnitType.name}.
                </p>
              </div>

              <button
                type="button"
                className="facility-modal-close"
                onClick={closePriceModal}
                aria-label="Close update price dialog"
                disabled={isSaving}
              >
                ×
              </button>
            </div>

            <div className="facility-modal-notice">
              Current price:{' '}
              <strong>
                {formatMoney(
                  editingUnitType.rentalPrice,
                )}
              </strong>
              . This update does not rewrite historical
              price snapshots.
            </div>

            <form
              className="facility-form"
              onSubmit={handleUpdatePrice}
            >
              <div className="facility-detail-row">
                <span>Unit Type</span>
                <strong>{editingUnitType.name}</strong>
              </div>

              <div className="facility-detail-row">
                <span>Mode / Size</span>
                <strong>
                  {editingUnitType.mode} ·{' '}
                  {editingUnitType.size}
                </strong>
              </div>

              <div className="facility-form-field">
                <label htmlFor="unit-type-rental-price">
                  New Monthly Rental Price
                  <span>*</span>
                </label>

                <input
                  id="unit-type-rental-price"
                  type="number"
                  min="0"
                  step="0.01"
                  inputMode="decimal"
                  value={rentalPrice}
                  onChange={(event) => {
                    setRentalPrice(event.target.value)
                    setPriceErrors({})
                  }}
                  disabled={isSaving}
                />

                {priceErrors.rentalPrice && (
                  <small className="facility-form-error">
                    {priceErrors.rentalPrice}
                  </small>
                )}
              </div>

              <div className="facility-modal-actions">
                <button
                  type="button"
                  className="facility-secondary-button"
                  onClick={closePriceModal}
                  disabled={isSaving}
                >
                  Cancel
                </button>

                <button
                  type="submit"
                  className="facility-primary-button"
                  disabled={isSaving}
                >
                  {isSaving
                    ? 'Saving...'
                    : 'Save Price'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </section>
  )
}
