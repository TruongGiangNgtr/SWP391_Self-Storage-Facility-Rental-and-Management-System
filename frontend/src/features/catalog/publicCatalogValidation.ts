import type { ApiCollectionResponse, ApiResponse } from '../../api/api.types'
import type { FacilitySummary, PublicUnitTypeSummary, UnitTypeAvailability } from './catalog.types'
import { isUuid } from '../handover/handoverValidation'
import { isRentalMonth } from '../renewal/renewalValidation'
import { isSupportPagination } from '../customer-support/supportValidation'

export class PublicCatalogContractError extends Error {
  constructor() { super('Storage data could not be verified for this request. Refresh or contact the facility.'); this.name = 'PublicCatalogContractError' }
}
const nullableText = (value: unknown) => value === null || typeof value === 'string'
export function isPublicFacility(value: unknown): value is FacilitySummary {
  if (!value || typeof value !== 'object') return false
  const item = value as FacilitySummary
  return isUuid(item.facilityId) && typeof item.name === 'string' && Boolean(item.name.trim()) &&
    typeof item.address === 'string' && item.status === 'ACTIVE' && nullableText(item.contactInfo) && nullableText(item.description)
}
export function isPublicUnitType(value: unknown): value is PublicUnitTypeSummary {
  if (!value || typeof value !== 'object') return false
  const item = value as PublicUnitTypeSummary
  return isUuid(item.unitTypeId) && typeof item.name === 'string' && Boolean(item.name.trim()) &&
    ['PRIVATE', 'PUBLIC'].includes(item.mode) && typeof item.size === 'string' && Boolean(item.size.trim()) &&
    typeof item.rentalPrice === 'number' && Number.isFinite(item.rentalPrice) && item.rentalPrice >= 0 && nullableText(item.description)
}
export function isPublicAvailability(value: unknown): value is UnitTypeAvailability {
  if (!isPublicUnitType(value)) return false
  const item = value as UnitTypeAvailability
  return (item.requestedPeriod === null && item.availableCapacity === null) || Boolean(item.requestedPeriod &&
    isRentalMonth(item.requestedPeriod.startMonth) && isRentalMonth(item.requestedPeriod.endMonth) &&
    typeof item.availableCapacity === 'number' && Number.isInteger(item.availableCapacity) && item.availableCapacity >= 0)
}
export function readPublicCollection<T>(value: unknown, valid: (item: unknown) => item is T, id: (item: T) => string, page: number, pageSize: number): ApiCollectionResponse<T> {
  const response = value as ApiCollectionResponse<T> | null
  if (!response || !Array.isArray(response.data) || !response.data.every(valid) || !isSupportPagination(response.pagination) ||
    response.pagination.page !== page || response.pagination.pageSize !== pageSize || response.data.length > pageSize ||
    response.pagination.totalItems < response.data.length || response.pagination.totalPages !== Math.ceil(response.pagination.totalItems / pageSize) ||
    new Set(response.data.map(item => id(item).toLowerCase())).size !== response.data.length) throw new PublicCatalogContractError()
  return response
}
export function readPublicDetail<T>(value: unknown, valid: (item: unknown) => item is T): ApiResponse<T> {
  const response = value as ApiResponse<T> | null
  if (!response || !valid(response.data)) throw new PublicCatalogContractError()
  return response
}
