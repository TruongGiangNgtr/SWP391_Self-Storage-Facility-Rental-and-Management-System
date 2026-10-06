import type {
  FacilitySummary,
  UnitTypeAvailability,
} from '../features/catalog/catalog.types'
import type { ApiCollectionResponse } from './api.types'
import { httpClient } from './httpClient'

interface CatalogPageRequest {
  page?: number
  pageSize?: number
}

interface UnitTypeAvailabilityRequest extends CatalogPageRequest {
  startMonth: string
  endMonth: string
}

function collectionQuery({ page = 1, pageSize = 20 }: CatalogPageRequest): URLSearchParams {
  return new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
}

export const catalogApi = {
  listFacilities(
    request: CatalogPageRequest = {},
  ): Promise<ApiCollectionResponse<FacilitySummary>> {
    return httpClient.get(`/facilities?${collectionQuery(request)}`)
  },

  listFacilityUnitTypes(
    facilityId: string,
    request: UnitTypeAvailabilityRequest,
  ): Promise<ApiCollectionResponse<UnitTypeAvailability>> {
    const query = collectionQuery(request)
    query.set('startMonth', request.startMonth)
    query.set('endMonth', request.endMonth)

    return httpClient.get(
      `/facilities/${encodeURIComponent(facilityId)}/unit-types?${query}`,
    )
  },
}
