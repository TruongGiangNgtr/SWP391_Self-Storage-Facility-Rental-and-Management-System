export type DiscountStatus = 'ACTIVE' | 'INACTIVE'

export interface Discount {
  discountId: string
  customerId: string
  name: string
  percentage: number
  status: DiscountStatus
  effectiveFrom: string
  effectiveTo: string | null
}

export interface CreateCustomerDiscountRequest {
  name: string
  percentage: number
  effectiveFrom: string
  effectiveTo: string | null
}

export interface UpdateDiscountRequest {
  name: string
  percentage: number
  status: DiscountStatus
  effectiveFrom: string
  effectiveTo: string | null
}
