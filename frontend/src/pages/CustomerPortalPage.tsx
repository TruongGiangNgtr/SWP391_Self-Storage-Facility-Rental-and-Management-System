import { useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { contractApi } from '../api/contractApi'
import { billingApi } from '../api/billingApi'
import { visitApi } from '../api/visitApi'
import { listSupportTickets } from '../api/supportTicketApi'
import { useAuth } from '../auth/auth.context'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { EmptyState, LoadingState } from '../components/PageStates'
import { FlowIcon, type FlowIconName } from '../components/FlowIcon'
import { CustomerStatusBadge as StatusBadge } from '../components/CustomerStatusBadge'
import { CustomerInvoiceContext } from '../components/CustomerInvoiceContext'
import { contractTitle, contractUnit, customerLabel } from '../utils/customerPresentation'
import { useOverviewCollection } from '../features/customer-overview/useOverviewCollection'
import { isOverviewContract, isOverviewInvoice, isOverviewVisit } from '../features/customer-overview/overviewValidation'
import { isSupportTicket } from '../features/customer-support/supportValidation'
import { formatMoney, formatMonthRange, getCurrentBusinessDate } from '../utils/formatters'
import { StorageSearchPage } from './StorageSearchPage'

const readContracts = () => contractApi.list(1, 20)
const readVisits = () => visitApi.list(1, 20)
const readInvoices = () => billingApi.listInvoices(1, 20)
const readTickets = () => listSupportTickets({ page: 1, pageSize: 20 })
const VISIT_NAMES = { RESERVATION: 'Handover', ACCESS: 'Access', RETURN: 'Return' }

function PreviewPanel({ title, to, children }: { title: string; to: string; children: ReactNode }) {
  return <section className="panel dashboard-panel" aria-label={title}><div className="section-heading"><h2>{title}</h2><Link to={to}>View All →</Link></div>{children}</section>
}

export function CustomerPortalPage() {
  const { user } = useAuth()
  const [revision, setRevision] = useState(0)
  const contracts = useOverviewCollection(readContracts, revision, isOverviewContract)
  const visits = useOverviewCollection(readVisits, revision, isOverviewVisit)
  const invoices = useOverviewCollection(readInvoices, revision, isOverviewInvoice)
  const tickets = useOverviewCollection(readTickets, revision, isSupportTicket)
  const activeContracts = contracts.response?.data.filter(item => item.status === 'ACTIVE') ?? []
  const upcomingVisits = visits.response?.data.filter(item => item.status === 'SCHEDULED' && item.visitDate >= getCurrentBusinessDate()).sort((a, b) => a.visitDate.localeCompare(b.visitDate)) ?? []
  const unpaidInvoices = invoices.response?.data.filter(item => item.status === 'UNPAID' || item.status === 'OVERDUE') ?? []
  const openTickets = tickets.response?.data.filter(item => item.status === 'OPEN' || item.status === 'IN_PROGRESS') ?? []
  const busy = contracts.loading || visits.loading || invoices.loading || tickets.loading
  const metrics: { title: string; to: string; icon: FlowIconName; count: number; loading: boolean; available: boolean }[] = [
    { title: 'Active Contracts', to: '/customer/contracts', icon: 'unit', count: activeContracts.length, loading: contracts.loading, available: Boolean(contracts.response) },
    { title: 'Upcoming Visits', to: '/customer/visits', icon: 'calendar', count: upcomingVisits.length, loading: visits.loading, available: Boolean(visits.response) },
    { title: 'Unpaid Invoices', to: '/customer/invoices', icon: 'invoices', count: unpaidInvoices.length, loading: invoices.loading, available: Boolean(invoices.response) },
    { title: 'Open Support Tickets', to: '/customer/support-tickets', icon: 'visits', count: openTickets.length, loading: tickets.loading, available: Boolean(tickets.response) },
  ]

  return <main className="page-container flow-page">
    <section className="page-heading page-heading-actions"><div><p className="eyebrow">Customer Portal</p><h1>Welcome back{user?.fullName ? `, ${user.fullName}` : ''}</h1><p className="muted">Manage your storage, reservations, invoices and support requests in one place.</p></div><button className="button button-secondary" type="button" disabled={busy} onClick={() => setRevision(value => value + 1)}>Refresh Overview</button></section>
    <StorageSearchPage embedded />
    <section className="dashboard-metrics" aria-label="Current-page overview">
      {metrics.map(item => <Link className="panel dashboard-metric" to={item.to} key={item.title}><span className="unit-icon-tile"><FlowIcon name={item.icon} /></span><span>{item.title}</span><strong>{item.loading ? '…' : item.available ? item.count : '—'}</strong><small>{item.loading ? 'Loading…' : item.available ? 'In the first page of results' : 'Not available'}</small></Link>)}
    </section>
    <p className="dashboard-page-note">Preview counts cover the first 20 records in each list, not your entire account. Open a full list to review other pages.</p>
    <div className="dashboard-grid">
      <PreviewPanel title="Active Contracts" to="/customer/contracts">
        <ApiErrorAlert error={contracts.error} />
        {contracts.loading ? <LoadingState label="Loading contracts..." /> : contracts.response && (activeContracts.length === 0 ? <EmptyState message="No active contracts in this page. Find storage or open all contracts." /> : activeContracts.slice(0, 2).map(contract => <article className="dashboard-record" key={contract.contractId}>
          <div className="card-heading-row"><h3>{contractTitle(contract)}</h3><StatusBadge status={contract.status} /></div>
          <dl className="detail-list"><div><dt>Storage Unit</dt><dd>{contractUnit(contract)}</dd></div><div><dt>Rental Period</dt><dd>{formatMonthRange(contract.startMonth, contract.endMonth)}</dd></div></dl>
          <div className="action-row"><Link className="button" to={`/customer/contracts/${contract.contractId}`}>View Contract</Link><Link className="button button-secondary" to={`/customer/contracts/${contract.contractId}/fees-return`}>Fees & Return</Link></div>
        </article>))}
      </PreviewPanel>
      <PreviewPanel title="Upcoming Visits" to="/customer/visits">
        <ApiErrorAlert error={visits.error} />
        {visits.loading ? <LoadingState label="Loading visits..." /> : visits.response && (upcomingVisits.length === 0 ? <EmptyState message="No upcoming scheduled visits in this page." /> : upcomingVisits.slice(0, 3).map(visit => <article className="dashboard-record" key={visit.visitId}><div className="card-heading-row"><h3>{VISIT_NAMES[visit.visitType]} Visit</h3><StatusBadge status={visit.status} /></div><p><FlowIcon name="calendar" /> {visit.visitDate} (GMT+7)</p><Link className="button button-secondary" to={`/customer/visits/${visit.visitId}`}>View Visit</Link></article>))}
      </PreviewPanel>
      <PreviewPanel title="Deposit & Invoices" to="/customer/invoices">
        <ApiErrorAlert error={invoices.error} />
        {invoices.loading ? <LoadingState label="Loading invoices..." /> : invoices.response && (invoices.response.data.length === 0 ? <EmptyState message="No invoices have been returned." /> : invoices.response.data.slice(0, 3).map(invoice => <article className="dashboard-record" key={invoice.invoiceId}><div className="card-heading-row"><h3>{invoice.invoiceType === 'DEPOSIT' ? 'Deposit Invoice' : 'Rental Invoice'}</h3><StatusBadge status={invoice.status} /></div><CustomerInvoiceContext invoice={invoice} /><strong>{formatMoney(invoice.amountDue)}</strong><Link className="button button-secondary" to={`/customer/invoices/${invoice.invoiceId}`}>View Invoice</Link></article>))}
        <p className="muted">For deductions and recorded settlement amounts, open Fees & Return on the relevant contract.</p>
      </PreviewPanel>
      <PreviewPanel title="Recent Support Tickets" to="/customer/support-tickets">
        <ApiErrorAlert error={tickets.error} />
        {tickets.loading ? <LoadingState label="Loading tickets..." /> : tickets.response && (tickets.response.data.length === 0 ? <EmptyState message="You do not have any tickets in this page." /> : tickets.response.data.slice(0, 3).map(ticket => <article className="dashboard-record" key={ticket.supportTicketId}><div className="card-heading-row"><h3>{customerLabel(ticket.category)}</h3><StatusBadge status={ticket.status} /></div><p>{ticket.description}</p><Link className="button button-secondary" to={`/customer/support-tickets/${ticket.supportTicketId}`}>View Ticket</Link></article>))}
      </PreviewPanel>
    </div>
    <section className="panel dashboard-support"><span className="unit-icon-tile"><FlowIcon name="visits" /></span><div><h2>Need help with your storage?</h2><p className="muted">Create a support ticket for one of your contracts and track the response.</p></div><Link className="button" to="/customer/support-tickets/new">Create Support Ticket</Link></section>
  </main>
}
