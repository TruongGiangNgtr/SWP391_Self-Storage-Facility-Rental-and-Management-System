import { useEffect, useRef, useState } from 'react'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import { contractApi } from '../../api/contractApi'
import { ApiRequestError } from '../../api/httpClient'
import { visitApi } from '../../api/visitApi'
import type { HandoverContractDetail } from '../handover/contract.types'
import { getAccessContextError, isAccessWorkItem, type AccessWorkItem } from './accessValidation'
import type { VisitDetail } from './visit.types'

interface AccessContext {
  contract: HandoverContractDetail
  visit: VisitDetail
  workItem: AccessWorkItem | null
}

export function useStaffAccessVisit(contractId: string, visitId: string, workPage: number) {
  const [snapshot, setSnapshot] = useState<{ key: string; context: AccessContext | null; error: ApiErrorPresentation | null } | null>(null)
  const [retry, setRetry] = useState(0)
  const [action, setAction] = useState<'check-in' | 'check-out' | null>(null)
  const [actionError, setActionError] = useState<{ resourceKey: string; error: ApiErrorPresentation } | null>(null)
  const revision = useRef(0)
  const actionBusy = useRef(false)
  const mounted = useRef(false)
  const resourceKey = `${contractId}:${visitId}:${workPage}`
  const key = `${resourceKey}:${retry}`
  const loading = snapshot?.key !== key
  const context = loading ? null : snapshot?.context ?? null
  const error = (actionError?.resourceKey === resourceKey ? actionError.error : null) ?? (loading ? null : snapshot?.error ?? null)

  useEffect(() => {
    mounted.current = true
    const current = ++revision.current
    async function load() {
      try {
        const [{ data: contract }, { data: visit }] = await Promise.all([
          contractApi.get(contractId), visitApi.get(visitId),
        ])
        if (current !== revision.current) return
        const contextError = getAccessContextError(contract, visit, contractId, visitId)
        if (contextError) {
          setSnapshot({ key, context: null, error: presentValidationError(contextError) })
          return
        }
        const { data: items } = await visitApi.listStaffWorkItems({ date: visit.visitDate, page: workPage })
        if (current !== revision.current) return
        const workItem = items.find((item): item is AccessWorkItem =>
          isAccessWorkItem(item) && item.referenceId === visitId && item.entityId === contractId) ?? null
        if (workItem && workItem.customer.customerId !== contract.customerId) {
          setSnapshot({ key, context: null, error: presentValidationError('The work-list customer does not match this contract. Refresh before continuing.') })
          return
        }
        setSnapshot({ key, context: { contract, visit, workItem }, error: null })
      } catch (error) {
        if (current === revision.current) setSnapshot({ key, context: null, error: presentApiError(error) })
      }
    }
    void load()
    return () => { mounted.current = false; revision.current += 1 }
  }, [contractId, visitId, workPage, key])

  function refresh(retainedError: ApiErrorPresentation | null = null) {
    revision.current += 1
    setActionError(retainedError ? { resourceKey, error: retainedError } : null)
    setRetry((value) => value + 1)
  }

  async function process(nextAction: 'check-in' | 'check-out') {
    if (actionBusy.current || !context) return
    if (nextAction === 'check-in' && (context.visit.status !== 'SCHEDULED' || context.contract.status !== 'ACTIVE')) return
    if (nextAction === 'check-out' && context.visit.status !== 'CHECKED_IN') return
    actionBusy.current = true
    const current = revision.current
    setAction(nextAction)
    setActionError(null)
    try {
      if (nextAction === 'check-in') await visitApi.checkIn(visitId)
      else await visitApi.checkOut(visitId)
      if (current === revision.current) refresh()
    } catch (error) {
      if (current === revision.current) {
        const presentation = presentApiError(error)
        if (error instanceof ApiRequestError && error.status === 409) refresh(presentation)
        else setActionError({ resourceKey, error: presentation })
      }
    } finally {
      actionBusy.current = false
      if (mounted.current) setAction(null)
    }
  }

  return { context, loading, action, error, refresh, process }
}
