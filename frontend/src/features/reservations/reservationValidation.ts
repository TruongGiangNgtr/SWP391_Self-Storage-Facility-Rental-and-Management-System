import type { CreateReservationRequest } from './reservation.types'

function businessMonthParts(date: Date): { year: number; month: number } {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: 'Asia/Ho_Chi_Minh',
    year: 'numeric',
    month: '2-digit',
  }).formatToParts(date)

  return {
    year: Number(parts.find((part) => part.type === 'year')?.value),
    month: Number(parts.find((part) => part.type === 'month')?.value),
  }
}

function formatMonth(year: number, month: number): string {
  return `${year.toString().padStart(4, '0')}-${month.toString().padStart(2, '0')}`
}

export function getCurrentBusinessMonth(date = new Date()): string {
  const { year, month } = businessMonthParts(date)
  return formatMonth(year, month)
}

export function getNextBusinessMonth(date = new Date()): string {
  const { year, month } = businessMonthParts(date)
  return month === 12 ? formatMonth(year + 1, 1) : formatMonth(year, month + 1)
}

export function validateMonthRange(
  startMonth: string,
  endMonth: string,
  currentMonth = getCurrentBusinessMonth(),
): string | null {
  if (!startMonth || !endMonth) {
    return 'Please select both the check-in and check-out months.'
  }

  const monthPattern = /^\d{4}-(0[1-9]|1[0-2])$/
  if (!monthPattern.test(startMonth) || !monthPattern.test(endMonth)) {
    return 'Rental months must use a valid YYYY-MM format.'
  }

  if (startMonth <= currentMonth) {
    return 'The check-in month must be after the current month.'
  }

  if (endMonth < startMonth) {
    return 'The check-out month cannot be before the check-in month.'
  }

  return null
}

export function validateCreateReservation(
  request: CreateReservationRequest,
): string | null {
  if (!request.facilityId || !request.unitTypeId) {
    return 'The selected storage information is incomplete.'
  }

  return validateMonthRange(request.startMonth, request.endMonth)
}
