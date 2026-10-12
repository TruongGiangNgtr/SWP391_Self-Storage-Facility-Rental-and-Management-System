import type { ApiCollectionResponse, ApiResponse } from './api.types'
import { httpClient } from './httpClient'

/** REP-003. All financial and usage metrics are calculated by the server. */
export interface BusinessOverview {
  facilityCount: number
  activeContractCount: number
  usageRate: number
  rentalRevenue: number
}

/** CON-001 returns a ContractSummary, not the CON-002 ContractDetail payload. */
export interface BusinessContractSummary {
  contractId: string
  facilityId: string
  customerId: string
  storageUnitId: string
  unitCode: string
  unitTypeId: string
  unitTypeName: string
  unitTypeMode: 'PUBLIC' | 'PRIVATE'
  startMonth: string
  endMonth: string
  status: 'ACTIVE' | 'COMPLETED' | 'TERMINATED'
}

export const businessMonitoringApi = {
  overview(): Promise<ApiResponse<BusinessOverview>> {
    return httpClient.get('/reports/business/overview')
  },
  contracts(page = 1, pageSize = 20): Promise<ApiCollectionResponse<BusinessContractSummary>> {
    return httpClient.get(`/contracts?page=${page}&pageSize=${pageSize}`)
  },
}
