import { useEffect, useRef, useState } from 'react'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import { ApiRequestError } from '../../api/httpClient'
import { reservationApi } from '../../api/reservationApi'
import { visitApi } from '../../api/visitApi'
import type { ReservationDetail } from '../reservations/reservation.types'
import type { VisitDetail } from '../visits/visit.types'
import { isReservationWorkItem, type CompleteHandoverRequest, type CompleteHandoverResult, type ReservationWorkItem } from './handover.types'
import { getHandoverContextError, isUuid } from './handoverValidation'

interface HandoverContext {
  reservation: ReservationDetail
  visit: VisitDetail
  workItem: ReservationWorkItem | null
}

export function useReservationHandover(reservationId: string, visitId: string, workPage: number) {
  const [snapshot, setSnapshot] = useState<{ key: string; context: HandoverContext | null; error: ApiErrorPresentation | null } | null>(null)
  const [retry, setRetry] = useState(0)
  const [action, setAction] = useState<'check-in' | 'handover' | null>(null)
  const [actionError, setActionError] = useState<ApiErrorPresentation | null>(null)
  const revision = useRef(0)
  const actionBusy = useRef(false)
  const mounted = useRef(false)
  const key = `${reservationId}:${visitId}:${workPage}:${retry}`
  const loading = snapshot?.key !== key
  const context = loading ? null : snapshot?.context ?? null
  const error = actionError ?? (loading ? null : snapshot?.error ?? null)

  useEffect(() => {
    mounted.current = true
    const current = ++revision.current
    async function load() {
      try {
        const [reservationResponse, visitResponse] = await Promise.all([
          reservationApi.get(reservationId), visitApi.get(visitId),
        ])
        if (current !== revision.current) return
        const reservation = reservationResponse.data
        const visit = visitResponse.data
        const contextError = getHandoverContextError(reservation, visit, reservationId, visitId)
        if (contextError) {
          setSnapshot({ key, context: null, error: presentValidationError(contextError) })
          return
        }
        // Read exactly the work-list page from navigation, not an unbounded list scan.
        const workResponse = await visitApi.listStaffWorkItems({ date: visit.visitDate, page: workPage })
        if (current !== revision.current) return
        const workItem = workResponse.data.find((item): item is ReservationWorkItem =>
          isReservationWorkItem(item) && item.referenceId === visitId && item.entityId === reservationId) ?? null
        setSnapshot({ key, context: { reservation, visit, workItem }, error: null })
      } catch (caughtError) {
        if (current === revision.current) setSnapshot({ key, context: null, error: presentApiError(caughtError) })
      }
    }
    void load()
    return () => { mounted.current = false; revision.current += 1 }
  }, [reservationId, visitId, workPage, key])

  function refresh(retainedError: ApiErrorPresentation | null = null) {
    revision.current += 1
    setActionError(retainedError)
    setRetry((value) => value + 1)
  }

  async function checkIn() {
    if (actionBusy.current || !context || context.reservation.status !== 'CONFIRMED' || context.visit.status !== 'SCHEDULED') return
    actionBusy.current = true
    const current = revision.current
    setAction('check-in')
    setActionError(null)
    try {
      await visitApi.checkIn(visitId)
      if (current === revision.current) refresh()
    } catch (caughtError) {
      if (current === revision.current) {
        const presentation = presentApiError(caughtError)
        if (caughtError instanceof ApiRequestError && caughtError.status === 409) refresh(presentation)
        else setActionError(presentation)
      }
    } finally {
      actionBusy.current = false
      if (mounted.current) setAction(null)
    }
  }

  async function complete(request: CompleteHandoverRequest): Promise<CompleteHandoverResult | null> {
    if (actionBusy.current || !context || context.reservation.status !== 'CONFIRMED' || context.visit.status !== 'CHECKED_IN') return null
    actionBusy.current = true
    const current = revision.current
    setAction('handover')
    setActionError(null)
    try {
      const response = await visitApi.completeHandover(reservationId, request)
      if (current !== revision.current) return null
      const result = response?.data
      if (!result?.contract?.contractId || !isUuid(result.contract.contractId) || result.contract.status !== 'ACTIVE' ||
        result.reservationStatus !== 'COMPLETED' || result.visitStatus !== 'CHECKED_OUT' || result.storageUnitStatus !== 'IN_USE') {
        refresh(presentValidationError('The server returned an unexpected handover result. Refresh before retrying, and do not collect the rent again.'))
        return null
      }
      return result
    } catch (caughtError) {
      if (current === revision.current) {
        const presentation = presentApiError(caughtError)
        if (caughtError instanceof ApiRequestError && caughtError.status === 409) refresh(presentation)
        else setActionError(presentation)
      }
      return null
    } finally {
      actionBusy.current = false
      if (mounted.current) setAction(null)
    }
  }

  return { context, loading, action, error, refresh, checkIn, complete }
}
