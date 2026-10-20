import { useEffect, useRef, useState } from 'react'
import { contractApi } from '../../api/contractApi'
import { reservationApi } from '../../api/reservationApi'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import { isOverviewContract, isOverviewReservation } from '../customer-overview/overviewValidation'
import type { ReservationStatus } from '../reservations/reservation.types'
import type { HandoverContractDetail } from '../handover/contract.types'
import { contractTitle, contractUnit } from '../../utils/customerPresentation'
import type { VisitDetail } from './visit.types'

export interface VisitContext {
  facilityId: string
  unitTypeId?: string
  unitTypeName?: string
  unitCode?: string
  status: ReservationStatus | HandoverContractDetail['status']
}
interface ContextRead { data: VisitContext | null; error: ApiErrorPresentation | null }
export const visitContextKey = (visit: VisitDetail) => `${visit.visitType === 'RESERVATION' ? 'reservation' : 'contract'}:${visit.entityId}`

// The Visit projection has no facility/name fields. Resolve only distinct referenced
// resources on the current API page, with four workers and a page-instance cache.
export function useVisitContexts(visits: readonly VisitDetail[]) {
  const refsKey = [...new Set(visits.map(visitContextKey))].sort().join(',')
  const [revision, setRevision] = useState(0)
  const key = `${refsKey}:${revision}`
  const cache = useRef(new Map<string, VisitContext>())
  const [snapshot, setSnapshot] = useState<{ key: string; reads: Record<string, ContextRead> } | null>(null)
  const reads = snapshot?.key === key ? snapshot.reads : {}
  useEffect(() => {
    if (!refsKey) return
    let active = true, cursor = 0
    const refs = refsKey.split(',')
    const results: Record<string, ContextRead> = {}
    async function worker() {
      while (active && cursor < refs.length) {
        const ref = refs[cursor++]
        const [kind, id] = ref.split(':')
        let result: ContextRead
        try {
          let data = cache.current.get(ref)
          if (!data && kind === 'reservation') {
            const response = await reservationApi.get(id)
            if (isOverviewReservation(response?.data, id)) data = { facilityId: response.data.facilityId, unitTypeId: response.data.unitTypeId, status: response.data.status }
          } else if (!data) {
            const response = await contractApi.get(id)
            if (isOverviewContract(response?.data) && response.data.contractId === id) data = {
              facilityId: response.data.facilityId, status: response.data.status,
              unitTypeName: contractTitle(response.data), unitCode: contractUnit(response.data),
            }
          }
          if (data && active) cache.current.set(ref, data)
          result = data ? { data, error: null } : { data: null, error: presentValidationError('The related reservation or contract could not be verified. Please retry.') }
        } catch (error) { result = { data: null, error: presentApiError(error) } }
        if (!active) return
        results[ref] = result
      }
    }
    // Publish the complete set together so downstream catalog readers do not
    // restart in-flight requests as individual entity responses arrive.
    void Promise.all(Array.from({ length: Math.min(4, refs.length) }, worker)).then(() => {
      if (active) setSnapshot({ key, reads: results })
    })
    return () => { active = false }
  }, [refsKey, key])
  return { reads, loading: Boolean(refsKey) && visits.some(visit => !reads[visitContextKey(visit)]),
    error: Object.values(reads).find(read => read.error)?.error ?? null,
    retry: () => { cache.current.clear(); setRevision(value => value + 1) } }
}
