import type { RenewContractResult } from './renewal.types'

export function isRentalMonth(value: string): boolean {
  return /^\d{4}-(0[1-9]|1[0-2])$/.test(value) && value.slice(0, 4) !== '0000'
}

export function getExtensionStartMonth(oldEndMonth: string): string | null {
  if (!isRentalMonth(oldEndMonth) || oldEndMonth === '9999-12') return null
  const [year, month] = oldEndMonth.split('-').map(Number)
  return month === 12 ? `${String(year + 1).padStart(4, '0')}-01`
    : `${String(year).padStart(4, '0')}-${String(month + 1).padStart(2, '0')}`
}

export function validateRenewalMonth(oldEndMonth: string, newEndMonth: string): string | null {
  if (!isRentalMonth(oldEndMonth)) return 'The server has not supplied a valid current end month. Refresh the contract.'
  if (!newEndMonth) return 'Please select the new end month.'
  if (!isRentalMonth(newEndMonth)) return 'The new end month must use a valid YYYY-MM format.'
  if (newEndMonth <= oldEndMonth) return 'The new end month must be after the current contract end month.'
  return null
}

export function isRenewContractResult(value: unknown, contractId: string, targetEndMonth: string): value is RenewContractResult {
  if (!value || typeof value !== 'object') return false
  const result = value as Partial<RenewContractResult>
  return result.contractId === contractId && result.status === 'ACTIVE' &&
    typeof result.oldEndMonth === 'string' && isRentalMonth(result.oldEndMonth) &&
    typeof result.newEndMonth === 'string' && isRentalMonth(result.newEndMonth) &&
    result.newEndMonth === targetEndMonth && result.newEndMonth > result.oldEndMonth &&
    typeof result.appliedMonthlyPrice === 'number' && Number.isFinite(result.appliedMonthlyPrice) && result.appliedMonthlyPrice >= 0
}
