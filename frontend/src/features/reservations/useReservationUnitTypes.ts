import { useEffect, useRef, useState } from 'react'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import { catalogApi } from '../../api/catalogApi'
import type { UnitMode } from '../catalog/catalog.types'
import { isUuid } from '../handover/handoverValidation'

// Display-only projections: never consume current catalog prices for a Reservation.
export interface ReservationUnitTypeDisplay { unitTypeId: string; name: string; mode?: UnitMode; size?: string }
export interface ReservationFacilityDisplay { facilityId: string; name: string; address: string }
interface CatalogRead<T> { data: T | null; error: ApiErrorPresentation | null }
class ReservationCatalogContractError extends Error {}

async function readUnitType(id: string): Promise<ReservationUnitTypeDisplay> {
  const { data } = await catalogApi.getUnitType(id)
  if (!data || data.unitTypeId !== id || typeof data.name !== 'string' || !data.name.trim()) throw new ReservationCatalogContractError()
  return { unitTypeId: data.unitTypeId, name: data.name,
    mode: data.mode === 'PUBLIC' || data.mode === 'PRIVATE' ? data.mode : undefined,
    size: typeof data.size === 'string' ? data.size : undefined }
}
async function readFacility(id: string): Promise<ReservationFacilityDisplay> {
  const { data } = await catalogApi.getFacility(id)
  if (!data || data.facilityId !== id || typeof data.name !== 'string' || !data.name.trim() || typeof data.address !== 'string') throw new ReservationCatalogContractError()
  return { facilityId: data.facilityId, name: data.name, address: data.address }
}

// Resolve distinct catalog references, cache successful reads per page and cap parallelism.
// Both hooks use the same reader instead of issuing one request per Reservation card.
function useReservationCatalog<T>(catalogIds: readonly string[], read: (id: string) => Promise<T>) {
  const idsKey = [...new Set(catalogIds)].sort().join(',')
  const [revision, setRevision] = useState(0)
  const key = `${idsKey}:${revision}`
  const cache = useRef(new Map<string, T>())
  const [snapshot, setSnapshot] = useState<{ key: string; reads: Record<string, CatalogRead<T>> } | null>(null)
  const reads = snapshot?.key === key ? snapshot.reads : {}
  useEffect(() => {
    if (!idsKey) return
    let active = true
    let cursor = 0
    const ids = idsKey.split(',')
    async function worker() {
      while (cursor < ids.length && active) {
        const id = ids[cursor++]
        let result: CatalogRead<T>
        try {
          const cached = cache.current.get(id)
          if (cached) result = { data: cached, error: null }
          else {
            if (!isUuid(id)) throw new Error('Invalid catalog reference')
            const data = await read(id)
            if (active) cache.current.set(id, data)
            result = { data, error: null }
          }
        } catch (error) {
          result = { data: null, error: error instanceof ReservationCatalogContractError
            ? presentValidationError('Storage details could not be verified. Please retry.') : presentApiError(error) }
        }
        if (!active) return
        setSnapshot(current => ({ key, reads: { ...(current?.key === key ? current.reads : {}), [id]: result } }))
      }
    }
    void Promise.all(Array.from({ length: Math.min(4, ids.length) }, worker))
    return () => { active = false }
  }, [idsKey, key, read])
  return { reads, loading: Boolean(idsKey) && catalogIds.some(id => !reads[id]),
    error: Object.values(reads).find(result => result.error)?.error ?? null,
    retry: () => { cache.current.clear(); setRevision(value => value + 1) } }
}

export const useReservationUnitTypes = (ids: readonly string[]) => useReservationCatalog(ids, readUnitType)
export const useReservationFacilities = (ids: readonly string[]) => useReservationCatalog(ids, readFacility)
