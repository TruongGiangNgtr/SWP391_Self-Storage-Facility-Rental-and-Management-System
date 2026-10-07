export interface RenewContractRequest {
  newEndMonth: string
}

export interface RenewContractResult {
  contractId: string
  oldEndMonth: string
  newEndMonth: string
  appliedMonthlyPrice: number
  status: 'ACTIVE'
}
