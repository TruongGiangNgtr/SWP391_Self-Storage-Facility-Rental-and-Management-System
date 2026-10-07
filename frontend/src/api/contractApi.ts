import type { HandoverContractDetail } from '../features/handover/contract.types'
import type { RenewContractRequest, RenewContractResult } from '../features/renewal/renewal.types'
import type { ApiCollectionResponse, ApiResponse } from './api.types'
import { httpClient } from './httpClient'

export const contractApi = {
  list(page = 1, pageSize = 20): Promise<ApiCollectionResponse<HandoverContractDetail>> {
    return httpClient.get(`/contracts?page=${page}&pageSize=${pageSize}`)
  },

  get(contractId: string): Promise<ApiResponse<HandoverContractDetail>> {
    return httpClient.get(`/contracts/${encodeURIComponent(contractId)}`)
  },

  renew(contractId: string, request: RenewContractRequest): Promise<ApiResponse<RenewContractResult>> {
    return httpClient.post(`/contracts/${encodeURIComponent(contractId)}/renew`, {
      newEndMonth: request.newEndMonth,
    })
  },
}
