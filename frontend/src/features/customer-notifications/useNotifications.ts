import { useEffect, useState } from 'react'
import type { ApiCollectionResponse } from '../../api/api.types'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import { notificationsApi } from '../../api/notificationsApi'
import { useAuth } from '../../auth/auth.context'
import { NotificationContractError } from './notificationValidation'
import type { CustomerNotification } from './notification.types'

export function useNotifications(page = 1, pageSize = 20, enabled = true, revision = 0) {
  const { user } = useAuth()
  const [retry, setRetry] = useState(0)
  const scope = user?.role === 'CUSTOMER' && user.status === 'ACTIVE' ? user.userAccountId : ''
  const key = `${scope}:${page}:${pageSize}:${revision}:${retry}`
  const canRead = enabled && Boolean(scope)
  const [snapshot, setSnapshot] = useState<{ key: string; response: ApiCollectionResponse<CustomerNotification> | null; error: ApiErrorPresentation | null } | null>(null)
  const loading = canRead && snapshot?.key !== key

  useEffect(() => {
    if (!canRead) return
    const controller = new AbortController()
    notificationsApi.list(page, pageSize, controller.signal)
      .then(response => { if (!controller.signal.aborted) setSnapshot({ key, response, error: null }) })
      .catch((error: unknown) => {
        if (!controller.signal.aborted) setSnapshot({ key, response: null,
          error: error instanceof NotificationContractError ? presentValidationError(error.message) : presentApiError(error) })
      })
    return () => controller.abort()
  }, [canRead, key, page, pageSize])

  // Account-scoped snapshots prevent a prior Customer's content surviving a session change.
  return { loading, response: canRead && !loading ? snapshot?.response ?? null : null,
    error: canRead && !loading ? snapshot?.error ?? null : null, refresh: () => setRetry(value => value + 1) }
}
