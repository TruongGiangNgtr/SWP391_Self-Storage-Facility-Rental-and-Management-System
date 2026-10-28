import { useCallback, useEffect, useState } from 'react'
import { BarChart3, CircleDollarSign, RefreshCw, ShieldCheck, Warehouse } from 'lucide-react'
import { useAuth } from '../../auth/auth.context'
import { facilityReportApi, type FacilityUnitReport } from '../../api/facilityReportApi'
import type { ApiResponse } from '../../api/api.types'
import { catalogApi } from '../../api/catalogApi'
import { presentApiError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import { ApiErrorAlert } from '../../components/ApiErrorAlert'
import { EmptyState, LoadingState, PaginationControls } from '../../components/PageStates'
import { StatusBadge } from '../../components/StatusBadge'
import { formatMoney, formatUtcDateTime, getCurrentBusinessDate } from '../../utils/formatters'
import './facilityReports.css'

const statuses = [
  { label: 'Available', status: 'AVAILABLE', key: 'availableUnits', color: '#26a879' },
  { label: 'In use', status: 'IN_USE', key: 'inUseUnits', color: '#4964ee' },
  { label: 'Under inspection', status: 'INSPECTION', key: 'inspectionUnits', color: '#e9a022' },
  { label: 'Maintenance', status: 'MAINTENANCE', key: 'maintenanceUnits', color: '#8392aa' },
] as const
const percent = (value: number) => `${(value * 100).toFixed(1)}%`
const currentMonth = () => getCurrentBusinessDate().slice(0, 7)
const monthLabel = (month: string) => new Intl.DateTimeFormat('en-GB', {
  month: 'long', year: 'numeric', timeZone: 'Asia/Ho_Chi_Minh',
}).format(new Date(month + '-01T00:00:00+07:00'))

// A changed request key hides stale results before effect cleanup.
function useReportRead<T>(key: string, read: (signal: AbortSignal) => Promise<ApiResponse<T>>) {
  const [state, setState] = useState<{ key: string; data?: T; error?: ApiErrorPresentation }>({ key: '' })
  useEffect(() => {
    const controller = new AbortController()
    read(controller.signal).then(response => {
      if (!controller.signal.aborted) setState({ key, data: response.data })
    }).catch(reason => {
      if (!controller.signal.aborted) setState({ key, error: presentApiError(reason) })
    })
    return () => controller.abort()
  }, [key, read])
  return state.key === key ? { ...state, loading: false } : { loading: true, data: undefined, error: undefined }
}

export function FacilityReportsPage() {
  const { user } = useAuth()
  return <div className="fr-page">
    <header className="fr-header"><div><h1>Facility Reports</h1><p>Storage operations and rental revenue for your assigned facility.</p></div><span className="fr-scope-label"><ShieldCheck size={15} /> Facility-scoped reporting</span></header>
    {user?.facilityId ? <AssignedReports key={user.userAccountId + ':' + user.facilityId} facilityId={user.facilityId} /> : <div className="fr-message" role="alert">No facility is assigned to this account. Contact your administrator to access facility reports.</div>}
  </div>
}

function AssignedReports({ facilityId }: { facilityId: string }) {
  const [tab, setTab] = useState<'units' | 'revenue'>('units')
  const [revision, setRevision] = useState(0)
  const [periods, setPeriods] = useState(() => ({ units: currentMonth(), revenue: currentMonth() }))
  const period = periods[tab]
  const readFacility = useCallback(() => catalogApi.getFacility(facilityId), [facilityId])
  const facility = useReportRead(facilityId + ':' + revision, readFacility)
  return <>
    <nav className="fr-tabs" aria-label="Report sections">
      <button type="button" aria-current={tab === 'units' ? 'page' : undefined} onClick={() => setTab('units')}><Warehouse size={18} /> Unit Report</button>
      <button type="button" aria-current={tab === 'revenue' ? 'page' : undefined} onClick={() => setTab('revenue')}><CircleDollarSign size={18} /> Revenue Report</button>
    </nav>
    <section className="fr-facility" aria-label="Report scope"><div className="fr-facility-icon"><Warehouse size={24} /></div><div><strong>{facility.data?.name ?? 'Assigned Facility'}</strong><p>{facility.error ? 'Facility name is unavailable. Report access is checked separately.' : 'Reports are limited to your assigned facility.'}</p></div><button className="fr-refresh" type="button" onClick={() => setRevision(value => value + 1)}><RefreshCw size={16} /> Refresh reports</button></section>
    <section className="fr-period" aria-label="Reporting period">
      <label htmlFor="fr-report-month">Reporting period<input id="fr-report-month" type="month" min="0001-01" max={currentMonth()} value={period} onChange={event => {
        const value = event.target.value
        if (/^\d{4}-(0[1-9]|1[0-2])$/.test(value) && value >= '0001-01' && value <= currentMonth()) setPeriods(previous => ({ ...previous, [tab]: value }))
      }} /></label>
      <div><strong>{monthLabel(period)}</strong><p>GMT+7 business calendar. {tab === 'units' ? 'The selected month applies to daily history. Inventory and unit types show the current snapshot.' : 'Rental revenue recognized in the selected month, calculated by the server.'}</p></div>
    </section>
    {tab === 'units' ? <UnitReport key={period} facilityId={facilityId} month={period} revision={revision} /> : <RevenueReport facilityId={facilityId} month={period} revision={revision} />}
  </>
}

function UnitReport({ facilityId, month, revision }: { facilityId: string; month: string; revision: number }) {
  const [page, setPage] = useState(1)
  const [retry, setRetry] = useState(0)
  const readSnapshot = useCallback((signal: AbortSignal) => facilityReportApi.operations(facilityId, signal), [facilityId])
  const readUnits = useCallback((signal: AbortSignal) => facilityReportApi.units(facilityId, month, page, signal), [facilityId, month, page])
  const snapshot = useReportRead([facilityId, revision, retry].join(':'), readSnapshot)
  const units = useReportRead([facilityId, month, page, revision, retry].join(':'), readUnits)
  const onRetry = () => setRetry(value => value + 1)
  const report = snapshot.data
  const total = report ? statuses.reduce((sum, item) => sum + report[item.key], 0) : 0
  return <section aria-label="Unit Report">
    <div className="fr-current-heading"><h2>Current operational snapshot</h2><p>Live figures are independent of the selected month.{report && <> As of {formatUtcDateTime(report.asOf)} ICT.</>}</p></div>
    {snapshot.loading ? <LoadingState label="Loading current operations…" /> : snapshot.error ? <ReportError error={snapshot.error} retry={onRetry} /> : report ? <>
      <div className="fr-kpis"><Metric label="Total units" value={total} hint="Physical storage inventory" /><Metric label="Available units" value={report.availableUnits} hint="Current operational status" tone="green" /><Metric label="In use" value={report.inUseUnits} hint="Currently rented" tone="blue" /><Metric label="Usage rate" value={percent(report.usageRate)} hint="Across all physical units" tone="blue" /></div>
      <div className="fr-kpis fr-kpis-secondary"><Metric label="Under inspection" value={report.inspectionUnits} hint="Inspection in progress" tone="amber" /><Metric label="Maintenance" value={report.maintenanceUnits} hint="Unavailable for lease" /><Metric label="Overdue contracts" value={report.overdueContractCount} hint="Active contracts with overdue rent" tone="red" /></div>
      {total === 0 && <EmptyState message="No storage units are recorded. Current usage rate is 0%." />}
    </> : <EmptyState message="No current operations data is available." />}
    <div className="fr-monthly-heading"><h2>Daily history · {monthLabel(month)}</h2><span>GMT+7</span></div>
    {units.loading ? <LoadingState label="Loading unit report…" /> : units.error ? <ReportError error={units.error} retry={onRetry} /> : units.data ? <>
      <p className="fr-history-note">Inventory scope: {units.data.inventoryScope}. {units.data.historicalInventorySupported === false && 'Historical inventory is unavailable. Unit types, locations, contracts and customers below reflect the current snapshot.'} As of {formatUtcDateTime(units.data.asOf)} ICT.</p>
      <div className="fr-charts"><DailyHistory report={units.data} /><UnitTypeUtilization report={units.data} /></div>
      <section className="fr-card"><h2>Current unit inventory</h2><p>Current contract and customer details, independent of the selected history month.</p>
        <div className="fr-table-wrap"><table className="fr-unit-table"><caption className="fr-sr-only">Current facility unit inventory</caption><thead><tr>{['Unit code', 'Type & size', 'Location', 'Status', 'Contract', 'Customer', 'End month'].map(column => <th scope="col" key={column}>{column}</th>)}</tr></thead><tbody>{units.data.inventory.length ? units.data.inventory.map(unit => <tr key={unit.storageUnitId}><th scope="row">{unit.unitCode}</th><td>{unit.unitTypeName}<small className="fr-cell-detail">{unit.unitTypeSize}</small></td><td>{unit.locationInfo ?? '—'}</td><td><StatusBadge status={unit.status} label={statuses.find(status => status.status === unit.status)?.label} /></td><td className="fr-identifier">{unit.contractId ?? '—'}</td><td>{unit.customerName ?? '—'}</td><td>{unit.endMonth ?? '—'}</td></tr>) : <tr><td colSpan={7}><EmptyState message={units.data.pagination.totalItems ? 'No units on this page. Use Previous Page to return.' : 'No storage units are recorded for this facility.'} /></td></tr>}</tbody></table></div>
        <div className="fr-pagination"><span>Total units: {units.data.pagination.totalItems}</span><PaginationControls pagination={units.data.pagination} onPageChange={setPage} /></div>
      </section>
    </> : <EmptyState message="No unit report data is available." />}
  </section>
}

function DailyHistory({ report }: { report: FacilityUnitReport }) {
  const incomplete = report.dailyTrend.some(day => day.unknownUnits > 0 || day.usageRate === null)
  const maxUnits = Math.max(1, ...report.dailyTrend.map(day => day.knownUnits + day.unknownUnits))
  return <section className="fr-card"><h2><BarChart3 size={18} /> Daily operational status</h2><p>Known status counts by day; missing evidence is shown separately.</p>
    <div className="fr-legend">{statuses.map(item => <span key={item.key}><i className="fr-dot" style={{ background: item.color }} />{item.label}</span>)}<span><i className="fr-dot" style={{ background: '#cbd5e1' }} />Unknown</span></div>
    {!report.dailyTrend.length ? <EmptyState message="No daily history is available for this period." /> : <>
      <div className="fr-daily-chart" role="img" aria-label={'Daily unit status counts for ' + report.month + '. Exact counts and missing evidence are in the daily data table below.'}>{report.dailyTrend.map(day => <div className="fr-daily-column" key={day.date}><div className="fr-daily-stack" title={day.date + ': ' + (day.usageRate === null ? 'usage unknown' : percent(day.usageRate)) + ', ' + day.unknownUnits + ' unknown units'}>{statuses.map(status => <span key={status.key} style={{ height: (day[status.key] / maxUnits * 100) + '%', background: status.color }} />)}<span style={{ height: (day.unknownUnits / maxUnits * 100) + '%', background: '#cbd5e1' }} /></div><span>{Number(day.date.slice(-2))}</span></div>)}</div>
      {incomplete && <p className="fr-history-note">History is incomplete. Unknown units are not counted as available; occupancy is unavailable for days with missing evidence.</p>}
      <details className="fr-daily-details"><summary>View daily data</summary><div className="fr-table-wrap"><table><caption className="fr-sr-only">Daily status evidence for {report.month}</caption><thead><tr>{['Date', ...statuses.map(item => item.label), 'Known', 'Unknown', 'Usage'].map(label => <th key={label} scope="col">{label}</th>)}</tr></thead><tbody>{report.dailyTrend.map(day => <tr key={day.date}><th scope="row">{day.date}</th>{statuses.map(item => <td key={item.key}>{day[item.key]}</td>)}<td>{day.knownUnits}</td><td>{day.unknownUnits}</td><td>{day.usageRate === null ? 'Unavailable' : percent(day.usageRate)}</td></tr>)}</tbody></table></div></details>
    </>}
    <div className="fr-chart-footer"><span>Average occupancy: {report.averageOccupancy === null ? 'Unavailable' : percent(report.averageOccupancy)}</span><span>Peak day: {report.peakDay ?? 'Unavailable'}</span></div><p className="fr-note">Average and peak use only days with complete status evidence.</p>
  </section>
}

function UnitTypeUtilization({ report }: { report: FacilityUnitReport }) {
  return <section className="fr-card"><h2>Current unit type utilization</h2><p>Occupied units divided by all physical units of each type.</p>{report.unitTypes.length ? <div className="fr-utilization-list">{report.unitTypes.map(type => <div className="fr-utilization" key={type.unitTypeId}><div><strong>{type.name}</strong><span>{type.size} · {type.inUseUnits}/{type.totalUnits} in use</span><strong>{percent(type.usageRate)}</strong></div><div className="fr-track" role="img" aria-label={type.name + ': ' + percent(type.usageRate) + ' in use'}><div style={{ width: Math.min(100, Math.max(0, type.usageRate * 100)) + '%', background: '#4964ee' }} /></div><p>{type.availableUnits} available · {type.inspectionUnits} inspection · {type.maintenanceUnits} maintenance</p></div>)}</div> : <EmptyState message="No unit types with physical inventory are recorded." />}<div className="fr-chart-footer"><span>All four operational statuses enter the usage denominator.</span></div></section>
}

function RevenueReport({ facilityId, month, revision }: { facilityId: string; month: string; revision: number }) {
  const [retry, setRetry] = useState(0)
  const read = useCallback((signal: AbortSignal) => facilityReportApi.revenue(facilityId, month, signal), [facilityId, month])
  const result = useReportRead([facilityId, month, revision, retry].join(':'), read)
  const report = result.data
  const components = report ? [{ label: 'First-month rent', value: report.firstMonthRecognizedRevenue }, { label: 'Subsequent paid rent', value: report.subsequentPaidRentalRevenue }] : []
  const scale = Math.max(1, ...components.map(item => item.value))
  return <section aria-label="Revenue Report" aria-busy={result.loading}>
    {result.loading ? <LoadingState label="Loading monthly revenue…" /> : result.error ? <ReportError error={result.error} retry={() => setRetry(value => value + 1)} /> : report ? <>
      <div className="fr-kpis fr-kpis-secondary"><Metric label="Total rental revenue" value={formatMoney(report.rentalRevenue)} hint={monthLabel(month)} tone="blue" /><Metric label="First-month rent" value={formatMoney(report.firstMonthRecognizedRevenue)} hint="Recognized at contract start month" /><Metric label="Subsequent paid rent" value={formatMoney(report.subsequentPaidRentalRevenue)} hint="Paid later-month rental fee invoices" /></div>
      {report.rentalRevenue === 0 && <EmptyState message="No rental revenue is recorded for this month." />}
      <div className="fr-charts"><section className="fr-card"><h2>Revenue composition</h2><p>{monthLabel(month)} · VND</p><div className="fr-revenue-bars">{components.map(item => <div key={item.label}><div className="fr-revenue-line"><span>{item.label}</span><strong>{formatMoney(item.value)}</strong></div><div className="fr-track" aria-hidden="true"><div style={{ width: Math.max(0, item.value / scale * 100) + '%', background: '#4964ee' }} /></div></div>)}</div></section>
        <section className="fr-card"><h2>Revenue components</h2><div className="fr-revenue-line"><span>First-month rent</span><strong>{formatMoney(report.firstMonthRecognizedRevenue)}</strong></div><p>Rental price captured at reservation, recognized once per contract.</p><div className="fr-revenue-line"><span>Subsequent paid rent</span><strong>{formatMoney(report.subsequentPaidRentalRevenue)}</strong></div><p>Successfully paid rental fee invoices from the second month onward.</p><div className="fr-revenue-line fr-revenue-total"><span>Total rental revenue</span><strong>{formatMoney(report.rentalRevenue)}</strong></div></section></div>
      <section className="fr-card"><h2>Rental revenue summary</h2><p>{monthLabel(month)} · GMT+7</p><div className="fr-table-wrap"><table className="fr-revenue-table"><caption className="fr-sr-only">Rental revenue for {month}</caption><thead><tr><th scope="col">Component</th><th scope="col">Amount</th></tr></thead><tbody>{components.map(item => <tr key={item.label}><th scope="row">{item.label}</th><td>{formatMoney(item.value)}</td></tr>)}</tbody><tfoot><tr><th scope="row">Total rental revenue</th><td>{formatMoney(report.rentalRevenue)}</td></tr></tfoot></table></div><p className="fr-note">Rental revenue excludes deposits, late fees, extra fees and damage charges.</p></section>
    </> : <EmptyState message="No revenue report data is available." />}
  </section>
}

function ReportError({ error, retry }: { error: ApiErrorPresentation; retry: () => void }) {
  return <div className="fr-message fr-error"><ApiErrorAlert error={error} /><button className="fr-refresh" type="button" onClick={retry}>Try again</button></div>
}

function Metric({ label, value, hint, tone = '' }: { label: string; value: string | number; hint: string; tone?: string }) {
  return <div className={'fr-metric fr-tone-' + tone}><span>{label}</span><strong>{value}</strong><small>{hint}</small></div>
}
