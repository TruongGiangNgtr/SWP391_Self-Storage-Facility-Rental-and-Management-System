import { Link } from 'react-router-dom'
import type { InvoiceDetail } from '../features/billing/billing.types'

export function CustomerInvoiceContext({ invoice }: { invoice: InvoiceDetail }) {
  const deposit = invoice.invoiceType === 'DEPOSIT'
  return <div className="rental-context">
    <span>{deposit ? 'Reservation deposit' : `Rental month: ${invoice.billingMonth ?? 'Not recorded'}`}</span>
    <Link to={`/customer/${deposit ? 'reservations' : 'contracts'}/${encodeURIComponent(invoice.entityId)}`}>
      {deposit ? 'View Related Reservation' : 'View Related Contract'}
    </Link>
  </div>
}
