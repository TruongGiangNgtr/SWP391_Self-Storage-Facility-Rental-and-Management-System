import { RefreshCw, ShieldCheck, X } from 'lucide-react'
import { useMemo, useState } from 'react'
import { assignAdminEmployee, normalizeAdminApiError } from '../../api/adminApi'
import { EmployeeAssignmentDialog } from '../../components/admin/EmployeeAssignmentDialog'
import { useAdminEmployees } from '../../hooks/useAdminEmployees'
import { isFacilityScopedEmployeeRole, type AssignAdminEmployeeRequest } from '../../models/adminEmployee'
import { USER_ROLE_LABELS, type AdminApiErrorShape, type AdminUserAccount } from '../../models/adminUser'
import '../../styles/adminUserMonitoring.css'
import '../../styles/adminEmployeeManagement.css'
import '../../styles/adminAccessManagement.css'

const ACCESS_MATRIX = [
  { capability: 'Customer reservations / contracts', customer: 'Own', staff: 'Same-Facility detail', manager: 'Same-Facility list/detail', bom: 'System-wide read', admin: 'No business read grant' },
  { capability: 'Visit operations', customer: 'Own create/read', staff: 'Same-Facility process', manager: 'Same-Facility monitor', bom: 'System-wide read', admin: 'No' },
  { capability: 'StorageUnit management', customer: 'No', staff: 'Operational handling', manager: 'Same-Facility manage', bom: 'Read/monitor', admin: 'No' },
  { capability: 'Invoices / payments', customer: 'Own view/pay', staff: 'Same-Facility read only', manager: 'Same-Facility read', bom: 'System-wide read/report', admin: 'No manual PAID' },
  { capability: 'Policy / price / business masters', customer: 'Read where exposed', staff: 'Read', manager: 'Read', bom: 'Manage', admin: 'No' },
  { capability: 'Employee / account administration', customer: 'No', staff: 'No', manager: 'No', bom: 'No', admin: 'Manage' },
  { capability: 'LoginHistory / AuditLog', customer: 'No', staff: 'No', manager: 'No', bom: 'No', admin: 'View' },
] as const

interface Notice { tone: 'success' | 'error'; message: string; traceId?: string }

export default function AccessManagementPage() {
  const { rawAccounts, pagination, loading, error, loadPage, refresh } = useAdminEmployees()
  const [assignmentAccount, setAssignmentAccount] = useState<AdminUserAccount | null>(null)
  const [assignmentBusy, setAssignmentBusy] = useState(false)
  const [assignmentError, setAssignmentError] = useState<AdminApiErrorShape | null>(null)
  const [notice, setNotice] = useState<Notice | null>(null)

  const facilityScopedCount = useMemo(
    () => rawAccounts.filter((account) => account.role !== 'CUSTOMER' && isFacilityScopedEmployeeRole(account.role)).length,
    [rawAccounts],
  )
  const globalEmployeeCount = rawAccounts.length - facilityScopedCount

  async function saveAssignment(request: AssignAdminEmployeeRequest) {
    const employeeId = assignmentAccount?.profile?.employeeId
    if (!employeeId) return
    setAssignmentBusy(true)
    setAssignmentError(null)
    try {
      const response = await assignAdminEmployee(employeeId, request)
      setAssignmentAccount(null)
      setNotice({ tone: 'success', message: response.message ?? 'Employee access assignment updated.' })
      await refresh()
    } catch (requestError) {
      setAssignmentError(normalizeAdminApiError(requestError))
    } finally {
      setAssignmentBusy(false)
    }
  }

  return (
    <div className="awp01-page awp05-page" data-testid="awp05-page">
      <header className="awp01-page__header">
        <div>
          <p className="awp01-eyebrow">System Administrator · AWP-05 · SSP-17</p>
          <h1>Access management</h1>
          <p>Review the fixed Release 1 RBAC model and current Employee role/Facility assignments. FRMS does not implement arbitrary per-user permission grants: access changes are made through ADM-009, while every protected endpoint remains server-authoritative.</p>
        </div>
        <button className="awp01-button awp01-button--secondary" type="button" onClick={() => void refresh()} disabled={loading}><RefreshCw size={16} /> Refresh</button>
      </header>

      {notice && <div className={`awp05-notice awp05-notice--${notice.tone}`} role="status"><span>{notice.message}</span>{notice.traceId ? <small>Trace: {notice.traceId}</small> : null}<button type="button" onClick={() => setNotice(null)} aria-label="Dismiss notice"><X size={16} /></button></div>}

      <section className="awp01-summary">
        <div className="awp01-summary-card"><span>Fixed business roles</span><strong>5</strong></div>
        <div className="awp01-summary-card"><span>Facility-scoped employees on page</span><strong>{facilityScopedCount}</strong></div>
        <div className="awp01-summary-card"><span>Global employees on page</span><strong>{globalEmployeeCount}</strong></div>
      </section>

      <div className="awp05-rule-note"><ShieldCheck size={20} /><div><strong>Authorization boundary</strong><span>JWT role gates are only the first layer. Facility/resource ownership is re-checked by backend services; hiding a frontend control never grants or revokes authority.</span></div></div>

      <section className="awp05-matrix-card">
        <header><div><p className="awp01-eyebrow">SRS §10.6</p><h2>Release 1 role capability matrix</h2></div><span className="awp05-fixed-chip">Fixed policy</span></header>
        <div className="awp01-table-wrap"><table className="awp05-matrix"><thead><tr><th>Capability</th><th>Customer</th><th>Facility Staff</th><th>Facility Manager</th><th>BOM</th><th>Administrator</th></tr></thead><tbody>{ACCESS_MATRIX.map((row) => <tr key={row.capability}><td><strong>{row.capability}</strong></td><td>{row.customer}</td><td>{row.staff}</td><td>{row.manager}</td><td>{row.bom}</td><td>{row.admin}</td></tr>)}</tbody></table></div>
      </section>

      <section className="awp05-assignment-card">
        <header><div><p className="awp01-eyebrow">ADM-009</p><h2>Current Employee access assignments</h2><p>Facility Staff and Facility Manager require exactly one Facility; BOM and System Administrator are global roles.</p></div></header>

        {loading && <div className="awp01-state-grid">{Array.from({ length: 5 }).map((_, index) => <div className="awp01-skeleton" key={index} />)}</div>}
        {!loading && error && <div className="awp01-state-card awp01-state-card--error" role="alert"><strong>{error.code}</strong><p>{error.message}</p></div>}
        {!loading && !error && rawAccounts.length === 0 && <div className="awp01-state-card"><strong>No Employee accounts on this page</strong></div>}
        {!loading && !error && rawAccounts.length > 0 && <>
          <div className="awp01-table-wrap"><table className="awp05-assignments"><thead><tr><th>Employee</th><th>Role</th><th>Facility scope</th><th>Account</th><th>Action</th></tr></thead><tbody>{rawAccounts.map((account) => <tr key={account.userAccountId}><td><div className="awp01-user-cell"><strong>{account.profile?.fullName ?? 'Employee'}</strong><small>{account.email}</small></div></td><td><span className="awp01-role-chip">{USER_ROLE_LABELS[account.role]}</span></td><td><code>{account.profile?.facilityId ?? 'GLOBAL'}</code></td><td>{account.status}</td><td><button className="awp01-link-button" type="button" onClick={() => { setAssignmentError(null); setAssignmentAccount(account) }}>Edit access</button></td></tr>)}</tbody></table></div>
          <footer className="awp01-pagination"><span>API page {pagination.page} of {Math.max(pagination.totalPages, 1)}</span><div><button className="awp01-button awp01-button--secondary" disabled={pagination.page <= 1} onClick={() => void loadPage(pagination.page - 1)}>Previous</button><button className="awp01-button awp01-button--secondary" disabled={pagination.page >= pagination.totalPages} onClick={() => void loadPage(pagination.page + 1)}>Next</button></div></footer>
        </>}
      </section>

      {assignmentAccount && <EmployeeAssignmentDialog account={assignmentAccount} busy={assignmentBusy} error={assignmentError} onClose={() => setAssignmentAccount(null)} onSubmit={saveAssignment} />}
    </div>
  )
}
