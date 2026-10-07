import {
  RefreshCw,
  Search,
} from 'lucide-react'
import {
  useEffect,
  useState,
  type ChangeEvent,
  type MouseEvent,
} from 'react'
import { AccountStatusBadge } from '../../components/admin/AccountStatusBadge'
import { UserAccountDetailDrawer } from '../../components/admin/UserAccountDetailDrawer'
import {
  useAdminUserDetail,
  useAdminUsers,
} from '../../hooks/useAdminUsers'
import {
  ADMIN_USER_ROLES,
  ADMIN_USER_STATUSES,
  getAccountType,
  USER_ROLE_LABELS,
  type AdminUserAccount,
} from '../../models/adminUser'
import '../../styles/adminUserMonitoring.css'

function formatDateTime(value: string) {
  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return value
  }

  return new Intl.DateTimeFormat('en-GB', {
    timeZone: 'Asia/Ho_Chi_Minh',
    year: 'numeric',
    month: 'short',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).format(date)
}

function shortId(value: string) {
  if (value.length <= 16) {
    return value
  }

  return `${value.slice(0, 8)}…${value.slice(-4)}`
}

export default function UserAccountMonitoringPage() {
  const {
    accounts,
    pagination,
    loading,
    error,
    filters,
    setFilters,
    loadPage,
    refresh,
  } = useAdminUsers()

  const [selectedUserAccountId, setSelectedUserAccountId] = useState<string | null>(null)
  const detail = useAdminUserDetail(selectedUserAccountId)

  useEffect(() => {
    if (!selectedUserAccountId) {
      return undefined
    }

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setSelectedUserAccountId(null)
      }
    }

    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [selectedUserAccountId])

  function openAccount(account: AdminUserAccount) {
    setSelectedUserAccountId(account.userAccountId)
  }

  return (
    <div className="awp01-page" data-testid="awp01-page">
      <header className="awp01-page__header">
        <div>
          <p className="awp01-eyebrow">System Administrator · AWP-01</p>
          <h1>User account monitoring</h1>
          <p>
            View all Customer and Employee accounts with basic profile information. This feature is read-only and does not modify Customer business data.
          </p>
        </div>

        <button
          className="awp01-button awp01-button--secondary"
          type="button"
          onClick={() => void refresh()}
          disabled={loading}
        >
          <RefreshCw size={16} />
          Refresh
        </button>
      </header>

      <section className="awp01-summary" aria-label="Account list summary">
        <div className="awp01-summary-card">
          <span>Total accounts</span>
          <strong>{pagination.totalItems}</strong>
        </div>
        <div className="awp01-summary-card">
          <span>Current API page</span>
          <strong>{pagination.page}</strong>
        </div>
        <div className="awp01-summary-card">
          <span>Rows loaded</span>
          <strong>{pagination.pageSize > 0 ? Math.min(pagination.pageSize, Math.max(pagination.totalItems - (pagination.page - 1) * pagination.pageSize, 0)) : 0}</strong>
        </div>
      </section>

      <section className="awp01-toolbar" aria-label="Current-page account filters">
        <label>
          <span>Status</span>
          <select
            value={filters.status}
            onChange={(event: ChangeEvent<HTMLSelectElement>) => setFilters({
              ...filters,
              status: event.target.value as typeof filters.status,
            })}
          >
            <option value="ALL">All statuses</option>
            {ADMIN_USER_STATUSES.map((status) => (
              <option key={status} value={status}>{status}</option>
            ))}
          </select>
        </label>

        <label>
          <span>Role</span>
          <select
            value={filters.role}
            onChange={(event: ChangeEvent<HTMLSelectElement>) => setFilters({
              ...filters,
              role: event.target.value as typeof filters.role,
            })}
          >
            <option value="ALL">All roles</option>
            {ADMIN_USER_ROLES.map((role) => (
              <option key={role} value={role}>{USER_ROLE_LABELS[role]}</option>
            ))}
          </select>
        </label>

        <label className="awp01-toolbar__search">
          <span>Search current page</span>
          <div className="awp01-search-input">
            <Search size={16} aria-hidden="true" />
            <input
              value={filters.search}
              onChange={(event: ChangeEvent<HTMLInputElement>) => setFilters({
                ...filters,
                search: event.target.value,
              })}
              placeholder="Name, email, phone, account/profile ID…"
            />
          </div>
        </label>

        <p className="awp01-toolbar__note">
          Status, role, and text filters apply only to the currently loaded API page. Pagination remains server-authoritative.
        </p>
      </section>

      {loading && (
        <div className="awp01-state-grid" aria-label="Loading user accounts">
          {Array.from({ length: 6 }).map((_, index) => (
            <div className="awp01-skeleton" key={index} />
          ))}
        </div>
      )}

      {!loading && error && (
        <div className="awp01-state-card awp01-state-card--error" role="alert">
          <strong>{error.code}</strong>
          <p>{error.message}</p>
          {error.traceId ? <small>Trace: {error.traceId}</small> : null}
          <button
            className="awp01-button awp01-button--secondary"
            type="button"
            onClick={() => void refresh()}
          >
            Try again
          </button>
        </div>
      )}

      {!loading && !error && accounts.length === 0 && (
        <div className="awp01-state-card">
          <strong>No user accounts to show</strong>
          <p>
            {pagination.totalItems === 0
              ? 'No account records were returned by ADM-001.'
              : 'No row on this page matches the current-page filters.'}
          </p>
        </div>
      )}

      {!loading && !error && accounts.length > 0 && (
        <section className="awp01-table-card">
          <div className="awp01-table-wrap">
            <table className="awp01-table">
              <thead>
                <tr>
                  <th>User</th>
                  <th>Account type</th>
                  <th>Role</th>
                  <th>Contact</th>
                  <th>Status</th>
                  <th>Created (GMT+7)</th>
                  <th aria-label="Actions" />
                </tr>
              </thead>

              <tbody>
                {accounts.map((account) => (
                  <tr
                    key={account.userAccountId}
                    data-testid={`awp01-user-${account.userAccountId}`}
                  >
                    <td>
                      <div className="awp01-user-cell">
                        <strong>{account.profile?.fullName || 'Profile unavailable'}</strong>
                        <code title={account.userAccountId}>{shortId(account.userAccountId)}</code>
                      </div>
                    </td>
                    <td>{getAccountType(account)}</td>
                    <td><span className="awp01-role-chip">{USER_ROLE_LABELS[account.role]}</span></td>
                    <td>
                      <div className="awp01-contact-cell">
                        <span title={account.email}>{account.email}</span>
                        <small>{account.phoneNumber}</small>
                      </div>
                    </td>
                    <td><AccountStatusBadge status={account.status} /></td>
                    <td>{formatDateTime(account.createdAt)}</td>
                    <td>
                      <button
                        className="awp01-link-button"
                        type="button"
                        onClick={() => openAccount(account)}
                      >
                        View
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <footer className="awp01-pagination">
            <span>
              Page {pagination.page} of {Math.max(pagination.totalPages, 1)} · {pagination.totalItems} accounts
            </span>
            <div>
              <button
                type="button"
                className="awp01-button awp01-button--secondary"
                disabled={pagination.page <= 1}
                onClick={() => void loadPage(pagination.page - 1)}
              >
                Previous
              </button>
              <button
                type="button"
                className="awp01-button awp01-button--secondary"
                disabled={pagination.page >= pagination.totalPages}
                onClick={() => void loadPage(pagination.page + 1)}
              >
                Next
              </button>
            </div>
          </footer>
        </section>
      )}

      {selectedUserAccountId && (
        <div
          className="awp01-drawer-layer"
          role="presentation"
          onMouseDown={() => setSelectedUserAccountId(null)}
        >
          <div onMouseDown={(event: MouseEvent<HTMLDivElement>) => event.stopPropagation()}>
            <UserAccountDetailDrawer
              account={detail.account}
              loading={detail.loading}
              error={detail.error}
              onClose={() => setSelectedUserAccountId(null)}
            />
          </div>
        </div>
      )}
    </div>
  )
}
