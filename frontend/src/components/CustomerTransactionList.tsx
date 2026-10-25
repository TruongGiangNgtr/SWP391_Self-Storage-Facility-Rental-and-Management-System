import { ArrowRight, ReceiptText } from 'lucide-react'
import { Link } from 'react-router-dom'
import type { CustomerPaymentHistory } from '../features/customer-transactions/transaction.types'
import { formatMoney, formatUtcDateTime } from '../utils/formatters'
import { CustomerStatusBadge } from './CustomerStatusBadge'
import '../styles/customer-transactions.css'

const METHOD_LABELS = { PAYOS: 'payOS', MOMO: 'MoMo', VNPAY: 'VNPAY' }
const STATUS_LABELS = { PENDING: 'Pending', SUCCESS: 'Successful', FAILED: 'Failed' }

export function CustomerTransactionList({ items }: { items: CustomerPaymentHistory[] }) {
  return <ul className="customer-transaction-list" aria-label="Payment attempts">
    {items.map(item => <li className="panel transaction-card" key={item.paymentId}>
      <div className="transaction-heading">
        <span className="transaction-icon"><ReceiptText size={22} aria-hidden="true" /></span>
        <h2>{item.invoiceType === 'DEPOSIT' ? 'Deposit Payment' : 'Rental Fee Payment'}</h2>
        <div className="transaction-result"><strong>{formatMoney(item.amount)}</strong><CustomerStatusBadge status={item.status} label={STATUS_LABELS[item.status]} /></div>
      </div>
      <div className="transaction-details">
        <dl>
          <div><dt>Payment Method</dt><dd>{METHOD_LABELS[item.paymentMethod]}</dd></div>
          <div><dt>Created (GMT+7)</dt><dd><time dateTime={item.createdAt}>{formatUtcDateTime(item.createdAt)}</time></dd></div>
          <div><dt>Paid (GMT+7)</dt><dd>{item.paidAt ? <time dateTime={item.paidAt}>{formatUtcDateTime(item.paidAt)}</time> : 'Not recorded'}</dd></div>
        </dl>
        <Link className="transaction-invoice-link" to={`/customer/invoices/${item.invoiceId}`}>View Invoice <ArrowRight size={16} aria-hidden="true" /></Link>
      </div>
    </li>)}
  </ul>
}
