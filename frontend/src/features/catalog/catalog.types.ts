export type FacilityStatus = 'ACTIVE' | 'INACTIVE'
export type UnitMode = 'PUBLIC' | 'PRIVATE'

export interface FacilitySummary {
  facilityId: string
  name: string
  address: string
  contactInfo: string | null
  description: string | null
  status: FacilityStatus
}

export interface RequestedPeriod {
  startMonth: string
  endMonth: string
}

export interface UnitTypeAvailability {
  unitTypeId: string
  name: string
  mode: UnitMode
  size: string
  rentalPrice: number
  description: string | null
  requestedPeriod: RequestedPeriod | null
  availableCapacity: number | null
}

export type PublicUnitTypeSummary = Pick<UnitTypeAvailability,
  'unitTypeId' | 'name' | 'mode' | 'size' | 'rentalPrice' | 'description'>

export interface PublicCatalogFilters {
  location: string
  facilityId: string
  mode: UnitMode | ''
  size: string
  unitTypeId: string
  startMonth: string
  endMonth: string
}

export interface PublicFacilityQuery extends Partial<PublicCatalogFilters> {
  page?: number
  pageSize?: number
  sortBy?: 'name'
  sortDirection?: 'asc' | 'desc'
}

export interface PublicUnitTypeQuery extends Pick<PublicFacilityQuery,
  'mode' | 'size' | 'unitTypeId' | 'startMonth' | 'endMonth' | 'page' | 'pageSize' | 'sortDirection'> {
  sortBy?: 'name' | 'rentalPrice'
  availableOnly?: boolean
}
