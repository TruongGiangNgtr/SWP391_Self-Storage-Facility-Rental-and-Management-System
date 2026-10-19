import type { ApiCollectionResponse } from './api.types'
import { httpClient } from './httpClient'
import type { StorageUnitStatus } from './storageUnitApi'

export interface UnitStatusPeriod {
  status: StorageUnitStatus
  from: string
  to: string | null
}
export interface UnitTimeline {
  unitCode: string
  unitTypeName: string
  facilityName: string
  locationInfo: string | null
  periods: UnitStatusPeriod[]
}

// Approved frontend contract; backend implementation is pending.
// Date bounds are inclusive GMT+7 calendar dates; periods are [from, to) UTC.
// null to means ongoing through the response time, never a forecast.
export const storageUnitTimelineApi = {
  list(facilityId: string, from: string, to: string, page: number): Promise<ApiCollectionResponse<UnitTimeline>> {
    const query = new URLSearchParams({ from, to, page: String(page), pageSize: '20' })
    return httpClient.get(`/facilities/${encodeURIComponent(facilityId)}/storage-units/timeline?${query}`)
  },
}
