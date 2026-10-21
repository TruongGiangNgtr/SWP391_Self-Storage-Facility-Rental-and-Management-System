import { Bell, RefreshCw } from 'lucide-react'
import { useSearchParams } from 'react-router-dom'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { CustomerNotificationList } from '../components/CustomerNotificationList'
import { CustomerTabs } from '../components/CustomerTabs'
import { EmptyState, LoadingState, PaginationControls } from '../components/PageStates'
import { useNotifications } from '../features/customer-notifications/useNotifications'
import { presentValidationError } from '../api/apiErrorPresentation'
import '../styles/customer-notifications.css'

export function CustomerNotificationsPage() {
  const [params, setParams] = useSearchParams()
  const history = params.get('tab') === 'history'
  const pageValue = params.get('page')
  const page = pageValue === null ? 1 : Number(pageValue)
  const validPage = Number.isSafeInteger(page) && page >= 1 && page <= Math.floor(2147483647 / 20)
  const notifications = useNotifications(page, 20, !history && validPage)
  const labels = ['Notifications', 'Transaction History']
  function changePage(nextPage: number) {
    setParams(current => { const next = new URLSearchParams(current); if (nextPage === 1) next.delete('page'); else next.set('page', String(nextPage)); return next })
  }

  return <main className="page-container flow-page customer-notifications-page">
    <section className="page-heading notification-page-heading"><div><p className="eyebrow">Customer Portal</p><h1>Notifications & History</h1><p className="muted">Review sent notifications for your account and stay informed about your storage.</p></div>
      {!history && <button className="customer-icon-button" type="button" aria-label="Refresh Notifications" title="Refresh Notifications" disabled={notifications.loading || !validPage} onClick={notifications.refresh}><RefreshCw size={18} aria-hidden="true" /></button>}
    </section>
    <CustomerTabs labels={labels} ariaLabel="Notifications and transaction history" selected={history ? 1 : 0} onChange={index => setParams(current => { const next = new URLSearchParams(current); if (index === 1) next.set('tab', 'history'); else next.delete('tab'); return next })}>{({ tabId, panelId }) => <>
      <section role="tabpanel" id={panelId(0)} aria-labelledby={tabId(0)} hidden={history} tabIndex={0}>
        <p className="notification-history-note">Sent notification history · Times shown in GMT+7. This list does not track read or unread status.</p>
        <ApiErrorAlert error={validPage ? notifications.error : presentValidationError('This notification page is invalid. Return to the first page.')} />
        {(!validPage || notifications.error) && <div className="notification-recovery">{validPage && <button className="button button-secondary" type="button" onClick={notifications.refresh}>Retry Notifications</button>}{(!validPage || page > 1) && <button className="button button-secondary" type="button" onClick={() => changePage(1)}>First Page</button>}</div>}
        {notifications.loading ? <LoadingState label="Loading notifications…" /> : notifications.response && <>
          {notifications.response.data.length ? <><p className="notification-page-count">{notifications.response.pagination.totalItems} sent notifications · Page {page}</p><CustomerNotificationList items={notifications.response.data} /></> : <div className="panel notification-empty"><span className="notification-icon"><Bell size={24} aria-hidden="true" /></span><h2>{page > 1 ? 'No Notifications on This Page' : 'No Sent Notifications Yet'}</h2><EmptyState message={page > 1 ? 'The available notification history may have changed. Return to the first page.' : 'Sent notifications will appear here when they are available.'} />{page > 1 && <button className="button button-secondary" type="button" onClick={() => changePage(1)}>First Page</button>}</div>}
          <PaginationControls pagination={notifications.response.pagination} disabled={notifications.loading} onPageChange={changePage} />
        </>}
      </section>
      <section role="tabpanel" id={panelId(1)} aria-labelledby={tabId(1)} hidden={!history} tabIndex={0}><div className="panel notification-empty"><span className="notification-icon"><Bell size={24} aria-hidden="true" /></span><h2>Transaction History</h2><p role="status">This feature is not available yet. Please try again later.</p></div></section>
    </>}</CustomerTabs>
  </main>
}
