export type FacilityStatus =
  | 'ACTIVE'
  | 'INACTIVE'

export interface Facility {
  facilityId: string
  name: string
  address: string
  contactInfo: string | null
  description: string | null
  status: FacilityStatus
}

export interface CreateFacilityRequest {
  name: string
  address: string
  contactInfo?: string | null
  description?: string | null
}
export interface UpdateFacilityRequest {
  name: string
  address: string
  contactInfo?: string | null
  description?: string | null
}