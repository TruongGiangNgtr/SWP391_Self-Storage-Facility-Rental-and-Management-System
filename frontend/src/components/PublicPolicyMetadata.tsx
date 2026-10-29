import type { PublicPolicySummary } from '../features/public-help/publicPolicy.types'
import { formatUtcDateTime } from '../utils/formatters'

export function PublicPolicyMetadata({ policy }: { policy: PublicPolicySummary }) {
  return <dl className="help-policy-metadata">
    <div><dt>Policy rules version</dt><dd>{policy.version}</dd></div>
    <div><dt>Effective from (GMT+7)</dt><dd><time dateTime={policy.effectiveFrom}>{formatUtcDateTime(policy.effectiveFrom)}</time></dd></div>
    {policy.effectiveTo && <div><dt>Effective until (GMT+7)</dt><dd><time dateTime={policy.effectiveTo}>{formatUtcDateTime(policy.effectiveTo)}</time></dd></div>}
  </dl>
}
