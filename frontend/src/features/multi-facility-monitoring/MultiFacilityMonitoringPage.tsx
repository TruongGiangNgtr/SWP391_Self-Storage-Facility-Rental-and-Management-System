import { EmployeeModal } from '../../components/EmployeeModal'
import { StatusBadge } from '../../components/StatusBadge'
import { formatUtcDateTime } from '../../utils/formatters'
import { employeeContractLabel, employeeMonthRange, employeeUnitLabel, fetchAllEmployeePages } from '../../utils/employeePresentation'
import type { ReactNode } from 'react'
import { EmployeeEntityLabel, EmployeeReservationLabel, useEmployeeLabelLookup, type EmployeeLabelKind } from '../../components/EmployeeEntityLabel'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import {
  Activity, ArrowRight, Building2, CalendarDays, CreditCard, FileText,
  RefreshCw, Search, ShieldCheck, TrendingUp, X,
} from 'lucide-react'
import { Link } from 'react-router-dom'
import { businessApi } from '../../api/businessApi'
import { businessMonitoringApi, type BusinessContractSummary, type BusinessOverview } from '../../api/businessMonitoringApi'
import { reservationApi } from '../../api/reservationApi'
import { visitApi } from '../../api/visitApi'
import { billingApi } from '../../api/billingApi'
import { contractApi } from '../../api/contractApi'
import { presentApiError } from '../../api/apiErrorPresentation'
import { ApiRequestError } from '../../api/httpClient'
import type { ApiCollectionResponse } from '../../api/api.types'
import type { Facility } from '../../models/facility'
import type { ReservationDetail } from '../reservations/reservation.types'
import type { VisitDetail } from '../visits/visit.types'
import type { InvoiceDetail } from '../billing/billing.types'
import type { HandoverContractDetail } from '../handover/contract.types'
import './multiFacilityMonitoring.css'

type ActivityTab = 'reservations' | 'contracts' | 'visits' | 'invoices'
type ActivityRecord = ReservationDetail | BusinessContractSummary | VisitDetail | InvoiceDetail
type DetailRecord = ReservationDetail | HandoverContractDetail | VisitDetail | InvoiceDetail
interface DetailState { tab: ActivityTab; record: DetailRecord }

const PAGE_SIZE = 20
const tabs = [
  { id: 'reservations', label: 'Reservations', icon: CalendarDays },
  { id: 'contracts', label: 'Contracts', icon: FileText },
  { id: 'visits', label: 'Visits', icon: Activity },
  { id: 'invoices', label: 'Invoices', icon: CreditCard },
] as const

const number = (value: number) => new Intl.NumberFormat('en-US').format(value)
const vnd = (value: number) => new Intl.NumberFormat('vi-VN', {
  style: 'currency', currency: 'VND', maximumFractionDigits: 0,
}).format(value)
const textDate = (value: string | null | undefined) => value?.slice(0, 10) ?? '—'
const readableStatus = (value: string | null | undefined) => value === 'TERMINATED' ? 'Closed' : typeof value === 'string' ? value.replaceAll('_', ' ') : 'UNKNOWN'
const scopedTab = (tab: ActivityTab) => tab === 'reservations' || tab === 'contracts'

function readableError(error: unknown): string {
  const presentation = presentApiError(error)
  return `${presentation.message}${presentation.traceId ? ` · Trace ID: ${presentation.traceId}` : ''}`
}

function activityId(tab: ActivityTab, item: ActivityRecord): string {
  switch (tab) {
    case 'reservations': return (item as ReservationDetail).reservationId
    case 'contracts': return (item as BusinessContractSummary).contractId
    case 'visits': return (item as VisitDetail).visitId
    case 'invoices': return (item as InvoiceDetail).invoiceId
  }
}

function activityTitle(tab: ActivityTab, item: ActivityRecord): ReactNode {
  if (tab === 'contracts') return employeeContractLabel(item as BusinessContractSummary)
  if (tab === 'reservations') { const r = item as ReservationDetail; return <>Reservation · <EmployeeEntityLabel kind="unitType" id={r.unitTypeId} /></> }
  if (tab === 'visits') { const v = item as VisitDetail; return `${readableStatus(v.visitType)} visit · ${textDate(v.visitDate)}` }
  const i = item as InvoiceDetail
  return `${readableStatus(i.invoiceType)} invoice · ${i.billingMonth ?? textDate(i.dueDate)}`
}

function activityStatus(tab: ActivityTab, item: ActivityRecord): string {
  return tab === 'reservations' ? (item as ReservationDetail).status
    : tab === 'contracts' ? (item as BusinessContractSummary).status
      : tab === 'visits' ? (item as VisitDetail).status
        : (item as InvoiceDetail).status
}

function activityDescription(tab: ActivityTab, item: ActivityRecord, facilityName: (id: string) => string) {
  switch (tab) {
    case 'reservations': {
      const row = item as ReservationDetail
      return `${facilityName(row.facilityId)} · ${row.startMonth} → ${row.endMonth}`
    }
    case 'contracts': {
      const row = item as BusinessContractSummary
      return `${facilityName(row.facilityId)} · ${row.unitTypeName} · ${row.startMonth} → ${row.endMonth}`
    }
    case 'visits': {
      const row = item as VisitDetail
      return `${readableStatus(row.visitType)} · ${textDate(row.visitDate)}`
    }
    case 'invoices': {
      const row = item as InvoiceDetail
      return `${readableStatus(row.invoiceType)} · ${row.billingMonth ?? 'Deposit'} · ${vnd(row.amountDue)}`
    }
  }
}

function activitySearchText(
  tab: ActivityTab,
  item: ActivityRecord,
  facilityNames: ReadonlyMap<string, string>,
  lookupLabel: (kind: EmployeeLabelKind, id?: string | null, scope?: string) => string,
): string {
  switch (tab) {
    case 'reservations': {
      const r = item as ReservationDetail
      return [
        r.status,
        readableStatus(r.status),
        r.startMonth,
        r.endMonth,
        r.depositInvoice?.status ?? '',
        facilityNames.get(r.facilityId) ?? '',
        lookupLabel('customer', r.customerId),
        lookupLabel('unitType', r.unitTypeId),
      ].join(' ').toLowerCase()
    }
    case 'contracts': {
      const c = item as BusinessContractSummary
      return [
        c.status,
        readableStatus(c.status),
        c.startMonth,
        c.endMonth,
        c.unitCode ?? '',
        c.unitTypeName ?? '',
        employeeContractLabel(c),
        facilityNames.get(c.facilityId) ?? '',
        lookupLabel('customer', c.customerId),
      ].join(' ').toLowerCase()
    }
    case 'visits': {
      const v = item as VisitDetail
      return [
        v.visitType,
        readableStatus(v.visitType),
        v.status,
        readableStatus(v.status),
        v.visitDate ?? '',
        v.actualReturnDate ?? '',
        lookupLabel('contract', v.entityId),
        lookupLabel('contractUnit', v.entityId),
        lookupLabel('employee', v.employeeId),
      ].join(' ').toLowerCase()
    }
    case 'invoices': {
      const i = item as InvoiceDetail
      return [
        i.invoiceType,
        readableStatus(i.invoiceType),
        i.status,
        readableStatus(i.status),
        i.billingMonth ?? '',
        i.dueDate ?? '',
        String(i.amountDue),
        lookupLabel('contract', i.entityId),
        lookupLabel('contractUnit', i.entityId),
      ].join(' ').toLowerCase()
    }
  }
}

function statusBadge(status: string | null | undefined) {
  const safeStatus = typeof status === 'string' && status.trim() ? status : 'UNKNOWN'
  return <StatusBadge status={safeStatus} label={readableStatus(safeStatus)} />
}

function listRecords(tab: ActivityTab, page: number, pageSize: number): Promise<ApiCollectionResponse<ActivityRecord>> {
  switch (tab) {
    case 'reservations': return reservationApi.list(page, pageSize)
    case 'contracts': return businessMonitoringApi.contracts(page, pageSize)
    case 'visits': return visitApi.list(page, pageSize)
    case 'invoices': return billingApi.listInvoices(page, pageSize)
  }
}

async function loadDetail(tab: ActivityTab, item: ActivityRecord): Promise<DetailRecord> {
  const id = activityId(tab, item)
  switch (tab) {
    case 'reservations': return (await reservationApi.get(id)).data
    case 'contracts': return (await contractApi.get(id)).data
    case 'visits': return (await visitApi.get(id)).data
    case 'invoices': return (await billingApi.getInvoice(id)).data
  }
}

function detailFields(detail: DetailState, facilityNames: ReadonlyMap<string, string>): { label: string; value: ReactNode }[] {
  const { tab, record } = detail
  if (tab === 'reservations') {
    const r = record as ReservationDetail
    return [
      { label: 'Reservation', value: <EmployeeReservationLabel customerId={r.customerId} /> }, { label: 'Facility', value: facilityNames.get(r.facilityId) ?? <EmployeeEntityLabel kind="facility" id={r.facilityId} /> },
      { label: 'Customer', value: <EmployeeEntityLabel kind="customer" id={r.customerId} /> }, { label: 'Unit Type', value: <EmployeeEntityLabel kind="unitType" id={r.unitTypeId} /> },
      { label: 'Rental period', value: employeeMonthRange(r.startMonth, r.endMonth) },
      { label: 'Status', value: r.status }, { label: 'Deposit invoice status', value: r.depositInvoice?.status },
      { label: 'Locked monthly price', value: vnd(r.lockedRentalPrice) },
    ]
  }
  if (tab === 'contracts') {
    const r = record as HandoverContractDetail
    return [
      { label: 'Contract', value: employeeContractLabel(r) }, { label: 'Facility', value: facilityNames.get(r.facilityId) ?? <EmployeeEntityLabel kind="facility" id={r.facilityId} /> },
      { label: 'Customer', value: <EmployeeEntityLabel kind="customer" id={r.customerId} /> }, { label: 'Reservation', value: r.reservationId ? <EmployeeReservationLabel customerId={r.customerId} /> : '—' },
      { label: 'Storage Unit', value: employeeUnitLabel(r) }, { label: 'Status', value: readableStatus(r.status) },
      { label: 'Rental period', value: employeeMonthRange(r.startMonth, r.endMonth) },
      { label: 'Captured policy', value: <EmployeeEntityLabel kind="policy" id={r.policyId} /> },
      { label: 'Captured discount', value: <EmployeeEntityLabel kind="discount" id={r.discountId} scope={r.customerId} empty="No discount" /> },
    ]
  }
  if (tab === 'visits') {
    const r = record as VisitDetail
    return [
      { label: 'Visit', value: `${readableStatus(r.visitType)} · ${textDate(r.visitDate)}` }, { label: 'Related rental', value: r.visitType === 'RESERVATION' ? 'Reservation visit' : <EmployeeEntityLabel kind="contract" id={r.entityId} /> },
      { label: 'Visit type', value: r.visitType }, { label: 'Status', value: r.status },
      { label: 'Scheduled date', value: textDate(r.visitDate) },
      { label: 'Actual return', value: textDate(r.actualReturnDate) }, { label: 'Actual staff actor', value: <EmployeeEntityLabel kind="employee" id={r.employeeId} /> },
    ]
  }
  const r = record as InvoiceDetail
  return [
    { label: 'Invoice', value: `${readableStatus(r.invoiceType)} · ${r.billingMonth ?? textDate(r.dueDate)}` }, { label: 'Related rental', value: r.invoiceType === 'DEPOSIT' ? 'Reservation deposit' : <EmployeeEntityLabel kind="contract" id={r.entityId} /> },
    { label: 'Type', value: r.invoiceType }, { label: 'Billing month', value: r.billingMonth },
    { label: 'Status', value: r.status }, { label: 'Base amount', value: vnd(r.baseAmount) },
    { label: 'Discount', value: vnd(r.discountAmount) }, { label: 'Amount due', value: vnd(r.amountDue) },
    { label: 'Due date', value: textDate(r.dueDate) }, { label: 'Paid at (GMT+7)', value: r.paidAt ? formatUtcDateTime(r.paidAt) : undefined },
  ]
}

export function MultiFacilityMonitoringPage() {
  const lookupLabel = useEmployeeLabelLookup()
  const [revision, setRevision] = useState(0)
  const [facilities, setFacilities] = useState<Facility[]>([])
  const [facilityLoading, setFacilityLoading] = useState(true)
  const [facilityError, setFacilityError] = useState('')
  const [overview, setOverview] = useState<BusinessOverview | null>(null)
  const [overviewLoading, setOverviewLoading] = useState(true)
  const [overviewError, setOverviewError] = useState('')
  const [overviewUnavailable, setOverviewUnavailable] = useState(false)
  const [facilityId, setFacilityId] = useState('')
  const [facilitySearch, setFacilitySearch] = useState('')
  const [facilityStatus, setFacilityStatus] = useState<'ALL' | 'ACTIVE' | 'INACTIVE'>('ALL')
  const [tab, setTab] = useState<ActivityTab>('reservations')
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [rows, setRows] = useState<ActivityRecord[]>([])
  const [activityLoading, setActivityLoading] = useState(true)
  const [activityError, setActivityError] = useState('')
  const [detail, setDetail] = useState<DetailState | null>(null)
  const [detailLoading, setDetailLoading] = useState(false)
  const [detailError, setDetailError] = useState('')
  const [detailTarget, setDetailTarget] = useState<{ tab: ActivityTab; item: ActivityRecord } | null>(null)
  const detailRequest = useRef(0)
  const closeDetail = useCallback(() => { detailRequest.current += 1; setDetailTarget(null); setDetail(null); setDetailLoading(false); setDetailError('') }, [])
  useEffect(() => () => { detailRequest.current += 1 }, [])

  const isFacilityFiltered = Boolean(facilityId) && scopedTab(tab)

  useEffect(() => {
    let current = true
    setFacilityLoading(true)
    setFacilityError('')
    businessApi.listAllFacilities().then(result => {
      if (current) setFacilities(result)
    }).catch(error => {
      if (current) { setFacilities([]); setFacilityError(readableError(error)) }
    }).finally(() => { if (current) setFacilityLoading(false) })
    return () => { current = false }
  }, [revision])

  useEffect(() => {
    let current = true
    setOverview(null)
    setOverviewError('')
    setOverviewUnavailable(false)
    setOverviewLoading(true)
    businessMonitoringApi.overview().then(result => {
      if (current) setOverview(result.data)
    }).catch(error => {
      if (!current) return
      if (error instanceof ApiRequestError && (error.code === 'ENDPOINT_NOT_IMPLEMENTED' || error.status === 501)) {
        setOverviewUnavailable(true)
      } else {
        setOverviewError(readableError(error))
      }
    }).finally(() => { if (current) setOverviewLoading(false) })
    return () => { current = false }
  }, [revision])

  useEffect(() => {
    let current = true
    setActivityLoading(true)
    setActivityError('')
    setRows([])
    fetchAllEmployeePages((p, size) => listRecords(tab, p, size)).then(result => {
      if (!current) return
      const filteredByFacility = isFacilityFiltered
        ? result.items.filter(item => 'facilityId' in item && item.facilityId === facilityId)
        : result.items
      setRows(filteredByFacility)
    }).catch(error => {
      if (current) { setRows([]); setActivityError(readableError(error)) }
    }).finally(() => { if (current) setActivityLoading(false) })
    return () => { current = false }
  }, [tab, facilityId, revision, isFacilityFiltered])

  const facilityNames = useMemo(() => new Map(facilities.map(item => [item.facilityId, item.name])), [facilities])
  const facilityName = (id: string) => facilityNames.get(id) ?? 'Facility name unavailable'
  const filteredFacilities = useMemo(() => facilities.filter(item =>
    (facilityStatus === 'ALL' || item.status === facilityStatus) &&
    `${item.name} ${item.address} ${item.contactInfo ?? ''}`.toLowerCase().includes(facilitySearch.trim().toLowerCase()),
  ), [facilities, facilityStatus, facilitySearch])
  const currentFacility = facilities.find(item => item.facilityId === facilityId)
  const matchingRows = useMemo(() => {
    const needle = search.trim().toLowerCase()
    if (!needle) return rows
    return rows.filter(item => activitySearchText(tab, item, facilityNames, lookupLabel).includes(needle))
  }, [rows, search, tab, facilityNames, lookupLabel])
  const totalPages = Math.max(1, Math.ceil(matchingRows.length / PAGE_SIZE))
  const currentPage = Math.min(page, totalPages)
  const visibleRows = useMemo(
    () => matchingRows.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE),
    [matchingRows, currentPage],
  )
  const activeFacilities = facilities.filter(item => item.status === 'ACTIVE').length

  const chooseFacility = (id: string) => { closeDetail(); setFacilityId(id); setPage(1); setSearch('') }
  const chooseTab = (next: ActivityTab) => {
    if (next === tab) return
    // Clear records in the click batch, BEFORE React renders the new tab.
    // useEffect runs after render: without this reset, the new Visit/Invoice
    // renderer receives stale Reservation/Contract rows and may throw.
    setRows([])
    setActivityLoading(true)
    setActivityError('')
    setTab(next)
    setPage(1)
    setSearch(''); closeDetail()
    setDetailError('')
  }
  const refresh = () => { closeDetail(); setRevision(value => value + 1) }
  const openRecord = async (item: ActivityRecord, currentTab: ActivityTab = tab) => {
    const requestId = ++detailRequest.current
    setDetailTarget({ tab: currentTab, item })
    setDetailError('')
    setDetail(null); setDetailLoading(true)
    try {
      const record = await loadDetail(currentTab, item)
      if (requestId !== detailRequest.current) return
      if (!record) { setDetailError('Record details are not available. Refresh to retry.'); return }
      setDetail({ tab: currentTab, record })
    } catch (error) {
      if (requestId === detailRequest.current) { setDetailError(readableError(error)); setDetail(null) }
    } finally {
      if (requestId === detailRequest.current) setDetailLoading(false)
    }
  }

  return <div className="mfm-page" data-testid="bwp06-multi-facility-monitoring">
    <header className="mfm-page-header">
      <div>
        <p className="mfm-breadcrumb">Business Operations / Multi-Facility Monitoring</p>
        <h1>Multi-Facility Monitoring</h1>
        <p>View system-wide performance and inspect operational records across storage locations.</p>
      </div>
      <button type="button" className="mfm-button mfm-button--secondary" onClick={refresh}>
        <RefreshCw size={16} /> Refresh data
      </button>
    </header>

    <section className="mfm-section" aria-label="Business portfolio overview">
      <div className="mfm-section-heading">
        <div><h2><TrendingUp size={19} /> Business overview</h2>
          <p>Business figures are supplied by the server; rental revenue includes offline first-month recognition.</p></div>
        <span className="mfm-chip">SYSTEM-WIDE</span>
      </div>
      {overviewLoading ? <div className="mfm-message" role="status">Loading business overview…</div>
        : overview ? <div className="mfm-kpis">
          <div className="mfm-kpi"><span>Facilities</span><strong>{number(overview.facilityCount)}</strong><small>Portfolio total</small><Building2 size={19} /></div>
          <div className="mfm-kpi"><span>Active contracts</span><strong>{number(overview.activeContractCount)}</strong><small>Across all locations</small><FileText size={19} /></div>
          <div className="mfm-kpi"><span>Usage rate</span><strong>{(overview.usageRate * 100).toFixed(1)}%</strong><small>All physical units in denominator</small><Activity size={19} /></div>
          <div className="mfm-kpi"><span>Rental revenue</span><strong className="mfm-kpi-money">{vnd(overview.rentalRevenue)}</strong><small>Server-calculated rental revenue</small><TrendingUp size={19} /></div>
        </div>
          : <div className="mfm-notice" role="status">
            <ShieldCheck size={19} />
            <div><strong>{overviewUnavailable ? 'Business overview is awaiting backend implementation' : 'Business overview could not be loaded'}</strong>
              <p>{overviewUnavailable
                ? 'Business overview reporting is not available yet. Financial and usage figures are not estimated from other records.'
                : overviewError}</p></div>
          </div>}
    </section>

    <section className="mfm-section" aria-label="Facility portfolio">
      <div className="mfm-section-heading">
        <div><h2><Building2 size={19} /> Facility portfolio</h2><p>Search and select a Facility to monitor its reservations and contracts.</p></div>
        <Link to="/business/facilities" className="mfm-text-link">Manage Facilities <ArrowRight size={15} /></Link>
      </div>
      {facilityError && <div className="mfm-error" role="alert">{facilityError}</div>}
      {facilityLoading ? <div className="mfm-message" role="status">Loading Facility catalogue…</div> : !facilityError && <>
        <div className="mfm-portfolio-summary">
          <span><strong>{facilities.length}</strong> total Facilities</span>
          <span><strong>{activeFacilities}</strong> active</span>
          <span><strong>{facilities.length - activeFacilities}</strong> inactive</span>
          <span className="mfm-portfolio-hint">Facility directory counts, separate from report totals</span>
        </div>
        <div className="mfm-toolbar">
          <label className="mfm-search"><Search size={16}/><input type="search" value={facilitySearch}
            onChange={event => setFacilitySearch(event.target.value)} placeholder="Search Facility name, address or contact" aria-label="Search facilities" /></label>
          <label className="mfm-select-label">Status
            <select value={facilityStatus} onChange={event => setFacilityStatus(event.target.value as 'ALL' | 'ACTIVE' | 'INACTIVE')}>
              <option value="ALL">All statuses</option><option value="ACTIVE">Active</option><option value="INACTIVE">Inactive</option>
            </select>
          </label>
          <button type="button" className="mfm-button mfm-button--secondary" disabled={!facilityId} onClick={() => chooseFacility('')}>All Facilities</button>
        </div>
        {filteredFacilities.length === 0 ? <div className="mfm-message">No Facilities match the selected filters.</div> :
          <div className="mfm-facility-table" aria-label="Facility list">
            {filteredFacilities.map(facility => <button key={facility.facilityId} type="button"
              className={`mfm-facility-row ${facilityId === facility.facilityId ? 'mfm-facility-row--selected' : ''}`}
              aria-pressed={facilityId === facility.facilityId} onClick={() => chooseFacility(facility.facilityId)}>
              <span className="mfm-facility-icon"><Building2 size={17} /></span>
              <span className="mfm-facility-name"><strong>{facility.name}</strong><small>{facility.address}</small></span>
              <span className="mfm-facility-contact">{facility.contactInfo || 'No contact provided'}</span>
              {statusBadge(facility.status)}
              <span className="mfm-facility-action">{facilityId === facility.facilityId ? 'Selected' : 'Monitor'} <ArrowRight size={15} /></span>
            </button>)}
          </div>}
      </>}
    </section>

    <section className="mfm-section" aria-label="Operational activity" data-testid="bwp06-activity">
      <div className="mfm-section-heading"><div><h2><Activity size={19}/> Operational activity</h2>
        <p>Read-only system records, using the authorized Reservation, Contract, Visit and Invoice APIs.</p></div>
        <span className="mfm-chip">{currentFacility && scopedTab(tab) ? currentFacility.name : 'ALL FACILITIES'}</span></div>
      <div className="mfm-tabs" role="tablist" aria-label="Operational record type">
        {tabs.map(item => <button key={item.id} type="button" role="tab" aria-selected={tab === item.id}
          className={`mfm-tab ${tab === item.id ? 'mfm-tab--active' : ''}`} onClick={() => chooseTab(item.id)}>
          <item.icon size={16}/>{item.label}</button>)}
      </div>
      {facilityId && !scopedTab(tab) && <div className="mfm-context-note" role="status">
        Visits and Invoices are shown system-wide. Their list responses do not include FacilityId, so this view does not claim a Facility-level filter for these records.
      </div>}
      <div className="mfm-toolbar mfm-activity-toolbar">
        <label className="mfm-search"><Search size={16}/><input type="search" value={search}
          onChange={event => { setSearch(event.target.value); setPage(1) }}
          placeholder={`Search ${tab} by customer, unit, facility or status`} aria-label="Search activity records" /></label>
        <span className="mfm-subtle">{matchingRows.length} matching {tab}</span>
      </div>
      {activityLoading ? <div className="mfm-message" role="status">Loading authorized {tab}…</div> : activityError ? <div className="mfm-error" role="alert">
        {activityError} <button type="button" onClick={refresh}>Retry</button></div> : visibleRows.length === 0 ?
        <div className="mfm-message">No {tab} found for this view.</div> : <div className="mfm-activity-list">
          {visibleRows.map(item => <button type="button" className="mfm-activity-row" key={activityId(tab, item)} onClick={() => { void openRecord(item) }}>
            <span className="mfm-activity-copy"><strong>{activityTitle(tab, item)}</strong>
              <small>{activityDescription(tab, item, facilityName)}</small></span>
            {statusBadge(activityStatus(tab, item))}<ArrowRight size={16} aria-hidden="true" />
          </button>)}
        </div>}
      {!activityLoading && !activityError && totalPages > 1 && <div className="mfm-pagination">
        <span>Page {currentPage} of {totalPages}</span>
        <button type="button" className="mfm-button mfm-button--secondary" disabled={currentPage <= 1} onClick={() => setPage(Math.max(1, currentPage - 1))}>Previous</button>
        <button type="button" className="mfm-button mfm-button--secondary" disabled={currentPage >= totalPages} onClick={() => setPage(currentPage + 1)}>Next</button>
      </div>}
      {detailError && <div role="alert" className="mfm-error">Unable to open record: {detailError}</div>}
      {detailLoading && <div role="status" className="mfm-message">Loading record detail…</div>}
    </section>

    <p className="mfm-footnote">Read-only dashboard. Authoritative permissions, rental status, revenue and usage calculations remain on the server. Use the Reports page to review reporting and export availability.</p>

    {detailTarget && <EmployeeModal label={`${detailTarget.tab} record details`} onClose={closeDetail}><div className="mfm-drawer-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) closeDetail() }}>
      <aside className="mfm-drawer">
        <div className="mfm-drawer-heading"><div><span>System-wide read</span>
          <h2>{detailTarget.tab.slice(0, 1).toUpperCase() + detailTarget.tab.slice(1, -1)} details</h2></div>
          <button type="button" aria-label="Close detail" className="mfm-close" onClick={closeDetail}><X size={20}/></button></div>
        {detailLoading && !detail && <div role="status" className="mfm-message">Loading record detail…</div>}
        {detailError && <div role="alert" className="mfm-error">Unable to open record: {detailError} <button type="button" disabled={detailLoading} onClick={() => void openRecord(detailTarget.item, detailTarget.tab)}>Retry</button></div>}
        {detail && <dl className="mfm-details">{detailFields(detail, facilityNames).map(field => <div key={field.label}>
          <dt>{field.label}</dt><dd>{field.value ?? '—'}</dd>
        </div>)}</dl>}
        <p className="mfm-footnote">Detail fetched from the authorized server endpoint. All values are read-only.</p>
      </aside>
    </div></EmployeeModal>}
  </div>
}
