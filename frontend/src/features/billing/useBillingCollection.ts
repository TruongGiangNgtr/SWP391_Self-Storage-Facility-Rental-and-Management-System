import { useEffect, useState } from 'react'
import type { ApiCollectionResponse } from '../../api/api.types'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import { isSupportPagination } from '../customer-support/supportValidation'
import { isUuid } from '../handover/handoverValidation'
import { isRentalMonth } from '../renewal/renewalValidation'
import type { InvoiceDetail } from './billing.types'

const money = (value: unknown) => typeof value === 'number' && Number.isFinite(value) && value >= 0
const timestamp = (value: unknown) => typeof value === 'string' && Number.isFinite(Date.parse(value))
export function isBillingInvoice(value: InvoiceDetail, id = value?.invoiceId): boolean {
  return Boolean(value && isUuid(value.invoiceId) && value.invoiceId === id && isUuid(value.entityId) &&
    ['DEPOSIT', 'RENTAL_FEE'].includes(value.invoiceType) &&
    (value.invoiceType === 'DEPOSIT' ? value.billingMonth === null : typeof value.billingMonth === 'string' && isRentalMonth(value.billingMonth)) &&
    ['UNPAID', 'PAID', 'OVERDUE', 'CANCELLED'].includes(value.status) &&
    money(value.baseAmount) && money(value.discountAmount) && money(value.amountDue) &&
    (value.discountId === null || isUuid(value.discountId)) && timestamp(value.dueDate) &&
    (value.paidAt === null || timestamp(value.paidAt)))
}
export function isInvoiceListResponse(value: unknown, page: number): value is ApiCollectionResponse<InvoiceDetail> {
  const response = value as ApiCollectionResponse<InvoiceDetail> | null
  return Boolean(response && Array.isArray(response.data) && isSupportPagination(response.pagination) &&
    response.pagination.page === page && response.pagination.pageSize === 20 && response.data.length <= 20 &&
    response.pagination.totalItems >= response.data.length && response.pagination.totalPages === Math.ceil(response.pagination.totalItems / 20) &&
    new Set(response.data.map(invoice => invoice?.invoiceId)).size === response.data.length && response.data.every(invoice => isBillingInvoice(invoice)))
}

// Keyed snapshots prevent old records from appearing under a new page/context.
// Reads stay paginated; no account-wide financial totals are computed here.
export function useBillingCollection<T>(page: number, revision: number,
  load: (page: number) => Promise<unknown>,
  valid: (value: unknown, page: number) => value is ApiCollectionResponse<T>) {
  const key = `${page}:${revision}`
  const [snapshot, setSnapshot] = useState<{ key: string; response: ApiCollectionResponse<T> | null; error: ApiErrorPresentation | null } | null>(null)
  const loading = snapshot?.key !== key
  useEffect(() => {
    let active = true
    load(page).then(response => {
      if (active) setSnapshot(valid(response, page) ? { key, response, error: null }
        : { key, response: null, error: presentValidationError('We could not load your billing records. Refresh the page to check the latest invoices and payments.') })
    }).catch((error: unknown) => { if (active) setSnapshot({ key, response: null, error: presentApiError(error) }) })
    return () => { active = false }
  }, [page, key, load, valid])
  return { loading, response: loading ? null : snapshot?.response ?? null, error: loading ? null : snapshot?.error ?? null }
}
