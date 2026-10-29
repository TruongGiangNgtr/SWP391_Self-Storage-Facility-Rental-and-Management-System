import { httpClient } from './httpClient'
import { readPublicCollection } from '../features/catalog/publicCatalogValidation'
import { PUBLIC_POLICY_TOPICS, type PublicPolicyKey } from '../features/public-help/publicPolicy.types'
import { isPublicPolicyDetail, isPublicPolicySummary, PublicPolicyContractError, samePublicPolicySnapshot } from '../features/public-help/publicPolicyValidation'

// PUB-POL-001/002: public projection only, never the privileged BOM Policy list.
export const publicPolicyApi = {
  async list(signal?: AbortSignal) {
    const raw = await httpClient.getPublic('/public/policies?page=1&pageSize=20', signal)
    try {
      const response = readPublicCollection(raw, isPublicPolicySummary, item => item.policyKey, 1, 20)
      // All four topics fit on the first standard page. Do not label a
      // topic unpublished from a truncated page or mixed Policy snapshots.
      if (response.pagination.totalItems !== response.data.length || response.data.length > PUBLIC_POLICY_TOPICS.length ||
        response.data.some(item => !samePublicPolicySnapshot(response.data[0], item))) throw new PublicPolicyContractError()
      return response
    } catch { throw new PublicPolicyContractError() }
  },
  async get(policyKey: PublicPolicyKey, signal?: AbortSignal) {
    const response = await httpClient.getPublic<{ data: unknown }>(`/public/policies/${encodeURIComponent(policyKey)}`, signal)
    if (!response || !isPublicPolicyDetail(response.data) || response.data.policyKey !== policyKey) throw new PublicPolicyContractError()
    return { data: response.data }
  },
}
