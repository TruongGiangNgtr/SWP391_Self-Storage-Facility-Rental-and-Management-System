export interface ContractExtension {
  contractExtensionId: string
  oldEndMonth: string
  newEndMonth: string
  appliedMonthlyPrice: number
  createdAt: string
}

export interface ContractVisit {
  visitId: string
  visitType: 'RESERVATION' | 'ACCESS' | 'RETURN'
  visitDate: string
  actualReturnDate: string | null
  status: 'SCHEDULED' | 'CHECKED_IN' | 'CHECKED_OUT' | 'CANCELLED'
  employeeId: string | null
}

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
  extensions: ContractExtension[]
  visits: ContractVisit[]
}
