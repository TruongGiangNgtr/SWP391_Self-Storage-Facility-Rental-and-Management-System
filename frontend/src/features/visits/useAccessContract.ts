import { useEffect, useState } from 'react'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import { contractApi } from '../../api/contractApi'
import type { HandoverContractDetail } from '../handover/contract.types'

export function useAccessContract(contractId: string) {
  const [snapshot, setSnapshot] = useState<{ key: string; contract: HandoverContractDetail | null; error: ApiErrorPresentation | null } | null>(null)
  const [retry, setRetry] = useState(0)
  const key = `${contractId}:${retry}`
  const loading = snapshot?.key !== key

  useEffect(() => {
    let active = true
    contractApi.get(contractId)
      .then(({ data }) => {
        if (!active) return
        if (!data || data.contractId !== contractId) {
          setSnapshot({ key, contract: null, error: presentValidationError('The server returned a different contract. Refresh before continuing.') })
          return
        }
        setSnapshot({ key, contract: data, error: null })
      })
      .catch((error: unknown) => { if (active) setSnapshot({ key, contract: null, error: presentApiError(error) }) })
    return () => { active = false }
  }, [contractId, key])

  return {
    contract: loading ? null : snapshot.contract,
    error: loading ? null : snapshot.error,
    loading,
    refresh: () => setRetry((value) => value + 1),
  }
}
