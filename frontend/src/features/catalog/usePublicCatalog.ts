import { useEffect, useState } from 'react'
import { ApiRequestError } from '../../api/httpClient'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import { publicCatalogApi } from '../../api/publicCatalogApi'
import { PublicCatalogContractError } from './publicCatalogValidation'
import type { PublicFacilityQuery, PublicUnitTypeQuery } from './catalog.types'

export function publicCatalogError(error: unknown): ApiErrorPresentation {
  if (error instanceof PublicCatalogContractError) return presentValidationError(error.message)
  if (error instanceof ApiRequestError && error.status === 401) return { message: 'Public storage browsing is not available from the server yet. Please try again later.', traceId: error.traceId, details: [] }
  return presentApiError(error)
}
export function usePublicRead<T>(key: string, read: (key: string, signal: AbortSignal) => Promise<T>, enabled = true, presentError = publicCatalogError) {
  const [revision, setRevision] = useState(0)
  const current = `${key}:${revision}`
  const [snapshot, setSnapshot] = useState<{ key: string; data: T | null; error: ApiErrorPresentation | null } | null>(null)
  const loading = enabled && snapshot?.key !== current
  useEffect(() => {
    if (!enabled) return
    const controller = new AbortController()
    read(key, controller.signal).then(data => { if (!controller.signal.aborted) setSnapshot({ key: current, data, error: null }) })
      .catch((error: unknown) => { if (!controller.signal.aborted) setSnapshot({ key: current, data: null, error: presentError(error) }) })
    return () => controller.abort()
  }, [key, current, read, enabled, presentError])
  return { data: enabled && !loading ? snapshot?.data ?? null : null, error: enabled && !loading ? snapshot?.error ?? null : null, loading, refresh: () => setRevision(value => value + 1) }
}
const readFacilities = (key: string, signal: AbortSignal) => publicCatalogApi.listFacilities(JSON.parse(key) as PublicFacilityQuery, signal)
const readFacility = (key: string, signal: AbortSignal) => publicCatalogApi.getFacility(key, signal)
const readSizes = (key: string, signal: AbortSignal) => publicCatalogApi.listUnitTypes(JSON.parse(key), signal)
const readFacilityTypes = (key: string, signal: AbortSignal) => {
  const { facilityId, query } = JSON.parse(key) as { facilityId: string; query: PublicUnitTypeQuery }
  return publicCatalogApi.listFacilityUnitTypes(facilityId, query, signal)
}
export const usePublicFacilities = (query: PublicFacilityQuery, enabled = true) => usePublicRead(JSON.stringify(query), readFacilities, enabled)
export const usePublicFacility = (facilityId: string, enabled = true) => usePublicRead(facilityId, readFacility, enabled)
export const usePublicSizes = (mode: string, page = 1) => usePublicRead(JSON.stringify({ ...(mode ? { mode } : {}), page, pageSize: 100 }), readSizes, ['', 'PRIVATE', 'PUBLIC'].includes(mode))
export const usePublicUnitTypes = (query: NonNullable<Parameters<typeof publicCatalogApi.listUnitTypes>[0]>, enabled = true) => usePublicRead(JSON.stringify(query), readSizes, enabled)
export const usePublicFacilityTypes = (facilityId: string, query: PublicUnitTypeQuery, enabled = true) => usePublicRead(JSON.stringify({ facilityId, query }), readFacilityTypes, enabled)
