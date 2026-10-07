export type UnitTypeMode = 'PUBLIC' | 'PRIVATE'

export interface UnitType {
  unitTypeId: string
  name: string
  mode: UnitTypeMode
  size: string
  rentalPrice: number
  description: string | null
}

export interface UpdateUnitTypePriceRequest {
  rentalPrice: number
}
