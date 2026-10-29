import { AdminFilterScope, AdminListNavigation } from '../../components/admin/AdminListNavigation'
import { AdminModal } from '../../components/admin/AdminModal'
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
          message: 'This customer cannot be deactivated while they have a confirmed reservation or active contract.',
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
          <h1>Customer account status</h1>
          <p>Manage customer access. Deactivating an account preserves its history.</p>
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
        <div className="awp01-summary-card"><span>Customers on page</span><strong>{loading ? '—' : rawAccounts.length}</strong></div>
        <div className="awp01-summary-card"><span>Active on page</span><strong>{loading ? '—' : activeCount}</strong></div>
        <div className="awp01-summary-card"><span>Inactive on page</span><strong>{loading ? '—' : inactiveCount}</strong></div>
      </section>

      <section className="awp01-toolbar">
        <label>
          <span>Status</span>
          <select value={status} onChange={(event: ChangeEvent<HTMLSelectElement>) => setStatus(event.target.value as typeof status)}>
            <option value="ALL">All statuses</option><option value="ACTIVE">ACTIVE</option><option value="INACTIVE">INACTIVE</option>
          </select>
        </label>
        <label className="awp01-toolbar__search awp02-search-wide">
          <span>Search current page</span>
          <div className="awp01-search-input"><Search size={16} /><input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Name, email or phone…" /></div>
        </label>
      </section>

      <AdminFilterScope loading={loading} page={pagination.page} loaded={rawAccounts.length} matching={accounts.length} onClear={() => { setStatus('ALL'); setSearch('') }} />

      {loading && <div className="awp01-state-grid">{Array.from({ length: 5 }).map((_, index) => <div className="awp01-skeleton" key={index} />)}</div>}
      {!loading && error && <div className="awp01-state-card awp01-state-card--error" role="alert"><strong>Unable to load this information</strong><small>Code: {error.code}</small><p>{error.message}</p><button className="awp01-button awp01-button--secondary" onClick={() => void refresh()}>Try again</button></div>}
      {!loading && !error && accounts.length === 0 && <div className="awp01-state-card"><strong>No Customer accounts to show</strong><p>No customers on this page match. Clear the filters or browse another directory page.</p></div>}

      {!loading && !error && accounts.length > 0 && (
        <section className="awp01-table-card">
          <div className="awp01-table-wrap" role="region" aria-label="Account results (scroll horizontally for more columns)" tabIndex={0}>
            <table className="awp01-table awp02-table">
              <thead><tr><th>Customer</th><th>Contact</th><th>Status</th><th>Action</th></tr></thead>
              <tbody>{accounts.map((account) => (
                <tr key={account.userAccountId}>
                  <td><div className="awp01-user-cell"><strong>{account.profile?.fullName || 'Customer'}</strong></div></td>
                  <td><div className="awp01-contact-cell"><span>{account.email}</span><small>{account.phoneNumber}</small></div></td>
                  <td><AccountStatusBadge status={account.status} /></td>
                  <td><button className={account.status === 'ACTIVE' ? 'awp02-status-button awp02-status-button--danger' : 'awp02-status-button awp02-status-button--success'} type="button" onClick={() => { setActionError(null); setTarget(account) }}>{account.status === 'ACTIVE' ? 'Deactivate' : 'Activate'}</button></td>
                </tr>
              ))}</tbody>
            </table>
          </div>
        </section>
      )}

      {!error && <AdminListNavigation pagination={pagination} loading={loading} onPage={page => void loadPage(page)} />}

      {target && (
        <AdminModal className="awp02-modal-layer" label={target.status === 'ACTIVE' ? 'Deactivate customer' : 'Activate customer'} busy={busy} onClose={() => setTarget(null)}>
          <div className="awp02-modal" aria-labelledby="awp02-dialog-title">
            <header><div><h2 id="awp02-dialog-title">{target.status === 'ACTIVE' ? 'Deactivate Customer?' : 'Activate Customer?'}</h2></div><button className="awp01-icon-button" type="button" disabled={busy} aria-label="Close status confirmation" onClick={() => setTarget(null)}><X size={18} /></button></header>
            <div className="awp02-person"><strong>{target.profile?.fullName}</strong><span>{target.email}</span></div>
            {target.status === 'ACTIVE' ? <div className="awp02-guard"><ShieldAlert size={18} /><span>This customer cannot be deactivated while they have a confirmed reservation or an active contract. Their account history will be preserved.</span></div> : <p className="awp02-copy">The customer will be able to sign in again and use the features available to their account.</p>}
            {actionError && <div className="awp02-inline-error" role="alert"><strong>Unable to change account status</strong><small>Code: {actionError.code}</small><span>{actionError.message}</span>{actionError.traceId ? <small>Trace: {actionError.traceId}</small> : null}</div>}
            <footer><button className="awp01-button awp01-button--secondary" type="button" disabled={busy} onClick={() => setTarget(null)}>Cancel</button><button className={target.status === 'ACTIVE' ? 'awp02-confirm awp02-confirm--danger' : 'awp02-confirm'} type="button" disabled={busy} onClick={() => void confirmStatusChange()}>{busy ? 'Saving…' : target.status === 'ACTIVE' ? 'Deactivate Customer' : 'Activate Customer'}</button></footer>
          </div>
        </AdminModal>
      )}
    </div>
  )
}
