import type { PublicCatalogFilters, PublicFacilityQuery } from './catalog.types'
import { isUuid } from '../handover/handoverValidation'
import { getNextBusinessMonth, validateMonthRange } from '../reservations/reservationValidation'

export function initialPublicFilters(): PublicCatalogFilters {
  const month = getNextBusinessMonth()
  return { location: '', facilityId: '', mode: '', size: '', unitTypeId: '', startMonth: month, endMonth: month }
}
export function validatePublicFilters(filters: PublicCatalogFilters, requirePeriod = false): string | null {
  if (filters.mode && !['PRIVATE', 'PUBLIC'].includes(filters.mode)) return 'Select Private or Public Storage.'
  if ((filters.facilityId && !isUuid(filters.facilityId)) || (filters.unitTypeId && !isUuid(filters.unitTypeId))) return 'The storage selection link is invalid. Please start a new search.'
  if (requirePeriod || filters.startMonth || filters.endMonth) return validateMonthRange(filters.startMonth, filters.endMonth)
  return null
}
export function readPublicSearch(search: string) {
  const params = new URLSearchParams(search)
  const filters: PublicCatalogFilters = {
    location: params.get('location')?.trim() ?? '', facilityId: params.get('facilityId') ?? '',
    mode: (params.get('mode') ?? '') as PublicCatalogFilters['mode'], size: params.get('size')?.trim() ?? '',
    unitTypeId: params.get('unitTypeId') ?? '', startMonth: params.get('startMonth') ?? '', endMonth: params.get('endMonth') ?? '',
  }
  const page = params.has('page') ? Number(params.get('page')) : 1
  const pageSize = params.has('pageSize') ? Number(params.get('pageSize')) : 20
  const sortBy = params.get('sortBy') ?? 'name'
  const sortDirection = params.get('sortDirection') ?? 'asc'
  const error = validatePublicFilters(filters) || (!Number.isInteger(page) || page < 1 || !Number.isInteger(pageSize) || pageSize < 1 || pageSize > 100
    ? 'The search page is invalid. Please start a new search.' : sortBy !== 'name' || !['asc', 'desc'].includes(sortDirection)
      ? 'The search sort order is not supported.' : null)
  return { filters, page, pageSize, sortDirection: sortDirection as 'asc' | 'desc', error }
}
export function publicSearchQuery(filters: PublicCatalogFilters, extra: Pick<PublicFacilityQuery, 'page' | 'pageSize' | 'sortDirection'> = {}): string {
  const params = new URLSearchParams()
  for (const [key, value] of Object.entries(filters)) if (value) params.set(key, value)
  if (extra.page && extra.page !== 1) params.set('page', String(extra.page))
  if (extra.pageSize && extra.pageSize !== 20) params.set('pageSize', String(extra.pageSize))
  if (extra.sortDirection === 'desc') params.set('sortDirection', 'desc')
  return params.toString()
}
