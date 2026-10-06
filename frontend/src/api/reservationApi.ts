import type {
  CreateReservationRequest,
  ReservationDetail,
} from '../features/reservations/reservation.types'
import type { ApiResponse } from './api.types'
import { httpClient } from './httpClient'

export const reservationApi = {
  create(
    request: CreateReservationRequest,
  ): Promise<ApiResponse<ReservationDetail>> {
    return httpClient.post('/reservations', request)
  },
}
