import { useEffect, useState } from 'react'
import { Activity, BarChart3, Building2, Check, ChevronRight, Download, Info, Layers3, RefreshCw, Wallet } from 'lucide-react'
import { businessApi } from '../../api/businessApi'
import { presentApiError } from '../../api/apiErrorPresentation'
import type { Facility } from '../../models/facility'
import type { UnitType } from '../../models/unitType'
import './businessReports.css'

const tabs = [
  { id: 'facility', label: 'By Facility', icon: Building2 },
  { id: 'unitType', label: 'By Unit Type', icon: Layers3 },
  { id: 'revenue', label: 'Revenue', icon: Wallet },
  { id: 'status', label: 'Rental Status', icon: Activity },
] as const
type Tab = typeof tabs[number]['id']
type Filters = { facility: string; unitType: string; year: string; quarter: string }
const businessYear = () => new Intl.DateTimeFormat('en', { year: 'numeric', timeZone: 'Asia/Ho_Chi_Minh' }).format(new Date())
const defaults = (): Filters => ({ facility: '', unitType: '', year: businessYear(), quarter: '' })
const statuses = [
  { label: 'Available', color: '#10a88a' }, { label: 'In use', color: '#4b61ed' },
  { label: 'Inspection', color: '#e9a12a' }, { label: 'Maintenance', color: '#8793aa' },
]

export function BusinessReportsPage() {
  const [tab, setTab] = useState<Tab>('facility')
  const [draft, setDraft] = useState<Filters>(defaults)
  const [applied, setApplied] = useState<Filters>(defaults)
  const [facilities, setFacilities] = useState<Facility[]>([])
  const [unitTypes, setUnitTypes] = useState<UnitType[]>([])
  const [loading, setLoading] = useState(true)
  const [errors, setErrors] = useState<string[]>([])
  const [revision, setRevision] = useState(0)
  const [validation, setValidation] = useState('')

  useEffect(() => {
    let active = true
    async function load() {
      setLoading(true)
      setErrors([])
      const results = await Promise.allSettled([businessApi.listAllFacilities(), businessApi.listAllUnitTypes()])
      if (!active) return
      const failures: string[] = []
      results.forEach((result, index) => {
        if (result.status === 'rejected') {
          const error = presentApiError(result.reason)
          failures.push(`${index === 0 ? 'Facilities' : 'Unit types'}: ${error.message}${error.traceId ? ` (Trace: ${error.traceId})` : ''}`)
        }
      })
      if (results[0].status === 'fulfilled') setFacilities(results[0].value)
      else setFacilities([])
      if (results[1].status === 'fulfilled') setUnitTypes(results[1].value)
      else setUnitTypes([])
      setErrors(failures)
      setLoading(false)
    }
    void load()
    return () => { active = false }
  }, [revision])

  const snapshot = tab === 'status'
  const period = `${applied.quarter ? `Q${applied.quarter}` : 'Full year'} ${applied.year}`
  const facilityName = facilities.find(item => item.facilityId === applied.facility)?.name ?? 'All facilities'
  const typeName = unitTypes.find(item => item.unitTypeId === applied.unitType)?.name ?? 'All unit types'
  const revenue = tab === 'revenue'
  const metrics = snapshot
    ? [['Total units', 'All physical storage units'], ['In use', 'Currently occupied units'], ['Available', 'Current physical status'], ['Usage rate', 'In use / all physical units']]
    : revenue
      ? [['Rental revenue', 'Total recognized rental revenue'], ['First-month revenue', 'Recognized in contract start month'], ['Subsequent paid rent', 'Paid rental fees from month two onward']]
      : [[tab === 'facility' ? 'Facilities' : 'Unit types', 'Within the selected scope'], ['Rental revenue', period], ['Usage rate', 'Current physical-unit snapshot']]
  const columns = revenue
    ? ['Month', 'First-month revenue', 'Subsequent paid rent', 'Rental revenue']
    : [tab === 'unitType' ? 'Unit type' : 'Facility', ...(snapshot ? ['Unit type'] : []), 'Total units', 'Available', 'In use', 'Inspection', 'Maintenance', 'Usage rate', ...(!snapshot ? ['Rental revenue'] : [])]

  function reset() {
    const initial = defaults()
    setDraft(initial)
    setApplied(initial)
    setValidation('')
  }

  return <div className="br-page">
    <div className="br-breadcrumb">Business operations <ChevronRight size={13} /> Reports</div>
    <header className="br-header">
      <div><div className="br-title"><h1>Business Reports</h1><span>BWP-07</span></div><p>Explore rental revenue and storage usage across your facilities.</p></div>
      <div className="br-actions"><button type="button" onClick={() => setRevision(value => value + 1)} disabled={loading}><RefreshCw size={16} className={loading ? 'br-spin' : ''} /> {loading ? 'Loading filters…' : 'Refresh filters'}</button><button type="button" className="br-primary" disabled aria-describedby="br-export-note"><Download size={16} /> Export CSV</button></div>
    </header>

    <section className="br-controls" aria-label="Report controls">
      <nav className="br-tabs" aria-label="Report views">{tabs.map(({ id, label, icon: Icon }) => <button key={id} type="button" aria-current={tab === id ? 'page' : undefined} className={tab === id ? 'br-tab-active' : ''} onClick={() => setTab(id)}><Icon size={18} />{label}</button>)}</nav>
      <form onSubmit={event => { event.preventDefault(); if (!snapshot && !/^[1-9]\d{3}$/.test(draft.year)) { setValidation('Enter a four-digit reporting year.'); return } setValidation(''); setApplied({ ...draft }) }}>
        <div className="br-filter-grid">
          <label>Facility<select value={draft.facility} disabled={loading} onChange={event => setDraft({ ...draft, facility: event.target.value })}><option value="">All facilities</option>{facilities.map(item => <option key={item.facilityId} value={item.facilityId}>{item.name}{item.status === 'INACTIVE' ? ' (Inactive)' : ''}</option>)}</select></label>
          <label>Unit type<select value={draft.unitType} disabled={loading} onChange={event => setDraft({ ...draft, unitType: event.target.value })}><option value="">All unit types</option>{unitTypes.map(item => <option key={item.unitTypeId} value={item.unitTypeId}>{item.name}</option>)}</select></label>
          {!snapshot && <><label>Year<input type="number" min="1000" max="9999" step="1" required value={draft.year} onChange={event => setDraft({ ...draft, year: event.target.value })} /></label><label>Quarter<select value={draft.quarter} onChange={event => setDraft({ ...draft, quarter: event.target.value })}><option value="">Full year</option>{[1, 2, 3, 4].map(value => <option key={value} value={value}>Q{value}</option>)}</select></label></>}
        </div>
        <div className="br-filter-actions"><button className="br-primary" type="submit" disabled={loading}><Check size={15} /> Apply filters</button><button type="button" onClick={reset}>Reset</button><span>{snapshot ? 'Current snapshot · independent of reporting periods' : 'Reporting calendar: Asia/Ho_Chi_Minh (GMT+7)'}</span></div>
        {validation && <p className="br-error" role="alert">{validation}</p>}
      </form>
      <div className="br-applied" aria-live="polite"><span>Selected scope</span><strong>{facilityName}</strong><strong>{typeName}</strong><strong>{snapshot ? 'Current snapshot' : period}</strong></div>
    </section>

    {errors.length > 0 && <div className="br-error" role="alert">{errors.map(error => <p key={error}>{error}</p>)}<button type="button" disabled={loading} onClick={() => setRevision(value => value + 1)}>Retry loading filters</button></div>}
    {!loading && errors.length === 0 && (facilities.length === 0 || unitTypes.length === 0) && <p className="br-notice" role="status">{facilities.length === 0 ? 'No facilities are available in the catalogue. ' : ''}{unitTypes.length === 0 ? 'No unit types are available in the catalogue.' : ''}</p>}
    <div className="br-notice" id="br-export-note"><Info size={18} /><div><strong>Reporting data is coming soon</strong><p>You can explore the report views and select filters. Metrics, charts and CSV export will become available when reporting is connected.</p></div></div>

    <section className={`br-metrics ${snapshot ? 'br-four' : ''}`} aria-label="Report summary">{metrics.map(([label, hint]) => <article className="br-metric" key={label}><span>{label}</span><strong>—</strong><small>{hint}</small></article>)}</section>

    <section className={`br-charts ${snapshot || tab === 'unitType' || revenue ? 'br-chart-pair' : ''}`} aria-label="Report charts">
      <article className="br-card"><div className="br-card-heading"><h2>{snapshot ? 'Physical unit status distribution' : revenue ? 'Monthly rental revenue' : `Rental revenue by ${tab === 'facility' ? 'facility' : 'unit type'}`}</h2><span>{snapshot ? 'Current snapshot' : period}</span></div><p>{snapshot ? 'All four physical unit statuses within your selected scope.' : revenue ? `${applied.quarter ? 'Three' : 'Twelve'} monthly totals, split by revenue component.` : 'Compare recognized rental revenue within the selected reporting period.'}</p>{(snapshot || revenue) && <Legend revenue={revenue} />}<Pending title={snapshot ? 'Status distribution awaits data' : 'Revenue chart awaits data'} /></article>
      {(snapshot || tab === 'unitType' || revenue) && <article className="br-card"><div className="br-card-heading"><h2>{revenue ? 'Revenue components' : snapshot ? 'Unit status by facility' : 'Physical units by unit type'}</h2><span>{revenue ? period : 'Current snapshot'}</span></div>{revenue ? <><div className="br-component"><span>First-month revenue</span><strong>— VND</strong><p>Captured reservation price, recognized once in the contract start month.</p></div><div className="br-component"><span>Subsequent paid rent</span><strong>— VND</strong><p>Successfully paid rental fee invoices from the second month onward.</p></div><div className="br-component br-total"><span>Total rental revenue</span><strong>— VND</strong></div></> : <><p>Compare the current physical inventory across the selected scope.</p><Legend /><Pending title="Unit status comparison awaits data" /></>}</article>}
    </section>

    <section className="br-card br-detail"><div className="br-card-heading"><h2>{revenue ? 'Monthly revenue breakdown' : snapshot ? 'Current storage usage' : `${tab === 'facility' ? 'Facility' : 'Unit type'} performance`}</h2><span>{snapshot ? 'Snapshot time: —' : period}</span></div><p>{!snapshot && !revenue ? 'Revenue follows the selected period. Unit counts and usage rate reflect the current snapshot.' : revenue ? 'Rental revenue excludes deposits, late fees, extra fees and damage charges.' : 'Grouped by facility and unit type. Usage rate includes all physical units in its denominator.'}</p><div className="br-table-wrap"><table><caption className="br-sr-only">{tabs.find(item => item.id === tab)?.label} report awaiting data</caption><thead><tr>{columns.map(column => <th scope="col" key={column}>{column}</th>)}</tr></thead><tbody><tr><td colSpan={columns.length}><Pending title="Report details are not available yet" description="No reporting data has been loaded. Values will appear here once reporting is available." /></td></tr></tbody></table></div><div className="br-table-footer"><span>{revenue ? 'Monthly rows: —' : 'Total results: —'}</span>{!revenue && <div><button type="button" disabled>Previous</button><span>Page —</span><button type="button" disabled>Next</button></div>}</div></section>

    <details className="br-definitions"><summary><Info size={16} /> How these reports are calculated</summary><div><p><strong>Usage rate:</strong> In-use units divided by all physical units, including available, inspection and maintenance units. An empty inventory has a usage rate of 0%.</p><p><strong>Rental revenue:</strong> First-month rent recognized in the contract start month, plus successfully paid rental fee invoices from month two onward. Deposits and other fees are excluded.</p><p><strong>Time scope:</strong> Year and quarter filter rental revenue. Physical unit statuses always show the current snapshot. Available units are a physical status, not booking capacity.</p></div></details>
  </div>
}

function Legend({ revenue = false }: { revenue?: boolean }) {
  return <div className="br-legend">{(revenue ? [{ label: 'First-month revenue', color: '#4b61ed' }, { label: 'Subsequent paid rent', color: '#10a88a' }] : statuses).map(item => <span key={item.label}><i style={{ background: item.color }} />{item.label}</span>)}</div>
}

function Pending({ title, description = 'Figures will appear when reporting data is available.' }: { title: string; description?: string }) {
  return <div className="br-pending"><BarChart3 size={30} aria-hidden="true" /><strong>{title}</strong><p>{description}</p><span>Awaiting data</span></div>
}
