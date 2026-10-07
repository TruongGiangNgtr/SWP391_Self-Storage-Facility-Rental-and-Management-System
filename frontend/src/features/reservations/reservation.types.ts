export type ReservationStatus =
  | 'PENDING_DEPOSIT'
  | 'CONFIRMED'
  | 'COMPLETED'
  | 'CANCELLED'

export type InvoiceStatus = 'UNPAID' | 'PAID' | 'OVERDUE' | 'CANCELLED'

export interface DepositInvoiceSummary {
  invoiceId: string
  status: InvoiceStatus
  amountDue: number
  dueDate: string
}

export interface ReservationVisitSummary {
  visitId: string
  visitType: 'RESERVATION'
  visitDate: string
  status: 'SCHEDULED' | 'CHECKED_IN' | 'CHECKED_OUT' | 'CANCELLED'
}

export interface ReservationDetail {
  reservationId: string
  facilityId: string
  unitTypeId: string
  policyId: string
  startMonth: string
  endMonth: string
  lockedRentalPrice: number
  depositAmount: number
  status: ReservationStatus
  depositInvoice: DepositInvoiceSummary
  reservationVisit: ReservationVisitSummary | null
  createdAt: string
}

export interface CreateReservationRequest {
  facilityId: string
  unitTypeId: string
  startMonth: string
  endMonth: string
}

export interface ConfirmReservationRequest {
  reservationVisitDate: string
}

export interface ConfirmReservationResponse {
  reservationId: string
  status: 'CONFIRMED'
  reservationVisit: ReservationVisitSummary
}

export interface CancelReservationRequest {
  reason: string
}
