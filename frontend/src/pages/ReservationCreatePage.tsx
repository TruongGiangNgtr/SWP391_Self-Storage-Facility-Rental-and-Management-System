import { useEffect, useMemo, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { Link, useLocation, useNavigate, useSearchParams, type Location } from 'react-router-dom'
import { PackageCheck } from 'lucide-react'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { ApiRequestError } from '../api/httpClient'
import { reservationApi } from '../api/reservationApi'
import { authToken } from '../api/authToken'
import { useAuth } from '../auth/auth.context'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { CustomerActionFrame } from '../components/CustomerActionFrame'
import { FlowDialog } from '../components/FlowDialog'
import { ReservationCccdForm } from '../components/ReservationCccdForm'
import { FlowIcon } from '../components/FlowIcon'
import { BookingJourney } from '../components/BookingJourney'
import { FacilityImage } from '../components/FacilityImage'
import { publicSearchQuery, validatePublicFilters } from '../features/catalog/publicSearch'
import { usePublicFacility, usePublicFacilityTypes } from '../features/catalog/usePublicCatalog'
import { isUuid } from '../features/handover/handoverValidation'
import type { CreateReservationRequest } from '../features/reservations/reservation.types'
import { validateCreateReservation } from '../features/reservations/reservationValidation'
import { useReservationProfile } from '../features/reservations/useReservationProfile'
import { formatMoney, formatMonthRange } from '../utils/formatters'
import '../styles/guest-customer-remaining.css'
import '../styles/reservation-cccd.css'

export function ReservationCreatePage() {
  const { user } = useAuth()
  const { search } = useLocation()
  // Discard private form state when the account or public selection changes.
  return <ReservationReview key={`${user?.userAccountId}:${search}`} />
}

function ReservationReview() {
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
  const customerActive = user?.role === 'CUSTOMER' && user.status === 'ACTIVE'
  const profile = useReservationProfile(!validation && customerActive)
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
  const [deferred, setDeferred] = useState(false)
  const [completionRequested, setCompletionRequested] = useState(false)
  const [profileResumed, setProfileResumed] = useState(false)
  const busy = useRef(false)
  const reviewRoot = useRef<HTMLElement>(null)
  const active = useRef(false)
  useEffect(() => { active.current = true; return () => { active.current = false } }, [])
  const backUrl = `/facilities?${publicSearchQuery(filters)}`
  const available = Boolean(!validation && !readError && facility && unitType && unitType.availableCapacity !== null && unitType.availableCapacity > 0)
  const refreshCatalog = () => { setNeedsRefresh(false); setActionError(null); facilityResult.refresh(); typesResult.refresh() }
  const refresh = () => { refreshCatalog(); void profile.recheck() }
  const background = (state as { backgroundLocation?: Location } | null)?.backgroundLocation
  const inDialog = Boolean(background && (background.pathname === '/customer' || background.pathname.startsWith('/customer/')))
  const completing = !deferred && (completionRequested || (available && profile.phase === 'missing'))
  const working = submitting || profile.saving
  const deferCompletion = () => {
    setDeferred(true); setCompletionRequested(false)
    requestAnimationFrame(() => reviewRoot.current?.querySelector<HTMLButtonElement>('.reservation-create-submit')?.focus())
  }
  const resumeReview = () => {
    setCompletionRequested(false); setDeferred(false); setProfileResumed(true); refreshCatalog()
    requestAnimationFrame(() => reviewRoot.current?.focus())
  }

  async function handleCreate() {
    if (busy.current || loading || !available || needsRefresh || !customerActive || profile.saving) return
    const invalid = validateCreateReservation(request)
    if (invalid) { setActionError(presentValidationError(invalid)); return }
    busy.current = true; setSubmitting(true); setActionError(null)
    const token = authToken.get()
    try {
      // Re-read even if a previous read was ready: another tab may have cleared CCCD.
      const currentProfile = await profile.recheck()
      if (!active.current || authToken.get() !== token) return
      if (currentProfile !== 'ready') { setDeferred(false); return }
      const response = await reservationApi.create(request)
      if (!active.current || authToken.get() !== token) return
      if (!isUuid(response?.data?.reservationId)) {
        setActionError(presentValidationError('We could not confirm your reservation. Check My Reservations before trying again, as it may already have been created.')); setNeedsRefresh(true); return
      }
      navigate(`/customer/reservations/${response.data.reservationId}`, { replace: true })
    } catch (error) {
      if (!active.current || authToken.get() !== token) return
      if (error instanceof ApiRequestError && error.status === 400 && error.code === 'VALIDATION_ERROR' && error.errors && Object.hasOwn(error.errors, 'cccd')) {
        // Stable code + field key determine UX; never render provider/server prose as form guidance.
        profile.requireCompletion('Add a valid 12-digit National ID / CCCD to continue your reservation.')
        setCompletionRequested(true); setDeferred(false)
        return
      }
      setActionError(presentApiError(error))
      if (error instanceof ApiRequestError && ['CAPACITY_NOT_AVAILABLE', 'FACILITY_INACTIVE', 'INVALID_MONTH_RANGE'].includes(error.code)) setNeedsRefresh(true)
    } finally { busy.current = false; if (active.current) setSubmitting(false) }
  }

  const completionForm = <ReservationCccdForm profile={profile} onSaveStart={() => setCompletionRequested(true)} onContinue={resumeReview} onLater={deferCompletion} />
  return <><CustomerActionFrame className="reservation-create-design booking-review reservations-editorial-dialog" headingIcon={<PackageCheck size={24} />} title={inDialog && completing ? 'Complete Your Profile' : 'Confirm Your Reservation'} category="Reservation" subtitle={inDialog && completing ? 'Add your National ID, then return to review your reservation.' : 'A final look at your space, rental months and current availability.'} busy={working} returnTo={backUrl} backLabel="Back to Find Storage" onClose={inDialog && completing ? deferCompletion : undefined}>
    <BookingJourney step={1} />
    {inDialog && completing ? completionForm : <section ref={reviewRoot} tabIndex={-1} className="panel stack reservation-create-content">
      <div className="booking-review-layout"><div className="booking-review-main">
      {state && typeof state === 'object' && (state as { authResumed?: unknown }).authResumed === true && <p className="notice">Welcome back. Your selection has been restored; current price and availability are checked below.</p>}
      {profileResumed && <p className="notice" role="status">Your current profile includes a National ID. Review the updated price and availability, then confirm your reservation.</p>}
      <ApiErrorAlert error={validation ? presentValidationError(validation) : readError} />
      <ApiErrorAlert error={actionError} />
      <ApiErrorAlert error={completing ? null : profile.error} />
      {!validation && profile.phase === 'checking' && <LoadingState label="Checking your saved profile…" />}
      {profile.phase === 'unavailable' && <button className="button button-secondary" disabled={working} type="button" onClick={() => void profile.recheck()}>Reload Profile</button>}
      {profile.phase === 'missing' && deferred && <p className="notice" role="status">Add your National ID when you are ready to continue. Your selection has not created a reservation or held capacity.</p>}
      {loading ? <LoadingState label="Checking current price and availability…" /> : <>
        {!validation && !readError && !unitType && <p className="notice" role="status">Your selected storage is no longer available at this facility for every requested month. Return to Find Storage to choose another option.</p>}
        {facility && unitType && <div className="reservation-create-selection"><div className="booking-selection-overview"><FacilityImage facility={facility} className="booking-selection-image" /><div className="dialog-selection"><span className="unit-icon-tile"><FlowIcon name="unit" /></span><div><strong>{unitType.name}</strong><small>{facility.name}</small></div></div></div>
          <dl className="detail-list"><div><dt>Facility</dt><dd>{facility.name}</dd></div><div><dt>Address</dt><dd>{facility.address}</dd></div><div><dt>Unit Type</dt><dd>{unitType.name}</dd></div><div><dt>Storage Type / Size</dt><dd>{unitType.mode === 'PRIVATE' ? 'Private Storage' : 'Public Storage'} · {unitType.size}</dd></div><div><dt>Rental Period</dt><dd>{formatMonthRange(request.startMonth, request.endMonth)}</dd></div><div><dt>Current Listed Rent</dt><dd>{formatMoney(unitType.rentalPrice)}/month</dd></div></dl>
        </div>}
        {facility && unitType && <div className="remaining-info"><FlowIcon name="info" /><p>Your locked rental price and deposit amount will be shown after the reservation is created. Availability can change until the server accepts your reservation.</p></div>}
      </>}
      </div><aside className="booking-review-summary" aria-label="Reservation summary">
      <h2>Your reservation</h2>
      {unitType && <p className="booking-summary-price">{formatMoney(unitType.rentalPrice)}<small>Current listed rent / month</small></p>}
      <p>{formatMonthRange(request.startMonth, request.endMonth)}</p>
      <p>Your reservation is created only when you confirm. The locked rental price and deposit will appear in your reservation details.</p>
      <div className="reservation-create-actions">{available && <button className="button reservation-create-submit" disabled={working || loading || needsRefresh || profile.phase === 'checking' || profile.phase === 'unavailable'} onClick={profile.phase === 'missing' ? () => { setDeferred(false); setCompletionRequested(true) } : () => void handleCreate()} type="button">{submitting ? 'Checking and creating reservation…' : profile.phase === 'missing' ? 'Add National ID to Continue' : 'Confirm Reservation'}</button>}{!validation && <button className="button button-secondary" disabled={working || loading || profile.phase === 'checking'} type="button" onClick={refresh}>Refresh Selection</button>}<Link className="button button-secondary" to={backUrl} aria-disabled={working || undefined} onClick={event => { if (working) event.preventDefault() }}>Back to Find Storage</Link></div>
      {needsRefresh && <p className="muted">Refresh your selection and review the current result before submitting again.</p>}
      </aside></div>
    </section>}
  </CustomerActionFrame>
  {!inDialog && completing && createPortal(<FlowDialog className="reservation-cccd-dialog reservations-editorial-dialog" trapFocus title="Complete Your Profile" category="Reservation" subtitle="Add your National ID, then return to review your reservation." busy={profile.saving} onClose={deferCompletion}>{completionForm}</FlowDialog>, document.body)}
  </>
}
