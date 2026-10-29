// SRS V11 §0.0.11 public policy topics, in the approved display order.
export const PUBLIC_POLICY_TOPICS = [
  { key: 'reservation-cancellation', label: 'Reservation & Account Requirements' },
  { key: 'deposit-payment-refund', label: 'Deposit, Invoices & Refunds' },
  { key: 'storage-access-handover-contract', label: 'Storage Rental & Return' },
  { key: 'facility-rules-prohibited-items', label: 'Facility Rules & Prohibited Items' },
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
