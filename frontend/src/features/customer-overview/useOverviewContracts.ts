import { useEffect, useState } from 'react'
import type { ApiCollectionResponse } from '../../api/api.types'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import { contractApi } from '../../api/contractApi'
import { useAuth } from '../../auth/auth.context'
import { isContractListResponse, type ContractListItem } from '../contracts/contractListPresentation'
import { isUuid } from '../handover/handoverValidation'

// CON-001 may return summaries without reservationId. CON-002 supplies that
// reference for the selected preview; do not validate list rows as details.
export function useOverviewContracts(revision: number) {
  const { user } = useAuth()
  const customerId = user?.customerId ?? ''
  const enabled = user?.role === 'CUSTOMER' && isUuid(customerId)
  const key = `${customerId}:${revision}`
  const [snapshot, setSnapshot] = useState<{ key: string; response: ApiCollectionResponse<ContractListItem> | null; error: ApiErrorPresentation | null } | null>(null)
  const loading = enabled && snapshot?.key !== key
  useEffect(() => {
    if (!enabled) return
    let active = true
    contractApi.list(1, 20).then(response => {
      if (!active) return
      setSnapshot(isContractListResponse(response, 1) && response.data.every(item => item.customerId === customerId)
        ? { key, response, error: null }
        : { key, response: null, error: presentValidationError('This overview could not be loaded. Open the full list or try refreshing.') })
    }).catch((error: unknown) => { if (active) setSnapshot({ key, response: null, error: presentApiError(error) }) })
    return () => { active = false }
  }, [enabled, customerId, key])
  return { loading, response: enabled && !loading ? snapshot?.response ?? null : null, error: enabled && !loading ? snapshot?.error ?? null : null }
}
