export interface HandoverContractDetail {
  contractId: string
  reservationId: string
  customerId: string
  facilityId: string
  storageUnitId: string
  policyId: string
  discountId: string | null
  startMonth: string
  endMonth: string
  status: 'ACTIVE' | 'COMPLETED' | 'TERMINATED'
  // Flow 2 does not consume ContractExtension fields; their schema is not invented here.
  extensions: unknown[]
}
