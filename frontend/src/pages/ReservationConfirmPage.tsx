import { type FormEvent, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { CalendarDays } from 'lucide-react'
import { presentApiError } from '../api/apiErrorPresentation'
import type { ApiErrorPresentation } from '../api/apiErrorPresentation'
import { reservationApi } from '../api/reservationApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { CustomerActionFrame } from '../components/CustomerActionFrame'
import { FlowIcon } from '../components/FlowIcon'
import { CalendarDateField } from '../components/CalendarDateField'
import { CustomerStatusBadge } from '../components/CustomerStatusBadge'
import type { ReservationDetail } from '../features/reservations/reservation.types'
import { formatMoney, formatMonthRange } from '../utils/formatters'
import '../styles/customer-handover-schedule.css'

export function ReservationConfirmPage() {
  const { reservationId = '' } = useParams()
  const navigate = useNavigate()
  const [reservation, setReservation] = useState<ReservationDetail | null>(null)
  const [visitDate, setVisitDate] = useState('')
  const [dateError, setDateError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<ApiErrorPresentation | null>(null)

  useEffect(() => {
    let active = true
    reservationApi
      .get(reservationId)
      .then((response) => {
        if (active) {
          setReservation(response.data)
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
  }, [reservationId])

  async function handleConfirm(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!visitDate) {
      setDateError('Please select a handover date.')
      return
    }

    setSubmitting(true)
    setDateError(null)
    setError(null)
    try {
      const response = await reservationApi.confirm(reservationId, {
        reservationVisitDate: visitDate,
      })
      navigate(`/customer/visits/${response.data.reservationVisit.visitId}`, {
        replace: true,
      })
    } catch (requestError) {
      setError(presentApiError(requestError))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <CustomerActionFrame
      className="customer-handover-schedule"
      headingIcon={<CalendarDays size={24} />}
      category="Handover Visit"
      title="Confirm Reservation and Schedule Handover"
      subtitle="Select a date to collect your storage unit after paying the deposit."
      busy={submitting}
      returnTo={`/customer/reservations/${reservationId}`}
      backLabel="Back to Reservation"
    >
      {loading ? <LoadingState label="Checking deposit status..." /> :
      <section className="panel stack handover-schedule-content">
        <ApiErrorAlert error={error} />
        {reservation && (
          <>
            <dl className="handover-schedule-facts">
              <div className="handover-schedule-period">
                <dt>Rental Period</dt>
                <dd>{formatMonthRange(reservation.startMonth, reservation.endMonth)}</dd>
              </div>
              <div>
                <dt>Deposit</dt>
                <dd>{formatMoney(reservation.depositAmount)}</dd>
              </div>
              <div>
                <dt>Invoice Status</dt>
                <dd><CustomerStatusBadge status={reservation.depositInvoice.status} /></dd>
              </div>
            </dl>

            {reservation.status === 'PENDING_DEPOSIT' &&
            reservation.depositInvoice.status === 'PAID' ? (
              <form className="handover-schedule-form" onSubmit={handleConfirm}>
                <div className="handover-schedule-type"><FlowIcon name="visit-type" />Handover · RESERVATION</div>
                <CalendarDateField
                  id="reservationVisitDate"
                  label="Preferred Handover Date"
                  value={visitDate}
                  initialMonth={reservation.startMonth}
                  disabled={submitting}
                  error={dateError}
                  onChange={value => { setVisitDate(value); setDateError(null) }}
                />
                <div className="handover-schedule-notice"><FlowIcon name="info" /><p>The handover date must follow the reservation policy. Please bring identification to the facility.</p></div>
                <div className="handover-schedule-actions">
                  <Link
                    className="button button-secondary"
                    to={`/customer/reservations/${reservationId}`}
                  >
                    Back
                  </Link>
                  <button className="button" disabled={submitting} type="submit">
                    <FlowIcon name="visit-confirm" />
                    {submitting ? 'Confirming...' : 'Confirm Reservation and Visit'}
                  </button>
                </div>
              </form>
            ) : (
              <>
                <div className="handover-schedule-notice handover-schedule-ineligible">
                  <FlowIcon name="info" />
                  <p>This reservation is not eligible for confirmation. Check the reservation status and
                  deposit payment before selecting a handover date.</p>
                </div>
                <Link className="button button-secondary handover-schedule-back" to={`/customer/reservations/${reservationId}`}>
                  Back to Reservation
                </Link>
              </>
            )}
          </>
        )}
      </section>}
    </CustomerActionFrame>
  )
}
