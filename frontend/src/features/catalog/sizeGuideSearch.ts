import type { PublicCatalogFilters, UnitMode } from './catalog.types'
import { publicSearchQuery } from './publicSearch'
import type { PublicUnitTypeSummary } from './catalog.types'

export type SizeGuideSort = 'name:asc' | 'name:desc' | 'rentalPrice:asc' | 'rentalPrice:desc'
const SORTS: readonly SizeGuideSort[] = ['name:asc', 'name:desc', 'rentalPrice:asc', 'rentalPrice:desc']

export function readSizeGuideSearch(search: string) {
  const params = new URLSearchParams(search)
  const rawMode = params.get('mode') ?? 'PRIVATE'
  const mode: UnitMode = rawMode === 'PUBLIC' ? 'PUBLIC' : 'PRIVATE'
  const page = params.has('page') ? Number(params.get('page')) : 1
  const rawSort = `${params.get('sortBy') ?? 'name'}:${params.get('sortDirection') ?? 'asc'}`
  const sort: SizeGuideSort = SORTS.includes(rawSort as SizeGuideSort) ? rawSort as SizeGuideSort : 'name:asc'
  const [sortBy, sortDirection] = sort.split(':') as ['name' | 'rentalPrice', 'asc' | 'desc']
  const duplicate = ['mode', 'page', 'sortBy', 'sortDirection'].some(key => params.getAll(key).length > 1)
  const error = duplicate ? 'This Size Guide link contains repeated filters. Choose a storage type to start again.' :
    !['PRIVATE', 'PUBLIC'].includes(rawMode) ? 'Choose Private or Public Storage to open the Size Guide.' :
      !Number.isSafeInteger(page) || page < 1 ? 'This Size Guide page is invalid. Choose a storage type to start again.' :
        rawSort !== sort ? 'This Size Guide sort order is not supported. Choose a storage type to start again.' : null
  return { mode, page, sort, sortBy, sortDirection, error }
}

export function sizeGuideUrl(mode: UnitMode, page = 1, sort: SizeGuideSort = 'name:asc'): string {
  const params = new URLSearchParams({ mode })
  if (page !== 1) params.set('page', String(page))
  const [sortBy, sortDirection] = sort.split(':')
  if (sortBy !== 'name') params.set('sortBy', sortBy)
  if (sortDirection !== 'asc') params.set('sortDirection', sortDirection)
  return `/size-guide?${params}`
}

export function sizeGuideStorageUrl(item: PublicUnitTypeSummary): string {
  const filters: PublicCatalogFilters = { location: '', facilityId: '', mode: item.mode, size: item.size, unitTypeId: item.unitTypeId, startMonth: '', endMonth: '' }
  return `/facilities?${publicSearchQuery(filters)}`
}
