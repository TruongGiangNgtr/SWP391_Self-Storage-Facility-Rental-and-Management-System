import type { HandoverUnitAssignment } from './handover.types'
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
  if (!isUuid(storageUnitId.trim())) return 'A valid active Manager assignment is required before completing handover.'
  if (!receiptAcknowledged) return 'Confirm that you have received the full first-month rent offline before completing handover.'
  return null
}

export function isActiveHandoverAssignment(assignment: HandoverUnitAssignment | null | undefined, reservationId: string): assignment is HandoverUnitAssignment {
  return assignment != null && assignment.status === 'ACTIVE' &&
    assignment.reservationId === reservationId && isUuid(assignment.assignmentId) && isUuid(assignment.storageUnitId)
}

export function sameHandoverAssignment(left: HandoverUnitAssignment, right: HandoverUnitAssignment): boolean {
  return left.assignmentId === right.assignmentId && left.storageUnitId === right.storageUnitId &&
    left.assignedAt === right.assignedAt && left.unitCode === right.unitCode && left.locationInfo === right.locationInfo
}
