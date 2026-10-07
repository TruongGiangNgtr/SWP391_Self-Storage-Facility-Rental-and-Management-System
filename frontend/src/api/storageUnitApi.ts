import type {
  ApiCollectionResponse,
  ApiResponse,
} from './api.types'
import { httpClient } from './httpClient'

export type StorageUnitStatus =
  | 'AVAILABLE'
  | 'IN_USE'
  | 'INSPECTION'
  | 'MAINTENANCE'

export interface StorageUnit {
  storageUnitId: string
  facilityId: string
  unitTypeId: string
  unitCode: string
  locationInfo: string | null
  status: StorageUnitStatus
}

async function readAllPages(
  facilityId: string,
): Promise<StorageUnit[]> {
  const firstPage = await storageUnitApi.listByFacility(
    facilityId,
    1,
    100,
  )
  const items = [...firstPage.data]

  for (
    let page = 2;
    page <= firstPage.pagination.totalPages;
    page += 1
  ) {
    const response = await storageUnitApi.listByFacility(
      facilityId,
      page,
      100,
    )
    items.push(...response.data)
  }

  return items
}

export const storageUnitApi = {
  listByFacility(
    facilityId: string,
    page = 1,
    pageSize = 20,
  ): Promise<ApiCollectionResponse<StorageUnit>> {
    return httpClient.get(
      `/facilities/${encodeURIComponent(facilityId)}/storage-units?page=${page}&pageSize=${pageSize}`,
    )
  },

  listAllByFacility(
    facilityId: string,
  ): Promise<StorageUnit[]> {
    return readAllPages(facilityId)
  },

  get(
    storageUnitId: string,
  ): Promise<ApiResponse<StorageUnit>> {
    return httpClient.get(
      `/storage-units/${encodeURIComponent(storageUnitId)}`,
    )
  },
}
