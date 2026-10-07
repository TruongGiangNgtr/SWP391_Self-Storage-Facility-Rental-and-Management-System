import {
  Check,
  ClipboardCopy,
  ClipboardList,
  RefreshCw,
  Search,
  Warehouse,
} from 'lucide-react'
import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from 'react'
import { reservationApi } from '../../api/reservationApi'
import { visitApi } from '../../api/visitApi'
import {
  storageUnitApi,
  type StorageUnit,
} from '../../api/storageUnitApi'
import { ApiRequestError } from '../../api/httpClient'
import { useAuth } from '../../auth/auth.context'
import type { ReservationDetail } from '../reservations/reservation.types'

function shortId(value: string) {
  return value.length <= 12
    ? value
    : `${value.slice(0, 8)}…${value.slice(-4)}`
}

function reservationErrorMessage(error: unknown) {
  if (!(error instanceof ApiRequestError)) {
    return 'Unable to load handover reservations.'
  }

  if (error.status === 401) {
    return 'Your session is no longer authorized. Please sign in again.'
  }

  if (error.status === 403) {
    return 'Facility Manager reservation access is not available for this account or the current backend role gate.'
  }

  if (error.status === 501) {
    return 'Reservation operational read is not implemented by the current backend yet.'
  }

  return error.message
}

function unitErrorMessage(error: unknown) {
  if (!(error instanceof ApiRequestError)) {
    return 'Unable to load storage units.'
  }

  if (error.status === 403) {
    return 'Storage units are outside your assigned Facility or your Facility assignment is unavailable.'
  }

  return error.message
}

export function HandoverUnitSelectionPage() {
  const { user } = useAuth()
  const facilityId = user?.facilityId ?? null

  const [reservations, setReservations] =
    useState<ReservationDetail[]>([])
  const [units, setUnits] = useState<StorageUnit[]>([])
  const [selectedReservationId, setSelectedReservationId] =
    useState<string | null>(null)
  const [selectedUnit, setSelectedUnit] =
    useState<StorageUnit | null>(null)
  const [reservationSearch, setReservationSearch] = useState('')
  const [unitSearch, setUnitSearch] = useState('')
  const [isLoadingReservations, setIsLoadingReservations] =
    useState(true)
  const [isLoadingUnits, setIsLoadingUnits] = useState(true)
  const [verifyingReservationId, setVerifyingReservationId] =
    useState<string | null>(null)
  const [verifyingUnitId, setVerifyingUnitId] =
    useState<string | null>(null)
  const [reservationError, setReservationError] =
    useState<string | null>(null)
  const [unitError, setUnitError] = useState<string | null>(null)
  const [selectionError, setSelectionError] =
    useState<string | null>(null)
  const [copyFeedback, setCopyFeedback] = useState(false)

  const loadReservations = useCallback(async () => {
    setIsLoadingReservations(true)
    setReservationError(null)

    try {
      const response = await reservationApi.list(1, 100)
      setReservations(response.data)
    } catch (error) {
      setReservations([])
      setReservationError(reservationErrorMessage(error))
    } finally {
      setIsLoadingReservations(false)
    }
  }, [])

  const loadUnits = useCallback(async () => {
    if (!facilityId) {
      setUnits([])
      setUnitError(
        'Your account does not have an assigned Facility. MWP-01 requires a Facility-scoped Manager account.',
      )
      setIsLoadingUnits(false)
      return
    }

    setIsLoadingUnits(true)
    setUnitError(null)

    try {
      const data = await storageUnitApi.listAllByFacility(
        facilityId,
      )
      setUnits(data)
    } catch (error) {
      setUnits([])
      setUnitError(unitErrorMessage(error))
    } finally {
      setIsLoadingUnits(false)
    }
  }, [facilityId])

  const refresh = useCallback(async () => {
    setSelectedUnit(null)
    setSelectionError(null)
    await Promise.all([
      loadReservations(),
      loadUnits(),
    ])
  }, [loadReservations, loadUnits])

  useEffect(() => {
    void refresh()
  }, [refresh])

  const handoverReservations = useMemo(() => {
    const normalizedSearch = reservationSearch
      .trim()
      .toLowerCase()

    return reservations.filter((reservation) => {
      const visit = reservation.reservationVisit
      const isReadyForSelection =
        reservation.status === 'CONFIRMED' &&
        visit?.visitType === 'RESERVATION' &&
        visit.status === 'CHECKED_IN' &&
        (!facilityId || reservation.facilityId === facilityId)

      if (!isReadyForSelection) {
        return false
      }

      if (!normalizedSearch) {
        return true
      }

      return [
        reservation.reservationId,
        reservation.unitTypeId,
        visit.visitId,
      ].some((value) =>
        value.toLowerCase().includes(normalizedSearch),
      )
    })
  }, [facilityId, reservationSearch, reservations])

  const selectedReservation = useMemo(
    () => handoverReservations.find(
      (reservation) =>
        reservation.reservationId === selectedReservationId,
    ) ?? null,
    [handoverReservations, selectedReservationId],
  )

  useEffect(() => {
    if (
      selectedReservationId &&
      !handoverReservations.some(
        (reservation) =>
          reservation.reservationId === selectedReservationId,
      )
    ) {
      setSelectedReservationId(null)
      setSelectedUnit(null)
    }
  }, [handoverReservations, selectedReservationId])

  const eligibleUnits = useMemo(() => {
    if (!selectedReservation) {
      return []
    }

    const normalizedSearch = unitSearch
      .trim()
      .toLowerCase()

    return units.filter((unit) => {
      const isEligible =
        unit.status === 'AVAILABLE' &&
        unit.facilityId === selectedReservation.facilityId &&
        unit.unitTypeId === selectedReservation.unitTypeId

      if (!isEligible) {
        return false
      }

      if (!normalizedSearch) {
        return true
      }

      return [
        unit.unitCode,
        unit.locationInfo ?? '',
        unit.storageUnitId,
      ].some((value) =>
        value.toLowerCase().includes(normalizedSearch),
      )
    })
  }, [selectedReservation, unitSearch, units])

  async function selectReservation(
    reservation: ReservationDetail,
  ) {
    if (verifyingReservationId) {
      return
    }

    const visitId = reservation.reservationVisit?.visitId

    if (!visitId) {
      setReservationError(
        'This Reservation does not have a RESERVATION Visit to verify.',
      )
      return
    }

    setVerifyingReservationId(reservation.reservationId)
    setReservationError(null)
    setSelectionError(null)

    try {
      const [reservationResponse, visitResponse] =
        await Promise.all([
          reservationApi.get(reservation.reservationId),
          visitApi.get(visitId),
        ])

      const currentReservation = reservationResponse.data
      const currentVisit = visitResponse.data

      const stillReady =
        currentReservation.status === 'CONFIRMED' &&
        currentReservation.facilityId === facilityId &&
        currentVisit.visitType === 'RESERVATION' &&
        currentVisit.entityId === currentReservation.reservationId &&
        currentVisit.status === 'CHECKED_IN'

      if (!stillReady) {
        setSelectedReservationId(null)
        setSelectedUnit(null)
        setReservationError(
          'This Reservation is no longer ready for Manager unit selection. Refresh the handover queue.',
        )
        await loadReservations()
        return
      }

      setSelectedReservationId(currentReservation.reservationId)
      setSelectedUnit(null)
      setUnitSearch('')
    } catch (error) {
      setSelectedReservationId(null)
      setSelectedUnit(null)
      setReservationError(reservationErrorMessage(error))
    } finally {
      setVerifyingReservationId(null)
    }
  }

  async function verifyAndSelectUnit(unit: StorageUnit) {
    if (!selectedReservation || verifyingUnitId) {
      return
    }

    setVerifyingUnitId(unit.storageUnitId)
    setSelectionError(null)

    try {
      const response = await storageUnitApi.get(
        unit.storageUnitId,
      )
      const current = response.data

      const stillEligible =
        current.status === 'AVAILABLE' &&
        current.facilityId === selectedReservation.facilityId &&
        current.unitTypeId === selectedReservation.unitTypeId

      if (!stillEligible) {
        setSelectedUnit(null)
        setSelectionError(
          'This unit is no longer eligible for the selected Reservation. Refresh and choose another AVAILABLE matching unit.',
        )
        await loadUnits()
        return
      }

      setSelectedUnit(current)
    } catch (error) {
      setSelectedUnit(null)
      setSelectionError(unitErrorMessage(error))
    } finally {
      setVerifyingUnitId(null)
    }
  }

  async function copySelectedUnitId() {
    if (!selectedUnit) {
      return
    }

    try {
      await navigator.clipboard.writeText(
        selectedUnit.storageUnitId,
      )
      setCopyFeedback(true)
      window.setTimeout(() => setCopyFeedback(false), 1600)
    } catch {
      setCopyFeedback(false)
    }
  }

  return (
    <section className="handover-page">
      <div className="handover-heading-row">
        <div>
          <div className="handover-breadcrumb">
            Facility Manager / Handover Unit Selection
          </div>
          <span className="handover-eyebrow">MWP-01</span>
          <h1>Select Handover Unit</h1>
          <p>
            Select an AVAILABLE StorageUnit for a CONFIRMED
            Reservation after its RESERVATION Visit has been checked in.
          </p>
        </div>

        <button
          type="button"
          className="handover-refresh-button"
          onClick={() => void refresh()}
          disabled={isLoadingReservations || isLoadingUnits}
        >
          <RefreshCw size={17} />
          Refresh
        </button>
      </div>

      {!facilityId && (
        <div className="handover-alert handover-alert-error">
          Your Facility Manager account has no Facility assignment.
        </div>
      )}

      <div className="handover-grid">
        <section className="handover-panel">
          <div className="handover-panel-heading">
            <div>
              <span>Step 1</span>
              <h2>Checked-in Reservations</h2>
            </div>
            <strong>{handoverReservations.length}</strong>
          </div>

          <label className="handover-search">
            <Search size={17} />
            <input
              value={reservationSearch}
              onChange={(event) =>
                setReservationSearch(event.target.value)
              }
              placeholder="Search Reservation, Visit or Unit Type ID"
            />
          </label>

          {reservationError && (
            <div className="handover-alert handover-alert-error">
              {reservationError}
            </div>
          )}

          {isLoadingReservations ? (
            <div className="handover-empty">Loading Reservations…</div>
          ) : handoverReservations.length === 0 ? (
            <div className="handover-empty">
              <ClipboardList size={28} />
              <strong>No checked-in handover Reservation</strong>
              <span>
                Only CONFIRMED Reservations whose RESERVATION Visit is
                CHECKED_IN are eligible for Manager unit selection.
              </span>
            </div>
          ) : (
            <div className="handover-reservation-list">
              {handoverReservations.map((reservation) => {
                const isSelected =
                  reservation.reservationId === selectedReservationId

                return (
                  <button
                    key={reservation.reservationId}
                    type="button"
                    className={
                      isSelected
                        ? 'handover-reservation-card handover-reservation-card-selected'
                        : 'handover-reservation-card'
                    }
                    disabled={verifyingReservationId !== null}
                    onClick={() => void selectReservation(reservation)}
                  >
                    <div>
                      <strong>
                        Reservation {shortId(reservation.reservationId)}
                      </strong>
                      <span>
                        Visit {shortId(reservation.reservationVisit?.visitId ?? '')}
                      </span>
                    </div>

                    <dl>
                      <div>
                        <dt>Unit Type</dt>
                        <dd>{shortId(reservation.unitTypeId)}</dd>
                      </div>
                      <div>
                        <dt>Visit Date</dt>
                        <dd>{reservation.reservationVisit?.visitDate}</dd>
                      </div>
                    </dl>

                    <span className="handover-ready-badge">
                      {verifyingReservationId === reservation.reservationId
                        ? 'VERIFYING…'
                        : 'CHECKED_IN'}
                    </span>
                  </button>
                )
              })}
            </div>
          )}
        </section>

        <section className="handover-panel">
          <div className="handover-panel-heading">
            <div>
              <span>Step 2</span>
              <h2>Available Matching Units</h2>
            </div>
            <strong>{eligibleUnits.length}</strong>
          </div>

          <label className="handover-search">
            <Search size={17} />
            <input
              value={unitSearch}
              onChange={(event) =>
                setUnitSearch(event.target.value)
              }
              placeholder="Search unit code, location or ID"
              disabled={!selectedReservation}
            />
          </label>

          {unitError && (
            <div className="handover-alert handover-alert-error">
              {unitError}
            </div>
          )}

          {selectionError && (
            <div className="handover-alert handover-alert-error">
              {selectionError}
            </div>
          )}

          {!selectedReservation ? (
            <div className="handover-empty">
              <Warehouse size={28} />
              <strong>Select a Reservation first</strong>
              <span>
                Unit eligibility is evaluated against that Reservation's
                Facility and Unit Type.
              </span>
            </div>
          ) : isLoadingUnits ? (
            <div className="handover-empty">Loading StorageUnits…</div>
          ) : eligibleUnits.length === 0 ? (
            <div className="handover-empty">
              <Warehouse size={28} />
              <strong>No AVAILABLE matching StorageUnit</strong>
              <span>
                MWP-01 cannot select an IN_USE, INSPECTION, MAINTENANCE,
                different-Facility or different-UnitType unit.
              </span>
            </div>
          ) : (
            <div className="handover-unit-list">
              {eligibleUnits.map((unit) => {
                const isSelected =
                  selectedUnit?.storageUnitId === unit.storageUnitId
                const isVerifying =
                  verifyingUnitId === unit.storageUnitId

                return (
                  <article
                    key={unit.storageUnitId}
                    className={
                      isSelected
                        ? 'handover-unit-card handover-unit-card-selected'
                        : 'handover-unit-card'
                    }
                  >
                    <div className="handover-unit-icon">
                      <Warehouse size={20} />
                    </div>

                    <div className="handover-unit-content">
                      <div className="handover-unit-title-row">
                        <strong>{unit.unitCode}</strong>
                        <span>AVAILABLE</span>
                      </div>
                      <p>{unit.locationInfo || 'No location information'}</p>
                      <small>{shortId(unit.storageUnitId)}</small>
                    </div>

                    <button
                      type="button"
                      className={
                        isSelected
                          ? 'handover-select-button handover-select-button-selected'
                          : 'handover-select-button'
                      }
                      disabled={verifyingUnitId !== null}
                      onClick={() => void verifyAndSelectUnit(unit)}
                    >
                      {isSelected ? (
                        <>
                          <Check size={16} /> Selected
                        </>
                      ) : isVerifying ? (
                        'Checking…'
                      ) : (
                        'Select'
                      )}
                    </button>
                  </article>
                )
              })}
            </div>
          )}
        </section>
      </div>

      {selectedReservation && selectedUnit && (
        <section className="handover-selection-summary">
          <div>
            <span>Selected for handover</span>
            <h2>{selectedUnit.unitCode}</h2>
            <p>
              Reservation {shortId(selectedReservation.reservationId)} ·
              {' '}StorageUnit {shortId(selectedUnit.storageUnitId)}
            </p>
          </div>

          <div className="handover-summary-actions">
            <button
              type="button"
              className="handover-copy-button"
              onClick={() => void copySelectedUnitId()}
            >
              <ClipboardCopy size={16} />
              {copyFeedback ? 'Copied' : 'Copy StorageUnit ID'}
            </button>

            <button
              type="button"
              className="handover-clear-button"
              onClick={() => setSelectedUnit(null)}
            >
              Clear selection
            </button>
          </div>

          <p className="handover-boundary-note">
            This Manager selection is intentionally not persisted. Facility
            Staff performs Complete Handover and submits the selected
            StorageUnit ID through the Staff handover command.
          </p>
        </section>
      )}
    </section>
  )
}
