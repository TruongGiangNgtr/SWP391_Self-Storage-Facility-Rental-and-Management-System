import { isUuid } from '../handover/handoverValidation'
import { PUBLIC_POLICY_TOPICS, type PublicPolicyDetail, type PublicPolicyKey, type PublicPolicySummary } from './publicPolicy.types'

export class PublicPolicyContractError extends Error {
  constructor() { super('Public policy content could not be verified. Please refresh or contact the facility.'); this.name = 'PublicPolicyContractError' }
}
export function isPublicPolicyKey(value: unknown): value is PublicPolicyKey {
  return PUBLIC_POLICY_TOPICS.some(topic => topic.key === value)
}
function isUtcTimestamp(value: unknown): value is string {
  return typeof value === 'string' && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?Z$/.test(value) &&
    Number.isFinite(Date.parse(value)) && new Date(value).toISOString().slice(0, 19) === value.slice(0, 19)
}
const nonEmptyText = (value: unknown): value is string => typeof value === 'string' && Boolean(value.trim())
export function isPublicPolicySummary(value: unknown): value is PublicPolicySummary {
  if (!value || typeof value !== 'object') return false
  const item = value as PublicPolicySummary
  return isPublicPolicyKey(item.policyKey) && nonEmptyText(item.title) && nonEmptyText(item.summary) &&
    isUuid(item.policyId) && Number.isSafeInteger(item.version) && item.version >= 1 && isUtcTimestamp(item.effectiveFrom) &&
    (item.effectiveTo === null || (isUtcTimestamp(item.effectiveTo) && Date.parse(item.effectiveTo) > Date.parse(item.effectiveFrom)))
}
export function isPublicPolicyDetail(value: unknown): value is PublicPolicyDetail {
  if (!isPublicPolicySummary(value)) return false
  const item = value as PublicPolicyDetail
  return Array.isArray(item.sections) && item.sections.length > 0 && item.sections.every(section =>
    section && typeof section === 'object' && nonEmptyText(section.heading) && nonEmptyText(section.body))
}
export function samePublicPolicySnapshot(a: PublicPolicySummary, b: PublicPolicySummary): boolean {
  return a.policyId.toLowerCase() === b.policyId.toLowerCase() && a.version === b.version &&
    a.effectiveFrom === b.effectiveFrom && a.effectiveTo === b.effectiveTo
}
