import { Link, useSearchParams } from 'react-router-dom'
import { ArrowLeft, Bell, ShieldCheck } from 'lucide-react'
import { CustomerTabs } from '../components/CustomerTabs'
import '../styles/guest-customer-remaining.css'

export function CustomerUnavailablePage({ title, historyTabs = false }: { title: string; historyTabs?: boolean }) {
  const [params, setParams] = useSearchParams()
  const selected = params.get('tab') === 'history' ? 1 : 0
  const labels = ['Notifications', 'Transaction History']
  const notice = (label: string) => <section className="panel stack customer-unavailable-card">
    <span className="remaining-icon">{historyTabs ? <Bell size={24} aria-hidden="true" /> : <ShieldCheck size={24} aria-hidden="true" />}</span>
    <h2>{historyTabs ? label : 'Not Available Yet'}</h2>
    <p className="unavailable-message" role="status">This feature is not available yet. Please try again later.</p>
    <p className="muted">You can continue using the other sections of your Customer Portal.</p>
  </section>

  // UI scaffold only: no feature API request, mock records, fake trace ID or mutations.
  return <main className="page-container flow-page customer-unavailable-design support-unavailable-page">
    <section className="page-heading"><h1>{title}</h1><p className="muted">This section is planned but is not available in the current interface.</p></section>
    {historyTabs ? <CustomerTabs labels={labels} ariaLabel="Notifications and transaction history" selected={selected} onChange={index => setParams(current => { const next = new URLSearchParams(current); if (index === 1) next.set('tab', 'history'); else next.delete('tab'); return next })}>{({ tabId, panelId }) => <>
      {labels.map((label, index) => <div key={label} role="tabpanel" id={panelId(index)} aria-labelledby={tabId(index)} hidden={selected !== index} tabIndex={0}>{notice(label)}</div>)}
    </>}</CustomerTabs> : notice(title)}
    <div className="action-row"><Link className="button button-secondary" to="/customer"><ArrowLeft size={16} aria-hidden="true" />Back to Overview</Link></div>
  </main>
}
