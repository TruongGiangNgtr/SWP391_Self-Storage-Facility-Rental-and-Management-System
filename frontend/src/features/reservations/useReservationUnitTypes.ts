import { useEffect, useRef, useState } from 'react'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import { catalogApi } from '../../api/catalogApi'
import type { UnitTypeAvailability } from '../catalog/catalog.types'
import { isUuid } from '../handover/handoverValidation'

type UnitTypeDisplay = Pick<UnitTypeAvailability, 'unitTypeId' | 'name'>
interface UnitTypeRead { data: UnitTypeDisplay | null; error: ApiErrorPresentation | null }

// Resolve distinct types, not one request per Reservation. Reuse successful reads
// within this page instance, cap parallel requests and clear them on explicit retry.
// Catalog prices are deliberately never used in Reservation/Invoice money fields.
export function useReservationUnitTypes(unitTypeIds: readonly string[]) {
  const idsKey = [...new Set(unitTypeIds)].sort().join(',')
  const [revision, setRevision] = useState(0)
  const key = `${idsKey}:${revision}`
  const cache = useRef(new Map<string, UnitTypeDisplay>())
  const [snapshot, setSnapshot] = useState<{ key: string; reads: Record<string, UnitTypeRead> } | null>(null)
  const reads = snapshot?.key === key ? snapshot.reads : {}

  useEffect(() => {
    if (!idsKey) return
    let active = true
    let cursor = 0
    const ids = idsKey.split(',')
    async function worker() {
      while (cursor < ids.length && active) {
        const id = ids[cursor++]
        let read: UnitTypeRead
        try {
          const cached = cache.current.get(id)
          if (cached) read = { data: cached, error: null }
          else {
            if (!isUuid(id)) throw new Error('Invalid unit type reference')
            const { data } = await catalogApi.getUnitType(id)
            if (!data || data.unitTypeId !== id || typeof data.name !== 'string' || !data.name.trim()) {
              read = { data: null, error: presentValidationError('The unit type name could not be verified. Please retry.') }
            } else {
              const display = { unitTypeId: data.unitTypeId, name: data.name }
              if (active) cache.current.set(id, display)
              read = { data: display, error: null }
            }
          }
        } catch (error) { read = { data: null, error: presentApiError(error) } }
        if (!active) return
        setSnapshot(current => ({ key, reads: { ...(current?.key === key ? current.reads : {}), [id]: read } }))
      }
    }
    // This page's Reservation collection is already bounded by API pagination.
    void Promise.all(Array.from({ length: Math.min(4, ids.length) }, worker))
    return () => { active = false }
  }, [idsKey, key])

  return {
    reads,
    loading: Boolean(idsKey) && unitTypeIds.some(id => !reads[id]),
    error: Object.values(reads).find(read => read.error)?.error ?? null,
    retry: () => { cache.current.clear(); setRevision(value => value + 1) },
  }
}
