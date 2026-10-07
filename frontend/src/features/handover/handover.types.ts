export interface StaffWorkItem {
  workType: string
  referenceId: string
  scheduledDate: string
  status: string
  entityId?: string
  customer?: StaffWorkItemCustomer
}

export interface StaffWorkItemCustomer {
  customerId: string
  fullName: string
  phoneNumber: string
}

export interface ReservationWorkItem extends StaffWorkItem {
  workType: 'RESERVATION_VISIT'
  entityId: string
  customer: StaffWorkItemCustomer
}

export function isReservationWorkItem(item: StaffWorkItem): item is ReservationWorkItem {
  return item.workType === 'RESERVATION_VISIT' &&
    typeof item.entityId === 'string' && item.entityId.length > 0 &&
    typeof item.customer?.customerId === 'string' && item.customer.customerId.length > 0 &&
    typeof item.customer.fullName === 'string' && typeof item.customer.phoneNumber === 'string'
}

export interface StaffWorkItemsRequest {
  date?: string
  page?: number
  pageSize?: number
}

export interface CompleteHandoverRequest {
  visitId: string
  storageUnitId: string
  discountId: string | null
}

export interface CompleteHandoverResult {
  contract: {
    contractId: string
    status: 'ACTIVE'
    storageUnitId: string
    startMonth: string
    endMonth: string
  }
  reservationStatus: 'COMPLETED'
  visitStatus: 'CHECKED_OUT'
  storageUnitStatus: 'IN_USE'
}
