import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { presentApiError, presentValidationError } from '../api/apiErrorPresentation'
import type { ApiErrorPresentation } from '../api/apiErrorPresentation'
import { catalogApi } from '../api/catalogApi'
import { reservationApi } from '../api/reservationApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { FlowDialog } from '../components/FlowDialog'
import { FlowIcon } from '../components/FlowIcon'
import type {
  FacilitySummary,
  UnitTypeAvailability,
} from '../features/catalog/catalog.types'
import type { CreateReservationRequest } from '../features/reservations/reservation.types'
import { validateCreateReservation } from '../features/reservations/reservationValidation'
import { formatMoney, formatMonthRange } from '../utils/formatters'

export function ReservationCreatePage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const request = useMemo<CreateReservationRequest>(
    () => ({
      facilityId: searchParams.get('facilityId') ?? '',
      unitTypeId: searchParams.get('unitTypeId') ?? '',
      startMonth: searchParams.get('startMonth') ?? '',
      endMonth: searchParams.get('endMonth') ?? '',
    }),
    [searchParams],
  )
  const requestValidationError = validateCreateReservation(request)
  const [facility, setFacility] = useState<FacilitySummary | null>(null)
  const [unitType, setUnitType] = useState<UnitTypeAvailability | null>(null)
  const [loading, setLoading] = useState(!requestValidationError)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<ApiErrorPresentation | null>(() =>
    requestValidationError ? presentValidationError(requestValidationError) : null,
  )

  useEffect(() => {
    if (requestValidationError) {
      return
    }

    let active = true
    Promise.all([
      catalogApi.getFacility(request.facilityId),
      catalogApi.getUnitType(request.unitTypeId),
    ])
      .then(([facilityResponse, unitTypeResponse]) => {
        if (active) {
          setFacility(facilityResponse.data)
          setUnitType(unitTypeResponse.data)
        }
      })
      .catch((requestError: unknown) => {
        if (active) {
          setError(presentApiError(requestError))
        }
      })
      .finally(() => {
        if (active) {
          setLoading(false)
        }
      })

    return () => {
      active = false
    }
  }, [request, requestValidationError])

  async function handleCreate() {
    const validationError = validateCreateReservation(request)
    if (validationError) {
      setError(presentValidationError(validationError))
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      const response = await reservationApi.create(request)
      navigate(`/customer/reservations/${response.data.reservationId}`, { replace: true })
    } catch (requestError) {
      setError(presentApiError(requestError))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <FlowDialog
      title="Confirm Your Reservation"
      category="Reservation"
      subtitle="Review your selection before creating a reservation and paying the deposit."
      busy={submitting}
      onClose={() => navigate('/customer/storage-search', { replace: true })}
    >

      {loading ? (
        <LoadingState label="Checking your selection..." />
      ) : (
        <section className="panel">
          <ApiErrorAlert
            error={requestValidationError ? presentValidationError(requestValidationError) : error}
          />

          {!requestValidationError && facility && unitType && (
            <>
              <div className="dialog-selection">
                <span className="unit-icon-tile"><FlowIcon name="unit" /></span>
                <div><strong>{unitType.name}</strong><small>{facility.name}</small></div>
              </div>
              <dl className="detail-list">
                <div>
                  <dt>Facility</dt>
                  <dd>{facility.name}</dd>
                </div>
                <div>
                  <dt>Address</dt>
                  <dd>{facility.address}</dd>
                </div>
                <div>
                  <dt>Unit Type</dt>
                  <dd>{unitType.name}</dd>
                </div>
                <div>
                  <dt>Storage Mode / Size</dt>
                  <dd>
                    {unitType.mode === 'PRIVATE' ? 'Private Storage' : 'Public Storage'} ·{' '}
                    {unitType.size}
                  </dd>
                </div>
                <div>
                  <dt>Rental Period</dt>
                  <dd>{formatMonthRange(request.startMonth, request.endMonth)}</dd>
                </div>
                <div>
                  <dt>Current Listed Rent</dt>
                  <dd>{formatMoney(unitType.rentalPrice)}/month</dd>
                </div>
              </dl>

              <div className="notice">
                <FlowIcon name="info" />
                Your locked rental price and deposit amount will be shown after the reservation is created.
              </div>

              <div className="action-row">
                <Link className="button button-secondary" to="/customer/storage-search">
                  Back to Find Storage
                </Link>
                <button className="button" disabled={submitting} onClick={handleCreate} type="button">
                  {submitting ? 'Creating reservation...' : 'Confirm Reservation'}
                </button>
              </div>
            </>
          )}
          {(!facility || !unitType || requestValidationError) && (
            <Link className="button button-secondary" to="/customer/storage-search">
              Back to Find Storage
            </Link>
          )}
        </section>
      )}
    </FlowDialog>
  )
}
