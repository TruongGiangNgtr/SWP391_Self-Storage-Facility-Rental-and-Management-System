import { type FormEvent, useEffect, useState } from 'react'
import { Link, useLocation, useParams } from 'react-router-dom'
import { ArrowLeft, ArrowRight, CalendarDays, CreditCard, PackageCheck, ReceiptText, XCircle } from 'lucide-react'
import { presentApiError, presentValidationError } from '../api/apiErrorPresentation'
import type { ApiErrorPresentation } from '../api/apiErrorPresentation'
import { billingApi } from '../api/billingApi'
import { reservationApi } from '../api/reservationApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { CustomerStatusBadge as StatusBadge } from '../components/CustomerStatusBadge'
import { FirstMonthHandoverInfo } from '../components/FirstMonthHandoverInfo'
import { paymentReturnState } from '../features/billing/paymentReturnState'
import type { ReservationDetail } from '../features/reservations/reservation.types'
import { getReservationStatusLabel } from '../features/reservations/reservationPresentation'
import { useReservationUnitTypes } from '../features/reservations/useReservationUnitTypes'
import { formatMoney, formatMonthRange, formatUtcDateTime } from '../utils/formatters'
import '../styles/customer-reservation-details.css'

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
  const unitTypes = useReservationUnitTypes(reservation ? [reservation.unitTypeId] : [])

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
      const response = await billingApi.startPayOsPayment(
        reservation.depositInvoice.invoiceId,
        crypto.randomUUID(),
      )
      const paymentUrl = response.data.paymentUrl ? getSafePaymentUrl(response.data.paymentUrl) : null
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
    <main className="page-container flow-page customer-reservation-details">
      <Link className="reservation-detail-text-link reservation-detail-back" to="/customer/reservations"><ArrowLeft size={16} aria-hidden="true" />My Reservations</Link>
      <header className="page-heading reservation-detail-heading">
        <p className="eyebrow">Reservation Management</p>
        <h1>Reservation Details</h1>
        <p className="muted">Review your rental period, deposit invoice and handover visit.</p>
      </header>

      <ApiErrorAlert error={error} />
      <ApiErrorAlert error={unitTypes.error} />
      {unitTypes.error && <button className="button button-secondary" type="button" disabled={unitTypes.loading} onClick={unitTypes.retry}>Retry Unit Type Name</button>}
      {reservation && (
        <>
          <div className="reservation-detail-columns">
          <div className="reservation-detail-stack">
          <section className={`panel reservation-detail-card reservation-detail-summary reservation-detail-${reservation.status.toLowerCase().replaceAll('_', '-')}`}>
            <header className="reservation-detail-card-heading">
              <span className="reservation-detail-icon"><PackageCheck size={24} aria-hidden="true" /></span>
              <h2>Storage Reservation</h2>
              <StatusBadge status={reservation.status} label={getReservationStatusLabel(reservation)} />
            </header>
            <dl className="reservation-detail-facts">
              <div className="reservation-detail-unit-type"><dt>Unit Type</dt><dd>{unitTypes.reads[reservation.unitTypeId]?.data?.name ?? (unitTypes.reads[reservation.unitTypeId]?.error ? 'Unit type unavailable' : 'Loading unit type…')}</dd></div>
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

          {(reservation.status === 'CONFIRMED' || reservation.status === 'COMPLETED') && (
            <div className="reservation-detail-first-month"><FirstMonthHandoverInfo lockedRentalPrice={reservation.lockedRentalPrice} handoverCompleted={reservation.status === 'COMPLETED'} /></div>
          )}
          </div>

          <div className="reservation-detail-stack">
          <section className={`panel reservation-detail-card reservation-detail-invoice reservation-invoice-${reservation.depositInvoice.status.toLowerCase()}`}>
            <header className="reservation-detail-card-heading">
              <ReceiptText size={20} aria-hidden="true" />
              <h2>Deposit Invoice</h2>
              <StatusBadge status={reservation.depositInvoice.status} />
            </header>
            <dl className="reservation-detail-invoice-facts">
              <div className="reservation-detail-invoice-amount">
                <dt>Amount</dt>
                <dd>{formatMoney(reservation.depositInvoice.amountDue)}</dd>
              </div>
              <div>
                <dt>Payment Deadline</dt>
                <dd>{formatUtcDateTime(reservation.depositInvoice.dueDate)}</dd>
              </div>
            </dl>
            <div className="reservation-detail-invoice-actions">
              <Link
                className="button button-secondary"
                to={`/customer/invoices/${reservation.depositInvoice.invoiceId}`}
              >
                View Invoice<ArrowRight size={15} aria-hidden="true" />
              </Link>
              {reservation.status === 'PENDING_DEPOSIT' &&
                reservation.depositInvoice.status === 'UNPAID' && (
                  <button
                    className="button"
                    disabled={working}
                    onClick={handleStartPayment}
                    type="button"
                  >
                    <CreditCard size={16} aria-hidden="true" />
                    {working ? 'Connecting to payOS...' : 'Pay Deposit with payOS'}
                  </button>
                )}
              {reservation.status === 'PENDING_DEPOSIT' &&
                reservation.depositInvoice.status === 'PAID' && (
                  <Link
                    className="button"
                    to={`/customer/reservations/${reservation.reservationId}/confirm`}
                    state={{ backgroundLocation: location }}
                  >
                    <CalendarDays size={16} aria-hidden="true" />
                    Confirm and Schedule Handover
                  </Link>
                )}
            </div>
          </section>

          {reservation.reservationVisit && (
            <section className="panel reservation-detail-card reservation-detail-visit">
              <header className="reservation-detail-card-heading">
                <CalendarDays size={20} aria-hidden="true" />
                <h2>Handover Visit</h2>
                <StatusBadge status={reservation.reservationVisit.status} />
              </header>
              <p className="reservation-detail-visit-date">Visit Date: <strong>{reservation.reservationVisit.visitDate}</strong></p>
              <Link
                className="button button-secondary"
                to={`/customer/visits/${reservation.reservationVisit.visitId}`}
              >
                Manage Visit<ArrowRight size={15} aria-hidden="true" />
              </Link>
            </section>
          )}
          </div>
          </div>

          {(reservation.status === 'PENDING_DEPOSIT' || reservation.status === 'CONFIRMED') && (
            <section className="panel reservation-detail-card reservation-detail-cancel" aria-labelledby="reservation-cancel-heading">
              <div className="reservation-detail-card-heading"><span className="reservation-detail-icon"><XCircle size={24} aria-hidden="true" /></span><h2 id="reservation-cancel-heading">Reservation Cancellation</h2></div>
              {!showCancelForm ? (
                <button
                  className="button button-danger"
                  disabled={working}
                  onClick={() => setShowCancelForm(true)}
                  type="button"
                >
                  <XCircle size={16} aria-hidden="true" />
                  Cancel Reservation
                </button>
              ) : (
                <form className="form-grid reservation-detail-cancel-form" onSubmit={handleCancel}>
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
