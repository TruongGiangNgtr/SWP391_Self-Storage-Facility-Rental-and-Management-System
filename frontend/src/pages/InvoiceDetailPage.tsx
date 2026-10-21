import { Link, useParams } from 'react-router-dom'
import { ArrowLeft, BadgeCheck, CalendarDays, CalendarRange, Clock3, ReceiptText, RefreshCw, Wallet } from 'lucide-react'
import { presentApiError } from '../api/apiErrorPresentation'
import { billingApi } from '../api/billingApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { LoadingState } from '../components/PageStates'
import { CustomerStatusBadge as StatusBadge } from '../components/CustomerStatusBadge'
import { CustomerInvoiceContext } from '../components/CustomerInvoiceContext'
import { usePublicRead } from '../features/catalog/usePublicCatalog'
import { formatMoney, formatUtcDateTime } from '../utils/formatters'
import '../styles/customer-invoice-details.css'

const readInvoice = (invoiceId: string) => billingApi.getInvoice(invoiceId)

export function InvoiceDetailPage() {
  const { invoiceId = '' } = useParams()
  // Reuse the keyed reader with authenticated billingApi, not a public request.
  const { data: response, loading, error, refresh } = usePublicRead(invoiceId, readInvoice, true, presentApiError)
  const invoice = response?.data

  return (
    <main className="page-container flow-page customer-invoice-details">
      <div className="invoice-detail-navigation">
        <Link className="invoice-detail-text-link" to="/customer/invoices">
          <ArrowLeft size={16} aria-hidden="true" />My Invoices
        </Link>
        <button className="invoice-detail-text-button" type="button" disabled={loading} onClick={refresh}>
          <RefreshCw size={14} aria-hidden="true" />{loading ? 'Refreshing…' : 'Refresh Invoice'}
        </button>
      </div>
      <header className="page-heading invoice-detail-heading">
        <p className="eyebrow">Deposit &amp; Invoices</p>
        <h1>Invoice Details</h1>
        <p className="muted">Review your invoice amounts, billing details and recorded payment status.</p>
      </header>

      <ApiErrorAlert error={error} />
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
          </section>
        )
      )}
    </main>
  )
}
