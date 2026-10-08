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
  type MouseEvent,
} from 'react'
import { useSearchParams } from 'react-router-dom'
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

function shortId(value: string) {
  return value.length <= 16 ? value : `${value.slice(0, 8)}…${value.slice(-4)}`
}

function formatCode(value: string) {
  return value
    .toLowerCase()
    .split('_')
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(' ')
}

function prettyJson(value: string | null) {
  if (!value) return null

  try {
    return JSON.stringify(JSON.parse(value), null, 2)
  } catch {
    return value
  }
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
  const oldValue = prettyJson(entry.oldValue)
  const newValue = prettyJson(entry.newValue)

  return (
    <aside className="awp07-drawer" role="dialog" aria-modal="true" aria-labelledby="awp07-drawer-title">
      <header className="awp07-drawer__header">
        <div>
          <p className="awp01-eyebrow">AWP-07 · Audit event</p>
          <h2 id="awp07-drawer-title">{formatCode(entry.action)}</h2>
          <code title={entry.auditLogId}>{entry.auditLogId}</code>
        </div>
        <button className="awp07-icon-button" type="button" onClick={onClose} aria-label="Close audit log detail">
          <X size={18} />
        </button>
      </header>

      <div className="awp07-drawer__body">
        <section className="awp07-detail-grid" aria-label="Audit event metadata">
          <div><span>Recorded (GMT+7)</span><strong>{formatDateTime(entry.createdAt)}</strong></div>
          <div><span>Actor</span><strong>{entry.userAccountId ? shortId(entry.userAccountId) : 'SYSTEM / BACKGROUND'}</strong></div>
          <div><span>Action</span><strong>{entry.action}</strong></div>
          <div><span>Entity type</span><strong>{entry.entityType}</strong></div>
          <div><span>Entity ID</span><strong>{entry.entityId ? shortId(entry.entityId) : '—'}</strong></div>
          <div><span>Result history</span><strong>{resultSummary(entry)}</strong></div>
        </section>

        {entry.userAccountId ? (
          <section className="awp07-identifier-block">
            <span>Actor UserAccountId</span>
            <code>{entry.userAccountId}</code>
          </section>
        ) : null}

        {entry.entityId ? (
          <section className="awp07-identifier-block">
            <span>Business resource ID</span>
            <code>{entry.entityId}</code>
          </section>
        ) : null}

        <section className="awp07-snapshot-section">
          <div className="awp07-snapshot-card">
            <div className="awp07-snapshot-card__title">Previous state</div>
            {oldValue ? <pre>{oldValue}</pre> : <p>No previous-state snapshot was stored for this event.</p>}
          </div>
          <div className="awp07-snapshot-card">
            <div className="awp07-snapshot-card__title">Resulting state</div>
            {newValue ? <pre>{newValue}</pre> : <p>No resulting-state snapshot was stored for this event.</p>}
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

  useEffect(() => {
    if (!selected) return undefined

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setSelected(null)
    }

    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [selected])

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
      setClientError('Actor User Account ID must be a valid UUID.')
      return
    }

    if (entityId && !UUID_PATTERN.test(entityId)) {
      setClientError('Entity ID must be a valid UUID.')
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
          <p className="awp01-eyebrow">System Administrator · AWP-07 · SSP-18</p>
          <h1>Activity log management</h1>
          <p>
            Search recorded business and security activity through ADM-011. Review the actor, action, affected resource, event time, and stored before/after result snapshots without mutating audit history.
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
        <div className="awp01-summary-card"><span>Total matching events</span><strong>{pagination.totalItems}</strong></div>
        <div className="awp01-summary-card"><span>User-attributed on page</span><strong>{userEventsOnPage}</strong></div>
        <div className="awp01-summary-card"><span>System/background on page</span><strong>{systemEventsOnPage}</strong></div>
      </section>

      <form className="awp06-filters awp07-filters" onSubmit={applyFilters} noValidate>
        <label className="awp07-actor-filter">
          <span>Actor User Account ID</span>
          <div className="awp01-search-input">
            <Search size={16} aria-hidden="true" />
            <input
              value={filters.userAccountId}
              onChange={(event) => setFilters((current) => ({ ...current, userAccountId: event.target.value }))}
              placeholder="UUID or leave blank"
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
            {KNOWN_ACTIONS.map((action) => <option key={action} value={action} />)}
          </datalist>
        </label>

        <label>
          <span>Entity type</span>
          <input
            list="awp07-entity-types"
            value={filters.entityType}
            onChange={(event) => setFilters((current) => ({ ...current, entityType: event.target.value }))}
            placeholder="Exact entity type"
          />
          <datalist id="awp07-entity-types">
            {KNOWN_ENTITY_TYPES.map((entityType) => <option key={entityType} value={entityType} />)}
          </datalist>
        </label>

        <label>
          <span>Entity ID</span>
          <input
            value={filters.entityId}
            onChange={(event) => setFilters((current) => ({ ...current, entityId: event.target.value }))}
            placeholder="UUID or leave blank"
          />
        </label>

        <label><span>From (GMT+7)</span><input type="datetime-local" value={filters.fromLocal} onChange={(event) => setFilters((current) => ({ ...current, fromLocal: event.target.value }))} /></label>
        <label><span>To (GMT+7)</span><input type="datetime-local" value={filters.toLocal} onChange={(event) => setFilters((current) => ({ ...current, toLocal: event.target.value }))} /></label>

        <div className="awp06-filter-actions">
          <button className="awp01-button awp01-button--secondary" type="button" onClick={clearFilters}>Clear</button>
          <button className="awp06-primary-button" type="submit">Apply filters</button>
        </div>
        {clientError ? <p className="awp06-client-error" role="alert">{clientError}</p> : null}
      </form>

      <div className="awp06-integrity-note awp07-integrity-note">
        <ShieldCheck size={18} aria-hidden="true" />
        <span>AuditLog is append-only. System/background events may have no actor, but still identify the action, resource, and recorded UTC event time.</span>
      </div>

      {loading && (
        <div className="awp01-state-grid" aria-label="Loading activity logs">
          {Array.from({ length: 6 }).map((_, index) => <div className="awp01-skeleton" key={index} />)}
        </div>
      )}

      {!loading && error && (
        <div className="awp01-state-card awp01-state-card--error" role="alert">
          <strong>{error.code}</strong>
          <p>{error.message}</p>
          {error.traceId ? <small>Trace: {error.traceId}</small> : null}
          <button className="awp01-button awp01-button--secondary" type="button" onClick={() => void load(pagination.page || 1, appliedFilters)}>Try again</button>
        </div>
      )}

      {!loading && !error && items.length === 0 && (
        <div className="awp01-state-card">
          <strong>No activity events found</strong>
          <p>No AuditLog row matches the applied server-side filters.</p>
        </div>
      )}

      {!loading && !error && items.length > 0 && (
        <section className="awp01-table-card">
          <div className="awp01-table-wrap">
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
                        <code title={item.userAccountId}>{shortId(item.userAccountId)}</code>
                      ) : (
                        <span className="awp07-system-actor">SYSTEM</span>
                      )}
                    </td>
                    <td><span className="awp07-action-chip" title={item.action}>{item.action}</span></td>
                    <td>
                      <div className="awp07-resource-cell">
                        <strong>{item.entityType}</strong>
                        <code title={item.entityId ?? ''}>{item.entityId ? shortId(item.entityId) : 'No entity ID'}</code>
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
        <div className="awp07-drawer-layer" role="presentation" onMouseDown={() => setSelected(null)}>
          <div onMouseDown={(event: MouseEvent<HTMLDivElement>) => event.stopPropagation()}>
            <AuditLogDetailDrawer entry={selected} onClose={() => setSelected(null)} />
          </div>
        </div>
      ) : null}
    </div>
  )
}
