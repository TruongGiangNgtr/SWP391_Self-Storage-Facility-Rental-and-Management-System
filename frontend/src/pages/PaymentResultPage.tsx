import { useCallback, useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft, ArrowRight, CircleCheck, CircleX, Clock3, Info, ReceiptText } from 'lucide-react'
import { presentApiError, presentValidationError } from '../api/apiErrorPresentation'
import type { ApiErrorPresentation } from '../api/apiErrorPresentation'
import { billingApi } from '../api/billingApi'
import { authToken } from '../api/authToken'
import { useAuth } from '../auth/auth.context'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { CustomerStatusBadge as StatusBadge } from '../components/CustomerStatusBadge'
import type { PaymentDetail } from '../features/billing/billing.types'
import { isPaymentForInvoice } from '../features/billing/invoicePayment'
import {
  paymentReturnState,
  type PendingPaymentReturn,
} from '../features/billing/paymentReturnState'
import { formatMoney, formatUtcDateTime } from '../utils/formatters'
import '../styles/guest-customer-remaining.css'

const MAX_AUTOMATIC_CHECKS = 6

export function PaymentResultPage() {
  const { user } = useAuth()
  return <PaymentResult key={user?.userAccountId} userAccountId={user?.userAccountId ?? ''} />
}

function PaymentResult({ userAccountId }: { userAccountId: string }) {
  const checking = useRef(false)
  const active = useRef(false)
  const [pendingPayment] = useState<PendingPaymentReturn | null>(() => {
    const pending = paymentReturnState.get()
    return pending?.invoiceId && pending.userAccountId !== userAccountId ? null : pending
  })
  const invoiceReturn = Boolean(pendingPayment?.invoiceId)
  const listPath = invoiceReturn ? '/customer/invoices?tab=invoices' : '/customer/reservations'
  const backPath = pendingPayment?.invoiceId
    ? `/customer/invoices/${encodeURIComponent(pendingPayment.invoiceId)}`
    : pendingPayment ? `/customer/reservations/${encodeURIComponent(pendingPayment.reservationId!)}` : listPath
  const [payment, setPayment] = useState<PaymentDetail | null>(null)
  const [loading, setLoading] = useState(Boolean(pendingPayment))
  const [checkCount, setCheckCount] = useState(0)
  const [error, setError] = useState<ApiErrorPresentation | null>(
    pendingPayment
      ? null
      : presentValidationError(
          'We could not identify the payment on this page. Open your reservation or invoice to check its payment status before paying again.',
        ),
  )

  const checkPayment = useCallback(async () => {
    if (!pendingPayment || checking.current) {
      return
    }

    checking.current = true
    const token = authToken.get()
    try {
      const response = await billingApi.getPayment(pendingPayment.paymentId)
      if (!active.current || authToken.get() !== token) return
      if (!isPaymentForInvoice(response.data, pendingPayment.paymentId, pendingPayment.invoiceId ?? response.data?.invoiceId)) {
        setError(presentValidationError('We could not confirm the payment details for this invoice. Refresh its status before paying again.'))
        return
      }
      setPayment(response.data)
      setError(null)
      setCheckCount((current) => current + 1)
    } catch (requestError) {
      if (active.current && authToken.get() === token) setError(presentApiError(requestError))
    } finally {
      checking.current = false
      if (active.current) setLoading(false)
    }
  }, [pendingPayment])

  function handleManualCheck() {
    setLoading(true)
    setError(null)
    void checkPayment()
  }

  useEffect(() => {
    active.current = true
    void checkPayment()
    return () => { active.current = false }
  }, [checkPayment])

  useEffect(() => {
    if (error || loading || payment?.status !== 'PENDING' || checkCount >= MAX_AUTOMATIC_CHECKS) {
      return
    }

    const timer = window.setTimeout(() => {
      setLoading(true)
      void checkPayment()
    }, 2000)

    return () => window.clearTimeout(timer)
  }, [checkCount, checkPayment, payment?.status, error, loading])

  return (
    <main className="page-container flow-page narrow-page customer-payment-result">
      <nav className="remaining-page-navigation" aria-label="Payment navigation"><Link to={listPath}><ArrowLeft size={16} aria-hidden="true" />{invoiceReturn ? 'My Invoices' : 'My Reservations'}</Link></nav>
      <section className="page-heading">
        <p className="eyebrow">{invoiceReturn ? 'Invoice Payment' : 'Pay Deposit'}</p>
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
        <Link className="button button-secondary" to="/customer/invoices?tab=invoices">
          View Invoices
        </Link>
      )}
      {pendingPayment && !payment && <Link className="button button-secondary" to={backPath}>{invoiceReturn ? 'Back to Invoice' : 'Back to Reservation'}</Link>}
      {loading && !payment ? (
        <LoadingState label="Checking payment status..." />
      ) : (
        payment && (
          <section className={`panel stack payment-result-card payment-result-${payment.status.toLowerCase()}`}>
            <div className="payment-result-outcome" aria-live="polite">
              <span className="payment-result-icon" aria-hidden="true">{payment.status === 'SUCCESS' ? <CircleCheck size={30} /> : payment.status === 'FAILED' ? <CircleX size={30} /> : <Clock3 size={30} />}</span>
              <h2>{payment.status === 'SUCCESS' ? 'Payment Successful' : payment.status === 'FAILED' ? 'Payment Unsuccessful' : 'Payment Pending'}</h2>
              <p>{payment.status === 'SUCCESS'
                ? invoiceReturn ? 'FRMS has recorded the payment result. Return to your invoice to check its latest status.' : 'FRMS has recorded the payment result. Return to your reservation to check the invoice and schedule handover.'
                : payment.status === 'FAILED'
                  ? invoiceReturn ? 'FRMS has recorded this payment as failed. Return to your invoice to review its latest status before trying again.' : 'FRMS has recorded this payment as failed. Return to your reservation to review the deposit invoice.'
                  : 'Your payment has not been recorded as successful yet. Check its status below.'}</p>
            </div>
            <div className="remaining-card-heading">
              <span className="remaining-icon"><ReceiptText size={22} aria-hidden="true" /></span><h3>Payment Details</h3>
              <StatusBadge status={payment.status} />
            </div>
            <dl className="remaining-facts payment-result-facts">
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
            <details className="payment-result-reference"><summary>Payment Reference</summary><p>{payment.paymentId}</p></details>

            {payment.status === 'PENDING' && (
              <div className="remaining-info payment-result-pending-note"><Info size={18} aria-hidden="true" /><p>
                {checkCount < MAX_AUTOMATIC_CHECKS && !error
                  ? 'Payment is pending. The system will check again automatically.'
                  : `Payment is still pending. You can check again or return to your ${invoiceReturn ? 'invoice' : 'reservation'}.`}
              </p></div>
            )}

            <div className="remaining-actions payment-result-actions">
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
                  to={backPath}
                >
                  {invoiceReturn ? 'Back to Invoice' : 'Back to Reservation'}
                  <ArrowRight size={16} aria-hidden="true" />
                </Link>
              )}
            </div>
          </section>
        )
      )}
    </main>
  )
}
