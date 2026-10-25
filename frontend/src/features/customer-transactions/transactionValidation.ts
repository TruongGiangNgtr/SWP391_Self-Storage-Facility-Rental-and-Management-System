import type { ApiCollectionResponse } from '../../api/api.types'
import { isSupportPagination } from '../customer-support/supportValidation'
import { isUuid } from '../handover/handoverValidation'
import type { CustomerPaymentHistory } from './transaction.types'

export class TransactionContractError extends Error {
  constructor() {
    super('We could not load your transaction history. Refresh to check for updates.')
    this.name = 'TransactionContractError'
  }
}

function isUtcTimestamp(value: unknown): value is string {
  return typeof value === 'string' && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,7})?(Z|\+00:00)$/.test(value) &&
    Number.isFinite(Date.parse(value)) && new Date(value).toISOString().slice(0, 19) === value.slice(0, 19)
}

function isPayment(value: unknown): value is CustomerPaymentHistory {
  if (!value || typeof value !== 'object') return false
  const item = value as CustomerPaymentHistory
  return isUuid(item.paymentId) && isUuid(item.invoiceId) &&
    ['DEPOSIT', 'RENTAL_FEE'].includes(item.invoiceType) &&
    typeof item.amount === 'number' && Number.isFinite(item.amount) && item.amount >= 0 &&
    ['PAYOS', 'MOMO', 'VNPAY'].includes(item.paymentMethod) &&
    ['PENDING', 'SUCCESS', 'FAILED'].includes(item.status) &&
    isUtcTimestamp(item.createdAt) && (item.paidAt === null || isUtcTimestamp(item.paidAt))
}

export function readTransactionCollection(value: unknown, page: number, pageSize: number): ApiCollectionResponse<CustomerPaymentHistory> {
  const response = value as ApiCollectionResponse<CustomerPaymentHistory> | null
  if (!response || !Array.isArray(response.data) || !response.data.every(isPayment) ||
    !isSupportPagination(response.pagination) || response.pagination.page !== page || response.pagination.pageSize !== pageSize ||
    !Number.isSafeInteger(response.pagination.totalItems) || response.data.length > pageSize ||
    response.pagination.totalItems < response.data.length ||
    response.pagination.totalPages !== Math.ceil(response.pagination.totalItems / pageSize) ||
    new Set(response.data.map(item => item.paymentId.toLowerCase())).size !== response.data.length) throw new TransactionContractError()
  return response
}
