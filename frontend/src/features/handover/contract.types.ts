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
  // Optional read fields already supplied by CON-001/002; older consumers need only IDs.
  unitCode?: string
  unitTypeName?: string
  unitTypeMode?: string
  storageUnit?: {
    storageUnitId: string
    unitCode: string
    locationInfo: string | null
    status: string
    unitTypeId: string
    unitTypeName: string
    unitTypeMode: string
    unitTypeSize: string
  }
  // Flow 2 does not consume ContractExtension fields; their schema is not invented here.
  extensions: unknown[]
}
