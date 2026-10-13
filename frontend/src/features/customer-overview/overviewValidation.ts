import type { HandoverContractDetail } from '../handover/contract.types'
import type { VisitDetail } from '../visits/visit.types'
import type { InvoiceDetail } from '../billing/billing.types'
import { isUuid } from '../handover/handoverValidation'
import { isCalendarDate } from '../visits/accessValidation'
import { isRentalMonth } from '../renewal/renewalValidation'

// Validate only the fields displayed in these previews, not backend business eligibility.
export function isOverviewContract(value: HandoverContractDetail): boolean {
  return Boolean(value && isUuid(value.contractId) && isUuid(value.storageUnitId) &&
    isRentalMonth(value.startMonth) && isRentalMonth(value.endMonth) &&
    ['ACTIVE', 'COMPLETED', 'TERMINATED'].includes(value.status) &&
    (value.unitCode === undefined || typeof value.unitCode === 'string') &&
    (value.unitTypeName === undefined || typeof value.unitTypeName === 'string'))
}
export function isOverviewVisit(value: VisitDetail): boolean {
  return Boolean(value && isUuid(value.visitId) && isCalendarDate(value.visitDate) &&
    ['RESERVATION', 'ACCESS', 'RETURN'].includes(value.visitType) &&
    ['SCHEDULED', 'CHECKED_IN', 'CHECKED_OUT', 'CANCELLED'].includes(value.status))
}
export function isOverviewInvoice(value: InvoiceDetail): boolean {
  return Boolean(value && isUuid(value.invoiceId) && ['DEPOSIT', 'RENTAL_FEE'].includes(value.invoiceType) &&
    ['UNPAID', 'PAID', 'OVERDUE', 'CANCELLED'].includes(value.status) &&
    typeof value.amountDue === 'number' && Number.isFinite(value.amountDue) && value.amountDue >= 0)
}
