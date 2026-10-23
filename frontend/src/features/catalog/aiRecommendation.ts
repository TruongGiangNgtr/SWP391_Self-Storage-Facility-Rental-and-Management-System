import { ApiRequestError } from '../../api/httpClient'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import type { PublicUnitTypeSummary, UnitMode } from './catalog.types'
import { isUuid } from '../handover/handoverValidation'
import { PublicCatalogContractError } from './publicCatalogValidation'

// AI-001 RecommendUnitTypeRequest and the current BE output validation.
export const AI_DESCRIPTION_MAX_LENGTH = 1000
const AI_REASON_MAX_LENGTH = 300
const AI_MAX_ALTERNATIVES = 3

export interface AiRecommendationRequest {
  storageDescription: string
  preferredMode: UnitMode
}

interface AiRecommendationCandidate {
  unitTypeId: string
  reason: string
}

export interface AiRecommendationResponse {
  recommendedUnitTypeId: string
  reason: string
  alternatives: AiRecommendationCandidate[]
}

export interface ResolvedAiCandidate {
  unitType: PublicUnitTypeSummary
  reason: string
}

export interface ResolvedAiRecommendation {
  recommended: ResolvedAiCandidate
  alternatives: ResolvedAiCandidate[]
}

export class AiRecommendationContractError extends Error {
  constructor() {
    super('We could not match this suggestion to the available storage types. Try again or compare storage types manually.')
    this.name = 'AiRecommendationContractError'
  }
}

export function validateAiDescription(description: string): string | null {
  if (!description.trim()) return 'Describe the items you would like to store.'
  if (description.length > AI_DESCRIPTION_MAX_LENGTH) return `Use no more than ${AI_DESCRIPTION_MAX_LENGTH.toLocaleString('en-US')} characters.`
  return null
}

function validReason(value: unknown): value is string {
  return typeof value === 'string' && Boolean(value.trim()) &&
    value.length <= AI_REASON_MAX_LENGTH && !/\p{Cc}/u.test(value)
}

export function readAiRecommendation(value: unknown): AiRecommendationResponse {
  if (!value || typeof value !== 'object' || !('data' in value)) throw new AiRecommendationContractError()
  const item = value.data as Partial<AiRecommendationResponse> | null
  if (!item || typeof item !== 'object' || typeof item.recommendedUnitTypeId !== 'string' || !isUuid(item.recommendedUnitTypeId) || !validReason(item.reason) ||
      !Array.isArray(item.alternatives) || item.alternatives.length > AI_MAX_ALTERNATIVES) throw new AiRecommendationContractError()
  const ids = new Set([item.recommendedUnitTypeId.toLowerCase()])
  const alternatives: AiRecommendationCandidate[] = []
  for (const candidate of item.alternatives) {
    if (!candidate || typeof candidate !== 'object' || typeof candidate.unitTypeId !== 'string' || !isUuid(candidate.unitTypeId) ||
        !validReason(candidate.reason) || ids.has(candidate.unitTypeId.toLowerCase())) throw new AiRecommendationContractError()
    ids.add(candidate.unitTypeId.toLowerCase())
    alternatives.push({ unitTypeId: candidate.unitTypeId, reason: candidate.reason.trim() })
  }
  return { recommendedUnitTypeId: item.recommendedUnitTypeId, reason: item.reason.trim(), alternatives }
}

export function presentAiError(error: unknown): ApiErrorPresentation {
  if (error instanceof AiRecommendationContractError || error instanceof PublicCatalogContractError) return presentValidationError(error.message)
  const presentation = presentApiError(error)
  if (!(error instanceof ApiRequestError)) return presentation
  const messages: Record<string, string> = {
    RATE_LIMIT_EXCEEDED: 'You have asked for several suggestions in a short time. Please wait a moment before trying again, or choose a storage type manually.',
    EXTERNAL_PROVIDER_UNAVAILABLE: 'AI recommendations are temporarily unavailable. Please try again later or choose a storage type manually.',
    ENDPOINT_NOT_IMPLEMENTED: 'AI recommendations are not available from the server yet. You can still choose a storage type manually.',
  }
  const message = Object.hasOwn(messages, error.code) ? messages[error.code] : undefined
  return { ...presentation, message: message ?? presentation.message, customerMessage: message }
}
