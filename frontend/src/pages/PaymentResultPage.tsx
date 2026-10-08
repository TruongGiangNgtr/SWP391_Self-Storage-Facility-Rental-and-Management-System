import { useCallback, useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { presentApiError, presentValidationError } from '../api/apiErrorPresentation'
import type { ApiErrorPresentation } from '../api/apiErrorPresentation'
import { billingApi } from '../api/billingApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { StatusBadge } from '../components/StatusBadge'
import { FlowIcon } from '../components/FlowIcon'
import type { PaymentDetail } from '../features/billing/billing.types'
import {
  paymentReturnState,
  type PendingPaymentReturn,
} from '../features/billing/paymentReturnState'
import { formatMoney, formatUtcDateTime } from '../utils/formatters'

const MAX_AUTOMATIC_CHECKS = 6

export function PaymentResultPage() {
  const checking = useRef(false)
  const [pendingPayment] = useState<PendingPaymentReturn | null>(() =>
    paymentReturnState.get(),
  )
  const [payment, setPayment] = useState<PaymentDetail | null>(null)
  const [loading, setLoading] = useState(Boolean(pendingPayment))
  const [checkCount, setCheckCount] = useState(0)
  const [error, setError] = useState<ApiErrorPresentation | null>(
    pendingPayment
      ? null
      : presentValidationError(
          'No payment reference was found in this browser session.',
        ),
  )

  const checkPayment = useCallback(async () => {
    if (!pendingPayment || checking.current) {
      return
    }

    checking.current = true
    try {
      const response = await billingApi.getPayment(pendingPayment.paymentId)
      setPayment(response.data)
      setError(null)
      setCheckCount((current) => current + 1)
    } catch (requestError) {
      setError(presentApiError(requestError))
    } finally {
      checking.current = false
      setLoading(false)
    }
  }, [pendingPayment])

  function handleManualCheck() {
    setLoading(true)
    setError(null)
    void checkPayment()
  }

  useEffect(() => {
    if (!pendingPayment) {
      return
    }

    let active = true
    billingApi
      .getPayment(pendingPayment.paymentId)
      .then((response) => {
        if (active) {
          setPayment(response.data)
          setCheckCount(1)
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
  }, [pendingPayment])

  useEffect(() => {
    if (payment?.status !== 'PENDING' || checkCount >= MAX_AUTOMATIC_CHECKS) {
      return
    }

    const timer = window.setTimeout(() => {
      setLoading(true)
      void checkPayment()
    }, 2000)

    return () => window.clearTimeout(timer)
  }, [checkCount, checkPayment, payment?.status])

  return (
    <main className="page-container flow-page narrow-page">
      <section className="page-heading">
        <p className="eyebrow">Pay Deposit</p>
        <h1>Payment Result</h1>
        <p className="muted">
          Check the payment status recorded by FRMS after paying with payOS.
        </p>
      </section>

      <ApiErrorAlert error={error} />
      {pendingPayment && error && !payment && (
        <button className="button" disabled={loading} onClick={handleManualCheck} type="button">
          {loading ? 'Checking...' : 'Check Payment Status'}
        </button>
      )}
      {!pendingPayment && (
        <Link className="button button-secondary" to="/customer/reservations">
          View Reservation
        </Link>
      )}
      {loading && !payment ? (
        <LoadingState label="Checking payment status..." />
      ) : (
        payment && (
          <section className="panel stack">
            {payment.status === 'SUCCESS' && (
              <div className="payment-success-heading"><span><FlowIcon name="paid" /></span><h2>Payment Successful</h2><p className="muted">FRMS has recorded the payment result. Return to your reservation to check the invoice and schedule handover.</p></div>
            )}
            <div className="card-heading-row">
              <h2>Payment {payment.paymentId}</h2>
              <StatusBadge status={payment.status} />
            </div>
            <dl className="detail-list compact">
              <div>
                <dt>Amount</dt>
                <dd>{formatMoney(payment.amount)}</dd>
              </div>
              <div>
                <dt>Payment Method</dt>
                <dd>{payment.paymentMethod}</dd>
              </div>
              <div>
                <dt>Transaction Reference</dt>
                <dd>{payment.transactionCode ?? 'Not Available'}</dd>
              </div>
              <div>
                <dt>Paid At</dt>
                <dd>{payment.paidAt ? formatUtcDateTime(payment.paidAt) : 'Not Recorded'}</dd>
              </div>
            </dl>

            {payment.status === 'PENDING' && (
              <div className="notice">
                {checkCount < MAX_AUTOMATIC_CHECKS && !error
                  ? 'Payment is pending. The system will check again automatically.'
                  : 'Payment is still pending. You can check again or return to your reservation.'}
              </div>
            )}

            <div className="action-row">
              {payment.status === 'PENDING' && (
                <button
                  className="button"
                  disabled={loading}
                  onClick={handleManualCheck}
                  type="button"
                >
                  {loading ? 'Checking...' : 'Check Payment Status'}
                </button>
              )}
              {pendingPayment && (
                <Link
                  className="button button-secondary"
                  to={`/customer/reservations/${pendingPayment.reservationId}`}
                >
                  Back to Reservation
                </Link>
              )}
            </div>
          </section>
        )
      )}
    </main>
  )
}
