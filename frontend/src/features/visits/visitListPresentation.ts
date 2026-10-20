import type { ApiCollectionResponse } from '../../api/api.types'
import { isSupportPagination } from '../customer-support/supportValidation'
import { isOverviewVisit } from '../customer-overview/overviewValidation'
import { isCalendarDate } from './accessValidation'
import { isUuid } from '../handover/handoverValidation'
import type { VisitDetail, VisitStatus, VisitType } from './visit.types'

export const VISIT_TABS = ['All Visits', 'Scheduled', 'Checked In', 'Checked Out', 'Cancelled'] as const
export const VISIT_STATUSES: readonly (VisitStatus | '')[] = ['', 'SCHEDULED', 'CHECKED_IN', 'CHECKED_OUT', 'CANCELLED']
export const VISIT_NAMES: Record<VisitType, string> = { RESERVATION: 'Handover Visit', ACCESS: 'Access Visit', RETURN: 'Return Visit' }
export const VISIT_DATE_RANGES = ['', 'week', 'month', 'past', 'custom'] as const
export interface VisitFilters { query: string; type: VisitType | ''; facility: string; range: typeof VISIT_DATE_RANGES[number]; from: string; to: string }

export function isVisitListResponse(value: unknown, page: number): value is ApiCollectionResponse<VisitDetail> {
  const response = value as ApiCollectionResponse<VisitDetail> | null
  return Boolean(response && Array.isArray(response.data) && isSupportPagination(response.pagination) &&
    response.pagination.page === page && response.pagination.pageSize === 20 && response.data.length <= 20 &&
    response.pagination.totalItems >= response.data.length && response.pagination.totalPages === Math.ceil(response.pagination.totalItems / 20) &&
    new Set(response.data.map(item => item?.visitId)).size === response.data.length && response.data.every(item => isOverviewVisit(item) &&
      (item.employeeId === null || isUuid(item.employeeId)) &&
      (item.actualReturnDate === null || (item.visitType === 'RETURN' && isCalendarDate(item.actualReturnDate)))))
}

export function visitFilterError(filters: VisitFilters): string | null {
  if (filters.range !== 'custom') return null
  return !isCalendarDate(filters.from) || !isCalendarDate(filters.to) ? 'Select a valid start and end date.'
    : filters.from > filters.to ? 'The end date must be on or after the start date.' : null
}
const shiftDay = (day: string, delta: number) => {
  const date = new Date(`${day}T00:00:00Z`)
  date.setUTCDate(date.getUTCDate() + delta)
  return date.toISOString().slice(0, 10)
}
// Date-only comparisons are presentation filters, never visit eligibility decisions.
export function matchesVisitDate(day: string, filters: VisitFilters, today: string): boolean {
  if (filters.range === 'week') return day >= today && day <= shiftDay(today, 6)
  if (filters.range === 'month') return day.slice(0, 7) === today.slice(0, 7)
  if (filters.range === 'past') return day >= shiftDay(today, -29) && day <= today
  if (filters.range === 'custom') return !visitFilterError(filters) && day >= filters.from && day <= filters.to
  return true
}

export function visitNotice(visit: VisitDetail): string {
  if (visit.status === 'CANCELLED') return 'This visit has been cancelled. Review the related reservation or contract before arranging another visit.'
  if (visit.status === 'CHECKED_OUT') return visit.visitType === 'RETURN'
    ? 'Your return visit is checked out. View return tracking for inspection and settlement progress.'
    : 'This visit is checked out. Review the related reservation or contract for its current status.'
  if (visit.status === 'CHECKED_IN') return visit.visitType === 'RESERVATION'
    ? 'You are checked in. Staff will complete handover after receiving the first-month rent offline and using the unit selected by the Manager.'
    : visit.visitType === 'RETURN' ? 'You are checked in for return. Staff records the actual return date; inspection and settlement are tracked separately.'
      : 'You are checked in. Please check out with the facility staff when your visit is finished.'
  return visit.visitType === 'RESERVATION'
    ? 'Bring your identity document and pay the first-month rent offline at the facility. Your storage unit is selected at handover.'
    : visit.visitType === 'RETURN' ? 'Staff will confirm the actual return date. Inspection and settlement follow the return process.'
      : 'Visit your rented storage space on the scheduled date. Facility staff will check you in and out.'
}
