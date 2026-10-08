import { RefreshCw, Search, ShieldAlert, X } from 'lucide-react'
import { useState, type ChangeEvent } from 'react'
import { activateAdminCustomer, deactivateAdminCustomer, normalizeAdminApiError } from '../../api/adminApi'
import { AccountStatusBadge } from '../../components/admin/AccountStatusBadge'
import { useAdminCustomers } from '../../hooks/useAdminCustomers'
import type { AdminApiErrorShape, AdminUserAccount } from '../../models/adminUser'
import '../../styles/adminUserMonitoring.css'
import '../../styles/adminCustomerStatus.css'

interface Notice {
  tone: 'success' | 'error'
  title: string
  message: string
  traceId?: string
}

function shortId(value: string) {
  return value.length <= 16 ? value : `${value.slice(0, 8)}…${value.slice(-4)}`
}

export default function CustomerAccountStatusPage() {
  const {
    accounts, rawAccounts, pagination, loading, error, status, setStatus,
    search, setSearch, loadPage, refresh,
  } = useAdminCustomers()
  const [target, setTarget] = useState<AdminUserAccount | null>(null)
  const [busy, setBusy] = useState(false)
  const [actionError, setActionError] = useState<AdminApiErrorShape | null>(null)
  const [notice, setNotice] = useState<Notice | null>(null)

  const activeCount = rawAccounts.filter((account) => account.status === 'ACTIVE').length
  const inactiveCount = rawAccounts.length - activeCount

  async function confirmStatusChange() {
    const customerId = target?.profile?.customerId
    if (!target || !customerId) return

    setBusy(true)
    setActionError(null)
    setNotice(null)
    const activating = target.status === 'INACTIVE'

    try {
      const response = activating
        ? await activateAdminCustomer(customerId)
        : await deactivateAdminCustomer(customerId)
      setNotice({
        tone: 'success',
        title: activating ? 'Customer activated' : 'Customer deactivated',
        message: response.message ?? `Account is now ${activating ? 'ACTIVE' : 'INACTIVE'}.`,
      })
      setTarget(null)
      await refresh()
    } catch (requestError) {
      const normalized = normalizeAdminApiError(requestError)
      setActionError(normalized)
      if (normalized.code === 'CUSTOMER_HAS_ACTIVE_LIFECYCLE') {
        setNotice({
          tone: 'error',
          title: 'Deactivation blocked',
          message: 'BR-ACC-02 blocks deactivation while the Customer has a CONFIRMED Reservation or ACTIVE Contract.',
          traceId: normalized.traceId,
        })
      }
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="awp01-page awp02-page" data-testid="awp02-page">
      <header className="awp01-page__header">
        <div>
          <p className="awp01-eyebrow">System Administrator · AWP-02</p>
          <h1>Customer account status</h1>
          <p>Activate or deactivate Customer accounts through ADM-003/ADM-004. Deactivation is server-guarded by BR-ACC-02 and never deletes historical business records.</p>
        </div>
        <button className="awp01-button awp01-button--secondary" type="button" onClick={() => void refresh()} disabled={loading}>
          <RefreshCw size={16} /> Refresh
        </button>
      </header>

      {notice && (
        <div className={`awp02-notice awp02-notice--${notice.tone}`} role={notice.tone === 'error' ? 'alert' : 'status'}>
          <div><strong>{notice.title}</strong><span>{notice.message}</span>{notice.traceId ? <small>Trace: {notice.traceId}</small> : null}</div>
          <button type="button" aria-label="Dismiss notice" onClick={() => setNotice(null)}><X size={16} /></button>
        </div>
      )}

      <section className="awp01-summary" aria-label="Customer account summary">
        <div className="awp01-summary-card"><span>Customers on API page</span><strong>{rawAccounts.length}</strong></div>
        <div className="awp01-summary-card"><span>Active on page</span><strong>{activeCount}</strong></div>
        <div className="awp01-summary-card"><span>Inactive on page</span><strong>{inactiveCount}</strong></div>
      </section>

      <section className="awp01-toolbar">
        <label>
          <span>Status</span>
          <select value={status} onChange={(event: ChangeEvent<HTMLSelectElement>) => setStatus(event.target.value as typeof status)}>
            <option value="ALL">All statuses</option><option value="ACTIVE">ACTIVE</option><option value="INACTIVE">INACTIVE</option>
          </select>
        </label>
        <label className="awp01-toolbar__search awp02-search-wide">
          <span>Search current API page</span>
          <div className="awp01-search-input"><Search size={16} /><input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Name, email, phone, Customer ID…" /></div>
        </label>
        <p className="awp01-toolbar__note">ADM-001 remains the account-list source. Status and search filters narrow only the currently loaded server page.</p>
      </section>

      {loading && <div className="awp01-state-grid">{Array.from({ length: 5 }).map((_, index) => <div className="awp01-skeleton" key={index} />)}</div>}
      {!loading && error && <div className="awp01-state-card awp01-state-card--error" role="alert"><strong>{error.code}</strong><p>{error.message}</p><button className="awp01-button awp01-button--secondary" onClick={() => void refresh()}>Try again</button></div>}
      {!loading && !error && accounts.length === 0 && <div className="awp01-state-card"><strong>No Customer accounts to show</strong><p>No Customer on this server page matches the current filters.</p></div>}

      {!loading && !error && accounts.length > 0 && (
        <section className="awp01-table-card">
          <div className="awp01-table-wrap">
            <table className="awp01-table awp02-table">
              <thead><tr><th>Customer</th><th>Contact</th><th>Customer ID</th><th>Status</th><th>Action</th></tr></thead>
              <tbody>{accounts.map((account) => (
                <tr key={account.userAccountId}>
                  <td><div className="awp01-user-cell"><strong>{account.profile?.fullName || 'Customer'}</strong><code>{shortId(account.userAccountId)}</code></div></td>
                  <td><div className="awp01-contact-cell"><span>{account.email}</span><small>{account.phoneNumber}</small></div></td>
                  <td><code title={account.profile?.customerId ?? ''}>{shortId(account.profile?.customerId ?? '')}</code></td>
                  <td><AccountStatusBadge status={account.status} /></td>
                  <td><button className={account.status === 'ACTIVE' ? 'awp02-status-button awp02-status-button--danger' : 'awp02-status-button awp02-status-button--success'} type="button" onClick={() => { setActionError(null); setTarget(account) }}>{account.status === 'ACTIVE' ? 'Deactivate' : 'Activate'}</button></td>
                </tr>
              ))}</tbody>
            </table>
          </div>
          <footer className="awp01-pagination"><span>API page {pagination.page} of {Math.max(pagination.totalPages, 1)} · {pagination.totalItems} total accounts</span><div><button className="awp01-button awp01-button--secondary" disabled={pagination.page <= 1} onClick={() => void loadPage(pagination.page - 1)}>Previous</button><button className="awp01-button awp01-button--secondary" disabled={pagination.page >= pagination.totalPages} onClick={() => void loadPage(pagination.page + 1)}>Next</button></div></footer>
        </section>
      )}

      {target && (
        <div className="awp02-modal-layer" role="presentation">
          <div className="awp02-modal" role="dialog" aria-modal="true" aria-labelledby="awp02-dialog-title">
            <header><div><p className="awp01-eyebrow">{target.status === 'ACTIVE' ? 'ADM-004 · BR-ACC-02' : 'ADM-003'}</p><h2 id="awp02-dialog-title">{target.status === 'ACTIVE' ? 'Deactivate Customer?' : 'Activate Customer?'}</h2></div><button className="awp01-icon-button" type="button" disabled={busy} onClick={() => setTarget(null)}><X size={18} /></button></header>
            <div className="awp02-person"><strong>{target.profile?.fullName}</strong><span>{target.email}</span></div>
            {target.status === 'ACTIVE' ? <div className="awp02-guard"><ShieldAlert size={18} /><span>The server will reject this action if a CONFIRMED Reservation or ACTIVE Contract exists. Existing historical data remains intact.</span></div> : <p className="awp02-copy">Activation restores login/action eligibility subject to the account's role and normal authorization rules.</p>}
            {actionError && <div className="awp02-inline-error" role="alert"><strong>{actionError.code}</strong><span>{actionError.message}</span>{actionError.traceId ? <small>Trace: {actionError.traceId}</small> : null}</div>}
            <footer><button className="awp01-button awp01-button--secondary" type="button" disabled={busy} onClick={() => setTarget(null)}>Cancel</button><button className={target.status === 'ACTIVE' ? 'awp02-confirm awp02-confirm--danger' : 'awp02-confirm'} type="button" disabled={busy} onClick={() => void confirmStatusChange()}>{busy ? 'Saving…' : target.status === 'ACTIVE' ? 'Deactivate Customer' : 'Activate Customer'}</button></footer>
          </div>
        </div>
      )}
    </div>
  )
}
