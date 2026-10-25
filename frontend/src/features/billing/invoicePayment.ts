import { isUuid } from '../handover/handoverValidation'
import type { InvoiceDetail, InvoicePayOsPaymentResponse, PaymentDetail } from './billing.types'

// Presentation guards only; PAY-001 remains authoritative for eligibility and money.
export function canPayInvoice(invoice: InvoiceDetail): boolean {
  return ['UNPAID', 'OVERDUE'].includes(invoice.status) &&
    Number.isSafeInteger(invoice.amountDue) && invoice.amountDue > 0
}

export function isInvoicePayment(value: InvoicePayOsPaymentResponse, invoice: InvoiceDetail): boolean {
  return Boolean(value && isUuid(value.paymentId) && value.invoiceId === invoice.invoiceId &&
    value.paymentMethod === 'PAYOS' && value.amount === invoice.amountDue &&
    ['PENDING', 'SUCCESS', 'FAILED'].includes(value.status) &&
    (value.paymentUrl === null || typeof value.paymentUrl === 'string'))
}

export function secureCheckoutUrl(value: string | null): string | null {
  if (!value) return null
  try {
    const url = new URL(value)
    return url.protocol === 'https:' && !url.username && !url.password ? url.href : null
  } catch {
    return null
  }
}

export function isPaymentForInvoice(value: PaymentDetail, paymentId: string, invoiceId: string): boolean {
  return Boolean(value && value.paymentId === paymentId && value.invoiceId === invoiceId &&
    ['PENDING', 'SUCCESS', 'FAILED'].includes(value.status) &&
    ['PAYOS', 'MOMO', 'VNPAY'].includes(value.paymentMethod) &&
    Number.isFinite(value.amount) && value.amount >= 0 &&
    typeof value.createdAt === 'string' && Number.isFinite(Date.parse(value.createdAt)) &&
    (value.paidAt === null || (typeof value.paidAt === 'string' && Number.isFinite(Date.parse(value.paidAt)))) &&
    (value.transactionCode === null || typeof value.transactionCode === 'string'))
}
