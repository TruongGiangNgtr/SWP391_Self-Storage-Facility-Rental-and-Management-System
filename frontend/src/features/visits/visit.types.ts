export type VisitType = 'RESERVATION' | 'ACCESS' | 'RETURN'
export type VisitStatus = 'SCHEDULED' | 'CHECKED_IN' | 'CHECKED_OUT' | 'CANCELLED'

export interface VisitDetail {
  visitId: string
  entityId: string
  visitType: VisitType
  visitDate: string
  actualReturnDate: string | null
  status: VisitStatus
  employeeId: string | null
}

export interface RescheduleVisitRequest {
  visitDate: string
}

export interface CreateAccessVisitRequest {
  visitDate: string
}

export interface CancelVisitRequest {
  reason: string
}
