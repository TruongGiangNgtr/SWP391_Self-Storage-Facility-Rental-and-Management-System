import type { CustomerDiscount } from '../features/handover/discount.types'
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
import type {
  UnitType,
  UpdateUnitTypePriceRequest,
} from '../models/unitType'
import type {
  CreatePolicyVersionRequest,
  Policy,
} from '../models/policy'
import type {
  CreateCustomerDiscountRequest,
  Discount,
  UpdateDiscountRequest,
} from '../models/discount'

async function readAllPages<T>(
  readPage: (page: number) => Promise<ApiCollectionResponse<T>>,
): Promise<T[]> {
  const firstPage = await readPage(1)
  const items = [...firstPage.data]

  for (
    let page = 2;
    page <= firstPage.pagination.totalPages;
    page += 1
  ) {
    const response = await readPage(page)
    items.push(...response.data)
  }

  return items
}

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

  listUnitTypes(
    page = 1,
    pageSize = 20,
  ): Promise<ApiCollectionResponse<UnitType>> {
    return httpClient.get(
      `/business/unit-types?page=${page}&pageSize=${pageSize}`,
    )
  },

  listAllUnitTypes(): Promise<UnitType[]> {
    return readAllPages((page) =>
      businessApi.listUnitTypes(page, 100),
    )
  },

  updateUnitTypePrice(
    unitTypeId: string,
    request: UpdateUnitTypePriceRequest,
  ): Promise<ApiResponse<UnitType>> {
    return httpClient.patch(
      `/business/unit-types/${encodeURIComponent(unitTypeId)}/price`,
      request,
    )
  },

  listPolicies(
    page = 1,
    pageSize = 20,
  ): Promise<ApiCollectionResponse<Policy>> {
    return httpClient.get(
      `/business/policies?page=${page}&pageSize=${pageSize}`,
    )
  },

  listAllPolicies(): Promise<Policy[]> {
    return readAllPages((page) =>
      businessApi.listPolicies(page, 100),
    )
  },

  createPolicyVersion(
    request: CreatePolicyVersionRequest,
  ): Promise<ApiResponse<Policy>> {
    return httpClient.post(
      '/business/policies',
      request,
    )
  },

  listCustomerDiscounts(customerId: string, page = 1): Promise<ApiCollectionResponse<CustomerDiscount>> {
    return httpClient.get(`/business/customers/${encodeURIComponent(customerId)}/discounts?page=${page}&pageSize=20`)
  },
}
