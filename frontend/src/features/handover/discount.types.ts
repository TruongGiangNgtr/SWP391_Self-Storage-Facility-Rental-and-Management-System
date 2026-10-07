export interface CustomerDiscount {
  discountId: string
  customerId: string
  name: string
  percentage: number
  status: 'ACTIVE' | 'INACTIVE'
  effectiveFrom: string
  effectiveTo: string | null
}
