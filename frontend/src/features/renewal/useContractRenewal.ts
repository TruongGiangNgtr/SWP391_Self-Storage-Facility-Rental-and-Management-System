import { useEffect, useRef, useState } from 'react'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import { contractApi } from '../../api/contractApi'
import { ApiRequestError } from '../../api/httpClient'
import { useAccessContract } from '../visits/useAccessContract'
import type { RenewContractResult } from './renewal.types'
import { isRenewContractResult, validateRenewalMonth } from './renewalValidation'

export function useContractRenewal(contractId: string) {
  const reader = useAccessContract(contractId)
  const [working, setWorking] = useState(false)
  const [actionError, setActionError] = useState<ApiErrorPresentation | null>(null)
  const [result, setResult] = useState<RenewContractResult | null>(null)
  const [lastAttemptEndMonth, setLastAttemptEndMonth] = useState<string | null>(null)
  const busy = useRef(false)
  const mounted = useRef(false)
  const revision = useRef(0)

  useEffect(() => {
    mounted.current = true
    revision.current += 1
    return () => { mounted.current = false; revision.current += 1 }
  }, [contractId])

  function refreshContract() {
    if (busy.current) return
    revision.current += 1
    // Keep a confirmed result or uncertainty visible; a GET is not a second renewal.
    reader.refresh()
  }

  async function renew(newEndMonth: string) {
    if (busy.current || reader.loading || !reader.contract || result) return
    if (reader.contract.status !== 'ACTIVE') {
      setActionError(presentValidationError('Only an ACTIVE contract can be renewed.'))
      return
    }
    const inputError = validateRenewalMonth(reader.contract.endMonth, newEndMonth)
    if (inputError) { setActionError(presentValidationError(inputError)); return }
    busy.current = true
    const current = revision.current
    setWorking(true)
    setActionError(null)
    setLastAttemptEndMonth(newEndMonth)
    try {
      const response = await contractApi.renew(contractId, { newEndMonth })
      if (!mounted.current || current !== revision.current) return
      if (!isRenewContractResult(response?.data, contractId, newEndMonth)) {
        setActionError(presentValidationError('The server returned an unexpected renewal result. Refresh and review the contract before retrying; the request may already have been processed.'))
      } else {
        setResult(response.data)
      }
      reader.refresh()
    } catch (error) {
      if (!mounted.current || current !== revision.current) return
      setActionError(presentApiError(error))
      // Refetch after conflicts or uncertain failures without automatically replaying POST.
      // A newer EndMonth makes an already-applied target ineligible for resubmission.
      if (!(error instanceof ApiRequestError && error.status === 401)) reader.refresh()
    } finally {
      busy.current = false
      if (mounted.current) setWorking(false)
    }
  }

  return {
    contract: reader.contract,
    loading: reader.loading,
    readError: reader.error,
    actionError,
    working,
    result,
    lastAttemptEndMonth,
    refreshContract,
    renew,
  }
}
