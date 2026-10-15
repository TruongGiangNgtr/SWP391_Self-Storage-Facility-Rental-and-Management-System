import { useEffect, useState } from 'react'
import { BarChart3, CircleDollarSign, RefreshCw, ShieldCheck, Warehouse } from 'lucide-react'
import { useAuth } from '../../auth/auth.context'
import { facilityReportApi, type FacilityOperationsReport } from '../../api/facilityReportApi'
import { catalogApi } from '../../api/catalogApi'
import { presentApiError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import './facilityReports.css'

const unitStatuses = [
  { label: 'Available', status: 'AVAILABLE', key: 'availableUnits', color: '#26a879' },
  { label: 'In use', status: 'IN_USE', key: 'inUseUnits', color: '#4964ee' },
  { label: 'Under inspection', status: 'INSPECTION', key: 'inspectionUnits', color: '#e9a022' },
  { label: 'Maintenance', status: 'MAINTENANCE', key: 'maintenanceUnits', color: '#8392aa' },
] as const
const percent = (value: number) => `${(value * 100).toFixed(1)}%`
const currentBusinessMonth = () => {
  const parts = new Intl.DateTimeFormat('en-GB', { timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit' }).formatToParts(new Date())
  return `${parts.find(part => part.type === 'year')?.value}-${parts.find(part => part.type === 'month')?.value}`
}

export function FacilityReportsPage() {
  const { user } = useAuth()
  const facilityId = user?.facilityId
  const [tab, setTab] = useState('units')
  const [revision, setRevision] = useState(0)
  const [snapshot, setSnapshot] = useState<FacilityOperationsReport | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<ApiErrorPresentation | null>(null)
  const [facility, setFacility] = useState<{ id: string; name: string } | null>(null)
  const [facilityError, setFacilityError] = useState<ApiErrorPresentation | null>(null)
  const [periods, setPeriods] = useState(() => ({ units: currentBusinessMonth(), revenue: currentBusinessMonth() }))
  const period = tab === 'units' ? periods.units : periods.revenue
  const periodLabel = period ? new Intl.DateTimeFormat('en-GB', { month: 'long', year: 'numeric', timeZone: 'Asia/Ho_Chi_Minh' }).format(new Date(`${period}-01T00:00:00+07:00`)) : 'Select a month'

  useEffect(() => {
    let active = true
    if (facilityId) {
      catalogApi.getFacility(facilityId).then(response => {
        if (active) { setFacility({ id: facilityId, name: response.data.name }); setFacilityError(null) }
      }).catch(reason => { if (active) { setFacility(null); setFacilityError(presentApiError(reason)) } })
    }
    return () => { active = false }
  }, [facilityId, revision])

  useEffect(() => {
    let active = true
    setSnapshot(null)
    setError(null)
    if (!facilityId) { setLoading(false); return }
    setLoading(true)
    facilityReportApi.operations(facilityId)
      .then(response => { if (active) setSnapshot(response.data) })
      .catch(reason => { if (active) setError(presentApiError(reason)) })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [facilityId, revision])

  // Never retain another facility's snapshot during a session scope change.
  const report = snapshot?.facilityId === facilityId ? snapshot : null
  const total = report ? unitStatuses.reduce((sum, item) => sum + report[item.key], 0) : 0

  return <div className="fr-page">
    <header className="fr-header"><div><p className="fr-eyebrow">FACILITY OPERATIONS / REPORTS</p><h1>Facility Reports</h1><p>Storage operations and rental revenue for your assigned facility.</p></div><span className="fr-scope-label"><ShieldCheck size={15} /> Facility-scoped reporting</span></header>
    <nav className="fr-tabs" aria-label="Report sections">
      <button type="button" aria-current={tab === 'units' ? 'page' : undefined} onClick={() => setTab('units')}><Warehouse size={18} /> Unit Report</button>
      <button type="button" aria-current={tab === 'revenue' ? 'page' : undefined} onClick={() => setTab('revenue')}><CircleDollarSign size={18} /> Revenue Report</button>
    </nav>
    <section className="fr-facility" aria-label="Report scope"><div className="fr-facility-icon"><Warehouse size={24} /></div><div><strong>{!facilityId ? 'No facility assigned' : facility?.id === facilityId ? facility.name : 'Assigned Facility'}</strong><p>{facilityError ? 'Facility name is unavailable.' : 'Reports are limited to your assigned facility.'}</p></div><div className="fr-updated">{tab === 'units' && report ? <>Current snapshot as of <time dateTime={report.asOf}>{new Date(report.asOf).toLocaleString('en-GB', { timeZone: 'Asia/Ho_Chi_Minh' })} ICT</time></> : tab === 'revenue' ? 'Rental revenue overview' : 'Current unit snapshot'}</div>{tab === 'units' && <button className="fr-refresh" type="button" disabled={loading || !facilityId} onClick={() => setRevision(value => value + 1)}><RefreshCw size={16} /> {loading ? 'Loading…' : 'Refresh current snapshot'}</button>}</section>
    <section className="fr-period" aria-label="Reporting period">
      <label htmlFor="fr-report-month">Reporting period<input id="fr-report-month" type="month" value={period} max={currentBusinessMonth()} disabled={!facilityId} onChange={event => { const value = event.target.value; if (value && value <= currentBusinessMonth()) setPeriods(previous => ({ ...previous, [tab]: value })) }} /></label>
      <div><strong>{periodLabel}</strong><p>Business calendar: GMT+7. Monthly reports will be available when this feature is ready.</p></div>
      <span className="fr-pending-badge">Period data coming soon</span>
    </section>
    {!facilityId ? <div className="fr-message" role="alert">No facility is assigned to this account. Contact your administrator to access facility reports.</div> : tab === 'units' ? <section aria-label="Unit Report" aria-busy={loading}>
      {loading ? <div className="fr-message" role="status">Loading facility report…</div> : error ? <div className="fr-message fr-error" role="alert"><strong>Unable to load the unit report</strong><p>{error.message}</p>{error.traceId && <small>Trace: {error.traceId}</small>}<button className="fr-refresh" type="button" onClick={() => setRevision(value => value + 1)}>Try again</button></div> : report ? <>
        <div className="fr-current-heading"><h2>Current operational snapshot</h2><p>Live figures below are independent of the selected reporting month.</p></div>
        <div className="fr-kpis">
          <Metric label="Total units" value={total} hint="Physical storage inventory" />
          <Metric label="Available units" value={report.availableUnits} hint="Ready for lease" tone="green" />
          <Metric label="In use" value={report.inUseUnits} hint="Currently rented" tone="blue" />
          <Metric label="Usage rate" value={percent(report.usageRate)} hint="Across all physical units" tone="blue" />
        </div>
        <div className="fr-kpis fr-kpis-secondary"><Metric label="Under inspection" value={report.inspectionUnits} hint="Inspection in progress" tone="amber" /><Metric label="Maintenance" value={report.maintenanceUnits} hint="Unavailable for lease" /><Metric label="Overdue contracts" value={report.overdueContractCount} hint="Active contracts with overdue rent" tone="red" /></div>
        {total === 0 && <p className="fr-message" role="status">No storage units are recorded for this facility. Usage rate is 0%.</p>}
      </> : <div className="fr-message" role="status">No report data is available.</div>}
      <div className="fr-monthly-heading"><h2>Monthly unit report</h2><span>{periodLabel} · GMT+7</span></div>
      <div className="fr-charts">
        <section className="fr-card"><h2><BarChart3 size={18} /> Occupancy &amp; operational status trend</h2><p>Daily unit status distribution for {periodLabel}.</p><div className="fr-legend">{unitStatuses.map(item => <span key={item.key}><i className="fr-dot" style={{ background: item.color }} />{item.label}</span>)}</div><PendingReport title="Daily history is not available yet" description="The daily trend will appear once historical unit reporting is ready." /><div className="fr-chart-footer"><span>Average occupancy: —</span><span>Peak day: —</span></div></section>
        <section className="fr-card"><h2>Unit type utilization</h2><p>Occupancy by storage type at the reporting snapshot.</p><PendingReport title="Unit type report is coming soon" description="Storage type names, sizes, occupied units and usage rates will appear here." /><div className="fr-chart-footer"><span>Includes all four operational statuses</span></div></section>
      </div>
      <section className="fr-card"><h2>Detailed unit inventory</h2><p>Unit details at the selected reporting snapshot · {periodLabel}.</p><div className="fr-table-wrap"><table className="fr-unit-table"><caption className="fr-sr-only">Unit inventory for the selected reporting period, awaiting data</caption><thead><tr>{['Unit code', 'Type & size', 'Location', 'Status', 'Contract', 'Customer', 'End month', 'Actions'].map(column => <th scope="col" key={column}>{column}</th>)}</tr></thead><tbody><tr><td colSpan={8}><PendingReport title="Monthly unit details are coming soon" description="The unit list will appear when reporting data is available. Historical records have not been loaded." /></td></tr></tbody></table></div><div className="fr-pagination"><span>Total units: —</span><div><button type="button" disabled>Previous</button><span>Page —</span><button type="button" disabled>Next</button></div></div></section>
    </section> : <section aria-label="Revenue Report">
      <div className="fr-kpis fr-kpis-secondary"><Metric label="Total rental revenue" value="— ₫" hint="Awaiting revenue data" tone="blue" /><Metric label="First-month rent" value="— ₫" hint="Recognized at contract start month" /><Metric label="Subsequent paid rent" value="— ₫" hint="Paid rental fee invoices" /></div>
      <div className="fr-charts"><section className="fr-card"><h2>Revenue composition</h2><p>First-month rent and subsequent paid rental fees.</p><div className="fr-revenue-empty" role="status"><CircleDollarSign size={38} /><strong>Revenue reporting is coming soon</strong><span>Revenue data is not available yet. Figures will appear when this feature is ready.</span></div></section><section className="fr-card"><h2>Revenue components</h2><div className="fr-revenue-line"><span>First-month rent</span><strong>— ₫</strong></div><p>Rental price captured at reservation, recognized once per contract.</p><div className="fr-revenue-line"><span>Subsequent paid rent</span><strong>— ₫</strong></div><p>Successfully paid rental fee invoices from the second month onward.</p><div className="fr-revenue-line fr-revenue-total"><span>Total rental revenue</span><strong>— ₫</strong></div></section></div>
      <section className="fr-card"><h2>Rental revenue summary</h2><p>{periodLabel} · Awaiting monthly revenue data.</p><div className="fr-table-wrap"><table><caption className="fr-sr-only">Revenue summary awaiting reporting data</caption><thead><tr><th scope="col">Component</th><th scope="col">Amount</th></tr></thead><tbody><tr><th scope="row">First-month rent</th><td>— ₫</td></tr><tr><th scope="row">Subsequent paid rental invoices</th><td>— ₫</td></tr></tbody><tfoot><tr><th scope="row">Total rental revenue</th><td>— ₫</td></tr></tfoot></table></div><p className="fr-note">Rental revenue excludes deposits, late fees, extra fees and damage charges. Figures for the selected month will appear when revenue reporting is ready.</p></section>
    </section>}
  </div>
}

function Metric({ label, value, hint, tone = '' }: { label: string; value: string | number; hint: string; tone?: string }) {
  return <div className={`fr-metric fr-tone-${tone}`}><span>{label}</span><strong>{value}</strong><small>{hint}</small></div>
}

function PendingReport({ title, description }: { title: string; description: string }) {
  return <div className="fr-pending-report" role="status"><BarChart3 size={32} aria-hidden="true" /><strong>{title}</strong><p>{description}</p><span>Awaiting reporting data</span></div>
}
