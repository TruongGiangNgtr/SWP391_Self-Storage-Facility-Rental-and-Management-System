import type { CreateReservationRequest } from './reservation.types'

function businessMonthParts(date: Date): { year: number; month: number } {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: 'Asia/Ho_Chi_Minh',
    year: 'numeric',
    month: '2-digit',
  }).formatToParts(date)

  const year = Number(parts.find((part) => part.type === 'year')?.value)
  const month = Number(parts.find((part) => part.type === 'month')?.value)

  return { year, month }
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
    return 'Vui lòng chọn tháng bắt đầu và tháng kết thúc thuê.'
  }

  if (startMonth <= currentMonth) {
    return 'Tháng bắt đầu thuê phải sau tháng hiện tại.'
  }

  if (endMonth < startMonth) {
    return 'Tháng kết thúc thuê không được trước tháng bắt đầu.'
  }

  return null
}

export function validateCreateReservation(
  request: CreateReservationRequest,
): string | null {
  if (!request.facilityId) {
    return 'Vui lòng chọn địa điểm lưu trữ.'
  }

  const monthError = validateMonthRange(request.startMonth, request.endMonth)
  if (monthError) {
    return monthError
  }

  if (!request.unitTypeId) {
    return 'Vui lòng chọn loại và kích thước kho.'
  }

  return null
}
