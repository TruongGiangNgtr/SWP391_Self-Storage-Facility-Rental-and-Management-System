import type { InvoiceStatus } from '../reservations/reservation.types'

export type InvoiceType = 'DEPOSIT' | 'RENTAL_FEE'
export type PaymentStatus = 'PENDING' | 'SUCCESS' | 'FAILED'

export interface InvoiceDetail {
  invoiceId: string
  entityId: string
  invoiceType: InvoiceType
  billingMonth: string | null
  baseAmount: number
  discountId: string | null
  discountAmount: number
  amountDue: number
  dueDate: string
  status: InvoiceStatus
  paidAt: string | null
}

export interface PaymentDetail {
  paymentId: string
  invoiceId: string | null
  amount: number
  paymentMethod: 'MOMO'
  transactionCode: string | null
  status: PaymentStatus
  paidAt: string | null
  createdAt: string
}

export interface InvoiceMomoPaymentResponse {
  paymentId: string
  invoiceId: string
  amount: number
  paymentMethod: 'MOMO'
  status: 'PENDING'
  paymentUrl: string
}

export interface StartMomoPaymentRequest {
  returnUrl: string
}
