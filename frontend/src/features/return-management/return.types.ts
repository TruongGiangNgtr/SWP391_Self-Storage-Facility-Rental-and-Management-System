export type InspectionStatus = 'PENDING' | 'IN_PROGRESS' | 'COMPLETED'
export type DamageDecisionStatus = 'PENDING' | 'APPROVED' | 'REJECTED'
export type EvidenceType = 'IMAGE' | 'VIDEO' | 'DOCUMENT'
export type FinalStorageUnitStatus = 'AVAILABLE' | 'MAINTENANCE'
export type ReturnClassification = 'NORMAL' | 'EARLY'

export interface ConfirmActualReturnResult {
  visitId: string
  actualReturnDate: string
  inspectionId: string
  inspectionStatus: 'PENDING'
  storageUnitStatus: 'INSPECTION'
  returnClassification: ReturnClassification
}

export interface DamageRecordDetail {
  damageRecordId: string
  inspectionId: string
  damageTypeId: string
  damageAmount: number
  note: string | null
  status: DamageDecisionStatus
  createdAt: string
}

export interface ExtraFeeDetail {
  extraFeeId: string
  inspectionId: string
  extraFeeTypeId: string
  amount: number
  reason: string
  createdAt: string
}

export interface InspectionEvidenceDetail {
  inspectionEvidenceId: string
  inspectionId: string
  evidenceType: EvidenceType
  createdAt: string
}

export interface InspectionDetail {
  inspectionId: string
  contractId: string
  storageUnitId: string
  visitId: string | null
  employeeId: string | null
  status: InspectionStatus
  conditionNote: string | null
  damages: DamageRecordDetail[]
  extraFees: ExtraFeeDetail[]
  evidence: InspectionEvidenceDetail[]
  completedAt: string | null
}

export interface DamageTypeDetail {
  damageTypeId: string
  name: string
  defaultAmount: number | null
  status: 'ACTIVE' | 'INACTIVE'
}

export interface ExtraFeeTypeDetail {
  extraFeeTypeId: string
  name: 'KEY_REPLACEMENT' | 'ACCESS_CARD_REPLACEMENT' | 'LOCK_REPLACEMENT' | 'CLEANING_FEE' | 'OTHER'
  defaultAmount: number
  status: 'ACTIVE' | 'INACTIVE'
}

export interface RecordDamageRequest {
  damageTypeId: string
  damageAmount: number
  note: string | null
}

export interface RecordExtraFeeRequest {
  extraFeeTypeId: string
  amount: number
  reason: string
}

export interface CompleteInspectionRequest {
  conditionNote: string
}

export interface FinalizeReturnRequest {
  storageUnitStatus: FinalStorageUnitStatus
}

export interface DepositSettlementResult {
  depositSettlementId: string
  totalDeduction: number
  refundAmount: number
  additionalAmountDue: number
  status: 'PENDING' | 'FINALIZED'
}

export interface FinalizeReturnResult {
  contractId: string
  contractStatus: 'COMPLETED' | 'TERMINATED'
  storageUnitStatus: FinalStorageUnitStatus
  settlement: DepositSettlementResult
}
