import type {
  FacilitySummary,
  UnitTypeAvailability,
} from '../features/catalog/catalog.types'
import type { ApiCollectionResponse, ApiResponse } from './api.types'
import { httpClient } from './httpClient'

interface CatalogPageRequest {
  page?: number
  pageSize?: number
}

interface UnitTypeRequest extends CatalogPageRequest {
  startMonth?: string
  endMonth?: string
}

function pageQuery({ page = 1, pageSize = 20 }: CatalogPageRequest): URLSearchParams {
  return new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
}

async function readCatalogPages<T>(
  readPage: (page: number) => Promise<ApiCollectionResponse<T>>,
): Promise<T[]> {
  const first = await readPage(1)
  const items = [...first.data]
  for (let page = 2; page <= first.pagination.totalPages; page += 1) {
    const response = await readPage(page)
    items.push(...response.data)
  }
  return items
}

export const catalogApi = {
  listAllFacilities(): Promise<FacilitySummary[]> {
    return readCatalogPages((page) => catalogApi.listFacilities({ page, pageSize: 100 }))
  },

  listAllFacilityUnitTypes(
    facilityId: string,
    period: Pick<UnitTypeRequest, 'startMonth' | 'endMonth'> = {},
  ): Promise<UnitTypeAvailability[]> {
    return readCatalogPages((page) =>
      catalogApi.listFacilityUnitTypes(facilityId, { ...period, page, pageSize: 100 }),
    )
  },
  listFacilities(
    request: CatalogPageRequest = {},
  ): Promise<ApiCollectionResponse<FacilitySummary>> {
    return httpClient.get(`/facilities?${pageQuery(request)}`)
  },

  getFacility(facilityId: string): Promise<ApiResponse<FacilitySummary>> {
    return httpClient.get(`/facilities/${encodeURIComponent(facilityId)}`)
  },

  listFacilityUnitTypes(
    facilityId: string,
    request: UnitTypeRequest = {},
  ): Promise<ApiCollectionResponse<UnitTypeAvailability>> {
    const query = pageQuery(request)
    if (request.startMonth) {
      query.set('startMonth', request.startMonth)
    }
    if (request.endMonth) {
      query.set('endMonth', request.endMonth)
    }

    return httpClient.get(
      `/facilities/${encodeURIComponent(facilityId)}/unit-types?${query}`,
    )
  },

  getUnitType(unitTypeId: string): Promise<ApiResponse<UnitTypeAvailability>> {
    return httpClient.get(`/unit-types/${encodeURIComponent(unitTypeId)}`)
  },
}
