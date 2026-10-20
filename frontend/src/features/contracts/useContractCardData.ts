import { useEffect, useRef, useState } from 'react'
import { contractApi } from '../../api/contractApi'
import { reservationApi } from '../../api/reservationApi'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import { isOverviewContract, isOverviewReservation } from '../customer-overview/overviewValidation'
import type { HandoverContractDetail } from '../handover/contract.types'
import type { ReservationDetail } from '../reservations/reservation.types'
import type { ContractListItem } from './contractListPresentation'

interface CardData { contract: HandoverContractDetail | null; reservation: ReservationDetail | null; error: ApiErrorPresentation | null }
function matchesDetail(detail: HandoverContractDetail, item: ContractListItem): boolean {
  return isOverviewContract(detail) && detail.contractId === item.contractId && detail.customerId === item.customerId &&
    detail.facilityId === item.facilityId && detail.storageUnitId === item.storageUnitId && detail.status === item.status &&
    detail.startMonth === item.startMonth && detail.endMonth === item.endMonth &&
    (!item.reservationId || item.reservationId === detail.reservationId) &&
    (detail.storageUnit === undefined || Boolean(detail.storageUnit && detail.storageUnit.storageUnitId === item.storageUnitId &&
      typeof detail.storageUnit.unitTypeName === 'string' && typeof detail.storageUnit.unitCode === 'string' &&
      (!item.unitTypeId || detail.storageUnit.unitTypeId === item.unitTypeId)))
}

// Hydrate only this paginated collection. The server supplies every amount;
// current catalog prices and client-side financial formulas are never used.
export function useContractCardData(items: readonly ContractListItem[]) {
  const refsKey = JSON.stringify(items.map(({ contractId, customerId, facilityId, storageUnitId, startMonth, endMonth, status, reservationId, unitTypeId }) =>
    ({ contractId, customerId, facilityId, storageUnitId, startMonth, endMonth, status, reservationId, unitTypeId })))
  const [revision, setRevision] = useState(0)
  const key = `${refsKey}:${revision}`
  const cache = useRef(new Map<string, CardData>())
  const [snapshot, setSnapshot] = useState<{ key: string; reads: Record<string, CardData> } | null>(null)
  const reads = snapshot?.key === key ? snapshot.reads : {}
  useEffect(() => {
    const refs: ContractListItem[] = JSON.parse(refsKey)
    if (!refs.length) return
    let active = true, cursor = 0
    const results: Record<string, CardData> = {}
    // A shared Reservation is fetched once, including while its GET is in flight.
    const reservations = new Map<string, ReturnType<typeof reservationApi.get>>()
    async function worker() {
      while (active && cursor < refs.length) {
        const item = refs[cursor++], cacheKey = JSON.stringify(item)
        let result = cache.current.get(cacheKey)
        if (!result) {
          result = { contract: null, reservation: null, error: null }
          try {
            const { data: detail } = await contractApi.get(item.contractId)
            if (!matchesDetail(detail, item)) {
              result.error = presentValidationError('Contract details have changed or could not be verified. Refresh the list before choosing an action.')
            } else {
              result.contract = detail
              let pending = reservations.get(detail.reservationId)
              if (!pending) { pending = reservationApi.get(detail.reservationId); reservations.set(detail.reservationId, pending) }
              const { data: original } = await pending
              const invoice = original?.depositInvoice
              if (isOverviewReservation(original, detail.reservationId) && original.customerId === detail.customerId &&
                original.facilityId === detail.facilityId && (!item.unitTypeId || original.unitTypeId === item.unitTypeId) &&
                (detail.storageUnit === undefined || (detail.storageUnit && original.unitTypeId === detail.storageUnit.unitTypeId)) &&
                invoice && Number.isFinite(invoice.amountDue) && invoice.amountDue >= 0) result.reservation = original
              else result.error = presentValidationError('The original rental and deposit snapshot could not be verified. Please retry.')
            }
          } catch (error) { result.error = presentApiError(error) }
          if (!result.error && active) cache.current.set(cacheKey, result)
        }
        if (!active) return
        results[item.contractId] = result
      }
    }
    void Promise.all(Array.from({ length: Math.min(4, refs.length) }, worker)).then(() => {
      if (active) setSnapshot({ key, reads: results })
    })
    return () => { active = false }
  }, [refsKey, key])
  return { reads, loading: items.some(item => !reads[item.contractId]), error: Object.values(reads).find(read => read.error)?.error ?? null,
    retry: () => { cache.current.clear(); setRevision(value => value + 1) } }
}
