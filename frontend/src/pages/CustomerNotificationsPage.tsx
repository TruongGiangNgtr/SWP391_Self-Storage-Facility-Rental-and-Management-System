import { Bell, ReceiptText, RefreshCw } from 'lucide-react'
import { useSearchParams } from 'react-router-dom'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { CustomerNotificationList } from '../components/CustomerNotificationList'
import { CustomerTabs } from '../components/CustomerTabs'
import { CustomerTransactionList } from '../components/CustomerTransactionList'
import { EmptyState, LoadingState, PaginationControls } from '../components/PageStates'
import { useNotifications } from '../features/customer-notifications/useNotifications'
import { useTransactions } from '../features/customer-transactions/useTransactions'
import { presentValidationError } from '../api/apiErrorPresentation'
import '../styles/customer-notifications.css'
import '../styles/customer-overview-supporting.css'

export function CustomerNotificationsPage() {
  const [params, setParams] = useSearchParams()
  const history = params.get('tab') === 'history'
  const pageValue = params.get('page')
  const page = pageValue === null ? 1 : Number(pageValue)
  const validPage = Number.isSafeInteger(page) && page >= 1 && page <= Math.floor(2147483647 / 20)
  const notifications = useNotifications(page, 20, !history && validPage)
  const transactions = useTransactions(page, 20, history && validPage)
  const active = history ? transactions : notifications
  const refreshLabel = history ? 'Refresh Transaction History' : 'Refresh Notifications'
  const labels = ['Notifications', 'Transaction History']
  function changePage(nextPage: number) {
    setParams(current => { const next = new URLSearchParams(current); if (nextPage === 1) next.delete('page'); else next.set('page', String(nextPage)); return next })
  }

  return <main className="page-container flow-page customer-notifications-page">
    <section className="page-heading notification-page-heading"><div><p className="account-area-label">Your account activity</p><h1>Notifications & History</h1><p className="muted">Review your notifications and payment history in one place.</p></div>
      <button className="customer-icon-button" type="button" aria-label={refreshLabel} title={refreshLabel} disabled={active.loading || !validPage} onClick={active.refresh}><RefreshCw size={18} aria-hidden="true" /><span>Refresh</span></button>
    </section>
    <CustomerTabs labels={labels} ariaLabel="Notifications and transaction history" selected={history ? 1 : 0} onChange={index => { if ((index === 1) === history) return; setParams(current => { const next = new URLSearchParams(current); next.delete('page'); if (index === 1) next.set('tab', 'history'); else next.delete('tab'); return next }) }}>{({ tabId, panelId }) => <>
      <section role="tabpanel" id={panelId(0)} aria-labelledby={tabId(0)} hidden={history} tabIndex={0}>
        <p className="notification-history-note">Updates about your storage activity · Times shown in GMT+7.</p>
        <ApiErrorAlert error={validPage ? notifications.error : presentValidationError('This notification page is invalid. Return to the first page.')} />
        {(!validPage || notifications.error) && <div className="notification-recovery">{validPage && <button className="button button-secondary" type="button" onClick={notifications.refresh}>Retry Notifications</button>}{(!validPage || page > 1) && <button className="button button-secondary" type="button" onClick={() => changePage(1)}>First Page</button>}</div>}
        {notifications.loading ? <LoadingState label="Loading notifications…" /> : notifications.response && <>
          {notifications.response.data.length ? <><p className="notification-page-count">{notifications.response.pagination.totalItems} {notifications.response.pagination.totalItems === 1 ? 'notification' : 'notifications'} · Page {page}</p><CustomerNotificationList items={notifications.response.data} /></> : <div className="panel notification-empty"><span className="notification-icon"><Bell size={24} aria-hidden="true" /></span><h2>{page > 1 ? 'No Notifications on This Page' : 'No Notifications Yet'}</h2><EmptyState message={page > 1 ? 'The available notifications may have changed. Return to the first page.' : 'Updates about your storage activity will appear here once recorded.'} />{page > 1 && <button className="button button-secondary" type="button" onClick={() => changePage(1)}>First Page</button>}</div>}
          <PaginationControls pagination={notifications.response.pagination} disabled={notifications.loading} onPageChange={changePage} />
        </>}
      </section>
      <section role="tabpanel" id={panelId(1)} aria-labelledby={tabId(1)} hidden={!history} tabIndex={0}>
        <p className="notification-history-note">Each entry is a payment attempt. Pending means a result is still awaited, and does not confirm payment. Times shown in GMT+7.</p>
        <ApiErrorAlert error={validPage ? transactions.error : presentValidationError('This transaction history page is invalid. Return to the first page.')} />
        {(!validPage || transactions.error) && <div className="notification-recovery">
          {validPage && <button className="button button-secondary" type="button" onClick={transactions.refresh}>Retry Transaction History</button>}
          {(!validPage || page > 1) && <button className="button button-secondary" type="button" onClick={() => changePage(1)}>First Page</button>}
        </div>}
        {transactions.loading ? <LoadingState label="Loading transaction history…" /> : transactions.response && <>
          {transactions.response.data.length ? <>
            <p className="notification-page-count">{transactions.response.pagination.totalItems} payment attempts · Page {page} · Newest first</p>
            <CustomerTransactionList items={transactions.response.data} />
          </> : <div className="panel notification-empty">
            <span className="notification-icon"><ReceiptText size={24} aria-hidden="true" /></span>
            <h2>{page > 1 ? 'No Transactions on This Page' : 'No Transactions Yet'}</h2>
            <EmptyState message={page > 1 ? 'The available payment history may have changed. Return to the first page.' : 'Your payment attempts will appear here once recorded. You can view your invoices in Deposit & Invoices.'} />
            {page > 1 && <button className="button button-secondary" type="button" onClick={() => changePage(1)}>First Page</button>}
          </div>}
          <PaginationControls pagination={transactions.response.pagination} disabled={transactions.loading} onPageChange={changePage} />
        </>}
      </section>
    </>}</CustomerTabs>
  </main>
}
