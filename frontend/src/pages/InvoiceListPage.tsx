import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { FileText, RefreshCw, Search, SlidersHorizontal } from 'lucide-react'
import { billingApi } from '../api/billingApi'
import { contractApi } from '../api/contractApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { CustomerTabs } from '../components/CustomerTabs'
import { CustomerInvoiceContext } from '../components/CustomerInvoiceContext'
import { CustomerStatusBadge } from '../components/CustomerStatusBadge'
import { EmptyState, LoadingState, PaginationControls } from '../components/PageStates'
import { DepositContractPanel } from '../features/billing/DepositContractPanel'
import { isInvoiceListResponse, useBillingCollection } from '../features/billing/useBillingCollection'
import { isContractListResponse } from '../features/contracts/contractListPresentation'
import { contractTitle, contractUnit, customerLabel } from '../utils/customerPresentation'
import { reservationPeriod } from '../features/reservations/reservationListPresentation'
import { formatMoney, formatUtcDateTime } from '../utils/formatters'
import '../styles/customer-billing.css'

const pageNumber = (value: string | null) => { const page = Number(value ?? 1); return Number.isSafeInteger(page) && page > 0 ? page : 1 }
const STATUSES = ['UNPAID', 'PAID', 'OVERDUE', 'CANCELLED'] as const

function AllInvoices({ page, revision, query, status, type, onChange }: {
  page: number; revision: number; query: string; status: string; type: string; onChange(values: Record<string, string>): void
}) {
  const list = useBillingCollection(page, revision, billingApi.listInvoices, isInvoiceListResponse)
  const invoices = list.response?.data ?? []
  const visible = invoices.filter(invoice => (!status || invoice.status === status) && (!type || invoice.invoiceType === type) &&
    `${customerLabel(invoice.invoiceType)} ${invoice.billingMonth ?? ''} ${customerLabel(invoice.status)}`.toLowerCase().includes(query.trim().toLowerCase()))
  return <div className="billing-stack">
    <section className="panel billing-filter" aria-label="Filter invoices on this page">
      <div className="form-field"><label htmlFor="billing-search">Search this page</label><div className="billing-search"><Search size={16} aria-hidden="true" /><input id="billing-search" type="search" placeholder="Invoice type, rental month or status" value={query} onChange={event => onChange({ q: event.target.value })} /></div></div>
      <div className="form-field"><label htmlFor="billing-type">Invoice Type</label><select id="billing-type" value={type} onChange={event => onChange({ type: event.target.value })}><option value="">All Types</option><option value="DEPOSIT">Deposit</option><option value="RENTAL_FEE">Rental Fee</option></select></div>
      <div className="form-field"><label htmlFor="billing-status">Status</label><select id="billing-status" value={status} onChange={event => onChange({ status: event.target.value })}><option value="">All Statuses</option>{STATUSES.map(value => <option key={value} value={value}>{customerLabel(value)}</option>)}</select></div>
      <button className="button button-secondary" type="button" onClick={() => onChange({ q: '', type: '', status: '' })}><SlidersHorizontal size={16} aria-hidden="true" />Clear Filters</button>
      <p className="muted billing-filter-note" aria-live="polite">{list.loading ? 'Loading invoices…' : `${visible.length} matching records on this page. Filters do not search other pages.`}</p>
    </section>
    <ApiErrorAlert error={list.error} />
    {list.loading ? <LoadingState label="Loading invoices…" /> : list.error ? null : !invoices.length ? <EmptyState message="You do not have any invoices yet. Reservation deposits will appear here even before a contract is created." /> : !visible.length ? <EmptyState message="No invoices on this page match your filters. Clear filters or review another page." /> :
      <section className="panel billing-invoice-list" aria-label="Invoices">
        <div className="billing-section-heading"><div><h2>Your Invoices</h2><p className="muted">Deposit and rental fee records, as reported by the system.</p></div><FileText size={20} aria-hidden="true" /></div>
        <div className="billing-table-wrap" tabIndex={0} role="region" aria-label="Invoice records"><table className="billing-table"><thead><tr><th scope="col">Invoice</th><th scope="col">Payment Deadline</th><th scope="col">Amount Due</th><th scope="col">Status</th><th scope="col"><span className="billing-sr-only">Actions</span></th></tr></thead><tbody>
          {visible.map(invoice => <tr key={invoice.invoiceId}><td><strong>{invoice.invoiceType === 'DEPOSIT' ? 'Reservation Deposit' : 'Rental Fee'}</strong><CustomerInvoiceContext invoice={invoice} /></td><td>{formatUtcDateTime(invoice.dueDate)}{invoice.paidAt && <small>Paid: {formatUtcDateTime(invoice.paidAt)}</small>}</td><td><strong>{formatMoney(invoice.amountDue)}</strong></td><td><CustomerStatusBadge status={invoice.status} /></td><td><Link className="button button-secondary" to={`/customer/invoices/${invoice.invoiceId}`}>View Invoice</Link></td></tr>)}
        </tbody></table></div>
      </section>}
    {list.response && <PaginationControls pagination={list.response.pagination} disabled={list.loading} onPageChange={next => onChange({ page: String(next) })} />}
  </div>
}

function ContractDeposits({ page, revision, selectedId, onChange }: { page: number; revision: number; selectedId: string; onChange(values: Record<string, string>): void }) {
  const list = useBillingCollection(page, revision, contractApi.list, isContractListResponse)
  const contracts = list.response?.data ?? []
  const selected = contracts.find(item => item.contractId === selectedId) ?? (selectedId ? undefined : contracts.find(item => item.status === 'ACTIVE') ?? contracts[0])
  return <div className="billing-stack">
    <ApiErrorAlert error={list.error} />
    {list.loading ? <LoadingState label="Loading your contracts…" /> : list.error ? null : !contracts.length ? <section className="panel billing-empty"><EmptyState message="No contracts on this page. You can still view and pay a reservation deposit from My Reservations." /><Link className="button button-secondary" to="/customer/invoices?tab=invoices">View All Invoices</Link></section> : <>
      <section className="panel billing-contract-picker" aria-label="Choose deposit context">
        <span className="billing-icon"><FileText size={22} aria-hidden="true" /></span><div><strong>Viewing deposit details by contract</strong><p className="muted">Choose a rental to review its deposit and recorded charges.</p></div>
        <div className="form-field"><label htmlFor="deposit-contract">Switch Contract</label><select id="deposit-contract" value={selected?.contractId ?? ''} onChange={event => onChange({ contract: event.target.value })}>{!selected && <option value="">Choose a contract on this page</option>}{contracts.map(item => <option key={item.contractId} value={item.contractId}>{contractTitle(item)} · {contractUnit(item)} · {reservationPeriod(item.startMonth, item.endMonth)} · {customerLabel(item.status)}</option>)}</select></div>
      </section>
      {selected ? <DepositContractPanel key={`${selected.contractId}:${revision}`} item={selected} /> : <EmptyState message="The selected contract is not on this page. Choose a contract above or review another page." />}
    </>}
    {list.response && <PaginationControls pagination={list.response.pagination} disabled={list.loading} onPageChange={next => onChange({ contractPage: String(next), contract: '' })} />}
  </div>
}

export function InvoiceListPage() {
  const [params, setParams] = useSearchParams()
  const [revision, setRevision] = useState(0)
  const selected = params.get('tab') === 'invoices' ? 1 : 0
  function change(values: Record<string, string>, replace = false) {
    const next = new URLSearchParams(params)
    Object.entries(values).forEach(([key, value]) => { if (value) next.set(key, value); else next.delete(key) })
    setParams(next, { replace })
  }
  return <main className="page-container flow-page customer-billing">
    <header className="billing-page-heading"><div><p className="eyebrow">Deposit Management</p><h1>Deposit &amp; Invoices</h1><p className="muted">Review your security deposits, recorded charges and rental invoices.</p></div><button className="button button-secondary" type="button" onClick={() => setRevision(value => value + 1)}><RefreshCw size={16} aria-hidden="true" />Refresh Records</button></header>
    <CustomerTabs labels={['Deposit & Charges', 'All Invoices']} ariaLabel="Deposit and invoice sections" selected={selected} onChange={index => change({ tab: index ? 'invoices' : '' })}>{({ tabId, panelId }) => <section role="tabpanel" id={panelId(selected)} aria-labelledby={tabId(selected)} tabIndex={0} className="billing-stack">
      {selected === 0 ? <ContractDeposits page={pageNumber(params.get('contractPage'))} revision={revision} selectedId={params.get('contract') ?? ''} onChange={change} /> : <AllInvoices page={pageNumber(params.get('page'))} revision={revision} query={params.get('q') ?? ''} status={STATUSES.find(value => value === params.get('status')) ?? ''} type={['DEPOSIT', 'RENTAL_FEE'].find(value => value === params.get('type')) ?? ''} onChange={values => change(values, !('page' in values))} />}
    </section>}</CustomerTabs>
  </main>
}
