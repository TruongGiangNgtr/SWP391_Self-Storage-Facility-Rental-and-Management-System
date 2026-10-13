import { useEffect, useState } from 'react'
import type { ApiCollectionResponse } from '../../api/api.types'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import { isSupportPagination } from '../customer-support/supportValidation'

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
