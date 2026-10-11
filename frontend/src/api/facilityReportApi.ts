import type { ApiResponse } from './api.types'
import { httpClient } from './httpClient'

/** REP-001 is a server-calculated snapshot, not a client-side occupancy formula. */
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

export const facilityReportApi = {
  operations(facilityId: string): Promise<ApiResponse<FacilityOperationsReport>> {
    return httpClient.get(`/reports/facilities/${encodeURIComponent(facilityId)}/operations`)
  },
}
