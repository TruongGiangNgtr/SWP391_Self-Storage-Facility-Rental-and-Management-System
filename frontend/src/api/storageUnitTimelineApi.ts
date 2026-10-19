import type { ApiCollectionResponse } from './api.types'
import { httpClient } from './httpClient'
import type { StorageUnitStatus } from './storageUnitApi'

export interface UnitStatusPeriod {
  status: StorageUnitStatus
  from: string
  to: string
}
export interface UnitTimeline {
  storageUnitId: string
  unitCode: string
  unitTypeName: string
  facilityName: string
  locationInfo: string | null
  intervals: UnitStatusPeriod[]
}

// Month uses the GMT+7 business calendar; intervals are [from, to) UTC.
// The server bounds intervals at its response time, never a forecast.
export const storageUnitTimelineApi = {
  list(facilityId: string, month: string, page: number): Promise<ApiCollectionResponse<UnitTimeline>> {
    const query = new URLSearchParams({ month, page: String(page), pageSize: '20' })
    return httpClient.get(`/facilities/${encodeURIComponent(facilityId)}/storage-units/timeline?${query}`)
  },
}
