import { useMemo, useRef, useState } from 'react'
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom'
import { PackageCheck } from 'lucide-react'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { ApiRequestError } from '../api/httpClient'
import { reservationApi } from '../api/reservationApi'
import { useAuth } from '../auth/auth.context'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { CustomerActionFrame } from '../components/CustomerActionFrame'
import { FlowIcon } from '../components/FlowIcon'
import { publicSearchQuery, validatePublicFilters } from '../features/catalog/publicSearch'
import { usePublicFacility, usePublicFacilityTypes } from '../features/catalog/usePublicCatalog'
import { isUuid } from '../features/handover/handoverValidation'
import type { CreateReservationRequest } from '../features/reservations/reservation.types'
import { validateCreateReservation } from '../features/reservations/reservationValidation'
import { formatMoney, formatMonthRange } from '../utils/formatters'
import '../styles/guest-customer-remaining.css'

export function ReservationCreatePage() {
  const [searchParams] = useSearchParams()
  const { state } = useLocation()
  const navigate = useNavigate()
  const { user } = useAuth()
  const request = useMemo<CreateReservationRequest>(() => ({
    facilityId: searchParams.get('facilityId') ?? '', unitTypeId: searchParams.get('unitTypeId') ?? '',
    startMonth: searchParams.get('startMonth') ?? '', endMonth: searchParams.get('endMonth') ?? '',
  }), [searchParams])
  const filters = { ...request, location: '', mode: '' as const, size: '' }
  const validation = validateCreateReservation(request) || validatePublicFilters(filters, true)
  const facilityResult = usePublicFacility(request.facilityId, !validation)
  // Re-read the matching type at this Facility across the full requested period,
  // not a global UnitType or a saved price/stock from before authentication.
  const typesResult = usePublicFacilityTypes(request.facilityId, {
    unitTypeId: request.unitTypeId, startMonth: request.startMonth, endMonth: request.endMonth,
    availableOnly: true, page: 1, pageSize: 1,
  }, !validation)
  const facility = facilityResult.data?.data ?? null
  const unitType = typesResult.data?.data[0] ?? null
  const loading = facilityResult.loading || typesResult.loading
  const readError = facilityResult.error || typesResult.error
  const [submitting, setSubmitting] = useState(false)
  const [needsRefresh, setNeedsRefresh] = useState(false)
  const [actionError, setActionError] = useState<ApiErrorPresentation | null>(null)
  const busy = useRef(false)
  const backUrl = `/facilities?${publicSearchQuery(filters)}`
  const available = Boolean(!validation && !readError && facility && unitType && unitType.availableCapacity !== null && unitType.availableCapacity > 0)
  const refresh = () => { setNeedsRefresh(false); setActionError(null); facilityResult.refresh(); typesResult.refresh() }

  async function handleCreate() {
    if (busy.current || loading || !available || needsRefresh || user?.role !== 'CUSTOMER' || user.status !== 'ACTIVE') return
    const invalid = validateCreateReservation(request)
    if (invalid) { setActionError(presentValidationError(invalid)); return }
    busy.current = true; setSubmitting(true); setActionError(null)
    try {
      const response = await reservationApi.create(request)
      if (!isUuid(response?.data?.reservationId)) {
        setActionError(presentValidationError('The reservation response could not be verified. Check My Reservations before trying again.')); setNeedsRefresh(true); return
      }
      navigate(`/customer/reservations/${response.data.reservationId}`, { replace: true })
    } catch (error) {
      setActionError(presentApiError(error))
      if (error instanceof ApiRequestError && ['CAPACITY_NOT_AVAILABLE', 'FACILITY_INACTIVE', 'INVALID_MONTH_RANGE'].includes(error.code)) setNeedsRefresh(true)
    } finally { busy.current = false; setSubmitting(false) }
  }

  return <CustomerActionFrame className="reservation-create-design" headingIcon={<PackageCheck size={24} />} title="Confirm Your Reservation" category="Reservation" subtitle="Review your current selection before creating a reservation and paying the deposit." busy={submitting} returnTo={backUrl} backLabel="Back to Find Storage">
    <section className="panel stack reservation-create-content">
      {state && typeof state === 'object' && (state as { authResumed?: unknown }).authResumed === true && <p className="notice">Welcome back. Your selection has been restored; current price and availability are checked below.</p>}
      <ApiErrorAlert error={validation ? presentValidationError(validation) : readError} />
      <ApiErrorAlert error={actionError} />
      {loading ? <LoadingState label="Checking current price and availability…" /> : <>
        {!validation && !readError && !unitType && <p className="notice" role="status">Your selected storage is no longer available at this facility for every requested month. Return to Find Storage to choose another option.</p>}
        {facility && unitType && <div className="reservation-create-selection"><div className="dialog-selection"><span className="unit-icon-tile"><FlowIcon name="unit" /></span><div><strong>{unitType.name}</strong><small>{facility.name}</small></div></div>
          <dl className="detail-list"><div><dt>Facility</dt><dd>{facility.name}</dd></div><div><dt>Address</dt><dd>{facility.address}</dd></div><div><dt>Unit Type</dt><dd>{unitType.name}</dd></div><div><dt>Storage Type / Size</dt><dd>{unitType.mode === 'PRIVATE' ? 'Private Storage' : 'Public Storage'} · {unitType.size}</dd></div><div><dt>Rental Period</dt><dd>{formatMonthRange(request.startMonth, request.endMonth)}</dd></div><div><dt>Current Listed Rent</dt><dd>{formatMoney(unitType.rentalPrice)}/month</dd></div></dl>
        </div>}
        {facility && unitType && <div className="remaining-info"><FlowIcon name="info" /><p>Your locked rental price and deposit amount will be shown after the reservation is created. Availability can change until the server accepts your reservation.</p></div>}
      </>}
      <div className="reservation-create-actions">{available && <button className="button reservation-create-submit" disabled={submitting || loading || needsRefresh} onClick={handleCreate} type="button">{submitting ? 'Creating reservation…' : 'Confirm Reservation'}</button>}{!validation && <button className="button button-secondary" disabled={submitting || loading} type="button" onClick={refresh}>Refresh Selection</button>}<Link className="button button-secondary" to={backUrl}>Back to Find Storage</Link></div>
      {needsRefresh && <p className="muted">Refresh your selection and review the current result before submitting again.</p>}
    </section>
  </CustomerActionFrame>
}
