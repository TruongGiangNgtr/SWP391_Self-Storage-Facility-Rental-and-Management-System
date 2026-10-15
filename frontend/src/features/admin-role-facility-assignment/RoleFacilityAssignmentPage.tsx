import {
  RefreshCw,
  Search,
  ShieldCheck,
  X,
  LayoutList,
  LockKeyhole,
  ArrowRight,
} from 'lucide-react'
import {
  useEffect,
  useMemo,
  useState,
  type ChangeEvent,
} from 'react'
import { useSearchParams } from 'react-router-dom'
import { RolePermissionsPanel } from './RolePermissionsPanel'
import {
  assignAdminEmployee,
  listAllAdminFacilities,
  normalizeAdminApiError,
  type AdminFacilityLookup,
} from '../../api/adminApi'
import { AccountStatusBadge } from '../../components/admin/AccountStatusBadge'
import { EmployeeAssignmentDialog } from '../../components/admin/EmployeeAssignmentDialog'
import { useAdminEmployees } from '../../hooks/useAdminEmployees'
import {
  ADMIN_EMPLOYEE_ROLES,
  isFacilityScopedEmployeeRole,
  type AssignAdminEmployeeRequest,
} from '../../models/adminEmployee'
import {
  ADMIN_USER_STATUSES,
  USER_ROLE_LABELS,
  type AdminApiErrorShape,
  type AdminUserAccount,
} from '../../models/adminUser'
import '../../styles/adminUserMonitoring.css'
import '../../styles/adminEmployeeManagement.css'
import '../../styles/adminRoleFacilityAssignment.css'
import '../../styles/adminAccessManagement.css'

function shortId(value: string) {
  if (value.length <= 16) return value
  return `${value.slice(0, 8)}…${value.slice(-4)}`
}

interface PageNotice {
  tone: 'success' | 'error'
  title: string
  message: string
  traceId?: string
}

function EmployeeAssignmentsPanel() {
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

  const [assignmentAccount, setAssignmentAccount] = useState<AdminUserAccount | null>(null)
  const [assignmentBusy, setAssignmentBusy] = useState(false)
  const [assignmentError, setAssignmentError] = useState<AdminApiErrorShape | null>(null)
  const [notice, setNotice] = useState<PageNotice | null>(null)

  // Display Facility names rather than opaque IDs in the assignment table.
  // Assignment still sends the original FacilityId via ADM-009.
  const [facilities, setFacilities] = useState<AdminFacilityLookup[]>([])
  const [facilityDirectoryError, setFacilityDirectoryError] = useState(false)

  useEffect(() => {
    let active = true
    void listAllAdminFacilities().then((list) => {
      if (active) {
        setFacilities(list)
        setFacilityDirectoryError(false)
      }
    }).catch(() => {
      if (active) setFacilityDirectoryError(true)
    })
    return () => { active = false }
  }, [])

  const facilityNames = useMemo(() => new Map(
    facilities.map((facility) => [facility.facilityId.toLowerCase(), facility.name]),
  ), [facilities])

  const facilityScopedCount = useMemo(
    () => rawAccounts.filter((account) =>
      account.role !== 'CUSTOMER' && isFacilityScopedEmployeeRole(account.role),
    ).length,
    [rawAccounts],
  )
  const globalCount = rawAccounts.length - facilityScopedCount

  function openAssignment(account: AdminUserAccount) {
    setNotice(null)
    setAssignmentError(null)
    setAssignmentAccount(account)
  }

  async function submitAssignment(request: AssignAdminEmployeeRequest) {
    const employeeId = assignmentAccount?.profile?.employeeId
    if (!employeeId) {
      setAssignmentError({
        code: 'RESOURCE_NOT_FOUND',
        message: 'The selected account does not contain an Employee ID.',
      })
      return
    }

    setAssignmentBusy(true)
    setAssignmentError(null)
    setNotice(null)

    try {
      const response = await assignAdminEmployee(employeeId, request)
      setAssignmentAccount(null)
      setNotice({
        tone: 'success',
        title: 'Assignment updated',
        message: response.message ?? 'Employee role and Facility assignment were updated.',
      })
      await refresh()
    } catch (requestError) {
      setAssignmentError(normalizeAdminApiError(requestError))
    } finally {
      setAssignmentBusy(false)
    }
  }

  return (
    <div className="awp45-panel-body" id="awp45-assignments-panel" role="tabpanel" aria-labelledby="awp45-assignments-tab">
      <div className="awp45-panel-header">
        <div>
          <p className="awp01-eyebrow">AWP-04 · ADM-009</p>
          <h2>Employee assignments</h2>
          <p>Review Employee access and update one role and Facility scope at a time. Changes are enforced by the backend.</p>
        </div>
        <button
          className="awp01-button awp01-button--secondary"
          type="button"
          onClick={() => void refresh()}
          disabled={loading || assignmentBusy}
        >
          <RefreshCw size={16} />
          Refresh
        </button>
      </div>

      <section className="awp01-summary" aria-label="Assignment summary">
        <div className="awp01-summary-card">
          <span>Employees on API page</span>
          <strong>{rawAccounts.length}</strong>
        </div>
        <div className="awp01-summary-card">
          <span>Facility-scoped on page</span>
          <strong>{facilityScopedCount}</strong>
        </div>
        <div className="awp01-summary-card">
          <span>Global roles on page</span>
          <strong>{globalCount}</strong>
        </div>
      </section>

      <section className="awp04-rule-strip" aria-label="BR-EMP-01 assignment rules">
        <ShieldCheck size={18} aria-hidden="true" />
        <div>
          <strong>BR-EMP-01</strong>
          <span>FACILITY_STAFF / FACILITY_MANAGER → one Facility · BUSINESS_OPERATIONS_MANAGER / SYSTEM_ADMINISTRATOR → no Facility.</span>
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

      <section className="awp01-toolbar" aria-label="Employee assignment filters">
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
          <span>Search loaded employee rows</span>
          <div className="awp01-search-input">
            <Search size={16} aria-hidden="true" />
            <input
              value={filters.search}
              onChange={(event: ChangeEvent<HTMLInputElement>) => setFilters({
                ...filters,
                search: event.target.value,
              })}
              placeholder="Name, email, phone, employee/facility ID…"
            />
          </div>
        </label>

        <p className="awp01-toolbar__note">
          Showing Employee records from the current ADM-001 API page only. Assignment changes use ADM-009; Facility scope and role eligibility are validated server-side.
          {facilityDirectoryError ? ' Facility names are unavailable; IDs are shown as a fallback.' : ''}
        </p>
      </section>

      {loading && (
        <div className="awp01-state-grid" aria-label="Loading employee assignments">
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
          <strong>No Employee assignments to show</strong>
          <p>
            {rawAccounts.length === 0
              ? 'The current ADM-001 page contains no Employee profiles. Use Previous/Next to inspect another API page.'
              : 'No loaded Employee row matches the current filters.'}
          </p>
        </div>
      )}

      {!loading && !error && accounts.length > 0 && (
        <section className="awp01-table-card">
          <div className="awp01-table-wrap">
            <table className="awp01-table awp03-table awp04-table">
              <thead>
                <tr>
                  <th>Employee</th>
                  <th>Current role</th>
                  <th>Facility</th>
                  <th>Status</th>
                  <th>Contact</th>
                  <th>Assignment</th>
                </tr>
              </thead>
              <tbody>
                {accounts.map((account) => (
                  <tr key={account.userAccountId}>
                    <td>
                      <div className="awp01-user-cell">
                        <strong>{account.profile?.fullName || 'Unnamed employee'}</strong>
                        <code title={account.profile?.employeeId ?? account.userAccountId}>
                          {shortId(account.profile?.employeeId ?? account.userAccountId)}
                        </code>
                      </div>
                    </td>
                    <td>
                      <span className="awp01-role-chip">{USER_ROLE_LABELS[account.role]}</span>
                    </td>
                    <td>
                      {account.profile?.facilityId ? (
                        <span className="awp45-facility-cell" title={account.profile.facilityId}>
                          {facilityNames.get(account.profile.facilityId.toLowerCase()) ?? shortId(account.profile.facilityId)}
                        </span>
                      ) : (
                        <span className="awp45-global-chip">Global</span>
                      )}
                    </td>
                    <td><AccountStatusBadge status={account.status} /></td>
                    <td>
                      <div className="awp01-contact-cell">
                        <span>{account.email}</span>
                        <small>{account.phoneNumber}</small>
                      </div>
                    </td>
                    <td>
                      <button
                        className="awp01-button awp03-button--primary awp04-assign-button"
                        type="button"
                        onClick={() => openAssignment(account)}
                        disabled={assignmentBusy || !account.profile?.employeeId}
                      >
                        <ShieldCheck size={15} />
                        Edit assignment
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="awp01-pagination">
            <span>
              ADM-001 API page {pagination.page}{pagination.totalPages > 0 ? ` of ${pagination.totalPages}` : ''} · {pagination.totalItems} total user accounts
            </span>
            <div>
              <button
                className="awp01-button awp01-button--secondary"
                type="button"
                disabled={pagination.page <= 1 || loading}
                onClick={() => void loadPage(pagination.page - 1)}
              >
                Previous
              </button>
              <button
                className="awp01-button awp01-button--secondary"
                type="button"
                disabled={pagination.totalPages === 0 || pagination.page >= pagination.totalPages || loading}
                onClick={() => void loadPage(pagination.page + 1)}
              >
                Next
              </button>
            </div>
          </div>
        </section>
      )}

      {assignmentAccount && (
        <EmployeeAssignmentDialog
          account={assignmentAccount}
          busy={assignmentBusy}
          error={assignmentError}
          onClose={() => {
            if (!assignmentBusy) {
              setAssignmentAccount(null)
              setAssignmentError(null)
            }
          }}
          onSubmit={submitAssignment}
        />
      )}
    </div>
  )
}

export default function RoleFacilityAssignmentPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const activeTab = searchParams.get('tab') === 'permissions' ? 'permissions' : 'assignments'

  function switchTab(tab: 'assignments' | 'permissions') {
    const next = new URLSearchParams(searchParams)
    if (tab === 'permissions') next.set('tab', 'permissions')
    else next.delete('tab')
    setSearchParams(next)
  }

  return (
    <div className="awp01-page awp03-page awp04-page awp45-page" data-testid="awp04-page">
      <header className="awp45-page-heading">
        <div className="awp45-page-heading__icon"><ShieldCheck size={23} aria-hidden="true" /></div>
        <div className="awp45-page-heading__content">
          <p className="awp01-eyebrow">System Administrator · AWP-04 / AWP-05</p>
          <h1>Role &amp; access management</h1>
          <p>Manage Employee role and Facility assignments, and review the fixed Release 1 access policies in one place.</p>
        </div>
        <span className="awp45-heading-badge">5 fixed roles</span>
      </header>

      <nav className="awp45-tabs" aria-label="Role and access sections" role="tablist">
        <button
          id="awp45-assignments-tab"
          className={`awp45-tab ${activeTab === 'assignments' ? 'awp45-tab--active' : ''}`}
          role="tab"
          type="button"
          aria-selected={activeTab === 'assignments'}
          aria-controls="awp45-assignments-panel"
          tabIndex={activeTab === 'assignments' ? 0 : -1}
          onClick={() => switchTab('assignments')}
          onKeyDown={(event) => {
            if (event.key === 'ArrowRight' || event.key === 'ArrowLeft') {
              event.preventDefault()
              switchTab('permissions')
              document.getElementById('awp45-permissions-tab')?.focus()
            }
          }}
        >
          <LayoutList size={17} aria-hidden="true" />
          <span><strong>Employee assignments</strong><small>Role &amp; Facility · AWP-04</small></span>
          <ArrowRight className="awp45-tab__arrow" size={15} aria-hidden="true" />
        </button>
        <button
          id="awp45-permissions-tab"
          className={`awp45-tab ${activeTab === 'permissions' ? 'awp45-tab--active' : ''}`}
          role="tab"
          type="button"
          aria-selected={activeTab === 'permissions'}
          aria-controls="awp45-permissions-panel"
          tabIndex={activeTab === 'permissions' ? 0 : -1}
          onClick={() => switchTab('permissions')}
          onKeyDown={(event) => {
            if (event.key === 'ArrowRight' || event.key === 'ArrowLeft') {
              event.preventDefault()
              switchTab('assignments')
              document.getElementById('awp45-assignments-tab')?.focus()
            }
          }}
        >
          <LockKeyhole size={17} aria-hidden="true" />
          <span><strong>RBAC permissions</strong><small>Access policies · AWP-05</small></span>
          <ArrowRight className="awp45-tab__arrow" size={15} aria-hidden="true" />
        </button>
      </nav>

      {activeTab === 'assignments' ? <EmployeeAssignmentsPanel /> : <RolePermissionsPanel />}
    </div>
  )
}
