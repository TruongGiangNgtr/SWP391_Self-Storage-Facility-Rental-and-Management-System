import { httpClient } from './httpClient'
import type { ApiResponse } from './api.types'
import { publicCatalogApi } from './publicCatalogApi'
import {
  AiRecommendationContractError,
  readAiRecommendation,
  type AiRecommendationRequest,
  type ResolvedAiRecommendation,
} from '../features/catalog/aiRecommendation'

export interface AiStoragePolicyItem { id: string; title: string; examples: string[] }
export interface AiStoragePolicyReference { documentNumber: string; title: string; officialUrl: string }
export interface AiStoragePolicyView {
  policyKey: string
  approvalStatus: string
  version: string
  title: string
  summary: string
  prohibitedWarning: string
  prohibited: AiStoragePolicyItem[]
  allowed: AiStoragePolicyItem[]
  legalReferences: AiStoragePolicyReference[]
}

export const aiRecommendationApi = {
  storagePolicy(signal?: AbortSignal): Promise<ApiResponse<AiStoragePolicyView>> {
    return httpClient.getPublic('/ai/storage-policy', signal)
  },
  async recommend(input: AiRecommendationRequest, signal: AbortSignal): Promise<ResolvedAiRecommendation> {
    const raw = await httpClient.postPublic<unknown>('/ai/unit-type-recommendations', input, signal)
    const answer = readAiRecommendation(raw)
    const candidates = [
      { unitTypeId: answer.recommendedUnitTypeId, reason: answer.reason },
      ...answer.alternatives,
    ]
    // At most four exact-ID reads, independent of the visible catalog page.
    // CAT-005 confirms each type is still publicly offered in the requested mode.
    // Names, sizes and prices never come from provider prose or fabricated IDs.
    const resolved = await Promise.all(candidates.map(async candidate => {
      const response = await publicCatalogApi.listUnitTypes({
        mode: input.preferredMode,
        unitTypeId: candidate.unitTypeId,
        page: 1,
        pageSize: 20,
      }, signal)
      const unitType = response.data[0]
      if (response.data.length !== 1 || !unitType ||
          unitType.unitTypeId.toLowerCase() !== candidate.unitTypeId.toLowerCase() ||
          unitType.mode !== input.preferredMode) throw new AiRecommendationContractError()
      return { unitType, reason: candidate.reason }
    }))
    return { recommended: resolved[0], alternatives: resolved.slice(1) }
  },
}
