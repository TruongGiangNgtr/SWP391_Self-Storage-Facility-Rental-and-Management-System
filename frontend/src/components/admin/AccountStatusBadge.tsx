import type { UserAccountStatus } from '../../models/adminUser'

export function AccountStatusBadge({ status }: { status: UserAccountStatus }) {
  return (
    <span className={`awp01-status awp01-status--${status.toLowerCase()}`}>
      <span className="awp01-status__dot" aria-hidden="true" />
      {status}
    </span>
  )
}
