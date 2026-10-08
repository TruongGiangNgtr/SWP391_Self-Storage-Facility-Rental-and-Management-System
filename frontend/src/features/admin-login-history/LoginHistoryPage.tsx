import { RefreshCw, Search, ShieldCheck } from 'lucide-react'
import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import { listAdminLoginHistory, normalizeAdminApiError } from '../../api/adminApi'
import type { Pagination } from '../../api/api.types'
import type { AdminApiErrorShape } from '../../models/adminUser'
import type { AdminLoginHistoryEntry, AdminLoginHistoryFilters } from '../../models/adminLoginHistory'
import '../../styles/adminUserMonitoring.css'
import '../../styles/adminLoginHistory.css'

const PAGE_SIZE = 20
const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i
const EMPTY_PAGINATION: Pagination = { page: 1, pageSize: PAGE_SIZE, totalItems: 0, totalPages: 0 }

function formatDateTime(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat('en-GB', {
    timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: 'short', day: '2-digit',
    hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false,
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

const EMPTY_FILTERS: AdminLoginHistoryFilters = { userAccountId: '', status: 'ALL', fromLocal: '', toLocal: '' }

export default function LoginHistoryPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const initialUserAccountId = searchParams.get('userAccountId') ?? ''
  const [filters, setFilters] = useState<AdminLoginHistoryFilters>({ ...EMPTY_FILTERS, userAccountId: initialUserAccountId })
  const [appliedFilters, setAppliedFilters] = useState<AdminLoginHistoryFilters>({ ...EMPTY_FILTERS, userAccountId: initialUserAccountId })
  const [items, setItems] = useState<AdminLoginHistoryEntry[]>([])
  const [pagination, setPagination] = useState<Pagination>(EMPTY_PAGINATION)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<AdminApiErrorShape | null>(null)
  const [clientError, setClientError] = useState<string | null>(null)

  const load = useCallback(async (page: number, currentFilters: AdminLoginHistoryFilters) => {
    setLoading(true)
    setError(null)
    try {
      const response = await listAdminLoginHistory({
        page,
        pageSize: PAGE_SIZE,
        userAccountId: currentFilters.userAccountId.trim() || undefined,
        status: currentFilters.status === 'ALL' ? undefined : currentFilters.status,
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

  useEffect(() => { void load(1, appliedFilters) }, [appliedFilters, load])

  const successCount = useMemo(() => items.filter((item) => item.status === 'SUCCESS').length, [items])
  const failedCount = items.length - successCount

  function applyFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const userAccountId = filters.userAccountId.trim()
    if (userAccountId && !UUID_PATTERN.test(userAccountId)) {
      setClientError('User Account ID must be a valid UUID.')
      return
    }
    const fromUtc = hcmLocalToUtcIso(filters.fromLocal)
    const toUtc = hcmLocalToUtcIso(filters.toLocal)
    if (fromUtc && toUtc && new Date(fromUtc) > new Date(toUtc)) {
      setClientError('From (GMT+7) must be earlier than or equal to To (GMT+7).')
      return
    }
    setClientError(null)
    const normalized = { ...filters, userAccountId }
    setAppliedFilters(normalized)
    const next = new URLSearchParams()
    if (userAccountId) next.set('userAccountId', userAccountId)
    setSearchParams(next, { replace: true })
  }

  function clearFilters() {
    setFilters(EMPTY_FILTERS)
    setClientError(null)
    setSearchParams({}, { replace: true })
    setAppliedFilters(EMPTY_FILTERS)
  }

  return (
    <div className="awp01-page awp06-page" data-testid="awp06-page">
      <header className="awp01-page__header">
        <div>
          <p className="awp01-eyebrow">System Administrator · AWP-06</p>
          <h1>Login history</h1>
          <p>Search append-only LoginHistory through ADM-010. Only attempts whose identifier resolves to a UserAccount appear here; unknown identifiers remain in technical/security logging.</p>
        </div>
        <button className="awp01-button awp01-button--secondary" type="button" onClick={() => void load(pagination.page || 1, appliedFilters)} disabled={loading}><RefreshCw size={16} /> Refresh</button>
      </header>

      <section className="awp01-summary" aria-label="Login history summary">
        <div className="awp01-summary-card"><span>Total matching attempts</span><strong>{pagination.totalItems}</strong></div>
        <div className="awp01-summary-card"><span>Success on page</span><strong>{successCount}</strong></div>
        <div className="awp01-summary-card"><span>Failed on page</span><strong>{failedCount}</strong></div>
      </section>

      <form className="awp06-filters" onSubmit={applyFilters} noValidate>
        <label className="awp06-user-filter"><span>User Account ID</span><div className="awp01-search-input"><Search size={16} /><input value={filters.userAccountId} onChange={(event) => setFilters((current) => ({ ...current, userAccountId: event.target.value }))} placeholder="UUID or leave blank" /></div></label>
        <label><span>Status</span><select value={filters.status} onChange={(event) => setFilters((current) => ({ ...current, status: event.target.value as AdminLoginHistoryFilters['status'] }))}><option value="ALL">All statuses</option><option value="SUCCESS">SUCCESS</option><option value="FAILED">FAILED</option></select></label>
        <label><span>From (GMT+7)</span><input type="datetime-local" value={filters.fromLocal} onChange={(event) => setFilters((current) => ({ ...current, fromLocal: event.target.value }))} /></label>
        <label><span>To (GMT+7)</span><input type="datetime-local" value={filters.toLocal} onChange={(event) => setFilters((current) => ({ ...current, toLocal: event.target.value }))} /></label>
        <div className="awp06-filter-actions"><button className="awp01-button awp01-button--secondary" type="button" onClick={clearFilters}>Clear</button><button className="awp06-primary-button" type="submit">Apply filters</button></div>
        {clientError ? <p className="awp06-client-error" role="alert">{clientError}</p> : null}
      </form>

      <div className="awp06-integrity-note"><ShieldCheck size={18} /><span>LoginHistory is append-only. This screen intentionally has no edit or delete control.</span></div>

      {loading && <div className="awp01-state-grid">{Array.from({ length: 6 }).map((_, index) => <div className="awp01-skeleton" key={index} />)}</div>}
      {!loading && error && <div className="awp01-state-card awp01-state-card--error" role="alert"><strong>{error.code}</strong><p>{error.message}</p>{error.traceId ? <small>Trace: {error.traceId}</small> : null}</div>}
      {!loading && !error && items.length === 0 && <div className="awp01-state-card"><strong>No login attempts found</strong><p>No LoginHistory row matches the applied server-side filters.</p></div>}

      {!loading && !error && items.length > 0 && (
        <section className="awp01-table-card">
          <div className="awp01-table-wrap"><table className="awp01-table awp06-table"><thead><tr><th>Attempt (GMT+7)</th><th>Status</th><th>User Account</th><th>IP address</th><th>Device</th><th>History ID</th></tr></thead><tbody>{items.map((item) => (
            <tr key={item.loginHistoryId}><td>{formatDateTime(item.loginAt)}</td><td><span className={`awp06-status awp06-status--${item.status.toLowerCase()}`}>{item.status}</span></td><td><code title={item.userAccountId}>{shortId(item.userAccountId)}</code></td><td>{item.ipAddress ?? '—'}</td><td><span className="awp06-device" title={item.deviceInfo ?? ''}>{item.deviceInfo ?? '—'}</span></td><td><code title={item.loginHistoryId}>{shortId(item.loginHistoryId)}</code></td></tr>
          ))}</tbody></table></div>
          <footer className="awp01-pagination"><span>Page {pagination.page} of {Math.max(pagination.totalPages, 1)} · {pagination.totalItems} attempts</span><div><button className="awp01-button awp01-button--secondary" disabled={pagination.page <= 1} onClick={() => void load(pagination.page - 1, appliedFilters)}>Previous</button><button className="awp01-button awp01-button--secondary" disabled={pagination.page >= pagination.totalPages} onClick={() => void load(pagination.page + 1, appliedFilters)}>Next</button></div></footer>
        </section>
      )}
    </div>
  )
}
