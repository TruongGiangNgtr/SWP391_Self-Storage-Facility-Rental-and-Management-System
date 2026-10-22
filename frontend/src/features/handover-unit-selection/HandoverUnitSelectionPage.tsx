import { employeeText } from '../../utils/employeePresentation'
import { EmployeeEntityLabel, EmployeeReservationLabel } from '../../components/EmployeeEntityLabel'
import {
  Check,
  ClipboardList,
  RefreshCw,
  Search,
  Warehouse,
} from 'lucide-react'
import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react'
import { handoverAssignmentApi } from '../../api/handoverAssignmentApi'
import { presentApiError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import { ApiErrorAlert } from '../../components/ApiErrorAlert'
import type { HandoverUnitAssignment } from '../handover/handover.types'
import { isActiveHandoverAssignment } from '../handover/handoverValidation'
import { reservationApi } from '../../api/reservationApi'
import { visitApi } from '../../api/visitApi'
import {
  storageUnitApi,
  type StorageUnit,
} from '../../api/storageUnitApi'
import { ApiRequestError } from '../../api/httpClient'
import { useAuth } from '../../auth/auth.context'
import type { ReservationDetail } from '../reservations/reservation.types'


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
  const [assignmentSnapshot, setAssignmentSnapshot] = useState<{
    key: string; assignment: HandoverUnitAssignment | null; error: ApiErrorPresentation | null
  } | null>(null)
  const [assignmentRetry, setAssignmentRetry] = useState(0)
  const [assignmentActionError, setAssignmentActionError] = useState<ApiErrorPresentation | null>(null)
  const [savingAssignment, setSavingAssignment] = useState(false)
  const assignmentBusy = useRef(false)
  const assignmentKey = `${selectedReservationId}:${assignmentRetry}`
  const loadingAssignment = selectedReservationId !== null && assignmentSnapshot?.key !== assignmentKey
  const selectedUnit = !loadingAssignment && assignmentSnapshot?.key === assignmentKey &&
    isActiveHandoverAssignment(assignmentSnapshot.assignment, selectedReservationId ?? '')
    ? assignmentSnapshot.assignment : null
  const assignmentError = assignmentActionError ?? (!loadingAssignment ? assignmentSnapshot?.error ?? null : null)

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

  const busy = savingAssignment || loadingAssignment || verifyingReservationId !== null || verifyingUnitId !== null

  useEffect(() => {
    if (!selectedReservationId) return
    let active = true
    void handoverAssignmentApi.get(selectedReservationId).then((response) => {
      if (active) setAssignmentSnapshot({ key: assignmentKey, assignment: response.data, error: null })
    }).catch((error: unknown) => {
      if (active) setAssignmentSnapshot({ key: assignmentKey, assignment: null, error: presentApiError(error) })
    })
    return () => { active = false }
  }, [selectedReservationId, assignmentKey])

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
    setAssignmentRetry((value) => value + 1)
    setAssignmentActionError(null)
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
        reservation.startMonth,
        reservation.endMonth,
        reservation.status,
        visit.visitDate,
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
    if (busy || assignmentBusy.current) {
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
    setAssignmentActionError(null)

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
        setReservationError(
          'This Reservation is no longer ready for Manager unit selection. Refresh the handover queue.',
        )
        await loadReservations()
        return
      }

      setSelectedReservationId(currentReservation.reservationId)
      setAssignmentRetry((value) => value + 1)
      setUnitSearch('')
    } catch (error) {
      setSelectedReservationId(null)
      setReservationError(reservationErrorMessage(error))
    } finally {
      setVerifyingReservationId(null)
    }
  }

  async function verifyAndSelectUnit(unit: StorageUnit) {
    if (!selectedReservation || busy || assignmentBusy.current) {
      return
    }

    assignmentBusy.current = true
    setVerifyingUnitId(unit.storageUnitId)
    setAssignmentActionError(null)
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
        setSelectionError(
          'This unit is no longer eligible for the selected Reservation. Refresh and choose another AVAILABLE matching unit.',
        )
        setAssignmentRetry((value) => value + 1)
        await loadUnits()
        return
      }

      const saved = await handoverAssignmentApi.save(selectedReservation.reservationId, current.storageUnitId)
      setAssignmentSnapshot({ key: assignmentKey, assignment: saved.data, error: null })
    } catch (error) {
      setAssignmentActionError(presentApiError(error))
      setAssignmentRetry((value) => value + 1)
      await loadUnits()
    } finally {
      assignmentBusy.current = false
      setVerifyingUnitId(null)
    }
  }

  async function releaseAssignment() {
    if (!selectedReservation || busy || assignmentBusy.current) return
    assignmentBusy.current = true
    setSavingAssignment(true)
    setAssignmentActionError(null)
    try {
      await handoverAssignmentApi.release(selectedReservation.reservationId)
    } catch (error) {
      setAssignmentActionError(presentApiError(error))
    } finally {
      // Always re-read, including ambiguous network outcomes and conflicts.
      setAssignmentRetry((value) => value + 1)
      await loadUnits()
      assignmentBusy.current = false
      setSavingAssignment(false)
    }
  }

  return (
    <section className="handover-page">
      <div className="handover-heading-row">
        <div>
          <div className="handover-breadcrumb">
            Facility Manager / Handover Unit Selection
          </div>
          <span className="handover-eyebrow">Handover coordination</span>
          <h1>Select Handover Unit</h1>
          <p>
            Choose an available matching unit for a checked-in customer, then save the assignment for staff.
          </p>
        </div>

        <button
          type="button"
          className="handover-refresh-button"
          onClick={() => void refresh()}
          disabled={busy || isLoadingReservations || isLoadingUnits}
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
              disabled={busy}
              value={reservationSearch}
              onChange={(event) =>
                setReservationSearch(event.target.value)
              }
              aria-label="Search reservations by rental period or status"
              placeholder="Search rental period or status"
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
                Reservations appear here once confirmed and checked in by staff.
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
                    disabled={busy}
                    onClick={() => void selectReservation(reservation)}
                  >
                    <div>
                      <strong>
                        <EmployeeReservationLabel customerId={reservation.customerId} startMonth={reservation.startMonth} endMonth={reservation.endMonth} />
                      </strong>
                      <span>
                        Reservation visit · {employeeText(reservation.reservationVisit?.visitDate)}
                      </span>
                    </div>

                    <dl>
                      <div>
                        <dt>Unit Type</dt>
                        <dd><EmployeeEntityLabel kind="unitType" id={reservation.unitTypeId} /></dd>
                      </div>
                      <div>
                        <dt>Visit Date</dt>
                        <dd>{employeeText(reservation.reservationVisit?.visitDate)}</dd>
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
              aria-label="Search available units by code or location"
              placeholder="Search unit code or location"
              disabled={!selectedReservation}
            />
          </label>

          {unitError && (
            <div className="handover-alert handover-alert-error">
              {unitError}
            </div>
          )}

          <ApiErrorAlert error={assignmentError} />
          {loadingAssignment && <p role="status">Loading saved assignment…</p>}
          {selectedReservation && !loadingAssignment && !selectedUnit && !assignmentError && <p>No unit assignment saved. Choose a unit to save it for Staff.</p>}

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
                Matching units must belong to the reservation’s facility and unit type.
              </span>
            </div>
          ) : isLoadingUnits ? (
            <div className="handover-empty">Loading StorageUnits…</div>
          ) : eligibleUnits.length === 0 ? (
            <div className="handover-empty">
              <Warehouse size={28} />
              <strong>No AVAILABLE matching StorageUnit</strong>
              <span>
                Only available units with a matching facility and unit type can be selected.
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
                        <strong>{employeeText(unit.unitCode)}</strong>
                        <span>AVAILABLE</span>
                      </div>
                      <p>{unit.locationInfo || 'No location information'}</p>
                      <small><EmployeeEntityLabel kind="unitType" id={unit.unitTypeId} /></small>
                    </div>

                    <button
                      type="button"
                      className={
                        isSelected
                          ? 'handover-select-button handover-select-button-selected'
                          : 'handover-select-button'
                      }
                      disabled={busy || isLoadingReservations || isLoadingUnits || !!assignmentSnapshot?.error}
                      onClick={() => void verifyAndSelectUnit(unit)}
                    >
                      {isSelected ? (
                        <>
                          <Check size={16} /> Saved
                        </>
                      ) : isVerifying ? (
                        'Saving…'
                      ) : (
                        'Save assignment'
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
            <h2>{employeeText(selectedUnit.unitCode)}</h2>
            <p>
              <EmployeeEntityLabel kind="customer" id={selectedReservation.customerId} /> · {selectedReservation.startMonth} → {selectedReservation.endMonth} ·
              {' '}Unit {employeeText(selectedUnit.unitCode)}
            </p>
          </div>

          <div className="handover-summary-actions">
            <button
              type="button"
              className="handover-clear-button"
              disabled={busy}
              onClick={() => void releaseAssignment()}
            >
              {savingAssignment ? 'Releasing…' : 'Release assignment'}
            </button>
          </div>

          <p className="handover-boundary-note">
            Assignment saved. Staff can open this Reservation to view the unit
            and complete handover. Select another unit to replace this assignment.
          </p>
        </section>
      )}
    </section>
  )
}
