import type {
  CancelVisitRequest,
  RescheduleVisitRequest,
  VisitDetail,
} from '../features/visits/visit.types'
import type { ApiCollectionResponse, ApiResponse } from './api.types'
import type {
  CompleteHandoverRequest,
  CompleteHandoverResult,
  StaffWorkItem,
  StaffWorkItemsRequest,
} from '../features/handover/handover.types'
import { httpClient } from './httpClient'

export const visitApi = {
  listStaffWorkItems({ date, page = 1, pageSize = 20 }: StaffWorkItemsRequest = {}): Promise<ApiCollectionResponse<StaffWorkItem>> {
    const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
    if (date) query.set('date', date)
    return httpClient.get(`/staff/work-items?${query}`)
  },

  checkIn(visitId: string): Promise<unknown> {
    return httpClient.post(`/visits/${encodeURIComponent(visitId)}/check-in`, {})
  },

  completeHandover(reservationId: string, request: CompleteHandoverRequest): Promise<ApiResponse<CompleteHandoverResult>> {
    return httpClient.post(`/reservations/${encodeURIComponent(reservationId)}/complete-handover`, {
      visitId: request.visitId,
      storageUnitId: request.storageUnitId,
      discountId: request.discountId,
    })
  },

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
