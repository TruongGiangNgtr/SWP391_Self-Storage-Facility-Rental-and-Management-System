import { useCallback, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
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
  { label: 'Available', status: 'AVAILABLE', key: 'availableUnits', color: 'var(--frms-success)' },
  { label: 'In use', status: 'IN_USE', key: 'inUseUnits', color: '#4964ee' },
  { label: 'Under inspection', status: 'INSPECTION', key: 'inspectionUnits', color: 'var(--frms-warning)' },
  { label: 'Maintenance', status: 'MAINTENANCE', key: 'maintenanceUnits', color: 'var(--frms-secondary)' },
] as const
const unknownColor = 'var(--frms-control-border)'
const percent = (value: number) => `${(value * 100).toFixed(1)}%`
const currentMonth = () => getCurrentBusinessDate().slice(0, 7)
const validMonth = (value: string) => /^\d{4}-(0[1-9]|1[0-2])$/.test(value) && value >= '0001-01' && value <= currentMonth()
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
  const [searchParams, setSearchParams] = useSearchParams()
  const tab = searchParams.get('tab') === 'revenue' ? 'revenue' : 'units'
  const [revision, setRevision] = useState(0)
  const requestedMonth = searchParams.get(tab === 'units' ? 'unitMonth' : 'revenueMonth') ?? ''
  const month = validMonth(requestedMonth) ? requestedMonth : currentMonth()
  const requestedPage = searchParams.get('page') ?? '1'
  const page = /^\d+$/.test(requestedPage) && Number.isSafeInteger(Number(requestedPage)) && Number(requestedPage) > 0 ? Number(requestedPage) : 1
  const updateQuery = (name: string, value: string) => setSearchParams(previous => {
    const next = new URLSearchParams(previous)
    next.set(name, value)
    return next
  })
  const onMonthChange = (value: string) => updateQuery(tab === 'units' ? 'unitMonth' : 'revenueMonth', value)
  const readFacility = useCallback(() => catalogApi.getFacility(facilityId), [facilityId])
  const facility = useReportRead(facilityId + ':' + revision, readFacility)
  return <>
    <nav className="fr-tabs" aria-label="Report sections">
      <button type="button" aria-current={tab === 'units' ? 'page' : undefined} onClick={() => updateQuery('tab', 'units')}><Warehouse size={18} /> Unit Report</button>
      <button type="button" aria-current={tab === 'revenue' ? 'page' : undefined} onClick={() => updateQuery('tab', 'revenue')}><CircleDollarSign size={18} /> Revenue Report</button>
    </nav>
    <section className="fr-facility" aria-label="Report scope"><div className="fr-facility-icon"><Warehouse size={24} /></div><div><strong>{facility.data?.name ?? 'Assigned Facility'}</strong><p>{facility.error ? 'Facility name is unavailable. Report access is checked separately.' : 'Reports are limited to your assigned facility.'}</p></div><button className="fr-refresh" type="button" onClick={() => setRevision(value => value + 1)}><RefreshCw size={16} /> Refresh reports</button></section>
    {tab === 'units'
      ? <UnitReport facilityId={facilityId} month={month} revision={revision} page={page} onPageChange={value => updateQuery('page', String(value))} onMonthChange={onMonthChange} />
      : <><ReportPeriod month={month} onChange={onMonthChange} history={false} /><RevenueReport facilityId={facilityId} month={month} revision={revision} /></>}
  </>
}

function ReportPeriod({ month, onChange, history }: { month: string; onChange: (month: string) => void; history: boolean }) {
  return <section className="fr-period" aria-label={history ? 'Daily history period' : 'Revenue period'}>
    <label htmlFor="fr-report-month">{history ? 'History month' : 'Revenue month'}<input id="fr-report-month" type="month" min="0001-01" max={currentMonth()} value={month} onChange={event => {
      if (validMonth(event.target.value)) onChange(event.target.value)
    }} /></label>
    <div><strong>{monthLabel(month)}</strong><p>{history ? 'Daily status history only. Current operations above are independent of this month.' : 'Rental revenue recognized in this month, calculated by the server.'} GMT+7 business calendar.</p></div>
  </section>
}

function UnitReport({ facilityId, month, revision, page, onPageChange, onMonthChange }: {
  facilityId: string; month: string; revision: number; page: number
  onPageChange: (page: number) => void; onMonthChange: (month: string) => void
}) {
  const [retry, setRetry] = useState(0)
  const readSnapshot = useCallback((signal: AbortSignal) => facilityReportApi.operations(facilityId, signal), [facilityId])
  const readUnits = useCallback((signal: AbortSignal) => facilityReportApi.units(facilityId, month, page, signal), [facilityId, month, page])
  const snapshot = useReportRead([facilityId, revision, retry].join(':'), readSnapshot)
  const units = useReportRead([facilityId, month, page, revision, retry].join(':'), readUnits)
  const onRetry = () => setRetry(value => value + 1)
  const report = snapshot.data
  const total = report ? statuses.reduce((sum, item) => sum + report[item.key], 0) : 0
  return <section aria-label="Unit Report">
    <section aria-labelledby="fr-current-heading">
    <div className="fr-current-heading"><h2 id="fr-current-heading">Current operations</h2><p>Current inventory, unit types and rental status.{report && <> As of {formatUtcDateTime(report.asOf)} ICT.</>}</p></div>
    {snapshot.loading ? <LoadingState label="Loading current operations…" /> : snapshot.error ? <ReportError error={snapshot.error} retry={onRetry} /> : report ? <>
      <div className="fr-kpis"><Metric label="Total units" value={total} hint="Physical storage inventory" /><Metric label="Available units" value={report.availableUnits} hint="Current operational status" tone="green" /><Metric label="In use" value={report.inUseUnits} hint="Currently rented" tone="blue" /><Metric label="Usage rate" value={percent(report.usageRate)} hint="Across all physical units" tone="blue" /></div>
      <div className="fr-kpis fr-kpis-secondary"><Metric label="Under inspection" value={report.inspectionUnits} hint="Inspection in progress" tone="amber" /><Metric label="Maintenance" value={report.maintenanceUnits} hint="Unavailable for lease" /><Metric label="Overdue contracts" value={report.overdueContractCount} hint="Active contracts with overdue rent" tone="red" /></div>
      {total === 0 && <EmptyState message="No storage units are recorded. Current usage rate is 0%." />}
    </> : <EmptyState message="No current operations data is available." />}
    {units.loading ? <LoadingState label="Loading unit report…" /> : units.error ? <ReportError error={units.error} retry={onRetry} /> : units.data ? <>
      <p className="fr-history-note">Current unit details as of {formatUtcDateTime(units.data.asOf)} ICT.</p>
      <UnitTypeUtilization report={units.data} />
      <section className="fr-card"><h3>Current unit inventory</h3><p>Current contract and customer details.</p>
        <div className="fr-table-wrap" role="region" aria-label="Current unit inventory table" tabIndex={0}><table className="fr-unit-table"><caption className="fr-sr-only">Current facility unit inventory</caption><thead><tr>{['Unit code', 'Type & size', 'Location', 'Status', 'Contract', 'Customer', 'End month'].map(column => <th scope="col" key={column}>{column}</th>)}</tr></thead><tbody>{units.data.inventory.length ? units.data.inventory.map(unit => <tr key={unit.storageUnitId}><th scope="row">{unit.unitCode}</th><td>{unit.unitTypeName}<small className="fr-cell-detail">{unit.unitTypeSize}</small></td><td>{unit.locationInfo ?? '—'}</td><td><StatusBadge status={unit.status} label={statuses.find(status => status.status === unit.status)?.label} /></td><td className="fr-identifier">{unit.contractId ?? '—'}</td><td>{unit.customerName ?? '—'}</td><td>{unit.endMonth ?? '—'}</td></tr>) : <tr><td colSpan={7}><EmptyState message={units.data.pagination.totalItems ? 'No units on this page. Use Previous Page to return.' : 'No storage units are recorded for this facility.'} /></td></tr>}</tbody></table></div>
        <div className="fr-pagination"><span>Total units: {units.data.pagination.totalItems}</span><PaginationControls pagination={units.data.pagination} onPageChange={onPageChange} /></div>
      </section>
    </> : <EmptyState message="No unit report data is available." />}
    </section>
    <section className="fr-history-section" aria-labelledby="fr-history-heading">
      <div className="fr-monthly-heading"><h2 id="fr-history-heading">Daily history</h2></div>
      <ReportPeriod month={month} onChange={onMonthChange} history />
      {units.loading ? <LoadingState label="Loading daily history…" /> : units.error ? <ReportError error={units.error} retry={onRetry} /> : units.data
        ? <DailyHistory key={month} report={units.data} /> : <EmptyState message="No daily history is available." />}
    </section>
  </section>
}

function DailyHistory({ report }: { report: FacilityUnitReport }) {
  const [selectedDate, setSelectedDate] = useState<string | null>(null)
  const selectedDay = report.dailyTrend.find(day => day.date === selectedDate)
  const incomplete = report.dailyTrend.some(day => day.unknownUnits > 0 || day.usageRate === null)
  const maxUnits = Math.max(1, ...report.dailyTrend.map(day => day.knownUnits + day.unknownUnits))
  const tickStep = Math.max(1, Math.ceil(maxUnits / 4))
  const chartMaximum = tickStep * 4
  return <section className="fr-card"><h3><BarChart3 size={18} /> Daily operational status · {monthLabel(report.month)}</h3><p>Number of units by day. Select a day for exact counts; missing history is shown as Unknown.</p>
    <div className="fr-legend">{statuses.map(item => <span key={item.key}><i className="fr-dot" style={{ background: item.color }} />{item.label}</span>)}<span><i className="fr-dot" style={{ background: unknownColor }} />Unknown</span></div>
    {!report.dailyTrend.length ? <EmptyState message="No daily history is available for this period." /> : <>
      <div className="fr-chart-unit" aria-hidden="true">Units</div>
      <div className="fr-chart-plot">
        <div className="fr-chart-axis" aria-hidden="true">{[4, 3, 2, 1, 0].map(tick => <span key={tick}>{tick * tickStep}</span>)}</div>
        <div className="fr-daily-chart" role="group" aria-label={'Daily status counts for ' + report.month}>{report.dailyTrend.map(day => {
          const description = `${day.date}: ${statuses.map(status => `${status.label}: ${day[status.key]}`).join(', ')}, Unknown: ${day.unknownUnits}, Usage: ${day.usageRate === null ? 'Unavailable' : percent(day.usageRate)}`
          return <button type="button" className="fr-daily-column" key={day.date} aria-label={description} aria-pressed={day.date === selectedDate} title={description} onClick={() => setSelectedDate(day.date)}>
            <span className="fr-daily-stack" aria-hidden="true">{statuses.map(status => <span key={status.key} style={{ height: (day[status.key] / chartMaximum * 100) + '%', background: status.color }} />)}<span style={{ height: (day.unknownUnits / chartMaximum * 100) + '%', background: unknownColor }} /></span><span aria-hidden="true">{Number(day.date.slice(-2))}</span>
          </button>
        })}</div>
      </div>
      <div className="fr-day-detail" aria-live="polite" aria-atomic="true">{selectedDay ? <><strong>{selectedDay.date}</strong><dl>{statuses.map(status => <div key={status.key}><dt>{status.label}</dt><dd>{selectedDay[status.key]}</dd></div>)}<div><dt>Unknown</dt><dd>{selectedDay.unknownUnits}</dd></div><div><dt>Usage rate</dt><dd>{selectedDay.usageRate === null ? 'Unavailable' : percent(selectedDay.usageRate)}</dd></div></dl></> : <p>Select a day above or open the daily data table below.</p>}</div>
      {incomplete && <p className="fr-history-note">History is incomplete. Unknown units are not counted as available; occupancy is unavailable for days with missing evidence.</p>}
      <details className="fr-daily-details"><summary>View daily data</summary><div className="fr-table-wrap" role="region" aria-label="Daily status data table" tabIndex={0}><table><caption className="fr-sr-only">Daily status evidence for {report.month}</caption><thead><tr>{['Date', ...statuses.map(item => item.label), 'Known', 'Unknown', 'Usage'].map(label => <th key={label} scope="col">{label}</th>)}</tr></thead><tbody>{report.dailyTrend.map(day => <tr key={day.date}><th scope="row">{day.date}</th>{statuses.map(item => <td key={item.key}>{day[item.key]}</td>)}<td>{day.knownUnits}</td><td>{day.unknownUnits}</td><td>{day.usageRate === null ? 'Unavailable' : percent(day.usageRate)}</td></tr>)}</tbody></table></div></details>
    </>}
    <div className="fr-chart-footer"><span>Average occupancy: {report.averageOccupancy === null ? 'Unavailable' : percent(report.averageOccupancy)}</span><span>Peak day: {report.peakDay ?? 'Unavailable'}</span></div><p className="fr-note">Average and peak use only days with complete status evidence.</p>
  </section>
}

function UnitTypeUtilization({ report }: { report: FacilityUnitReport }) {
  return <section className="fr-card"><h3>Current unit type utilization</h3><p>Occupied units divided by all physical units of each type.</p>{report.unitTypes.length ? <div className="fr-utilization-list">{report.unitTypes.map(type => <div className="fr-utilization" key={type.unitTypeId}><div><strong>{type.name}</strong><span>{type.size} · {type.inUseUnits}/{type.totalUnits} in use</span><strong>{percent(type.usageRate)}</strong></div><div className="fr-track" role="img" aria-label={type.name + ': ' + percent(type.usageRate) + ' in use'}><div style={{ width: Math.min(100, Math.max(0, type.usageRate * 100)) + '%', background: '#4964ee' }} /></div><p>{type.availableUnits} available · {type.inspectionUnits} inspection · {type.maintenanceUnits} maintenance</p></div>)}</div> : <EmptyState message="No unit types with physical inventory are recorded." />}<div className="fr-chart-footer"><span>Available, in-use, inspection and maintenance units are all included in the total.</span></div></section>
}

function RevenueReport({ facilityId, month, revision }: { facilityId: string; month: string; revision: number }) {
  const [retry, setRetry] = useState(0)
  const read = useCallback((signal: AbortSignal) => facilityReportApi.revenue(facilityId, month, signal), [facilityId, month])
  const result = useReportRead([facilityId, month, revision, retry].join(':'), read)
  const report = result.data
  const components = report ? [
    { label: 'First-month rent', value: report.firstMonthRecognizedRevenue, description: 'Rental price captured at reservation, recognized once per contract in its start month.' },
    { label: 'Subsequent paid rent', value: report.subsequentPaidRentalRevenue, description: 'Successfully paid rental fee invoices from the second month onward.' },
  ] : []
  return <section aria-label="Revenue Report" aria-busy={result.loading}>
    {result.loading ? <LoadingState label="Loading monthly revenue…" /> : result.error ? <ReportError error={result.error} retry={() => setRetry(value => value + 1)} /> : report ? <>
      <div className="fr-revenue-summary"><Metric label="Total rental revenue" value={formatMoney(report.rentalRevenue)} hint={monthLabel(month) + ' · GMT+7'} tone="blue" /></div>
      {report.rentalRevenue === 0 && <EmptyState message="No rental revenue is recorded for this month." />}
      <section className="fr-card"><h2>Revenue breakdown</h2><p>Recognized rental revenue · VND</p><div className="fr-table-wrap" role="region" aria-label="Revenue breakdown table" tabIndex={0}><table className="fr-revenue-table"><caption className="fr-sr-only">Rental revenue components for {month}</caption><thead><tr><th scope="col">Component</th><th scope="col">Amount</th></tr></thead><tbody>{components.map(item => <tr key={item.label}><th scope="row">{item.label}<small className="fr-cell-detail">{item.description}</small></th><td>{formatMoney(item.value)}</td></tr>)}</tbody></table></div><p className="fr-note">Rental revenue excludes deposits, late fees, extra fees and damage charges.</p></section>
    </> : <EmptyState message="No revenue report data is available." />}
  </section>
}

function ReportError({ error, retry }: { error: ApiErrorPresentation; retry: () => void }) {
  return <div className="fr-message fr-error"><ApiErrorAlert error={error} /><button className="fr-refresh" type="button" onClick={retry}>Try again</button></div>
}

function Metric({ label, value, hint, tone = '' }: { label: string; value: string | number; hint: string; tone?: string }) {
  return <div className={'fr-metric fr-tone-' + tone}><span>{label}</span><strong>{value}</strong><small>{hint}</small></div>
}
