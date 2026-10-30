import { EmployeeModal } from '../../components/EmployeeModal'
import { formatUtcDateTime } from '../../utils/formatters'
import { employeeContractLabel, employeeMonthRange, employeeUnitLabel } from '../../utils/employeePresentation'
import { EmployeeEntityLabel, EmployeeReservationLabel } from '../../components/EmployeeEntityLabel'
import type { ReactNode } from 'react'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Activity, ArrowUpRight, CalendarDays, ClipboardList, CreditCard, FileText, RefreshCw, Search, Warehouse, X } from 'lucide-react'
import { useAuth } from '../../auth/auth.context'
import { reservationApi } from '../../api/reservationApi'
import { contractApi } from '../../api/contractApi'
import { visitApi } from '../../api/visitApi'
import { billingApi } from '../../api/billingApi'
import { storageUnitApi, type StorageUnit } from '../../api/storageUnitApi'
import { facilityReportApi, type FacilityOperationsReport } from '../../api/facilityReportApi'
import { presentApiError } from '../../api/apiErrorPresentation'
import type { ApiCollectionResponse } from '../../api/api.types'
import type { ReservationDetail } from '../reservations/reservation.types'
import type { HandoverContractDetail } from '../handover/contract.types'
import type { VisitDetail } from '../visits/visit.types'
import type { InvoiceDetail } from '../billing/billing.types'
import type { ContractBillingSummary, ContractReturnSummary } from '../fee-return-tracking/tracking.types'
import '../manager-portal.css'
import { catalogApi } from '../../api/catalogApi'
import { StorageUnitTimeline } from './StorageUnitTimeline'

type Tab = 'reservations' | 'contracts' | 'visits' | 'invoices' | 'units'
type RecordItem = ReservationDetail | HandoverContractDetail | VisitDetail | InvoiceDetail | StorageUnit
interface DetailState { tab: Tab; item: RecordItem; billing?: ContractBillingSummary; returns?: ContractReturnSummary }
const PAGE_SIZE = 20
const tabs: { key: Tab; label: string; icon: typeof ClipboardList }[] = [
  { key: 'reservations', label: 'Reservations', icon: ClipboardList },
  { key: 'contracts', label: 'Contracts', icon: FileText },
  { key: 'visits', label: 'Visits', icon: CalendarDays },
  { key: 'invoices', label: 'Invoices', icon: CreditCard },
  { key: 'units', label: 'Storage units', icon: Warehouse },
]

const money = (amount: number) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(amount)
const date = (input?: string | null) => input ? input.slice(0, 10) : '—'
const errorText = (error: unknown) => {
  const e = presentApiError(error)
  return `${e.message}${e.traceId ? ` (Trace: ${e.traceId})` : ''}`
}
const pageError = (error: unknown) => errorText(error)
const status = (value: string) => <span className={`mop-status mop-status-${value.toLowerCase().replace(/_/g, '-')}`}>{value.replace(/_/g, ' ')}</span>

function itemId(tab: Tab, item: RecordItem): string {
  switch (tab) {
    case 'reservations': return (item as ReservationDetail).reservationId
    case 'contracts': return (item as HandoverContractDetail).contractId
    case 'visits': return (item as VisitDetail).visitId
    case 'invoices': return (item as InvoiceDetail).invoiceId
    case 'units': return (item as StorageUnit).storageUnitId
  }
}
function titleOf(tab: Tab, item: RecordItem) {
  if (tab === 'units') return (item as StorageUnit).unitCode
  if (tab === 'reservations') { const r = item as ReservationDetail; return <>Reservation · <EmployeeEntityLabel kind="unitType" id={r.unitTypeId} /></> }
  if (tab === 'contracts') return employeeContractLabel(item as HandoverContractDetail)
  if (tab === 'visits') { const v = item as VisitDetail; return `${v.visitType.replaceAll('_', ' ')} visit · ${date(v.visitDate)}` }
  const i = item as InvoiceDetail
  return `${i.invoiceType.replaceAll('_', ' ')} invoice · ${i.billingMonth ?? date(i.dueDate)}`
}
function itemSearch(tab: Tab, item: RecordItem): string {
  return JSON.stringify(item).toLowerCase() + (tab === 'units' ? ` ${(item as StorageUnit).unitCode}` : '')
}
function ListRow({ tab, item, onSelect, unitTypeNames }: { tab: Tab; item: RecordItem; onSelect: () => void; unitTypeNames: Record<string, string> }) {
  let sub: ReactNode = ''
  let secondary: ReactNode = ''
  let state = ''
  switch (tab) {
    case 'reservations': {
      const r = item as ReservationDetail
      sub = <><EmployeeReservationLabel customerId={r.customerId} startMonth={r.startMonth} endMonth={r.endMonth} /></>
      secondary = `Deposit: ${r.depositInvoice?.status ?? '—'}`
      state = r.status
      break
    }
    case 'contracts': {
      const r = item as HandoverContractDetail
      sub = <><EmployeeEntityLabel kind="customer" id={r.customerId} /> · {r.startMonth} → {r.endMonth}</>
      secondary = employeeUnitLabel(r)
      state = r.status
      break
    }
    case 'visits': {
      const r = item as VisitDetail
      sub = `${r.visitType} · ${date(r.visitDate)}`
      secondary = r.actualReturnDate ? `Returned ${date(r.actualReturnDate)}` : 'Not physically returned'
      state = r.status
      break
    }
    case 'invoices': {
      const r = item as InvoiceDetail
      sub = `${r.invoiceType} · ${r.billingMonth ?? 'Deposit'} · ${money(r.amountDue)}`
      secondary = `Due ${date(r.dueDate)}`
      state = r.status
      break
    }
    case 'units': {
      const r = item as StorageUnit
      sub = `${r.locationInfo || 'Location not specified'} · ${unitTypeNames[r.unitTypeId] || 'Type name unavailable'}`
      secondary = ''
      state = r.status
    }
  }
  return <button type="button" className="mop-table-row" onClick={onSelect}>
    <span className="mop-record"><strong>{tab === 'units' ? `${unitTypeNames[(item as StorageUnit).unitTypeId] || 'Type name unavailable'} · ${(item as StorageUnit).unitCode}` : titleOf(tab, item)}</strong><small>{sub}</small></span>
    <span className="mop-secondary">{secondary}</span>
    {status(state)}
    <ArrowUpRight size={17} aria-hidden="true" />
  </button>
}
function DetailField({ label, value }: { label: string; value?: ReactNode }) {
  return <div className="mop-detail-field"><dt>{label}</dt><dd>{value ?? '—'}</dd></div>
}
function RecordDetail({ detail, refreshing, facilityName, unitTypeNames }: { detail: DetailState; refreshing: boolean; facilityName: string; unitTypeNames: Record<string, string> }) {
  const { tab, item, billing, returns } = detail
  return <>
      <p className="mop-muted">Review details here. Use the relevant workflow to make changes.</p>
      <dl className="mop-detail-grid">
        <DetailField label="Record" value={titleOf(tab, item)} />
        {tab === 'reservations' && (() => { const r = item as ReservationDetail; return <>
          <DetailField label="Reservation" value={<EmployeeReservationLabel customerId={r.customerId} startMonth={r.startMonth} endMonth={r.endMonth} visitDate={r.reservationVisit?.visitDate} />}/><DetailField label="Status" value={r.status}/><DetailField label="Customer" value={<EmployeeEntityLabel kind="customer" id={r.customerId} />}/><DetailField label="Unit type" value={<EmployeeEntityLabel kind="unitType" id={r.unitTypeId} />}/><DetailField label="Rental period" value={employeeMonthRange(r.startMonth, r.endMonth)}/><DetailField label="Locked first-month rent" value={money(r.lockedRentalPrice)}/><DetailField label="Deposit" value={`${money(r.depositAmount)} · ${r.depositInvoice?.status ?? '—'}`}/><DetailField label="Reservation visit" value={r.reservationVisit ? `${date(r.reservationVisit.visitDate)} · ${r.reservationVisit.status}` : 'Not scheduled'} />
        </> })()}
        {tab === 'contracts' && (() => { const c = item as HandoverContractDetail; return <>
          <DetailField label="Status" value={c.status}/><DetailField label="Customer" value={<EmployeeEntityLabel kind="customer" id={c.customerId} />}/><DetailField label="Storage unit" value={employeeUnitLabel(c)}/><DetailField label="Original reservation" value={c.reservationId ? 'Linked reservation · original dates unavailable in this view' : '—'}/><DetailField label="Rental period" value={employeeMonthRange(c.startMonth, c.endMonth)}/><DetailField label="Captured discount" value={<EmployeeEntityLabel kind="discount" id={c.discountId} scope={c.customerId} empty="No discount" />}/>
        </> })()}
        {tab === 'visits' && (() => { const v = item as VisitDetail; return <>
          <DetailField label="Type" value={v.visitType}/><DetailField label="Status" value={v.status}/><DetailField label="Related rental" value={v.visitType === 'RESERVATION' ? 'Reservation visit' : <EmployeeEntityLabel kind="contract" id={v.entityId} />}/><DetailField label="Scheduled date" value={date(v.visitDate)}/><DetailField label="Actual return date" value={date(v.actualReturnDate)}/><DetailField label="Handling staff" value={<EmployeeEntityLabel kind="employee" id={v.employeeId} />}/>
        </> })()}
        {tab === 'invoices' && (() => { const i = item as InvoiceDetail; return <>
          <DetailField label="Type" value={i.invoiceType}/><DetailField label="Status" value={i.status}/><DetailField label="Related rental" value={i.invoiceType === 'DEPOSIT' ? 'Reservation deposit' : <EmployeeEntityLabel kind="contract" id={i.entityId} />}/><DetailField label="Billing month" value={i.billingMonth}/><DetailField label="Amount due" value={money(i.amountDue)}/><DetailField label="Discount" value={money(i.discountAmount)}/><DetailField label="Due date" value={date(i.dueDate)}/><DetailField label="Paid at (GMT+7)" value={i.paidAt ? formatUtcDateTime(i.paidAt) : undefined}/>
        </> })()}
        {tab === 'units' && (() => { const u = item as StorageUnit; return <>
          <DetailField label="Unit code" value={u.unitCode}/><DetailField label="Status" value={u.status}/><DetailField label="Location" value={u.locationInfo}/><DetailField label="Facility" value={facilityName}/><DetailField label="Unit type" value={unitTypeNames[u.unitTypeId] || 'Type name unavailable'}/>
        </> })()}
      </dl>
      {tab === 'contracts' && (refreshing ? <p className="mop-muted">Loading billing and return summaries…</p> : <>
        {billing && <section className="mop-detail-section"><h3>Billing summary</h3><p>{billing.invoices.length} online invoices · {billing.lateFees.length} late-fee records</p><div className="mop-mini-list">{billing.invoices.map((i) => <div key={i.invoiceId}><span>{i.invoiceType} {i.billingMonth ?? ''}</span><strong>{money(i.amountDue)}</strong>{status(i.status)}</div>)}</div><p className="mop-muted">First month is collected offline at handover; it has no Invoice.</p></section>}
        {returns && <section className="mop-detail-section"><h3>Return progress</h3><div className="mop-mini-list"><div><span>Visit</span><strong>{returns.returnVisit?.status ?? 'None'}</strong></div><div><span>Inspection</span><strong>{returns.inspection?.status ?? 'None'}</strong></div><div><span>Pending damage</span><strong>{returns.damages.filter(d => d.status === 'PENDING').length}</strong></div><div><span>Settlement</span><strong>{returns.settlement?.status ?? 'Not finalized'}</strong></div></div></section>}
      </>)}
  </>
}

export function FacilityOperationsPage() {
  const { user } = useAuth()
  const facilityId = user?.facilityId || null
  const [unitView, setUnitView] = useState<'list' | 'timeline'>('list')
  const [facilityName, setFacilityName] = useState('Name unavailable')
  const [unitTypeNames, setUnitTypeNames] = useState<Record<string, string>>({})
  const [tab, setTab] = useState<Tab>('reservations')
  const [page, setPage] = useState(1)
  const [query, setQuery] = useState('')
  const [report, setReport] = useState<FacilityOperationsReport | null>(null)
  const [reportError, setReportError] = useState('')
  const [records, setRecords] = useState<RecordItem[]>([])
  const [totalPages, setTotalPages] = useState(0)
  const [totalItems, setTotalItems] = useState(0)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const [reportLoading, setReportLoading] = useState(true)
  const [selected, setSelected] = useState<DetailState | null>(null)
  const [detailError, setDetailError] = useState('')
  const [detailLoading, setDetailLoading] = useState(false)
  const [revision, setRevision] = useState(0)
  const scopeKey = `${user?.userAccountId}:${user?.role}:${facilityId}`
  const [detailTarget, setDetailTarget] = useState<{ tab: Tab; record: RecordItem; scope: string } | null>(null)
  const detailRequest = useRef(0)
  const closeDetail = useCallback(() => {
    detailRequest.current += 1
    setDetailTarget(null)
    setSelected(null)
    setDetailLoading(false)
    setDetailError('')
  }, [])
  useEffect(() => () => { detailRequest.current += 1 }, [scopeKey])


  useEffect(() => {
    let active = true
    setFacilityName('Name unavailable'); setUnitTypeNames({})
    if (!facilityId) return
    catalogApi.getFacility(facilityId).then(response => { if (active) setFacilityName(response.data.name) }).catch(() => {})
    return () => { active = false }
  }, [facilityId, revision])

  useEffect(() => {
    let active = true
    const units = records.filter((item): item is StorageUnit => 'storageUnitId' in item && 'unitTypeId' in item && 'unitCode' in item)
    if (tab !== 'units') return
    void Promise.allSettled([...new Set(units.map(unit => unit.unitTypeId))].map(async id => {
      const response = await catalogApi.getUnitType(id)
      if (active) setUnitTypeNames(names => ({ ...names, [id]: response.data.name }))
    }))
    return () => { active = false }
  }, [records, tab])

  useEffect(() => {
    let alive = true
    if (!facilityId) { setReportLoading(false); setReportError('No Facility is assigned to this Manager account.'); return }
    setReportLoading(true)
    setReportError('')
    facilityReportApi.operations(facilityId).then(r => { if (alive) setReport(r.data) }).catch(e => { if (alive) { setReport(null); setReportError(errorText(e)) } }).finally(() => { if (alive) setReportLoading(false) })
    return () => { alive = false }
  }, [facilityId, revision])

  useEffect(() => {
    let alive = true
    setRecords([])
    setError('')
    if (!facilityId) { setLoading(false); setError('Assigned Facility is missing. Contact an administrator.'); return }
    setLoading(true)
    let work: Promise<ApiCollectionResponse<unknown>>
    switch (tab) {
      case 'reservations': work = reservationApi.list(page, PAGE_SIZE); break
      case 'contracts': work = contractApi.list(page, PAGE_SIZE); break
      case 'visits': work = visitApi.list(page, PAGE_SIZE); break
      case 'invoices': work = billingApi.listInvoices(page, PAGE_SIZE); break
      case 'units': work = storageUnitApi.listByFacility(facilityId, page, PAGE_SIZE)
    }
    work.then(r => {
      if (!alive) return
      // All Manager list endpoints must already be facility-scoped on the server.
      // This additional check only prevents inconsistent data from appearing in this workspace.
      const items = (r.data as RecordItem[]).filter(i => {
        if (tab === 'units') return (i as StorageUnit).facilityId === facilityId
        if (tab === 'reservations') return (i as ReservationDetail).facilityId === facilityId
        if (tab === 'contracts') return (i as HandoverContractDetail).facilityId === facilityId
        return true // Visits and invoices do not expose a FacilityId in their canonical DTOs.
      })
      setRecords(items)
      setTotalPages(r.pagination.totalPages)
      setTotalItems(r.pagination.totalItems)
    }).catch(e => { if (alive) { setError(pageError(e)); setTotalPages(0); setTotalItems(0) } }).finally(() => { if (alive) setLoading(false) })
    return () => { alive = false }
  }, [facilityId, tab, page, revision])

  const visible = useMemo(() => records.filter(r => itemSearch(tab, r).includes(query.trim().toLowerCase())), [records, tab, query])
  const chooseTab = (next: Tab) => {
    if (next === tab) return
    // Clear the previous record type before rendering the newly selected tab.
    setRecords([])
    setLoading(true)
    setError('')
    setTotalPages(0)
    setTotalItems(0)
    setTab(next)
    setPage(1)
    setQuery('')
    closeDetail()
  }
  const refresh = () => { closeDetail(); setRevision(v => v + 1) }
  const openRecord = useCallback(async (record: RecordItem, currentTab: Tab) => {
    const id = itemId(currentTab, record)
    const requestId = ++detailRequest.current
    setDetailTarget({ tab: currentTab, record, scope: scopeKey })
    setSelected(null)
    setDetailLoading(true)
    setDetailError('')
    try {
      let item: RecordItem
      switch (currentTab) {
        case 'reservations': item = (await reservationApi.get(id)).data; break
        case 'contracts': item = (await contractApi.get(id)).data; break
        case 'visits': item = (await visitApi.get(id)).data; break
        case 'invoices': item = (await billingApi.getInvoice(id)).data; break
        case 'units': item = (await storageUnitApi.get(id)).data; break
      }
      if (requestId !== detailRequest.current) return
      if (!item) { setDetailError('Record details are not available. Refresh to retry.'); return }
      setSelected({ tab: currentTab, item })
      if (currentTab === 'contracts') {
        const results = await Promise.allSettled([contractApi.getBilling(id), contractApi.getReturnSummary(id)])
        if (requestId !== detailRequest.current) return
        setSelected(current => current && itemId(current.tab, current.item) === id ? {
          ...current,
          billing: results[0].status === 'fulfilled' ? results[0].value.data : undefined,
          returns: results[1].status === 'fulfilled' ? results[1].value.data : undefined,
        } : current)
        const failures = results.filter(r => r.status === 'rejected')
        if (failures.length) setDetailError('One or more contract summaries could not be loaded. Refresh the detail to retry.')
      }
    } catch (e) { if (requestId === detailRequest.current) { setDetailError(errorText(e)); setSelected(null) } }
    finally { if (requestId === detailRequest.current) setDetailLoading(false) }
  }, [scopeKey])
  const occupied = report ? report.availableUnits + report.inUseUnits + report.inspectionUnits + report.maintenanceUnits : 0

  return <main className="mop-page" data-testid="manager-operations-page">
    <header className="mop-page-header"><div><h1>Facility Operations Monitoring</h1><p>Operational records, rental activity and financial status for your assigned Facility.</p><span className="mop-facility-id">{facilityId ? facilityName : 'Not assigned'}</span></div><button className="mop-btn mop-btn-secondary" type="button" onClick={refresh}><RefreshCw size={16}/> Refresh data</button></header>
    <section className="mop-section" aria-label="Facility operations snapshot"><div className="mop-section-heading"><div><h2><Activity size={19}/> Operational snapshot</h2><p>Current facility totals. Usage rate compares occupied units with all physical units.</p></div>{report && <span className="mop-small">As of {new Date(report.asOf).toLocaleString('en-GB', { timeZone: 'Asia/Ho_Chi_Minh' })} ICT</span>}</div>
      {reportLoading ? <p className="mop-empty">Loading facility totals…</p> : reportError ? <div role="alert" className="mop-alert">{reportError}</div> : report && <><div className="mop-stats"><div><span>Available units</span><strong>{report.availableUnits}</strong><small>Ready for allocation</small></div><div><span>Rented / in use</span><strong>{report.inUseUnits}</strong><small>Physically occupied</small></div><div><span>Under inspection</span><strong>{report.inspectionUnits}</strong><small>Awaiting release</small></div><div><span>Maintenance</span><strong>{report.maintenanceUnits}</strong><small>Temporarily unavailable</small></div><div><span>Overdue contracts</span><strong>{report.overdueContractCount}</strong><small>Server-determined cases</small></div><div><span>Usage rate</span><strong>{(report.usageRate * 100).toFixed(1)}%</strong><small>{report.inUseUnits} / {occupied} total units</small></div></div><div className="mop-usage-track" aria-label={`Usage rate ${(report.usageRate * 100).toFixed(1)}%`}><div style={{width: `${Math.max(0, Math.min(100, report.usageRate * 100))}%`}} /></div></>}
    </section>
    <section className="mop-section"><div className="mop-section-heading"><div><h2>Facility activity</h2><p>Review reservation status, customer/contract references, visit progress and persisted invoice payments.</p></div></div>
      <div className="mop-tabs" role="tablist" aria-label="Operations record type">{tabs.map(t => { const Icon = t.icon; return <button key={t.key} type="button" role="tab" aria-selected={tab === t.key} className={`mop-tab ${tab === t.key ? 'active' : ''}`} onClick={() => chooseTab(t.key)}><Icon size={15}/>{t.label}</button> })}</div>
      {tab === 'units' && <div className="mop-table-toolbar"><strong>Storage units</strong><div><button type="button" className="mop-btn mop-btn-secondary" aria-pressed={unitView === 'list'} onClick={() => setUnitView('list')}>List</button> <button type="button" className="mop-btn mop-btn-secondary" aria-pressed={unitView === 'timeline'} onClick={() => { setUnitView('timeline'); closeDetail() }}>Timeline</button></div></div>}
      {tab === 'units' && unitView === 'timeline' ? (facilityId ? <StorageUnitTimeline key={facilityId} facilityId={facilityId} revision={revision} /> : <p className="mop-alert">No facility is assigned.</p>) : <>
      <div className="mop-table-toolbar"><label className="mop-search"><Search size={16}/><input value={query} onChange={e => setQuery(e.target.value)} placeholder={`Filter ${tab} on this page`} aria-label={`Filter ${tab} on current page`}/></label><span className="mop-small">{totalItems} records · Page {page} {totalPages > 0 ? `of ${totalPages}` : ''}</span></div>
      {loading ? <div className="mop-empty">Loading {tab}…</div> : error ? <div className="mop-alert" role="alert">{error}<button type="button" onClick={refresh}>Retry</button></div> : visible.length === 0 ? <div className="mop-empty">{records.length ? 'No records match the current-page filter.' : `No ${tab} found in this Facility.`}</div> : <div className="mop-table" role="tabpanel">{visible.map(i => <ListRow unitTypeNames={unitTypeNames} key={itemId(tab, i)} tab={tab} item={i} onSelect={() => void openRecord(i, tab)}/>)}</div>}
      <div className="mop-pagination"><button type="button" className="mop-btn mop-btn-secondary" disabled={loading || page <= 1} onClick={() => setPage(p => p - 1)}>Previous</button><span>Page {page} / {Math.max(1, totalPages)}</span><button type="button" className="mop-btn mop-btn-secondary" disabled={loading || page >= totalPages} onClick={() => setPage(p => p + 1)}>Next</button></div>
      </>}
    </section>
    <p className="mop-footnote">Revenue reporting is currently unavailable. Use Reports to review available facility information.</p>
    {detailTarget?.scope === scopeKey && <EmployeeModal label={`${detailTarget.tab} detail`} onClose={closeDetail}>
      <div className="mop-drawer-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) closeDetail() }}>
        <aside className="mop-drawer">
          <div className="mop-drawer-head"><h2>{detailTarget.tab === 'units' ? 'Storage unit' : detailTarget.tab.slice(0, -1)} detail</h2><button type="button" className="mop-icon-button" onClick={closeDetail} aria-label="Close details"><X size={20} aria-hidden="true" /></button></div>
          {detailLoading && !selected && <p className="mop-alert" role="status">Loading record detail...</p>}
          {detailError && <div className="mop-alert" role="alert">{detailError}<button type="button" disabled={detailLoading} onClick={() => void openRecord(detailTarget.record, detailTarget.tab)}>Retry detail</button></div>}
          {selected && <RecordDetail facilityName={facilityName} unitTypeNames={unitTypeNames} detail={selected} refreshing={detailLoading} />}
        </aside>
      </div>
    </EmployeeModal>}
  </main>
}
