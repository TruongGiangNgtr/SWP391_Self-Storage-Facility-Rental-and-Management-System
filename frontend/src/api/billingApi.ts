import type {
  InvoiceDetail,
  InvoicePayOsPaymentResponse,
  PaymentDetail,
} from '../features/billing/billing.types'
import type { ApiCollectionResponse, ApiResponse } from './api.types'
import { httpClient } from './httpClient'

export const billingApi = {
  listInvoices(page = 1, pageSize = 20): Promise<ApiCollectionResponse<InvoiceDetail>> {
    return httpClient.get(`/invoices?page=${page}&pageSize=${pageSize}`)
  },

  getInvoice(invoiceId: string): Promise<ApiResponse<InvoiceDetail>> {
    return httpClient.get(`/invoices/${encodeURIComponent(invoiceId)}`)
  },

  startPayOsPayment(
    invoiceId: string,
    idempotencyKey: string,
  ): Promise<ApiResponse<InvoicePayOsPaymentResponse>> {
    return httpClient.post(
      `/invoices/${encodeURIComponent(invoiceId)}/payments/payos`,
      undefined,
      { 'Idempotency-Key': idempotencyKey },
    )
  },

  getPayment(paymentId: string): Promise<ApiResponse<PaymentDetail>> {
    return httpClient.get(`/payments/${encodeURIComponent(paymentId)}`)
  },
}
