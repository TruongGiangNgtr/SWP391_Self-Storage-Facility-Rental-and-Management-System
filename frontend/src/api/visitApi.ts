import type {
  CancelVisitRequest,
  RescheduleVisitRequest,
  VisitDetail,
} from '../features/visits/visit.types'
import type { ApiCollectionResponse, ApiResponse } from './api.types'
import { httpClient } from './httpClient'

export const visitApi = {
  list(page = 1, pageSize = 20): Promise<ApiCollectionResponse<VisitDetail>> {
    return httpClient.get(`/visits?page=${page}&pageSize=${pageSize}`)
  },

  get(visitId: string): Promise<ApiResponse<VisitDetail>> {
    return httpClient.get(`/visits/${encodeURIComponent(visitId)}`)
  },

  reschedule(visitId: string, request: RescheduleVisitRequest): Promise<unknown> {
    return httpClient.patch(`/visits/${encodeURIComponent(visitId)}/schedule`, request)
  },

  cancel(visitId: string, request: CancelVisitRequest): Promise<unknown> {
    return httpClient.post(`/visits/${encodeURIComponent(visitId)}/cancel`, request)
  },
}
