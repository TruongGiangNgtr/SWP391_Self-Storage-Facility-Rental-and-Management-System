import type { ApiCollectionResponse } from '../../api/api.types'
import type { ReservationDetail, ReservationStatus } from './reservation.types'
import { isSupportPagination } from '../customer-support/supportValidation'
import { isUuid } from '../handover/handoverValidation'
import { isCalendarDate } from '../visits/accessValidation'
import { isRentalMonth } from '../renewal/renewalValidation'

export const RESERVATION_LIST_STATUSES: readonly (ReservationStatus | '')[] = ['', 'PENDING_DEPOSIT', 'CONFIRMED', 'COMPLETED', 'CANCELLED']
export const RESERVATION_LIST_TABS = ['All Reservations', 'Pending Deposit / Date', 'Confirmed', 'Completed', 'Cancelled'] as const
const money = (value: unknown) => typeof value === 'number' && Number.isFinite(value) && value >= 0
const timestamp = (value: unknown) => typeof value === 'string' && Number.isFinite(Date.parse(value))

export function isReservationListResponse(value: unknown, page: number): value is ApiCollectionResponse<ReservationDetail> {
  const response = value as ApiCollectionResponse<ReservationDetail> | null
  return Boolean(response && Array.isArray(response.data) && isSupportPagination(response.pagination) &&
    response.pagination.page === page && response.pagination.pageSize === 20 && response.data.length <= 20 &&
    response.pagination.totalItems >= response.data.length && response.pagination.totalPages === Math.ceil(response.pagination.totalItems / 20) &&
    new Set(response.data.map(item => item?.reservationId)).size === response.data.length && response.data.every(item => item &&
      isUuid(item.reservationId) && isUuid(item.facilityId) && isUuid(item.unitTypeId) &&
      ['PENDING_DEPOSIT', 'CONFIRMED', 'COMPLETED', 'CANCELLED'].includes(item.status) &&
      isRentalMonth(item.startMonth) && isRentalMonth(item.endMonth) && item.endMonth >= item.startMonth &&
      money(item.lockedRentalPrice) && money(item.depositAmount) && timestamp(item.createdAt) &&
      item.depositInvoice && isUuid(item.depositInvoice.invoiceId) && money(item.depositInvoice.amountDue) && timestamp(item.depositInvoice.dueDate) &&
      ['UNPAID', 'PAID', 'OVERDUE', 'CANCELLED'].includes(item.depositInvoice.status) &&
      (item.reservationVisit === null || Boolean(item.reservationVisit && isUuid(item.reservationVisit.visitId) &&
        item.reservationVisit.visitType === 'RESERVATION' && isCalendarDate(item.reservationVisit.visitDate) &&
        ['SCHEDULED', 'CHECKED_IN', 'CHECKED_OUT', 'CANCELLED'].includes(item.reservationVisit.status))))
  )
}

const monthDate = (month: string) => { const [year, number] = month.split('-').map(Number); return new Date(Date.UTC(year, number - 1, 1)) }
export function reservationPeriod(startMonth: string, endMonth: string): string {
  const formatter = new Intl.DateTimeFormat('en-GB', { timeZone: 'UTC', month: 'short', year: 'numeric' })
  return `${formatter.format(monthDate(startMonth))} – ${formatter.format(monthDate(endMonth))}`
}
export function reservationDuration(startMonth: string, endMonth: string): string {
  const [startYear, start] = startMonth.split('-').map(Number), [endYear, end] = endMonth.split('-').map(Number)
  const months = (endYear - startYear) * 12 + end - start + 1
  return `${months} ${months === 1 ? 'month' : 'months'}`
}
export function reservationNotice(item: ReservationDetail): string {
  if (item.status === 'COMPLETED') return 'Handover is complete. Open My Contracts to review the resulting rental agreement.'
  if (item.status === 'CANCELLED') return 'This reservation is cancelled. Start a new storage search if you would like to reserve again.'
  if (item.status === 'CONFIRMED') return 'Your reservation is confirmed. Review your handover visit before arriving; your specific storage unit is selected at handover.'
  if (item.depositInvoice.status === 'PAID') return 'Your deposit payment is recorded. Choose a handover date to confirm your reservation.'
  if (item.depositInvoice.status === 'OVERDUE') return 'Your deposit invoice is overdue. Review its current details before attempting payment.'
  if (item.depositInvoice.status === 'CANCELLED') return 'Your deposit invoice is cancelled. Review your reservation details or contact support.'
  return 'Your deposit invoice is unpaid. Review your reservation and pay before the payment deadline shown below.'
}
