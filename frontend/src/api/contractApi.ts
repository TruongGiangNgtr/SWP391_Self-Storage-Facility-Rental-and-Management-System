import type { HandoverContractDetail } from '../features/handover/contract.types'
import type { ApiResponse } from './api.types'
import { httpClient } from './httpClient'

export const contractApi = {
  get(contractId: string): Promise<ApiResponse<HandoverContractDetail>> {
    return httpClient.get(`/contracts/${encodeURIComponent(contractId)}`)
  },
}
