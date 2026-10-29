import { ArrowRight, CreditCard, FileText, Handshake, ShieldCheck } from 'lucide-react'
import { Link } from 'react-router-dom'
import { useId } from 'react'
import type { PublicPolicyKey, PublicPolicySummary } from '../features/public-help/publicPolicy.types'
import { PublicPolicyMetadata } from './PublicPolicyMetadata'

const TOPIC_ICONS = {
  'reservation-cancellation': FileText, 'deposit-payment-refund': CreditCard,
  'storage-access-handover-contract': Handshake, 'facility-rules-prohibited-items': ShieldCheck,
}
export function PublicPolicyCard({ topic, policy }: { topic: { key: PublicPolicyKey; label: string }; policy?: PublicPolicySummary }) {
  const id = useId()
  const Icon = TOPIC_ICONS[topic.key]
  return <article className="help-policy-card" aria-labelledby={id}>
    <div className="help-card-top"><span className="help-icon-tile"><Icon size={20} aria-hidden="true" /></span><span className="help-public-label">{policy ? 'Policy Guide' : 'Not Published'}</span></div>
    <h3 id={id}>{policy?.title ?? topic.label}</h3>
    <p>{policy?.summary ?? 'No published article has been returned for this topic. Please check again later.'}</p>
    {policy ? <><PublicPolicyMetadata policy={policy} /><Link className="help-read-link" to={`/help/policies/${policy.policyKey}`} aria-label={`Read Policy: ${policy.title}`}>Read Policy<ArrowRight size={16} aria-hidden="true" /></Link></> : <span className="help-unpublished">Article not available yet</span>}
  </article>
}
