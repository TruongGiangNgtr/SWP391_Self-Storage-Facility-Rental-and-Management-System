import type { ReservationDetail } from '../reservations/reservation.types'
import type { VisitDetail } from '../visits/visit.types'

export function isUuid(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value)
}

export function getHandoverContextError(reservation: ReservationDetail, visit: VisitDetail, reservationId: string, visitId: string): string | null {
  if (reservation.reservationId !== reservationId || visit.visitId !== visitId ||
    visit.visitType !== 'RESERVATION' || visit.entityId !== reservation.reservationId) {
    return 'The server returned a visit that does not match this reservation. Return to the work list and refresh.'
  }
  if (!Number.isFinite(reservation.lockedRentalPrice) || reservation.lockedRentalPrice < 0) {
    return 'The server has not supplied a valid locked rental price. Handover cannot proceed.'
  }
  return null
}

export function getHandoverInputError(storageUnitId: string, receiptAcknowledged: boolean): string | null {
  if (!isUuid(storageUnitId.trim())) return 'Enter the valid Storage Unit ID supplied by the Manager.'
  if (!receiptAcknowledged) return 'Confirm that you have received the full first-month rent offline before completing handover.'
  return null
}
