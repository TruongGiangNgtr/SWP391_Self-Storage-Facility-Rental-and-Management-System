import type { HandoverUnitAssignment } from '../features/handover/handover.types'
import type { ApiResponse } from './api.types'
import { httpClient } from './httpClient'

function path(reservationId: string) {
  return `/reservations/${encodeURIComponent(reservationId)}/handover-unit-assignment`
}

export const handoverAssignmentApi = {
  get(reservationId: string): Promise<ApiResponse<HandoverUnitAssignment | null>> {
    return httpClient.get(path(reservationId))
  },
  save(reservationId: string, storageUnitId: string): Promise<ApiResponse<HandoverUnitAssignment>> {
    return httpClient.put(path(reservationId), { storageUnitId })
  },
  release(reservationId: string): Promise<void> {
    return httpClient.delete(path(reservationId))
  },
}
