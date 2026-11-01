import { AdminFilterScope, AdminListNavigation } from '../../components/admin/AdminListNavigation'
import { AdminModal } from '../../components/admin/AdminModal'
import { useAdminFacilities } from '../../hooks/useAdminFacilities'
import {
  KeyRound,
  MoreHorizontal,
  Plus,
  Power,
  PowerOff,
  RefreshCw,
  Search,
  X,
} from 'lucide-react'
import {
  useMemo,
  useState,
  type ChangeEvent,
  type MouseEvent,
} from 'react'
import {
  activateAdminEmployee,
  createAdminEmployee,
  deactivateAdminEmployee,
  normalizeAdminApiError,
  resendAdminEmployeeInitialCredential,
  updateAdminEmployee,
} from '../../api/adminApi'
import { AccountStatusBadge } from '../../components/admin/AccountStatusBadge'
import { EmployeeAccountDrawer } from '../../components/admin/EmployeeAccountDrawer'
import { EmployeeAccountFormDialog } from '../../components/admin/EmployeeAccountFormDialog'
import {
  useAdminUserDetail,
} from '../../hooks/useAdminUsers'
import { useAdminEmployees } from '../../hooks/useAdminEmployees'
import {
  ADMIN_EMPLOYEE_ROLES,
  type CreateAdminEmployeeRequest,
  type UpdateAdminEmployeeRequest,
} from '../../models/adminEmployee'
import {
  ADMIN_USER_STATUSES,
  USER_ROLE_LABELS,
  type AdminApiErrorShape,
  type AdminUserAccount,
} from '../../models/adminUser'
import '../../styles/adminUserMonitoring.css'
import '../../styles/adminEmployeeManagement.css'

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

type ConfirmAction = 'ACTIVATE' | 'DEACTIVATE' | 'RESEND_CREDENTIAL'

interface PendingConfirmation {
  action: ConfirmAction
  account: AdminUserAccount
}

interface PageNotice {
  tone: 'success' | 'warning' | 'error'
  title: string
  message: string
  traceId?: string
}

function actionTitle(action: ConfirmAction) {
  switch (action) {
    case 'ACTIVATE': return 'Activate employee account?'
    case 'DEACTIVATE': return 'Deactivate employee account?'
    case 'RESEND_CREDENTIAL': return 'Regenerate and resend initial credential?'
  }
}

function actionDescription(action: ConfirmAction) {
  switch (action) {
    case 'ACTIVATE':
      return 'The employee will be allowed to log in. This action does not send a credential email.'
    case 'DEACTIVATE':
      return 'The employee will no longer be able to log in or initiate new actions. Historical data remains intact.'
    case 'RESEND_CREDENTIAL':
      return 'FRMS will generate a new 16-character password, replace the current hash, and send the new credential by email. This is allowed only while the account is INACTIVE.'
  }
}

export default function EmployeeAccountManagementPage() {
  const { facilityName } = useAdminFacilities()
  const {
    accounts,
    rawAccounts,
    pagination,
    loading,
    error,
    filters,
    setFilters,
    loadPage,
    refresh,
  } = useAdminEmployees()

  const [selectedUserAccountId, setSelectedUserAccountId] = useState<string | null>(null)
  const detail = useAdminUserDetail(selectedUserAccountId)
  const [formMode, setFormMode] = useState<'create' | 'edit' | null>(null)
  const [formAccount, setFormAccount] = useState<AdminUserAccount | null>(null)
  const [formBusy, setFormBusy] = useState(false)
  const [formError, setFormError] = useState<AdminApiErrorShape | null>(null)
  const [pendingConfirmation, setPendingConfirmation] = useState<PendingConfirmation | null>(null)
  const [actionBusy, setActionBusy] = useState(false)
  const [notice, setNotice] = useState<PageNotice | null>(null)

  const activeOnPage = useMemo(
    () => rawAccounts.filter((account) => account.status === 'ACTIVE').length,
    [rawAccounts],
  )
  const inactiveOnPage = rawAccounts.length - activeOnPage

  function openAccount(account: AdminUserAccount) {
    setSelectedUserAccountId(account.userAccountId)
  }

  function openCreate() {
    setNotice(null)
    setFormError(null)
    setFormAccount(null)
    setFormMode('create')
  }

  function openEdit(account: AdminUserAccount) {
    setNotice(null)
    setFormError(null)
    setFormAccount(account)
    setFormMode('edit')
  }

  async function submitCreate(request: CreateAdminEmployeeRequest) {
    setFormBusy(true)
    setFormError(null)
    setNotice(null)

    try {
      const response = await createAdminEmployee(request)
      setFormMode(null)
      setNotice({
        tone: 'success',
        title: 'Employee created',
        message: response.message ?? 'The employee account was created and the initial credential email was accepted.',
      })
      await loadPage(1)
    } catch (requestError) {
      const normalized = normalizeAdminApiError(requestError)

      if (normalized.code === 'EXTERNAL_PROVIDER_UNAVAILABLE') {
        setFormMode(null)
        setNotice({
          tone: 'warning',
          title: 'Credential email was not delivered',
          message: 'This employee may already have been created as inactive. The list was refreshed; do not submit the create form again. After the email provider is available, use Resend credential on the INACTIVE account.',
          traceId: normalized.traceId,
        })
        await loadPage(1)
      } else {
        setFormError(normalized)
      }
    } finally {
      setFormBusy(false)
    }
  }

  async function submitUpdate(request: UpdateAdminEmployeeRequest) {
    const employeeId = formAccount?.profile?.employeeId
    if (!employeeId) {
      setFormError({
        code: 'RESOURCE_NOT_FOUND',
        message: 'The employee profile is unavailable. Refresh and try again.',
      })
      return
    }

    setFormBusy(true)
    setFormError(null)
    setNotice(null)

    try {
      const response = await updateAdminEmployee(employeeId, request)
      setFormMode(null)
      setFormAccount(null)
      setNotice({
        tone: 'success',
        title: 'Employee updated',
        message: response.message ?? 'The employee basic profile was updated.',
      })
      await refresh()
      if (selectedUserAccountId) {
        await detail.refresh()
      }
    } catch (requestError) {
      setFormError(normalizeAdminApiError(requestError))
    } finally {
      setFormBusy(false)
    }
  }

  async function confirmAction() {
    const pending = pendingConfirmation
    const employeeId = pending?.account.profile?.employeeId
    if (!pending || !employeeId) {
      return
    }

    setActionBusy(true)
    setNotice(null)

    try {
      const response = pending.action === 'ACTIVATE'
        ? await activateAdminEmployee(employeeId)
        : pending.action === 'DEACTIVATE'
          ? await deactivateAdminEmployee(employeeId)
          : await resendAdminEmployeeInitialCredential(employeeId)

      setPendingConfirmation(null)
      setNotice({
        tone: 'success',
        title: pending.action === 'ACTIVATE'
          ? 'Employee activated'
          : pending.action === 'DEACTIVATE'
            ? 'Employee deactivated'
            : 'Initial credential sent',
        message: response.message ?? 'The account was updated.',
      })
      await refresh()
      if (selectedUserAccountId === pending.account.userAccountId) {
        await detail.refresh()
      }
    } catch (requestError) {
      const normalized = normalizeAdminApiError(requestError)
      setPendingConfirmation(null)
      setNotice({
        tone: normalized.code === 'EXTERNAL_PROVIDER_UNAVAILABLE' ? 'warning' : 'error',
        title: normalized.code,
        message: normalized.code === 'EXTERNAL_PROVIDER_UNAVAILABLE'
          ? 'Credential delivery failed. The account remains INACTIVE; the regenerated password is not exposed by FRMS. Retry only after the email provider is available.'
          : normalized.message,
        traceId: normalized.traceId,
      })
      await refresh()
      if (selectedUserAccountId === pending.account.userAccountId) {
        await detail.refresh()
      }
    } finally {
      setActionBusy(false)
    }
  }

  return (
    <div className="awp01-page awp03-page" data-testid="awp03-page">
      <header className="awp01-page__header">
        <div>
          <h1>Employee account management</h1>
          <p>
            Create employee accounts, update profiles, manage access, and resend sign-in credentials.
          </p>
        </div>

        <div className="awp03-header-actions">
          <button
            className="awp01-button awp01-button--secondary"
            type="button"
            onClick={() => void refresh()}
            disabled={loading || actionBusy}
          >
            <RefreshCw size={16} />
            Refresh
          </button>
          <button
            className="awp01-button awp03-button--primary"
            type="button"
            onClick={openCreate}
            disabled={actionBusy}
          >
            <Plus size={16} />
            Create employee
          </button>
        </div>
      </header>

      <section className="awp01-summary" aria-label="Employee list summary">
        <div className="awp01-summary-card">
          <span>Employee rows loaded</span>
          <strong>{loading ? '—' : rawAccounts.length}</strong>
        </div>
        <div className="awp01-summary-card">
          <span>Active on page</span>
          <strong>{loading ? '—' : activeOnPage}</strong>
        </div>
        <div className="awp01-summary-card">
          <span>Inactive on page</span>
          <strong>{loading ? '—' : inactiveOnPage}</strong>
        </div>
      </section>

      {notice && (
        <div className={`awp03-notice awp03-notice--${notice.tone}`} role="status">
          <div>
            <strong>{notice.title}</strong>
            <p>{notice.message}</p>
            {notice.traceId ? <small>Trace: {notice.traceId}</small> : null}
          </div>
          <button
            className="awp01-icon-button"
            type="button"
            onClick={() => setNotice(null)}
            aria-label="Dismiss message"
          >
            <X size={16} />
          </button>
        </div>
      )}

      <section className="awp01-toolbar" aria-label="Employee filters">
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
          <span>Employee role</span>
          <select
            value={filters.role}
            onChange={(event: ChangeEvent<HTMLSelectElement>) => setFilters({
              ...filters,
              role: event.target.value as typeof filters.role,
            })}
          >
            <option value="ALL">All employee roles</option>
            {ADMIN_EMPLOYEE_ROLES.map((role) => (
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
              placeholder="Name, email or phone…"
            />
          </div>
        </label>

      </section>

      <AdminFilterScope loading={loading} page={pagination.page} loaded={rawAccounts.length} matching={accounts.length} onClear={() => { setFilters({ status: 'ALL', role: 'ALL', search: '' }) }} />

      {loading && (
        <div className="awp01-state-grid" aria-label="Loading employee accounts">
          {Array.from({ length: 6 }).map((_, index) => (
            <div className="awp01-skeleton" key={index} />
          ))}
        </div>
      )}

      {!loading && error && (
        <div className="awp01-state-card awp01-state-card--error" role="alert">
          <strong>Unable to load this information</strong><small>Code: {error.code}</small>
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
          <strong>No Employee rows to show on this page</strong>
          <p>
            {rawAccounts.length === 0
              ? 'No employees on this page. Browse another page or create an employee.'
              : 'No employees on this page match. Clear the filters or browse another directory page.'}
          </p>
        </div>
      )}

      {!loading && !error && accounts.length > 0 && (
        <section className="awp01-table-card">
          <div className="awp01-table-wrap" role="region" aria-label="Account results (scroll horizontally for more columns)" tabIndex={0}>
            <table className="awp01-table awp03-table" aria-label="Employee accounts">
              <thead>
                <tr>
                  <th>Employee</th>
                  <th>Role</th>
                  <th>Contact</th>
                  <th>Facility</th>
                  <th>Status</th>
                  <th>Created (GMT+7)</th>
                  <th aria-label="Actions" />
                </tr>
              </thead>
              <tbody>
                {accounts.map((account) => (
                  <tr
                    key={account.userAccountId}
                    data-testid={`awp03-employee-${account.profile?.employeeId ?? account.userAccountId}`}
                    onClick={() => openAccount(account)}
                  >
                    <td>
                      <div className="awp01-user-cell">
                        <strong>{account.profile?.fullName || 'Unnamed employee'}</strong>
                      </div>
                    </td>
                    <td><span className="awp01-role-chip">{USER_ROLE_LABELS[account.role]}</span></td>
                    <td>
                      <div className="awp01-contact-cell">
                        <span>{account.email}</span>
                        <small>{account.phoneNumber}</small>
                      </div>
                    </td>
                    <td>
                      <span>{facilityName(account.profile?.facilityId)}</span>
                    </td>
                    <td><AccountStatusBadge status={account.status} /></td>
                    <td>{formatDateTime(account.createdAt)}</td>
                    <td>
                      <button
                        className="awp01-icon-button"
                        type="button"
                        aria-label={`Open ${account.profile?.fullName || 'employee'} actions`}
                        onClick={(event: MouseEvent<HTMLButtonElement>) => {
                          event.stopPropagation()
                          openAccount(account)
                        }}
                      >
                        <MoreHorizontal size={17} />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}

      {!error && <AdminListNavigation pagination={pagination} loading={loading} onPage={page => void loadPage(page)} />}

      {selectedUserAccountId && (
        <AdminModal className="awp01-drawer-layer" label="Employee account detail" busy={actionBusy} onClose={() => setSelectedUserAccountId(null)}>
          <div data-admin-backdrop>
            <EmployeeAccountDrawer
              account={detail.account}
              loading={detail.loading}
              error={detail.error}
              actionBusy={actionBusy}
              onClose={() => setSelectedUserAccountId(null)}
              onEdit={openEdit}
              onActivate={(account) => setPendingConfirmation({ action: 'ACTIVATE', account })}
              onDeactivate={(account) => setPendingConfirmation({ action: 'DEACTIVATE', account })}
              onResendCredential={(account) => setPendingConfirmation({ action: 'RESEND_CREDENTIAL', account })}
            />
          </div>
        </AdminModal>
      )}

      {formMode === 'create' && (
        <EmployeeAccountFormDialog
          mode="create"
          busy={formBusy}
          error={formError}
          onClose={() => {
            if (!formBusy) {
              setFormMode(null)
              setFormError(null)
            }
          }}
          onSubmit={submitCreate}
        />
      )}

      {formMode === 'edit' && formAccount && (
        <EmployeeAccountFormDialog
          mode="edit"
          account={formAccount}
          busy={formBusy}
          error={formError}
          onClose={() => {
            if (!formBusy) {
              setFormMode(null)
              setFormAccount(null)
              setFormError(null)
            }
          }}
          onSubmit={submitUpdate}
        />
      )}

      {pendingConfirmation && (
        <AdminModal className="awp03-modal-layer" label={actionTitle(pendingConfirmation.action)} busy={actionBusy} onClose={() => setPendingConfirmation(null)}>
          <div
            className="awp03-confirm"
            aria-labelledby="awp03-confirm-title"
          >
            <header className="awp03-confirm__icon">
              {pendingConfirmation.action === 'RESEND_CREDENTIAL'
                ? <KeyRound size={20} />
                : pendingConfirmation.action === 'ACTIVATE'
                  ? <Power size={20} />
                  : <PowerOff size={20} />}
            </header>
            <h2 id="awp03-confirm-title">{actionTitle(pendingConfirmation.action)}</h2>
            <p>{actionDescription(pendingConfirmation.action)}</p>
            <div className="awp03-confirm__employee">
              <strong>{pendingConfirmation.account.profile?.fullName || 'Employee'}</strong>
              <span>{pendingConfirmation.account.email}</span>
            </div>
            <footer className="awp03-modal__actions">
              <button
                className="awp01-button awp01-button--secondary"
                type="button"
                disabled={actionBusy}
                onClick={() => setPendingConfirmation(null)}
              >
                Cancel
              </button>
              <button
                className={`awp01-button ${pendingConfirmation.action === 'DEACTIVATE' ? 'awp03-button--danger' : 'awp03-button--primary'}`}
                type="button"
                disabled={actionBusy}
                onClick={() => void confirmAction()}
              >
                {actionBusy ? 'Working…' : pendingConfirmation.action === 'RESEND_CREDENTIAL' ? 'Regenerate & send' : 'Confirm'}
              </button>
            </footer>
          </div>
        </AdminModal>
      )}
    </div>
  )
}
