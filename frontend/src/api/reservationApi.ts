import type {
  CancelReservationRequest,
  ConfirmReservationRequest,
  ConfirmReservationResponse,
  CreateReservationRequest,
  ReservationDetail,
} from '../features/reservations/reservation.types'
import type { ApiCollectionResponse, ApiResponse } from './api.types'
import { httpClient } from './httpClient'

export const reservationApi = {
  create(request: CreateReservationRequest): Promise<ApiResponse<ReservationDetail>> {
    return httpClient.post('/reservations', request)
  },

  list(page = 1, pageSize = 20): Promise<ApiCollectionResponse<ReservationDetail>> {
    return httpClient.get(`/reservations?page=${page}&pageSize=${pageSize}`)
  },

  get(reservationId: string): Promise<ApiResponse<ReservationDetail>> {
    return httpClient.get(`/reservations/${encodeURIComponent(reservationId)}`)
  },

  confirm(
    reservationId: string,
    request: ConfirmReservationRequest,
  ): Promise<ApiResponse<ConfirmReservationResponse>> {
    return httpClient.post(
      `/reservations/${encodeURIComponent(reservationId)}/confirm`,
      request,
    )
  },

  cancel(
    reservationId: string,
    request: CancelReservationRequest,
  ): Promise<ApiResponse<ReservationDetail>> {
    return httpClient.post(
      `/reservations/${encodeURIComponent(reservationId)}/cancel`,
      request,
    )
  },
}
