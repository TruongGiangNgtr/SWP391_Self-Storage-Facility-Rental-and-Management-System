import type { ApiResponse } from './api.types'
import { httpClient } from './httpClient'

export interface FacilityOperationsReport {
  facilityId: string
  asOf: string
  availableUnits: number
  inUseUnits: number
  inspectionUnits: number
  maintenanceUnits: number
  usageRate: number
  overdueContractCount: number
}

export const reportApi = {
  getFacilityOperations(
    facilityId: string,
  ): Promise<ApiResponse<FacilityOperationsReport>> {
    return httpClient.get(
      `/reports/facilities/${encodeURIComponent(facilityId)}/operations`,
    )
  },
}
