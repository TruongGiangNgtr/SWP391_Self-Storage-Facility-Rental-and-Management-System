import { type FormEvent, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { presentApiError, presentValidationError } from '../api/apiErrorPresentation'
import type { ApiErrorPresentation } from '../api/apiErrorPresentation'
import { reservationApi } from '../api/reservationApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { FlowDialog } from '../components/FlowDialog'
import { FlowIcon } from '../components/FlowIcon'
import { CalendarDateField } from '../components/CalendarDateField'
import type { ReservationDetail } from '../features/reservations/reservation.types'
import { formatMoney, formatMonthRange } from '../utils/formatters'

export function ReservationConfirmPage() {
  const { reservationId = '' } = useParams()
  const navigate = useNavigate()
  const [reservation, setReservation] = useState<ReservationDetail | null>(null)
  const [visitDate, setVisitDate] = useState('')
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
      setError(presentValidationError('Please select a handover date.'))
      return
    }

    setSubmitting(true)
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

  if (loading) {
    return (
      <main className="page-container flow-page">
        <LoadingState label="Checking deposit status..." />
      </main>
    )
  }

  return (
    <FlowDialog
      category="Handover Visit"
      title="Confirm Reservation and Schedule Handover"
      subtitle="Select a date to collect your storage unit after paying the deposit."
      busy={submitting}
      onClose={() => navigate(`/customer/reservations/${reservationId}`, { replace: true })}
    >

      <section className="panel stack">
        <ApiErrorAlert error={error} />
        {reservation && (
          <>
            <dl className="detail-list compact">
              <div>
                <dt>Rental Period</dt>
                <dd>{formatMonthRange(reservation.startMonth, reservation.endMonth)}</dd>
              </div>
              <div>
                <dt>Deposit</dt>
                <dd>{formatMoney(reservation.depositAmount)}</dd>
              </div>
              <div>
                <dt>Invoice Status</dt>
                <dd>{reservation.depositInvoice.status}</dd>
              </div>
            </dl>

            {reservation.status === 'PENDING_DEPOSIT' &&
            reservation.depositInvoice.status === 'PAID' ? (
              <form className="form-grid" onSubmit={handleConfirm}>
                <div className="flow-visit-type"><FlowIcon name="visit-type" />Handover · RESERVATION</div>
                <CalendarDateField id="reservationVisitDate" label="Preferred Handover Date" value={visitDate} initialMonth={reservation.startMonth} disabled={submitting} onChange={setVisitDate} />
                <div className="notice"><FlowIcon name="info" />The handover date must follow the reservation policy. Please bring identification to the facility.</div>
                <div className="action-row">
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
                <div className="notice">
                  This reservation is not eligible for confirmation. Check the reservation status and
                  deposit payment before selecting a handover date.
                </div>
                <Link className="button button-secondary" to={`/customer/reservations/${reservationId}`}>
                  Back to Reservation
                </Link>
              </>
            )}
          </>
        )}
      </section>
    </FlowDialog>
  )
}
