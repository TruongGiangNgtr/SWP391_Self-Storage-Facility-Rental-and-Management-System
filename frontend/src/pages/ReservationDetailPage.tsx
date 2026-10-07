import { type FormEvent, useEffect, useState } from 'react'
import { Link, useLocation, useParams } from 'react-router-dom'
import { presentApiError, presentValidationError } from '../api/apiErrorPresentation'
import type { ApiErrorPresentation } from '../api/apiErrorPresentation'
import { billingApi } from '../api/billingApi'
import { reservationApi } from '../api/reservationApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { StatusBadge } from '../components/StatusBadge'
import { FirstMonthHandoverInfo } from '../components/FirstMonthHandoverInfo'
import { paymentReturnState } from '../features/billing/paymentReturnState'
import type { ReservationDetail } from '../features/reservations/reservation.types'
import { formatMoney, formatMonthRange, formatUtcDateTime } from '../utils/formatters'

function getSafePaymentUrl(value: string): string | null {
  try {
    const url = new URL(value)
    return url.protocol === 'https:' || url.protocol === 'http:' ? url.toString() : null
  } catch {
    return null
  }
}

export function ReservationDetailPage() {
  const location = useLocation()
  const { reservationId = '' } = useParams()
  const [reservation, setReservation] = useState<ReservationDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [working, setWorking] = useState(false)
  const [cancelReason, setCancelReason] = useState('')
  const [showCancelForm, setShowCancelForm] = useState(false)
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

  async function handleStartPayment() {
    if (!reservation) {
      return
    }

    setWorking(true)
    setError(null)
    try {
      const response = await billingApi.startMomoPayment(
        reservation.depositInvoice.invoiceId,
        { returnUrl: `${window.location.origin}/customer/payments/result` },
      )
      const paymentUrl = getSafePaymentUrl(response.data.paymentUrl)
      if (!paymentUrl) {
        setError(
          presentValidationError('The payment gateway returned an invalid redirect URL.'),
        )
        return
      }

      paymentReturnState.set({
        paymentId: response.data.paymentId,
        reservationId: reservation.reservationId,
      })
      window.location.assign(paymentUrl)
    } catch (requestError) {
      setError(presentApiError(requestError))
    } finally {
      setWorking(false)
    }
  }

  async function handleCancel(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!cancelReason.trim()) {
      setError(presentValidationError('Please enter a reason for cancelling the reservation.'))
      return
    }

    setWorking(true)
    setError(null)
    try {
      const response = await reservationApi.cancel(reservationId, {
        reason: cancelReason.trim(),
      })
      setReservation(response.data)
      setCancelReason('')
      setShowCancelForm(false)
    } catch (requestError) {
      setError(presentApiError(requestError))
    } finally {
      setWorking(false)
    }
  }

  if (loading) {
    return (
      <main className="page-container flow-page">
        <LoadingState label="Loading reservation..." />
      </main>
    )
  }

  return (
    <main className="page-container flow-page narrow-page">
      <section className="page-heading page-heading-actions">
        <div>
          <p className="eyebrow">My Reservations</p>
          <h1>Reservation Details</h1>
        </div>
        <Link className="button button-secondary" to="/customer/reservations">
          My Reservations
        </Link>
      </section>

      <ApiErrorAlert error={error} />
      {reservation && (
        <>
          <section className="panel stack">
            <div className="card-heading-row">
              <h2>Reservation {reservation.reservationId}</h2>
              <StatusBadge status={reservation.status} />
            </div>
            <dl className="detail-list">
              <div>
                <dt>Rental Period</dt>
                <dd>{formatMonthRange(reservation.startMonth, reservation.endMonth)}</dd>
              </div>
              <div>
                <dt>Locked Monthly Rent</dt>
                <dd>{formatMoney(reservation.lockedRentalPrice)}/month</dd>
              </div>
              <div>
                <dt>Deposit</dt>
                <dd>{formatMoney(reservation.depositAmount)}</dd>
              </div>
              <div>
                <dt>Created At</dt>
                <dd>{formatUtcDateTime(reservation.createdAt)}</dd>
              </div>
            </dl>
          </section>

          <section className="panel stack">
            <div className="card-heading-row">
              <h2>Deposit Invoice</h2>
              <StatusBadge status={reservation.depositInvoice.status} />
            </div>
            <dl className="detail-list compact">
              <div>
                <dt>Amount</dt>
                <dd>{formatMoney(reservation.depositInvoice.amountDue)}</dd>
              </div>
              <div>
                <dt>Payment Deadline</dt>
                <dd>{formatUtcDateTime(reservation.depositInvoice.dueDate)}</dd>
              </div>
            </dl>
            <div className="action-row">
              <Link
                className="button button-secondary"
                to={`/customer/invoices/${reservation.depositInvoice.invoiceId}`}
              >
                View Invoice
              </Link>
              {reservation.status === 'PENDING_DEPOSIT' &&
                reservation.depositInvoice.status === 'UNPAID' && (
                  <button
                    className="button"
                    disabled={working}
                    onClick={handleStartPayment}
                    type="button"
                  >
                    {working ? 'Connecting to MoMo...' : 'Pay Deposit with MoMo'}
                  </button>
                )}
              {reservation.status === 'PENDING_DEPOSIT' &&
                reservation.depositInvoice.status === 'PAID' && (
                  <Link
                    className="button"
                    to={`/customer/reservations/${reservation.reservationId}/confirm`}
                    state={{ backgroundLocation: location }}
                  >
                    Confirm and Schedule Handover
                  </Link>
                )}
            </div>
          </section>

          {(reservation.status === 'CONFIRMED' || reservation.status === 'COMPLETED') && (
            <FirstMonthHandoverInfo lockedRentalPrice={reservation.lockedRentalPrice} handoverCompleted={reservation.status === 'COMPLETED'} />
          )}

          {reservation.reservationVisit && (
            <section className="panel stack">
              <div className="card-heading-row">
                <h2>Handover Visit</h2>
                <StatusBadge status={reservation.reservationVisit.status} />
              </div>
              <p>Visit Date: {reservation.reservationVisit.visitDate}</p>
              <Link
                className="button button-secondary"
                to={`/customer/visits/${reservation.reservationVisit.visitId}`}
              >
                Manage Visit
              </Link>
            </section>
          )}

          {(reservation.status === 'PENDING_DEPOSIT' || reservation.status === 'CONFIRMED') && (
            <section className="danger-zone">
              {!showCancelForm ? (
                <button
                  className="button button-danger"
                  disabled={working}
                  onClick={() => setShowCancelForm(true)}
                  type="button"
                >
                  Cancel Reservation
                </button>
              ) : (
                <form className="form-grid" onSubmit={handleCancel}>
                  <div className="form-field">
                    <label htmlFor="cancelReason">Cancellation Reason</label>
                    <textarea
                      id="cancelReason"
                      rows={3}
                      value={cancelReason}
                      onChange={(event) => setCancelReason(event.target.value)}
                    />
                  </div>
                  <div className="action-row">
                    <button
                      className="button button-secondary"
                      disabled={working}
                      onClick={() => setShowCancelForm(false)}
                      type="button"
                    >
                      Keep Reservation
                    </button>
                    <button className="button button-danger" disabled={working} type="submit">
                      {working ? 'Cancelling...' : 'Confirm Cancellation'}
                    </button>
                  </div>
                </form>
              )}
            </section>
          )}
        </>
      )}
    </main>
  )
}
