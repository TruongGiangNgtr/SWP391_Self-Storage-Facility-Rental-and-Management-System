import { useEffect, useState } from 'react'
import type { ApiCollectionResponse } from '../../api/api.types'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import { transactionsApi } from '../../api/transactionsApi'
import { useAuth } from '../../auth/auth.context'
import { TransactionContractError } from './transactionValidation'
import type { CustomerPaymentHistory } from './transaction.types'

export function useTransactions(page = 1, pageSize = 20, enabled = true) {
  const { user } = useAuth()
  const [retry, setRetry] = useState(0)
  const scope = user?.role === 'CUSTOMER' && user.status === 'ACTIVE' ? user.userAccountId : ''
  const key = `${scope}:${page}:${pageSize}:${retry}`
  const canRead = enabled && Boolean(scope)
  const [snapshot, setSnapshot] = useState<{
    key: string
    response: ApiCollectionResponse<CustomerPaymentHistory> | null
    error: ApiErrorPresentation | null
  } | null>(null)
  const loading = canRead && snapshot?.key !== key

  useEffect(() => {
    if (!canRead) return
    const controller = new AbortController()
    transactionsApi.list(page, pageSize, controller.signal)
      .then(response => { if (!controller.signal.aborted) setSnapshot({ key, response, error: null }) })
      .catch((error: unknown) => {
        if (!controller.signal.aborted) setSnapshot({ key, response: null,
          error: error instanceof TransactionContractError ? presentValidationError(error.message) : presentApiError(error) })
      })
    return () => controller.abort()
  }, [canRead, key, page, pageSize])

  // Never expose a prior account/page's evidence while the next read is pending.
  return {
    loading,
    response: canRead && !loading ? snapshot?.response ?? null : null,
    error: canRead && !loading ? snapshot?.error ?? null : null,
    refresh: () => setRetry(value => value + 1),
  }
}
