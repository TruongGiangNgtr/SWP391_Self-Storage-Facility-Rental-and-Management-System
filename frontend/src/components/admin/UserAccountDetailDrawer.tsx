import { X } from 'lucide-react'
import type {
  AdminApiErrorShape,
  AdminUserAccount,
} from '../../models/adminUser'
import {
  getAccountType,
  USER_ROLE_LABELS,
} from '../../models/adminUser'
import { AccountStatusBadge } from './AccountStatusBadge'

interface Props {
  account: AdminUserAccount | null
  loading: boolean
  error: AdminApiErrorShape | null
  onClose: () => void
}

function formatDateTime(value: string) {
  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return value
  }

  return new Intl.DateTimeFormat('en-GB', {
    timeZone: 'Asia/Ho_Chi_Minh',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).format(date)
}

function Value({ children }: { children: string | null | undefined }) {
  return <>{children?.trim() ? children : '—'}</>
}

export function UserAccountDetailDrawer({
  account,
  loading,
  error,
  onClose,
}: Props) {
  return (
    <aside
      className="awp01-drawer"
      aria-label="User account detail"
      data-testid="awp01-detail-drawer"
    >
      <div className="awp01-drawer__header">
        <div>
          <p className="awp01-eyebrow">Account detail</p>
          <h2>{account?.profile?.fullName || 'User account'}</h2>
        </div>

        <button
          className="awp01-icon-button"
          type="button"
          onClick={onClose}
          aria-label="Close account detail"
        >
          <X size={18} />
        </button>
      </div>

      {loading && (
        <div className="awp01-state-card">
          Loading account detail…
        </div>
      )}

      {error && !loading && (
        <div className="awp01-state-card awp01-state-card--error" role="alert">
          <strong>{error.code}</strong>
          <p>{error.message}</p>
          {error.traceId ? <small>Trace: {error.traceId}</small> : null}
        </div>
      )}

      {account && !loading && (
        <>
          <div className="awp01-drawer__status-row">
            <AccountStatusBadge status={account.status} />
            <span className="awp01-role-chip">{USER_ROLE_LABELS[account.role]}</span>
            <span className="awp01-type-chip">{getAccountType(account)}</span>
          </div>

          <div className="awp01-readonly-note">
            AWP-01 is read-only. Account status, employee management, and role/facility assignment are handled by separate administration features.
          </div>

          <dl className="awp01-detail-grid">
            <div>
              <dt>User Account ID</dt>
              <dd>{account.userAccountId}</dd>
            </div>
            <div>
              <dt>Full name</dt>
              <dd><Value>{account.profile?.fullName}</Value></dd>
            </div>
            <div>
              <dt>Email</dt>
              <dd><Value>{account.email}</Value></dd>
            </div>
            <div>
              <dt>Phone number</dt>
              <dd><Value>{account.phoneNumber}</Value></dd>
            </div>
            <div>
              <dt>Created (GMT+7)</dt>
              <dd>{formatDateTime(account.createdAt)}</dd>
            </div>
          </dl>

          {account.profile?.customerId && (
            <section className="awp01-detail-section">
              <h3>Customer profile</h3>
              <dl className="awp01-detail-grid">
                <div>
                  <dt>Customer ID</dt>
                  <dd>{account.profile.customerId}</dd>
                </div>
                <div>
                  <dt>Address</dt>
                  <dd><Value>{account.profile.address}</Value></dd>
                </div>
                <div>
                  <dt>CCCD</dt>
                  <dd><Value>{account.profile.cccd}</Value></dd>
                </div>
              </dl>
            </section>
          )}

          {account.profile?.employeeId && (
            <section className="awp01-detail-section">
              <h3>Employee profile</h3>
              <dl className="awp01-detail-grid">
                <div>
                  <dt>Employee ID</dt>
                  <dd>{account.profile.employeeId}</dd>
                </div>
                <div>
                  <dt>Facility ID</dt>
                  <dd><Value>{account.profile.facilityId}</Value></dd>
                </div>
              </dl>
            </section>
          )}
        </>
      )}
    </aside>
  )
}
