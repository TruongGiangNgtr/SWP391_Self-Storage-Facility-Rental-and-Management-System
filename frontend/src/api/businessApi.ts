import type {
  ApiCollectionResponse,
  ApiResponse,
} from './api.types'
import { httpClient } from './httpClient'

import type {
  CreateFacilityRequest,
  Facility,
  UpdateFacilityRequest,
} from '../models/facility'

export const businessApi = {
  listFacilities(
    page = 1,
    pageSize = 20,
  ): Promise<ApiCollectionResponse<Facility>> {
    return httpClient.get(
      `/business/facilities?page=${page}&pageSize=${pageSize}`,
    )
  },

  createFacility(
    request: CreateFacilityRequest,
  ): Promise<ApiResponse<Facility>> {
    return httpClient.post(
      '/business/facilities',
      request,
    )
  },
  updateFacility(
  facilityId: string,
  request: UpdateFacilityRequest,
): Promise<ApiResponse<Facility>> {
  return httpClient.patch(
    `/business/facilities/${facilityId}`,
    request,
  )
},

  activateFacility(
    facilityId: string,
  ): Promise<ApiResponse<Facility>> {
    return httpClient.post(
      `/business/facilities/${facilityId}/activate`,
    )
  },

  deactivateFacility(
    facilityId: string,
  ): Promise<ApiResponse<Facility>> {
    return httpClient.post(
      `/business/facilities/${facilityId}/deactivate`,
    )
  },
}