import { Link, useSearchParams } from 'react-router-dom'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { CustomerTabs } from '../components/CustomerTabs'

export function CustomerUnavailablePage({ title, historyTabs = false }: { title: string; historyTabs?: boolean }) {
  const [params, setParams] = useSearchParams()
  const selected = params.get('tab') === 'history' ? 1 : 0
  const labels = ['Notifications', 'Transaction History']
  const notice = (label: string) => <section className="panel stack">
    <h2>{historyTabs ? label : 'Not Available Yet'}</h2>
    <ApiErrorAlert error={{ message: 'This feature is not available yet. Please try again later.', details: [] }} />
    <p className="muted">You can continue using the other sections of your Customer Portal.</p>
  </section>

  // UI scaffold only: no feature API request, mock records, fake trace ID or mutations.
  return <main className="page-container flow-page">
    <section className="page-heading"><p className="eyebrow">Customer Portal</p><h1>{title}</h1><p className="muted">This section is planned but is not available in the current interface.</p></section>
    {historyTabs ? <CustomerTabs labels={labels} ariaLabel="Notifications and transaction history" selected={selected} onChange={index => setParams(index === 1 ? { tab: 'history' } : {}, { replace: true })}>{({ tabId, panelId }) => <>
      {labels.map((label, index) => <div key={label} role="tabpanel" id={panelId(index)} aria-labelledby={tabId(index)} hidden={selected !== index} tabIndex={0}>{notice(label)}</div>)}
    </>}</CustomerTabs> : notice(title)}
    <div className="action-row"><Link className="button button-secondary" to="/customer">Back to Overview</Link></div>
  </main>
}
