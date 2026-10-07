import type { CustomerDiscount } from '../features/handover/discount.types'
import type { ApiCollectionResponse } from './api.types'
import { httpClient } from './httpClient'

export const businessApi = {
  listCustomerDiscounts(customerId: string, page = 1): Promise<ApiCollectionResponse<CustomerDiscount>> {
    return httpClient.get(`/business/customers/${encodeURIComponent(customerId)}/discounts?page=${page}&pageSize=20`)
  },
}
