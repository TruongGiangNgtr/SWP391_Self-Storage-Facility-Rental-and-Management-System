import { useEffect, useState } from 'react'
import type { ApiCollectionResponse } from '../../api/api.types'
import { presentApiError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import { businessApi } from '../../api/businessApi'
import type { CustomerDiscount } from './discount.types'

export function useCustomerDiscounts(customerId: string | undefined, page: number) {
  const [snapshot, setSnapshot] = useState<{ key: string; response: ApiCollectionResponse<CustomerDiscount> | null; error: ApiErrorPresentation | null } | null>(null)
  const [retry, setRetry] = useState(0)
  const key = `${customerId ?? ''}:${page}:${retry}`
  const current = customerId && snapshot?.key === key ? snapshot : null
  const loading = Boolean(customerId) && !current

  useEffect(() => {
    let active = true
    if (!customerId) return
    businessApi.listCustomerDiscounts(customerId, page)
      .then((response) => { if (active) setSnapshot({ key, response, error: null }) })
      .catch((caughtError: unknown) => { if (active) setSnapshot({ key, response: null, error: presentApiError(caughtError) }) })
    return () => { active = false }
  }, [customerId, page, key])

  return { response: current?.response ?? null, loading, error: current?.error ?? null, refresh: () => setRetry((value) => value + 1) }
}
