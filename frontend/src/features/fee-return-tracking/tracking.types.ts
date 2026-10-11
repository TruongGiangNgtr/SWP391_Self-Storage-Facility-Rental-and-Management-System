import type { InvoiceDetail } from '../billing/billing.types'
import type { HandoverContractDetail } from '../handover/contract.types'
import type { DamageRecordDetail, DepositSettlementResult, ExtraFeeDetail, InspectionStatus } from '../return-management/return.types'
import type { VisitDetail } from '../visits/visit.types'

// Owner-approved API read models (2026-10-11) for CON-004/005.
// These are response contracts, not persistence entities or client-side calculations.
export interface LateFeeDetail {
  lateFeeId: string
  invoiceId: string
  overdueDays: number
  amount: number
  calculatedAt: string
}

export interface ContractBillingSummary {
  contractId: string
  invoices: InvoiceDetail[]
  lateFees: LateFeeDetail[]
}

export interface ReturnInspectionSummary {
  inspectionId: string
  status: InspectionStatus
  conditionNote: string | null
  completedAt: string | null
}

export interface ContractReturnSummary {
  contractId: string
  contractStatus: HandoverContractDetail['status']
  returnVisit: VisitDetail | null
  inspection: ReturnInspectionSummary | null
  damages: DamageRecordDetail[]
  extraFees: ExtraFeeDetail[]
  settlement: DepositSettlementResult | null
}
