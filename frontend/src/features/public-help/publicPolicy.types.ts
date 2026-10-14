// SRS V10 13.13 public policy topics, in the approved display order.
export const PUBLIC_POLICY_TOPICS = [
  { key: 'reservation-cancellation', label: 'Reservation & Cancellation Policy' },
  { key: 'identity-verification', label: 'Identity Verification Policy' },
  { key: 'deposit-payment-refund', label: 'Deposit, Payment & Refund Policy' },
  { key: 'storage-access-handover-contract', label: 'Storage Access, Handover & Contract Policy' },
  { key: 'facility-rules-prohibited-items', label: 'Facility Rules & Prohibited Items Policy' },
  { key: 'account-privacy', label: 'Account & Privacy Policy' },
] as const

export type PublicPolicyKey = typeof PUBLIC_POLICY_TOPICS[number]['key']
export interface PublicPolicySummary {
  policyKey: PublicPolicyKey
  title: string
  summary: string
  policyId: string
  version: number
  effectiveFrom: string
  effectiveTo: string | null
}
export interface PublicPolicyDetail extends PublicPolicySummary {
  sections: Array<{ heading: string; body: string }>
}
