import { useEffect, useState } from 'react'
import type { ApiCollectionResponse, ApiResponse } from '../../api/api.types'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import { isSupportPagination } from '../customer-support/supportValidation'
import { isUuid } from '../handover/handoverValidation'

// Bounded first-page previews, not an account-wide aggregate or an authorization decision.
export function useOverviewCollection<T>(load: () => Promise<ApiCollectionResponse<T>>, revision: number, isRecord: (value: T) => boolean) {
  const [snapshot, setSnapshot] = useState<{ revision: number; response: ApiCollectionResponse<T> | null; error: ApiErrorPresentation | null } | null>(null)
  const loading = snapshot?.revision !== revision
  useEffect(() => {
    let active = true
    load().then(response => {
      if (!active) return
      setSnapshot(Array.isArray(response?.data) && response.data.every(isRecord) && isSupportPagination(response.pagination)
        ? { revision, response, error: null }
        : { revision, response: null, error: presentValidationError('This overview could not be loaded. Open the full list or try refreshing.') })
    }).catch((error: unknown) => { if (active) setSnapshot({ revision, response: null, error: presentApiError(error) }) })
    return () => { active = false }
  }, [load, revision, isRecord])
  return { loading, response: loading ? null : snapshot.response, error: loading ? null : snapshot.error }
}

// Hydrate only the selected preview, never every row of the contract/visit lists.
export function useOverviewDetail<T>(id: string, revision: number, read: (id: string) => Promise<ApiResponse<T>>, valid: (data: T, id: string) => boolean) {
  const key = `${id}:${revision}`
  const enabled = isUuid(id)
  const [snapshot, setSnapshot] = useState<{ key: string; data: T | null; error: ApiErrorPresentation | null } | null>(null)
  const loading = enabled && snapshot?.key !== key
  useEffect(() => {
    if (!enabled) return
    let active = true
    read(id).then(response => {
      if (active) setSnapshot(valid(response?.data, id)
        ? { key, data: response.data, error: null }
        : { key, data: null, error: presentValidationError('We could not load these details. Refresh Overview or open the full details page.') })
    }).catch((error: unknown) => { if (active) setSnapshot({ key, data: null, error: presentApiError(error) }) })
    return () => { active = false }
  }, [id, key, enabled, read, valid])
  return { loading, data: enabled && !loading ? snapshot?.data ?? null : null, error: enabled && !loading ? snapshot?.error ?? null : null }
}
