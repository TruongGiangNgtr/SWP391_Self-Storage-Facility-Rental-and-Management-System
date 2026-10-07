import {
  KeyRound,
  Pencil,
  Power,
  PowerOff,
  X,
} from 'lucide-react'
import type {
  AdminApiErrorShape,
  AdminUserAccount,
} from '../../models/adminUser'
import { USER_ROLE_LABELS } from '../../models/adminUser'
import { AccountStatusBadge } from './AccountStatusBadge'

interface Props {
  account: AdminUserAccount | null
  loading: boolean
  error: AdminApiErrorShape | null
  actionBusy: boolean
  onClose: () => void
  onEdit: (account: AdminUserAccount) => void
  onActivate: (account: AdminUserAccount) => void
  onDeactivate: (account: AdminUserAccount) => void
  onResendCredential: (account: AdminUserAccount) => void
}

function Value({ children }: { children: string | null | undefined }) {
  return <>{children?.trim() ? children : '—'}</>
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

export function EmployeeAccountDrawer({
  account,
  loading,
  error,
  actionBusy,
  onClose,
  onEdit,
  onActivate,
  onDeactivate,
  onResendCredential,
}: Props) {
  return (
    <aside
      className="awp01-drawer awp03-drawer"
      aria-label="Employee account detail"
      data-testid="awp03-detail-drawer"
    >
      <div className="awp01-drawer__header">
        <div>
          <p className="awp01-eyebrow">Employee account</p>
          <h2>{account?.profile?.fullName || 'Employee'}</h2>
        </div>
        <button
          className="awp01-icon-button"
          type="button"
          onClick={onClose}
          aria-label="Close employee detail"
          disabled={actionBusy}
        >
          <X size={18} />
        </button>
      </div>

      {loading && <div className="awp01-state-card">Loading employee detail…</div>}

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
          </div>

          <div className="awp03-drawer-actions">
            <button
              className="awp01-button awp01-button--secondary"
              type="button"
              onClick={() => onEdit(account)}
              disabled={actionBusy}
            >
              <Pencil size={15} />
              Edit name
            </button>

            {account.status === 'ACTIVE' ? (
              <button
                className="awp01-button awp03-button--danger"
                type="button"
                onClick={() => onDeactivate(account)}
                disabled={actionBusy}
              >
                <PowerOff size={15} />
                Deactivate
              </button>
            ) : (
              <>
                <button
                  className="awp01-button awp03-button--primary"
                  type="button"
                  onClick={() => onActivate(account)}
                  disabled={actionBusy}
                >
                  <Power size={15} />
                  Activate
                </button>
                <button
                  className="awp01-button awp01-button--secondary"
                  type="button"
                  onClick={() => onResendCredential(account)}
                  disabled={actionBusy}
                >
                  <KeyRound size={15} />
                  Resend credential
                </button>
              </>
            )}
          </div>

          <div className="awp03-info-note">
            Role and Facility reassignment is deliberately outside AWP-03 and belongs to AWP-04. Activating an account does not send a credential email.
          </div>

          <dl className="awp01-detail-grid">
            <div>
              <dt>User Account ID</dt>
              <dd>{account.userAccountId}</dd>
            </div>
            <div>
              <dt>Employee ID</dt>
              <dd><Value>{account.profile?.employeeId}</Value></dd>
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
              <dt>Facility ID</dt>
              <dd><Value>{account.profile?.facilityId}</Value></dd>
            </div>
            <div>
              <dt>Created (GMT+7)</dt>
              <dd>{formatDateTime(account.createdAt)}</dd>
            </div>
          </dl>
        </>
      )}
    </aside>
  )
}
