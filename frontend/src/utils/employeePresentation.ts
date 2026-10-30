import type { ApiCollectionResponse } from '../api/api.types'
import type { HandoverContractDetail } from '../features/handover/contract.types'
import { customerLabel } from './customerPresentation'
import { formatMonthRange } from './formatters'

const UUID = /\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b/i
const MONTH = /^\d{4}-(0[1-9]|1[0-2])$/
type ContractDisplay = Partial<Pick<HandoverContractDetail, 'unitCode' | 'unitTypeName' | 'storageUnit' | 'startMonth' | 'endMonth' | 'status'>>

export async function fetchAllEmployeePages<T>(
  readPage: (page: number, pageSize: number) => Promise<ApiCollectionResponse<T>>,
  pageSize = 100,
  maxPages = 50,
): Promise<{ items: T[]; totalItems: number }> {
  const first = await readPage(1, pageSize)
  const items = [...(first.data ?? [])]
  const totalPages = Math.min(first.pagination?.totalPages ?? 1, maxPages)
  for (let page = 2; page <= totalPages; page += 1) {
    const next = await readPage(page, pageSize)
    items.push(...(next.data ?? []))
  }
  return {
    items,
    totalItems: Math.max(first.pagination?.totalItems ?? items.length, items.length),
  }
}

// Presentation only: use API-supplied names/codes, never derive a code from an ID.
export function employeeText(value: unknown, fallback = '—'): string {
  return typeof value === 'string' && value.trim() && !UUID.test(value) ? value.trim() : fallback
}

export function employeeMonthRange(start: unknown, end: unknown): string {
  return typeof start === 'string' && MONTH.test(start) && typeof end === 'string' && MONTH.test(end)
    ? formatMonthRange(start, end)
    : '—'
}

export function employeeUnitLabel(contract?: ContractDisplay | null): string {
  return employeeText(contract?.unitCode, '') || employeeText(contract?.storageUnit?.unitCode)
}

export function employeeContractLabel(contract?: ContractDisplay | null): string {
  if (!contract) return '—'
  const title = employeeText(contract.unitTypeName, '') || employeeText(contract.storageUnit?.unitTypeName, '')
  const unit = employeeUnitLabel(contract)
  if (!title && unit === '—') return '—'
  const period = employeeMonthRange(contract.startMonth, contract.endMonth)
  const status = employeeText(contract.status, '')
  return [title, unit === '—' ? '' : unit, period === '—' ? '' : period, status ? customerLabel(status) : '']
    .filter(Boolean).join(' · ') || '—'
}
