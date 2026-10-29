import { AdminModal } from '../../components/admin/AdminModal'
import { EmployeeEntityLabel } from '../../components/EmployeeEntityLabel'
import type { ReactNode } from 'react'
import {
  Activity,
  ChevronRight,
  RefreshCw,
  Search,
  ShieldCheck,
  X,
} from 'lucide-react'
import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type FormEvent,
} from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import {
  listAdminAuditLogs,
  normalizeAdminApiError,
} from '../../api/adminApi'
import type { Pagination } from '../../api/api.types'
import type {
  AdminAuditLogEntry,
  AdminAuditLogFilters,
} from '../../models/adminAuditLog'
import type { AdminApiErrorShape } from '../../models/adminUser'
import '../../styles/adminUserMonitoring.css'
import '../../styles/adminLoginHistory.css'
import '../../styles/adminActivityLogs.css'

const PAGE_SIZE = 20
const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i
const EMPTY_PAGINATION: Pagination = {
  page: 1,
  pageSize: PAGE_SIZE,
  totalItems: 0,
  totalPages: 0,
}

const KNOWN_ACTIONS = [
  'CREATE_RESERVATION',
  'CONFIRM_RESERVATION',
  'CANCEL_RESERVATION',
  'UPDATE_RESERVATION',
  'CREATE_VISIT',
  'CHECK_IN_VISIT',
  'CHECK_OUT_VISIT',
  'CANCEL_VISIT',
  'UPDATE_VISIT',
  'COMPLETE_HANDOVER',
  'FINALIZE_RETURN',
  'UPDATE_CONTRACT',
  'CREATE_DEPOSIT_INVOICE',
  'CREATE_RENTAL_INVOICE',
  'MARK_INVOICE_OVERDUE',
  'UPDATE_INVOICE',
  'PAYMENT_RESULT',
  'PAYMENT_CHANGE',
  'RENEW_CONTRACT',
  'CONFIRM_ACTUAL_RETURN',
  'CREATE_INSPECTION',
  'CLAIM_INSPECTION',
  'COMPLETE_INSPECTION',
  'UPDATE_INSPECTION',
  'RECORD_DAMAGE',
  'UPDATE_DAMAGE_STATUS',
  'DECIDE_DAMAGE',
  'RECORD_EXTRA_FEE',
  'CREATE_POLICY_VERSION',
  'UPDATE_UNIT_PRICE',
  'CREATE_OR_UPDATE_DISCOUNT',
  'CREATE_SUPPORT_TICKET',
  'ASSIGN_SUPPORT_TICKET',
  'COMPLETE_SUPPORT_TICKET',
  'CANCEL_SUPPORT_TICKET',
  'UPDATE_SUPPORT_TICKET',
  'CHANGE_ACCOUNT_STATUS',
  'CHANGE_ACCOUNT',
  'CHANGE_EMPLOYEE_ROLE_FACILITY',
] as const

const KNOWN_ENTITY_TYPES = [
  'USER_ACCOUNT',
  'EMPLOYEE',
  'RESERVATION',
  'VISIT',
  'CONTRACT',
  'CONTRACT_EXTENSION',
  'INVOICE',
  'PAYMENT',
  'INSPECTION',
  'DAMAGE_RECORD',
  'EXTRA_FEE',
  'DEPOSIT_SETTLEMENT',
  'POLICY',
  'UNIT_TYPE',
  'DISCOUNT',
  'SUPPORT_TICKET',
] as const

function formatDateTime(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value

  return new Intl.DateTimeFormat('en-GB', {
    timeZone: 'Asia/Ho_Chi_Minh',
    year: 'numeric',
    month: 'short',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
  }).format(date)
}

function hcmLocalToUtcIso(value: string) {
  if (!value) return undefined
  const date = new Date(`${value}:00+07:00`)
  return Number.isNaN(date.getTime()) ? undefined : date.toISOString()
}

function formatCode(value: string) {
  return value
    .toLowerCase()
    .split('_')
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(' ')
}

// Keep the immutable audit payload untouched; present named fields and omit
// persistence identifiers from the rendered snapshot.
function snapshotFields(value: unknown): ReactNode {
  if (Array.isArray(value)) return <ul>{value.map((item, index) => <li key={index}>{snapshotFields(item)}</li>)}</ul>
  if (value && typeof value === 'object') return <dl>{Object.entries(value).filter(([key]) => !/(?:^id$|^ids$|Id$|Ids$|ID$|IDs$|_id$|_ids$)/.test(key)).map(([key, item]) => <div key={key}><dt>{key.replace(/([a-z])([A-Z])/g, '$1 $2').replaceAll('_', ' ')}</dt><dd>{snapshotFields(item)}</dd></div>)}</dl>
  if (typeof value === 'string' && /^[0-9a-f]{8}-[0-9a-f-]{27,}$/i.test(value)) return 'Related record'
  return String(value ?? '—')
}
function prettySnapshot(value: string | null): ReactNode {
  if (!value) return null
  try { return snapshotFields(JSON.parse(value)) } catch { return snapshotFields(value) }
}
function AuditResource({ entry }: { entry: AdminAuditLogEntry }) {
  if (entry.entityType === 'USER_ACCOUNT') return <EmployeeEntityLabel kind="account" id={entry.entityId} />
  for (const snapshot of [entry.newValue, entry.oldValue]) {
    try {
      const data = JSON.parse(snapshot ?? 'null')
      if (data && typeof data === 'object') {
        for (const field of ['fullName', 'name', 'unitCode']) if (typeof data[field] === 'string' && data[field].trim()) return <>{data[field]}</>
        if (typeof data.version === 'number') return <>Policy version {data.version}</>
      }
    } catch { /* Older events may contain plain text rather than JSON. */ }
  }
  return <>{formatCode(entry.entityType)} · {formatDateTime(entry.createdAt)}</>
}

function resultSummary(item: AdminAuditLogEntry) {
  if (item.oldValue && item.newValue) return 'Before / after snapshots'
  if (item.newValue) return 'Result snapshot recorded'
  if (item.oldValue) return 'Previous snapshot recorded'
  return 'Action recorded'
}

function filtersFromSearchParams(searchParams: URLSearchParams): AdminAuditLogFilters {
  return {
    userAccountId: searchParams.get('userAccountId') ?? '',
    entityType: searchParams.get('entityType') ?? '',
    entityId: searchParams.get('entityId') ?? '',
    action: searchParams.get('action') ?? '',
    fromLocal: '',
    toLocal: '',
  }
}

const EMPTY_FILTERS: AdminAuditLogFilters = {
  userAccountId: '',
  entityType: '',
  entityId: '',
  action: '',
  fromLocal: '',
  toLocal: '',
}

function AuditLogDetailDrawer({
  entry,
  onClose,
}: {
  entry: AdminAuditLogEntry
  onClose: () => void
}) {
  const oldValue = prettySnapshot(entry.oldValue)
  const newValue = prettySnapshot(entry.newValue)

  return (
    <aside className="awp07-drawer" aria-labelledby="awp07-drawer-title">
      <header className="awp07-drawer__header">
        <div>
          <h2 id="awp07-drawer-title">{formatCode(entry.action)}</h2>
          <span>{formatDateTime(entry.createdAt)}</span>
        </div>
        <button className="awp07-icon-button" type="button" onClick={onClose} aria-label="Close audit log detail">
          <X size={18} />
        </button>
      </header>

      <div className="awp07-drawer__body">
        <section className="awp07-detail-grid" aria-label="Audit event metadata">
          <div><span>Recorded (GMT+7)</span><strong>{formatDateTime(entry.createdAt)}</strong></div>
          <div><span>Actor</span><strong>{entry.userAccountId ? <EmployeeEntityLabel kind="account" id={entry.userAccountId} /> : 'SYSTEM / BACKGROUND'}</strong></div>
          <div><span>Action</span><strong>{entry.action}</strong></div>
          <div><span>Resource type</span><strong>{entry.entityType}</strong></div>
          <div><span>Business resource</span><strong><AuditResource entry={entry} /></strong></div>
          <div><span>Result history</span><strong>{resultSummary(entry)}</strong></div>
        </section>

        {entry.userAccountId ? (
          <section className="awp07-identifier-block">
            <span>Actor</span>
            <EmployeeEntityLabel kind="account" id={entry.userAccountId} />
          </section>
        ) : null}

        {entry.entityId ? (
          <section className="awp07-identifier-block">
            <span>Business resource</span>
            <AuditResource entry={entry} />
          </section>
        ) : null}

        <section className="awp07-snapshot-section">
          <div className="awp07-snapshot-card">
            <div className="awp07-snapshot-card__title">Previous state</div>
            {oldValue ? <div>{oldValue}</div> : <p>No previous-state snapshot was stored for this event.</p>}
          </div>
          <div className="awp07-snapshot-card">
            <div className="awp07-snapshot-card__title">Resulting state</div>
            {newValue ? <div>{newValue}</div> : <p>No resulting-state snapshot was stored for this event.</p>}
          </div>
        </section>

        <div className="awp07-integrity-callout">
          <ShieldCheck size={18} aria-hidden="true" />
          <p>AuditLog is append-only. This view exposes recorded history only and does not provide edit or delete operations.</p>
        </div>
      </div>
    </aside>
  )
}

export default function ActivityLogManagementPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [filters, setFilters] = useState<AdminAuditLogFilters>(() => filtersFromSearchParams(searchParams))
  const [appliedFilters, setAppliedFilters] = useState<AdminAuditLogFilters>(() => filtersFromSearchParams(searchParams))
  const [items, setItems] = useState<AdminAuditLogEntry[]>([])
  const [pagination, setPagination] = useState<Pagination>(EMPTY_PAGINATION)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<AdminApiErrorShape | null>(null)
  const [clientError, setClientError] = useState<string | null>(null)
  const [selected, setSelected] = useState<AdminAuditLogEntry | null>(null)

  const load = useCallback(async (page: number, currentFilters: AdminAuditLogFilters) => {
    setLoading(true)
    setError(null)

    try {
      const response = await listAdminAuditLogs({
        page,
        pageSize: PAGE_SIZE,
        userAccountId: currentFilters.userAccountId.trim() || undefined,
        entityType: currentFilters.entityType.trim() || undefined,
        entityId: currentFilters.entityId.trim() || undefined,
        action: currentFilters.action.trim() || undefined,
        fromUtc: hcmLocalToUtcIso(currentFilters.fromLocal),
        toUtc: hcmLocalToUtcIso(currentFilters.toLocal),
      })

      setItems(response.data ?? [])
      setPagination(response.pagination ?? { ...EMPTY_PAGINATION, page })
    } catch (requestError) {
      setItems([])
      setError(normalizeAdminApiError(requestError))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load(1, appliedFilters)
  }, [appliedFilters, load])

  const userEventsOnPage = useMemo(
    () => items.filter((item) => item.userAccountId).length,
    [items],
  )
  const systemEventsOnPage = items.length - userEventsOnPage

  function applyFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const userAccountId = filters.userAccountId.trim()
    const entityId = filters.entityId.trim()
    const entityType = filters.entityType.trim()
    const action = filters.action.trim()

    if (userAccountId && !UUID_PATTERN.test(userAccountId)) {
      setClientError('Enter the complete actor account ID or clear this filter.')
      return
    }

    if (entityId && !UUID_PATTERN.test(entityId)) {
      setClientError('Enter the complete resource ID or clear this filter.')
      return
    }

    if (entityType.length > 150 || action.length > 150) {
      setClientError('Action and Entity Type must not exceed 150 characters.')
      return
    }

    const fromUtc = hcmLocalToUtcIso(filters.fromLocal)
    const toUtc = hcmLocalToUtcIso(filters.toLocal)
    if (fromUtc && toUtc && new Date(fromUtc) > new Date(toUtc)) {
      setClientError('From (GMT+7) must be earlier than or equal to To (GMT+7).')
      return
    }

    setClientError(null)
    const normalized: AdminAuditLogFilters = {
      ...filters,
      userAccountId,
      entityId,
      entityType,
      action,
    }
    setAppliedFilters(normalized)

    const next = new URLSearchParams()
    if (userAccountId) next.set('userAccountId', userAccountId)
    if (entityType) next.set('entityType', entityType)
    if (entityId) next.set('entityId', entityId)
    if (action) next.set('action', action)
    setSearchParams(next, { replace: true })
  }

  function clearFilters() {
    setFilters(EMPTY_FILTERS)
    setClientError(null)
    setSearchParams({}, { replace: true })
    setAppliedFilters(EMPTY_FILTERS)
  }

  return (
    <div className="awp01-page awp06-page awp07-page" data-testid="awp07-page">
      <header className="awp01-page__header">
        <div>
          <h1>Activity log management</h1>
          <p>
            Review account activity, affected resources, and changes over time.
          </p>
        </div>
        <button
          className="awp01-button awp01-button--secondary"
          type="button"
          onClick={() => void load(pagination.page || 1, appliedFilters)}
          disabled={loading}
        >
          <RefreshCw size={16} />
          Refresh
        </button>
      </header>

      <section className="awp01-summary" aria-label="Activity log summary">
        <div className="awp01-summary-card"><span>Total matching events</span><strong>{loading ? '—' : pagination.totalItems}</strong></div>
        <div className="awp01-summary-card"><span>User-attributed on page</span><strong>{loading ? '—' : userEventsOnPage}</strong></div>
        <div className="awp01-summary-card"><span>System/background on page</span><strong>{loading ? '—' : systemEventsOnPage}</strong></div>
      </section>

      <p className="admin-history-help">To filter by a person, <Link to="/admin/users">find their account</Link> and choose a history link in its details. Or paste an account ID below.</p>

      <form className="awp06-filters awp07-filters" onSubmit={applyFilters} noValidate>
        <label className="awp07-actor-filter">
          <span>Actor account ID (optional)</span>
          <div className="awp01-search-input">
            <Search size={16} aria-hidden="true" />
            <input
              value={filters.userAccountId}
              onChange={(event) => setFilters((current) => ({ ...current, userAccountId: event.target.value }))}
              placeholder="Paste account ID, or leave blank"
            />
          </div>
        </label>

        <label>
          <span>Action</span>
          <input
            list="awp07-actions"
            value={filters.action}
            onChange={(event) => setFilters((current) => ({ ...current, action: event.target.value }))}
            placeholder="Exact action"
          />
          <datalist id="awp07-actions">
            {KNOWN_ACTIONS.map((action) => <option key={action} value={action} label={formatCode(action)} />)}
          </datalist>
        </label>

        <label><span>From (GMT+7)</span><input type="datetime-local" value={filters.fromLocal} onChange={(event) => setFilters((current) => ({ ...current, fromLocal: event.target.value }))} /></label>
        <label><span>To (GMT+7)</span><input type="datetime-local" value={filters.toLocal} onChange={(event) => setFilters((current) => ({ ...current, toLocal: event.target.value }))} /></label>

        <details className="admin-advanced-filters" open={Boolean(filters.entityId || filters.entityType)}>
          <summary>Resource filters</summary>
          <div>
            <label>
              <span>Resource type</span>
              <input
                list="awp07-entity-types"
                value={filters.entityType}
                onChange={(event) => setFilters((current) => ({ ...current, entityType: event.target.value }))}
                placeholder="Exact entity type"
              />
              <datalist id="awp07-entity-types">
                {KNOWN_ENTITY_TYPES.map((entityType) => <option key={entityType} value={entityType} label={formatCode(entityType)} />)}
              </datalist>
            </label>

            <label>
              <span>Resource ID (optional)</span>
              <input
                value={filters.entityId}
                onChange={(event) => setFilters((current) => ({ ...current, entityId: event.target.value }))}
                placeholder="Paste resource ID, or leave blank"
              />
            </label>

          </div>
        </details>

        <div className="awp06-filter-actions">
          <button className="awp01-button awp01-button--secondary" type="button" onClick={clearFilters}>Clear</button>
          <button className="awp06-primary-button" type="submit">Apply filters</button>
        </div>
        {clientError ? <p className="awp06-client-error" role="alert">{clientError}</p> : null}
      </form>

      <div className="awp06-integrity-note awp07-integrity-note">
        <ShieldCheck size={18} aria-hidden="true" />
        <span>Activity history cannot be edited or deleted. Automated events may have no user account. Times are shown in GMT+7.</span>
      </div>

      {loading && (
        <div className="awp01-state-grid" aria-label="Loading activity logs">
          {Array.from({ length: 6 }).map((_, index) => <div className="awp01-skeleton" key={index} />)}
        </div>
      )}

      {!loading && error && (
        <div className="awp01-state-card awp01-state-card--error" role="alert">
          <strong>Unable to load this information</strong><small>Code: {error.code}</small>
          <p>{error.message}</p>
          {error.traceId ? <small>Trace: {error.traceId}</small> : null}
          <button className="awp01-button awp01-button--secondary" type="button" onClick={() => void load(pagination.page || 1, appliedFilters)}>Try again</button>
        </div>
      )}

      {!loading && !error && items.length === 0 && (
        <div className="awp01-state-card">
          <strong>No activity events found</strong>
          <p>No activity matches these filters. Clear them to see all activity.</p><button type="button" className="awp01-button awp01-button--secondary" onClick={clearFilters}>Clear filters</button>
        </div>
      )}

      {!loading && !error && items.length > 0 && (
        <section className="awp01-table-card">
          <div className="awp01-table-wrap" role="region" aria-label="Activity log results (scroll horizontally for more columns)" tabIndex={0}>
            <table className="awp01-table awp07-table">
              <thead>
                <tr>
                  <th>Recorded (GMT+7)</th>
                  <th>Actor</th>
                  <th>Action</th>
                  <th>Business resource</th>
                  <th>Result history</th>
                  <th aria-label="Actions" />
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr key={item.auditLogId} data-testid={`awp07-log-${item.auditLogId}`}>
                    <td>{formatDateTime(item.createdAt)}</td>
                    <td>
                      {item.userAccountId ? (
                        <EmployeeEntityLabel kind="account" id={item.userAccountId} />
                      ) : (
                        <span className="awp07-system-actor">SYSTEM</span>
                      )}
                    </td>
                    <td><span className="awp07-action-chip" title={item.action}>{item.action}</span></td>
                    <td>
                      <div className="awp07-resource-cell">
                        <strong>{item.entityType}</strong>
                        <span><AuditResource entry={item} /></span>
                      </div>
                    </td>
                    <td>
                      <div className="awp07-result-cell">
                        <Activity size={15} aria-hidden="true" />
                        <span>{resultSummary(item)}</span>
                      </div>
                    </td>
                    <td>
                      <button className="awp01-link-button awp07-view-button" type="button" onClick={() => setSelected(item)}>
                        View <ChevronRight size={15} />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <footer className="awp01-pagination">
            <span>Page {pagination.page} of {Math.max(pagination.totalPages, 1)} · {pagination.totalItems} events</span>
            <div>
              <button className="awp01-button awp01-button--secondary" type="button" disabled={pagination.page <= 1} onClick={() => void load(pagination.page - 1, appliedFilters)}>Previous</button>
              <button className="awp01-button awp01-button--secondary" type="button" disabled={pagination.page >= pagination.totalPages} onClick={() => void load(pagination.page + 1, appliedFilters)}>Next</button>
            </div>
          </footer>
        </section>
      )}

      {selected ? (
        <AdminModal className="awp07-drawer-layer" label="Activity detail" onClose={() => setSelected(null)}>
          <div data-admin-backdrop>
            <AuditLogDetailDrawer entry={selected} onClose={() => setSelected(null)} />
          </div>
        </AdminModal>
      ) : null}
    </div>
  )
}
