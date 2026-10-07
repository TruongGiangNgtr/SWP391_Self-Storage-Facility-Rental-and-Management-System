import type { HandoverContractDetail } from '../handover/contract.types'
import type { StaffWorkItem, StaffWorkItemCustomer } from '../handover/handover.types'
import type { VisitDetail } from './visit.types'

export interface AccessWorkItem extends StaffWorkItem {
  workType: 'ACCESS_VISIT'
  entityId: string
  customer: StaffWorkItemCustomer
}

export function isAccessWorkItem(item: StaffWorkItem): item is AccessWorkItem {
  return item.workType === 'ACCESS_VISIT' &&
    typeof item.entityId === 'string' && item.entityId.length > 0 &&
    typeof item.customer?.customerId === 'string' && item.customer.customerId.length > 0 &&
    typeof item.customer.fullName === 'string' && typeof item.customer.phoneNumber === 'string'
}

export function isCalendarDate(value: string): boolean {
  if (!/^\d{4}-(0[1-9]|1[0-2])-(0[1-9]|[12]\d|3[01])$/.test(value)) return false
  const date = new Date(`${value}T00:00:00Z`)
  return Number.isFinite(date.getTime()) && date.toISOString().slice(0, 10) === value
}

export function getAccessContextError(contract: HandoverContractDetail, visit: VisitDetail, contractId: string, visitId: string): string | null {
  if (contract.contractId !== contractId || visit.visitId !== visitId ||
    visit.visitType !== 'ACCESS' || visit.entityId !== contract.contractId || visit.actualReturnDate !== null) {
    return 'The server returned an access visit that does not match this contract. Return to the work list and refresh.'
  }
  return null
}
