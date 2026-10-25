import { useEffect, useRef, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ArrowLeft, BadgeCheck, CalendarDays, CalendarRange, Clock3, CreditCard, ReceiptText, RefreshCw, Wallet } from 'lucide-react'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { billingApi } from '../api/billingApi'
import { authToken } from '../api/authToken'
import { useAuth } from '../auth/auth.context'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { CustomerStatusBadge as StatusBadge } from '../components/CustomerStatusBadge'
import { CustomerInvoiceContext } from '../components/CustomerInvoiceContext'
import { usePublicRead } from '../features/catalog/usePublicCatalog'
import { isBillingInvoice } from '../features/billing/useBillingCollection'
import { canPayInvoice, isInvoicePayment, secureCheckoutUrl } from '../features/billing/invoicePayment'
import { paymentReturnState } from '../features/billing/paymentReturnState'
import { isUuid } from '../features/handover/handoverValidation'
import { formatMoney, formatUtcDateTime } from '../utils/formatters'
import '../styles/customer-invoice-details.css'

const readInvoice = (invoiceId: string) => billingApi.getInvoice(invoiceId)

export function InvoiceDetailPage() {
  const { invoiceId = '' } = useParams()
  const { user } = useAuth()
  return <InvoiceDetails key={`${user?.userAccountId}:${invoiceId}`} invoiceId={invoiceId} />
}

function InvoiceDetails({ invoiceId }: { invoiceId: string }) {
  const { user } = useAuth()
  const navigate = useNavigate()
  const [working, setWorking] = useState(false)
  const [paymentError, setPaymentError] = useState<ApiErrorPresentation | null>(null)
  const busy = useRef(false)
  const active = useRef(false)
  const attemptKey = useRef<string | null>(null)
  useEffect(() => { active.current = true; return () => { active.current = false } }, [])
  // Reuse the keyed reader with authenticated billingApi, not a public request.
  const validId = isUuid(invoiceId)
  const { data: response, loading, error, refresh } = usePublicRead(invoiceId, readInvoice, validId, presentApiError)
  const invoice = response?.data && isBillingInvoice(response.data, invoiceId) ? response.data : null
  const eligibleStatus = invoice && ['UNPAID', 'OVERDUE'].includes(invoice.status)
  const customerActive = user?.role === 'CUSTOMER' && user.status === 'ACTIVE'

  async function startPayment() {
    if (busy.current || loading || !invoice || !canPayInvoice(invoice) || !customerActive || !user) return
    busy.current = true; setWorking(true); setPaymentError(null)
    // An uncertain request is retried with this same key, never automatically.
    attemptKey.current ??= crypto.randomUUID()
    const token = authToken.get()
    try {
      const { data } = await billingApi.startPayOsPayment(invoice.invoiceId, attemptKey.current)
      if (!active.current || authToken.get() !== token) return
      if (!isInvoicePayment(data, invoice)) {
        setPaymentError(presentValidationError('We could not confirm this payment session. Refresh the invoice before trying again.'))
        refresh()
        return
      }
      paymentReturnState.set({ paymentId: data.paymentId, invoiceId: invoice.invoiceId, userAccountId: user.userAccountId })
      const checkoutUrl = data.status === 'PENDING' ? secureCheckoutUrl(data.paymentUrl) : null
      if (checkoutUrl) window.location.assign(checkoutUrl)
      else navigate('/customer/payments/result')
    } catch (caught) {
      if (active.current && authToken.get() === token) {
        setPaymentError(presentApiError(caught))
        refresh()
      }
    } finally {
      busy.current = false
      if (active.current) setWorking(false)
    }
  }

  return (
    <main className="page-container flow-page customer-invoice-details">
      <div className="invoice-detail-navigation">
        <Link className="invoice-detail-text-link" to="/customer/invoices">
          <ArrowLeft size={16} aria-hidden="true" />My Invoices
        </Link>
        <button className="invoice-detail-text-button" type="button" disabled={loading || working} onClick={refresh}>
          <RefreshCw size={14} aria-hidden="true" />{loading ? 'Refreshing…' : 'Refresh Invoice'}
        </button>
      </div>
      <header className="page-heading invoice-detail-heading">
        <p className="eyebrow">Deposit &amp; Invoices</p>
        <h1>Invoice Details</h1>
        <p className="muted">Review your invoice amounts, billing details and recorded payment status.</p>
      </header>

      <ApiErrorAlert error={error} />
      <ApiErrorAlert error={paymentError} />
      {(!validId || (response && !invoice)) && <ApiErrorAlert error={presentValidationError('We could not load a valid invoice. Return to My Invoices or refresh before paying.')} />}
      {paymentError && <p className="notice" role="note">Review the latest invoice before trying again. No payment is automatically resubmitted.</p>}
      {loading ? (
        <LoadingState />
      ) : (
        invoice && (
          <section className={`panel invoice-detail-card invoice-detail-${invoice.status.toLowerCase()}`} aria-label="Invoice information">
            <header className="invoice-detail-card-heading">
              <span className="invoice-detail-icon"><ReceiptText size={24} aria-hidden="true" /></span>
              <h2>{invoice.invoiceType === 'DEPOSIT' ? 'Deposit Invoice' : 'Rental Invoice'}</h2>
              <StatusBadge status={invoice.status} />
            </header>
            <CustomerInvoiceContext invoice={invoice} />
            <div className="invoice-detail-columns">
              <section className="invoice-detail-amounts" aria-labelledby="invoice-amount-heading">
                <h3 id="invoice-amount-heading"><Wallet size={18} aria-hidden="true" />Amount Breakdown</h3>
                <dl className="invoice-detail-breakdown">
                  <div><dt>Base Amount</dt><dd>{formatMoney(invoice.baseAmount)}</dd></div>
                  <div><dt>Discount</dt><dd>{formatMoney(invoice.discountAmount)}</dd></div>
                  <div className="invoice-detail-total"><dt>Amount Due</dt><dd>{formatMoney(invoice.amountDue)}</dd></div>
                </dl>
              </section>
              <section className="invoice-detail-billing" aria-labelledby="invoice-billing-heading">
                <h3 id="invoice-billing-heading"><CalendarRange size={18} aria-hidden="true" />Billing Details</h3>
                <dl className="invoice-detail-facts">
                  <div><dt><CalendarDays size={16} aria-hidden="true" />Billing Month</dt><dd>{invoice.billingMonth ?? 'Not Applicable'}</dd></div>
                  <div><dt><Clock3 size={16} aria-hidden="true" />Payment Deadline</dt><dd>{formatUtcDateTime(invoice.dueDate)}</dd></div>
                  <div><dt><BadgeCheck size={16} aria-hidden="true" />Paid At</dt><dd>{invoice.paidAt ? formatUtcDateTime(invoice.paidAt) : 'Not Recorded'}</dd></div>
                </dl>
              </section>
            </div>
            {eligibleStatus && <footer className="invoice-detail-payment">
              <div><h3>Pay with payOS</h3><p>You will continue to payOS to complete payment. Return here to check the status recorded by FRMS.</p>
                {!canPayInvoice(invoice) && <p role="status">This invoice amount cannot be paid online. Contact the facility for help.</p>}
              </div>
              <button className="button" type="button" disabled={working || !customerActive || !canPayInvoice(invoice)} onClick={() => void startPayment()}>
                <CreditCard size={18} aria-hidden="true" />{working ? 'Connecting to payOS…' : 'Pay with payOS'}
              </button>
            </footer>}
          </section>
        )
      )}
    </main>
  )
}
