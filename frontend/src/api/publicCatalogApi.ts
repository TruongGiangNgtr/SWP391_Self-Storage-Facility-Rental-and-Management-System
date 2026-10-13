import { httpClient } from './httpClient'
import type { PublicFacilityQuery, PublicUnitTypeQuery } from '../features/catalog/catalog.types'
import { isPublicAvailability, isPublicFacility, isPublicUnitType, PublicCatalogContractError, readPublicCollection, readPublicDetail } from '../features/catalog/publicCatalogValidation'

function queryString(request: object): string {
  const query = new URLSearchParams()
  for (const [key, value] of Object.entries(request)) if (value !== undefined && value !== '') query.set(key, String(value))
  return query.toString()
}
const equalText = (a: string, b: string) => a.trim().toLowerCase() === b.trim().toLowerCase()

// SRS V10 13.13: read-only public projections. No JWT, private fields or writes.
export const publicCatalogApi = {
  async listFacilities(request: PublicFacilityQuery = {}, signal?: AbortSignal) {
    const query = { page: 1, pageSize: 20, sortBy: 'name', sortDirection: 'asc', ...request }
    const raw = await httpClient.getPublic(`/facilities?${queryString(query)}`, signal)
    const response = readPublicCollection(raw, isPublicFacility, item => item.facilityId, query.page, query.pageSize)
    // Reject inconsistent responses; never filter an unfiltered page and pretend
    // its count/pagination/availability are authoritative search results.
    if (response.data.some(item => (request.facilityId && !equalText(item.facilityId, request.facilityId)) ||
      (request.location && !`${item.name} ${item.address}`.toLowerCase().includes(request.location.trim().toLowerCase())))) throw new PublicCatalogContractError()
    return response
  },
  async getFacility(facilityId: string, signal?: AbortSignal) {
    const raw = await httpClient.getPublic(`/facilities/${encodeURIComponent(facilityId)}`, signal)
    const response = readPublicDetail(raw, isPublicFacility)
    if (!equalText(response.data.facilityId, facilityId)) throw new PublicCatalogContractError()
    return response
  },
  async listFacilityUnitTypes(facilityId: string, request: PublicUnitTypeQuery = {}, signal?: AbortSignal) {
    const query = { view: 'public', page: 1, pageSize: 20, sortBy: 'name', sortDirection: 'asc', ...request }
    const raw = await httpClient.getPublic(`/facilities/${encodeURIComponent(facilityId)}/unit-types?${queryString(query)}`, signal)
    const response = readPublicCollection(raw, isPublicAvailability, item => item.unitTypeId, query.page, query.pageSize)
    if (response.data.some(item => (request.mode && item.mode !== request.mode) ||
      (request.size && !equalText(item.size, request.size)) || (request.unitTypeId && !equalText(item.unitTypeId, request.unitTypeId)) ||
      (request.startMonth ? item.requestedPeriod?.startMonth !== request.startMonth || item.requestedPeriod?.endMonth !== request.endMonth : item.requestedPeriod !== null) ||
      (request.availableOnly && (item.availableCapacity === null || item.availableCapacity <= 0)))) throw new PublicCatalogContractError()
    return response
  },
  async listUnitTypes(request: Pick<PublicUnitTypeQuery, 'mode' | 'size' | 'unitTypeId' | 'page' | 'pageSize' | 'sortBy' | 'sortDirection'> = {}, signal?: AbortSignal) {
    const query = { page: 1, pageSize: 20, sortBy: 'name', sortDirection: 'asc', ...request }
    const raw = await httpClient.getPublic(`/unit-types?${queryString(query)}`, signal)
    const response = readPublicCollection(raw, isPublicUnitType, item => item.unitTypeId, query.page, query.pageSize)
    if (response.data.some(item => (request.mode && item.mode !== request.mode) || (request.size && !equalText(item.size, request.size)) ||
      (request.unitTypeId && !equalText(item.unitTypeId, request.unitTypeId)))) throw new PublicCatalogContractError()
    return response
  },
  async getUnitType(unitTypeId: string, signal?: AbortSignal) {
    const raw = await httpClient.getPublic(`/unit-types/${encodeURIComponent(unitTypeId)}?view=public`, signal)
    const response = readPublicDetail(raw, isPublicUnitType)
    if (!equalText(response.data.unitTypeId, unitTypeId)) throw new PublicCatalogContractError()
    return response
  },
}
