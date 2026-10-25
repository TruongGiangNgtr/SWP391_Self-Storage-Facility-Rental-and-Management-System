// IMPL-CUSACT-02 / OD-07: one row per actual Payment attempt.
export interface CustomerPaymentHistory {
  paymentId: string
  invoiceId: string
  invoiceType: 'DEPOSIT' | 'RENTAL_FEE'
  amount: number
  paymentMethod: 'PAYOS' | 'MOMO' | 'VNPAY'
  status: 'PENDING' | 'SUCCESS' | 'FAILED'
  paidAt: string | null
  createdAt: string
}
