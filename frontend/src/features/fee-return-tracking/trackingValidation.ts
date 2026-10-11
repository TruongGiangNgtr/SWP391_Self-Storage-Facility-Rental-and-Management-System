import { isUuid } from '../handover/handoverValidation'
import { isCalendarDate } from '../visits/accessValidation'
import type { ContractBillingSummary, ContractReturnSummary } from './tracking.types'

const money = (value: unknown) => typeof value === 'number' && Number.isFinite(value) && value >= 0
const timestamp = (value: unknown) => typeof value === 'string' && Number.isFinite(Date.parse(value))
const nullableText = (value: unknown) => value === null || typeof value === 'string'

export function isBillingSummary(value: unknown, contractId: string): value is ContractBillingSummary {
  if (!value || typeof value !== 'object') return false
  const summary = value as ContractBillingSummary
  return summary.contractId === contractId && Array.isArray(summary.invoices) &&
    summary.invoices.every(invoice => invoice && isUuid(invoice.invoiceId) && isUuid(invoice.entityId) &&
      ['DEPOSIT', 'RENTAL_FEE'].includes(invoice.invoiceType) &&
      (invoice.invoiceType !== 'RENTAL_FEE' || invoice.entityId === contractId) &&
      (invoice.billingMonth === null || /^\d{4}-(0[1-9]|1[0-2])$/.test(invoice.billingMonth)) &&
      ['UNPAID', 'PAID', 'OVERDUE', 'CANCELLED'].includes(invoice.status) &&
      money(invoice.baseAmount) && money(invoice.discountAmount) && money(invoice.amountDue) &&
      (invoice.discountId === null || isUuid(invoice.discountId)) && timestamp(invoice.dueDate) &&
      (invoice.paidAt === null || timestamp(invoice.paidAt))) &&
    Array.isArray(summary.lateFees) && summary.lateFees.every(fee => fee && isUuid(fee.lateFeeId) &&
      isUuid(fee.invoiceId) && Number.isInteger(fee.overdueDays) && fee.overdueDays >= 1 &&
      money(fee.amount) && timestamp(fee.calculatedAt) &&
      summary.invoices.some(invoice => invoice.invoiceId === fee.invoiceId && invoice.invoiceType === 'RENTAL_FEE'))
}

export function isReturnSummary(value: unknown, contractId: string): value is ContractReturnSummary {
  if (!value || typeof value !== 'object') return false
  const summary = value as ContractReturnSummary
  const visit = summary.returnVisit, inspection = summary.inspection, settlement = summary.settlement
  return summary.contractId === contractId && ['ACTIVE', 'COMPLETED', 'TERMINATED'].includes(summary.contractStatus) &&
    (visit === null || Boolean(visit && isUuid(visit.visitId) && visit.entityId === contractId && visit.visitType === 'RETURN' &&
      ['SCHEDULED', 'CHECKED_IN', 'CHECKED_OUT', 'CANCELLED'].includes(visit.status) && isCalendarDate(visit.visitDate) &&
      (visit.actualReturnDate === null || (typeof visit.actualReturnDate === 'string' && isCalendarDate(visit.actualReturnDate))) &&
      (visit.employeeId === null || isUuid(visit.employeeId)))) &&
    (inspection === null || Boolean(inspection && isUuid(inspection.inspectionId) &&
      ['PENDING', 'IN_PROGRESS', 'COMPLETED'].includes(inspection.status) && nullableText(inspection.conditionNote) &&
      (inspection.completedAt === null || timestamp(inspection.completedAt)))) &&
    Array.isArray(summary.damages) && summary.damages.every(damage => damage && inspection &&
      isUuid(damage.damageRecordId) && damage.inspectionId === inspection.inspectionId && isUuid(damage.damageTypeId) &&
      ['PENDING', 'APPROVED', 'REJECTED'].includes(damage.status) && money(damage.damageAmount) &&
      nullableText(damage.note) && timestamp(damage.createdAt)) &&
    Array.isArray(summary.extraFees) && summary.extraFees.every(fee => fee && inspection && isUuid(fee.extraFeeId) &&
      fee.inspectionId === inspection.inspectionId && isUuid(fee.extraFeeTypeId) && money(fee.amount) &&
      typeof fee.reason === 'string' && timestamp(fee.createdAt)) &&
    (settlement === null || Boolean(settlement && isUuid(settlement.depositSettlementId) &&
      ['PENDING', 'FINALIZED'].includes(settlement.status) && money(settlement.totalDeduction) &&
      money(settlement.refundAmount) && money(settlement.additionalAmountDue) &&
      (settlement.status !== 'FINALIZED' || (summary.contractStatus !== 'ACTIVE' &&
        !summary.damages.some(damage => damage.status === 'PENDING')))))
}
