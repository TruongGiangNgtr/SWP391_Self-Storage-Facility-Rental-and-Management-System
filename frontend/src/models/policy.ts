export type PolicyStatus = 'ACTIVE' | 'INACTIVE'

export interface Policy {
  policyId: string
  version: number
  status: PolicyStatus
  effectiveFrom: string
  effectiveTo: string | null
  depositTimeoutHours: number
  reservationVisitStartDay: number
  reservationVisitEndDay: number
  monthlyPaymentDueDay: number
  overdueStartDay: number
  lateFeeDivisorDays: number
  earlyReturnWaiveFeeUntilDay: number
  createdAt: string
}

export interface CreatePolicyVersionRequest {
  depositTimeoutHours: number
  reservationVisitStartDay: number
  reservationVisitEndDay: number
  monthlyPaymentDueDay: number
  overdueStartDay: number
  lateFeeDivisorDays: number
  earlyReturnWaiveFeeUntilDay: number
}
