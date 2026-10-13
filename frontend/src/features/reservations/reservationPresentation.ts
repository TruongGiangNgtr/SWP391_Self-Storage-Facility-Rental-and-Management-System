import type { ReservationDetail, ReservationStatus } from './reservation.types'

export const RESERVATION_FILTER_LABELS: Record<ReservationStatus, string> = {
  PENDING_DEPOSIT: 'Awaiting Deposit / Handover Date',
  CONFIRMED: 'Confirmed',
  COMPLETED: 'Completed',
  CANCELLED: 'Cancelled',
}

// Presentation only: PAID does not transition a Reservation to CONFIRMED.
// RES-004 confirms it and creates the handover Visit after the customer chooses a date.
export function getReservationStatusLabel(reservation: Pick<ReservationDetail, 'status' | 'depositInvoice'>): string {
  if (reservation.status === 'PENDING_DEPOSIT') {
    return reservation.depositInvoice.status === 'PAID' ? 'Awaiting Handover Date' : 'Awaiting Deposit'
  }
  return RESERVATION_FILTER_LABELS[reservation.status]
}
