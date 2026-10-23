import { publicPolicyApi } from '../../api/publicPolicyApi'
import { ApiRequestError } from '../../api/httpClient'
import { presentApiError, presentValidationError } from '../../api/apiErrorPresentation'
import { usePublicRead } from '../catalog/usePublicCatalog'
import { isPublicPolicyKey, PublicPolicyContractError } from './publicPolicyValidation'
import type { PublicPolicyKey } from './publicPolicy.types'

function policyError(error: unknown) {
  if (error instanceof PublicPolicyContractError) return presentValidationError(error.message)
  if (error instanceof ApiRequestError && error.status === 401) return { message: 'Public policies are not available from the server yet. Please try again later.', traceId: error.traceId, details: [], unavailable: true }
  if (error instanceof ApiRequestError && error.code === 'RESOURCE_NOT_FOUND') return { message: 'The current public policy content is not available. Return to Help Center or try again later.', traceId: error.traceId, details: [], unavailable: true }
  return presentApiError(error)
}
const readPolicies = (_key: string, signal: AbortSignal) => publicPolicyApi.list(signal)
const readPolicy = (key: string, signal: AbortSignal) => publicPolicyApi.get(key as PublicPolicyKey, signal)
export const usePublicPolicies = () => usePublicRead('current', readPolicies, true, policyError)
export const usePublicPolicy = (key: string) => usePublicRead(key, readPolicy, isPublicPolicyKey(key), policyError)
