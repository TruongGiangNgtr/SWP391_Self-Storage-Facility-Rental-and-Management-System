import { useMemo, useState, type ChangeEvent } from 'react'
import { Info, LockKeyhole, Search, ShieldCheck } from 'lucide-react'

// Static SRS V10 §10.6 reference matrix. This UI does not grant permissions.
// Backend authorization policies and resource/facility checks remain authoritative.
const ROLE_COLUMNS = [
  { key: 'customer', label: 'Customer' },
  { key: 'staff', label: 'Facility Staff' },
  { key: 'manager', label: 'Facility Manager' },
  { key: 'bom', label: 'Business Operations Manager' },
  { key: 'admin', label: 'System Administrator' },
] as const

type RoleKey = (typeof ROLE_COLUMNS)[number]['key']
type RoleSelection = 'all' | RoleKey

type AccessRule = { capability: string } & Record<RoleKey, string>

const ACCESS_MATRIX: AccessRule[] = [
  {capability: "Own account/profile", customer: "Limited", staff: "Limited", manager: "Limited", bom: "Limited", admin: "View all basic accounts"},
  {capability: "Browse active Facility/UnitType", customer: "Yes", staff: "Read", manager: "Read", bom: "Read/manage", admin: "Read as needed"},
  {capability: "Reservation create/cancel", customer: "Own", staff: "No", manager: "No", bom: "No", admin: "No"},
  {capability: "Reservation read/list", customer: "Own", staff: "Same Facility detail and work list", manager: "Same Facility list/detail", bom: "System list/detail", admin: "No"},
  {capability: "Visit create", customer: "Own where applicable", staff: "No customer creation", manager: "Monitor", bom: "Read", admin: "No"},
  {capability: "Visit read/list", customer: "Own", staff: "Same Facility detail and work list", manager: "Same Facility list/detail", bom: "System list/detail", admin: "No"},
  {capability: "Visit check-in/out", customer: "No", staff: "Same Facility", manager: "Monitor", bom: "No", admin: "No"},
  {capability: "Complete Handover", customer: "No", staff: "Same Facility; command acknowledges offline first-month receipt", manager: "Unit selection/monitor", bom: "No", admin: "No"},
  {capability: "StorageUnit management", customer: "No", staff: "Operational handling only", manager: "Same Facility", bom: "Read/monitor", admin: "No"},
  {capability: "Contract read/list", customer: "Own", staff: "Same Facility detail", manager: "Same Facility list/detail", bom: "System list/detail", admin: "No"},
  {capability: "Contract renewal", customer: "Own request", staff: "No", manager: "Monitor", bom: "Monitor", admin: "No"},
  {capability: "Invoice/Payment", customer: "Own view/pay", staff: "Same Facility read/verify only", manager: "Same Facility read/monitor", bom: "System read/report", admin: "No manual PAID"},
  {capability: "Policy", customer: "Read if exposed", staff: "Read", manager: "Read", bom: "Create version", admin: "No"},
  {capability: "UnitType price/master", customer: "Read", staff: "Read", manager: "Read", bom: "Manage", admin: "No"},
  {capability: "Discount", customer: "Own read/eligible usage", staff: "Read for Customer with same-Facility Reservation/Contract", manager: "Same-Facility Customer read", bom: "Manage", admin: "No"},
  {capability: "ExtraFeeType catalogue", customer: "No", staff: "ACTIVE types read", manager: "ACTIVE types read", bom: "Read/manage", admin: "No"},
  {capability: "Inspection", customer: "Own return progress", staff: "Claim/perform same Facility", manager: "Monitor same Facility", bom: "Read", admin: "No"},
  {capability: "Damage/ExtraFee", customer: "Own settlement read", staff: "Record during authorized Inspection", manager: "Monitor", bom: "Manage type masters", admin: "No"},
  {capability: "SupportTicket", customer: "Own create/view/cancel", staff: "Process assigned", manager: "Assign/monitor same Facility", bom: "Read/monitor", admin: "No"},
  {capability: "Employee/account administration", customer: "No", staff: "No", manager: "No", bom: "No", admin: "Yes"},
  {capability: "LoginHistory/AuditLog", customer: "No requirement", staff: "No", manager: "No", bom: "No", admin: "View"}
]

export function RolePermissionsPanel() {
  const [search, setSearch] = useState('')
  const [selectedRole, setSelectedRole] = useState<RoleSelection>('all')
  const visibleRoles = selectedRole === 'all'
    ? ROLE_COLUMNS
    : ROLE_COLUMNS.filter((role) => role.key === selectedRole)
  const visibleRules = useMemo(() => ACCESS_MATRIX.filter((rule) =>
    rule.capability.toLowerCase().includes(search.trim().toLowerCase()),
  ), [search])

  return (
    <section className="awp45-panel-body" id="awp45-permissions-panel" role="tabpanel" aria-labelledby="awp45-permissions-tab" data-testid="awp05-panel">
      <div className="awp45-panel-header">
        <div>
          <p className="awp01-eyebrow">Access policies</p>
          <h2>RBAC capability matrix</h2>
          <p>Review the access available to each role.</p>
        </div>
        <span className="awp45-readonly-chip"><LockKeyhole size={14} aria-hidden="true" /> Read-only</span>
      </div>

      <div className="awp45-policy-highlights" aria-label="Policy summary">
        <div><span>Business roles</span><strong>{ROLE_COLUMNS.length}</strong><small>Available roles</small></div>
        <div><span>Capabilities documented</span><strong>{ACCESS_MATRIX.length}</strong><small>Access policies</small></div>
        <div><span>Assignment method</span><strong>Role and facility</strong><small>Role and Facility only</small></div>
      </div>

      <div className="awp45-policy-boundary">
        <ShieldCheck size={20} aria-hidden="true" />
        <div>
          <strong>Access follows role and facility assignments</strong>
          <p>Access depends on the assigned role, facility, and resource ownership. Permissions are checked for every protected action.</p>
        </div>
      </div>

      <div className="awp45-matrix-card">
        <div className="awp45-matrix-toolbar">
          <div>
            <h3>Role permissions</h3>
            <p>Filter a capability or focus on a specific role to review its scope.</p>
          </div>
          <div className="awp45-matrix-filters">
            <label>
              <span>Find capability</span>
              <span className="awp45-search-field">
                <Search size={15} aria-hidden="true" />
                <input
                  type="search"
                  value={search}
                  onChange={(event: ChangeEvent<HTMLInputElement>) => setSearch(event.target.value)}
                  placeholder="Search capabilities…"
                />
              </span>
            </label>
            <label>
              <span>View roles</span>
              <select value={selectedRole} onChange={(event: ChangeEvent<HTMLSelectElement>) => setSelectedRole(event.target.value as RoleSelection)}>
                <option value="all">Compare all roles</option>
                {ROLE_COLUMNS.map((role) => <option key={role.key} value={role.key}>{role.label}</option>)}
              </select>
            </label>
          </div>
        </div>

        {visibleRules.length === 0 ? (
          <div className="awp45-matrix-empty" role="status">No capability matches “{search}”.</div>
        ) : (
          <div className="awp01-table-wrap" role="region" aria-label="Role capability comparison" tabIndex={0}>
            <table className={`awp45-matrix ${selectedRole !== 'all' ? 'awp45-matrix--focused' : ''}`}>
              <thead>
                <tr>
                  <th scope="col">Capability</th>
                  {visibleRoles.map((role) => <th key={role.key} scope="col">{role.label}</th>)}
                </tr>
              </thead>
              <tbody>
                {visibleRules.map((rule) => (
                  <tr key={rule.capability}>
                    <th scope="row">{rule.capability}</th>
                    {visibleRoles.map((role) => (
                      <td key={role.key}>
                        <span className={rule[role.key] === 'No' ? 'awp45-policy-denied' : 'awp45-policy-allowed'}>
                          {rule[role.key]}
                        </span>
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        <div className="awp45-matrix-footer">
          <span>{visibleRules.length} of {ACCESS_MATRIX.length} capabilities</span>
          <span><Info size={14} aria-hidden="true" /> Role policy reference. Individual access also depends on facility and resource ownership.</span>
        </div>
      </div>
    </section>
  )
}
