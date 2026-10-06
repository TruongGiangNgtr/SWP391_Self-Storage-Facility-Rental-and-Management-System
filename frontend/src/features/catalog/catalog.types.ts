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
