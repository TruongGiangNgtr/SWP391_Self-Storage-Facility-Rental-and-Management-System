import type { HandoverContractDetail } from '../handover/contract.types'
import type { VisitDetail } from '../visits/visit.types'
import type { InvoiceDetail } from '../billing/billing.types'
import type { ReservationDetail } from '../reservations/reservation.types'
import { isUuid } from '../handover/handoverValidation'
import { isCalendarDate } from '../visits/accessValidation'
import { isRentalMonth } from '../renewal/renewalValidation'

// Validate only the fields displayed in these previews, not backend business eligibility.
export function isOverviewContract(value: HandoverContractDetail): boolean {
  return Boolean(value && isUuid(value.contractId) && isUuid(value.storageUnitId) &&
    isUuid(value.reservationId) && isUuid(value.facilityId) &&
    isRentalMonth(value.startMonth) && isRentalMonth(value.endMonth) &&
    ['ACTIVE', 'COMPLETED', 'TERMINATED'].includes(value.status) &&
    (value.unitCode === undefined || typeof value.unitCode === 'string') &&
    (value.unitTypeName === undefined || typeof value.unitTypeName === 'string'))
}
export function isOverviewVisit(value: VisitDetail): boolean {
  return Boolean(value && isUuid(value.visitId) && isUuid(value.entityId) && isCalendarDate(value.visitDate) &&
    ['RESERVATION', 'ACCESS', 'RETURN'].includes(value.visitType) &&
    ['SCHEDULED', 'CHECKED_IN', 'CHECKED_OUT', 'CANCELLED'].includes(value.status))
}
export function isOverviewInvoice(value: InvoiceDetail): boolean {
  return Boolean(value && isUuid(value.invoiceId) && ['DEPOSIT', 'RENTAL_FEE'].includes(value.invoiceType) &&
    ['UNPAID', 'PAID', 'OVERDUE', 'CANCELLED'].includes(value.status) &&
    typeof value.amountDue === 'number' && Number.isFinite(value.amountDue) && value.amountDue >= 0)
}

export function isOverviewReservation(value: ReservationDetail, id: string): boolean {
  const money = (amount: unknown) => typeof amount === 'number' && Number.isFinite(amount) && amount >= 0
  return Boolean(value && value.reservationId === id && isUuid(value.facilityId) && isUuid(value.unitTypeId) &&
    isRentalMonth(value.startMonth) && isRentalMonth(value.endMonth) && value.endMonth >= value.startMonth &&
    money(value.lockedRentalPrice) && money(value.depositAmount) &&
    ['PENDING_DEPOSIT', 'CONFIRMED', 'COMPLETED', 'CANCELLED'].includes(value.status) &&
    value.depositInvoice && isUuid(value.depositInvoice.invoiceId) &&
    ['UNPAID', 'PAID', 'OVERDUE', 'CANCELLED'].includes(value.depositInvoice.status))
}

// Format date-only business values without shifting them as UTC timestamps.
export function overviewDate(value: string): string {
  const [year, month, day] = value.split('-').map(Number)
  return new Intl.DateTimeFormat('en-GB', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' }).format(new Date(Date.UTC(year, month - 1, day)))
}
