import { useEffect, useRef, useState } from 'react'
import { Activity, Building2, Check, ChevronRight, Download, Info, Layers3, RefreshCw, Wallet } from 'lucide-react'
import { businessApi } from '../../api/businessApi'
import { businessReportApi, type BusinessExportView, type BusinessReportFilters, type BusinessReportBreakdown, type BusinessReportMonthly, type BusinessReportGroup, type PhysicalInventory } from '../../api/businessReportApi'
import { presentApiError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import { ApiErrorAlert } from '../../components/ApiErrorAlert'
import { EmptyState, LoadingState, PaginationControls } from '../../components/PageStates'
import { formatMoney, formatUtcDateTime, getCurrentBusinessDate } from '../../utils/formatters'
import type { Facility } from '../../models/facility'
import type { UnitType } from '../../models/unitType'
import './businessReports.css'

const tabs = [
  { id: 'facility', label: 'By Facility', icon: Building2 },
  { id: 'unitType', label: 'By Unit Type', icon: Layers3 },
  { id: 'revenue', label: 'Revenue', icon: Wallet },
  { id: 'status', label: 'Rental Status', icon: Activity },
] as const
const defaults = (): BusinessReportFilters => ({ year: Number(getCurrentBusinessDate().slice(0, 4)) })
const statuses = [
  { label: 'Available', key: 'availableUnits', color: '#166534' },
  { label: 'In use', key: 'inUseUnits', color: '#2563eb' },
  { label: 'Inspection', key: 'inspectionUnits', color: '#92400e' },
  { label: 'Maintenance', key: 'maintenanceUnits', color: '#64748b' },
] as const
const percent = (value: number) => `${(value * 100).toFixed(1)}%`
const number = (value: number) => value.toLocaleString('en-GB')
const periodLabel = (filters: BusinessReportFilters) => `${filters.quarter ? `Q${filters.quarter}` : 'Full year'} ${filters.year}`
const groupLabel = (row: BusinessReportGroup, view: BusinessExportView) => view === 'unitType'
  ? `${row.unitTypeName ?? 'Unnamed unit type'}${row.unitTypeSize ? ` (${row.unitTypeSize})` : ''}`
  : `${row.facilityName ?? 'Unnamed facility'}${view === 'status' ? ` / ${row.unitTypeName ?? 'Unnamed unit type'}` : ''}`

export function BusinessReportsPage() {
  const [tab, setTab] = useState<BusinessExportView>('facility')
  const [draft, setDraft] = useState(() => ({ facilityId: '', unitTypeId: '', year: String(defaults().year), quarter: '' }))
  const [applied, setApplied] = useState<BusinessReportFilters>(defaults)
  const [facilities, setFacilities] = useState<Facility[]>([])
  const [unitTypes, setUnitTypes] = useState<UnitType[]>([])
  const [loading, setLoading] = useState(true)
  const [errors, setErrors] = useState<string[]>([])
  const [revision, setRevision] = useState(0)
  const [validation, setValidation] = useState('')
  const [filtersOpen, setFiltersOpen] = useState(() => window.matchMedia('(min-width: 761px)').matches)
  const pending = draft.facilityId !== (applied.facilityId ?? '') || draft.unitTypeId !== (applied.unitTypeId ?? '') ||
    draft.year !== String(applied.year) || draft.quarter !== String(applied.quarter ?? '')
  const scope = [
    applied.facilityId ? facilities.find(item => item.facilityId === applied.facilityId)?.name ?? 'Selected facility' : 'All facilities',
    applied.unitTypeId ? unitTypes.find(item => item.unitTypeId === applied.unitTypeId)?.name ?? 'Selected unit type' : 'All unit types',
    tab === 'status' ? 'Current inventory' : periodLabel(applied),
  ].join(' · ')
  const filterSummary = useRef<HTMLElement>(null)

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
      setFacilities(results[0].status === 'fulfilled' ? results[0].value : [])
      setUnitTypes(results[1].status === 'fulfilled' ? results[1].value : [])
      setErrors(failures)
      setLoading(false)
    }
    void load()
    return () => { active = false }
  }, [revision])

  function closeMobileFilters() {
    if (window.matchMedia('(max-width: 760px)').matches) {
      setFiltersOpen(false)
      filterSummary.current?.focus()
    }
  }

  function reset() {
    const initial = defaults()
    setDraft({ facilityId: '', unitTypeId: '', year: String(initial.year), quarter: '' })
    setApplied(initial)
    setValidation('')
    closeMobileFilters()
  }

  return <div className="br-page">
    <div className="br-breadcrumb">Business operations <ChevronRight size={13} /> Reports</div>
    <header className="br-header"><div><div className="br-title"><h1>Business Reports</h1></div><p>Explore rental revenue and storage usage across your facilities.</p></div><button type="button" aria-label="Refresh reports" onClick={() => setRevision(value => value + 1)} disabled={loading}><RefreshCw size={16} className={loading ? 'br-spin' : ''} /> Refresh</button></header>
    <section className="br-controls" aria-label="Report controls">
      <nav className="br-tabs" aria-label="Report views">{tabs.map(({ id, label, icon: Icon }) => <button key={id} type="button" aria-current={tab === id ? 'page' : undefined} className={tab === id ? 'br-tab-active' : ''} onClick={() => setTab(id)}><Icon size={18} />{label}</button>)}</nav>
      <details className="br-filter-disclosure" open={filtersOpen} onToggle={event => setFiltersOpen(event.currentTarget.open)}>
      <summary ref={filterSummary}><span>Filters</span><span className="br-scope" title={scope}>{scope}</span>{pending && <strong className="br-pending-label">Changes not applied</strong>}</summary>
      <form onSubmit={event => {
        event.preventDefault()
        const year = Number(draft.year)
        if (!/^\d{1,4}$/.test(draft.year) || !Number.isInteger(year) || year < 1 || year > 9998) {
          setValidation('Enter a reporting year from 1 to 9998.')
          return
        }
        setValidation('')
        setApplied({ year, quarter: draft.quarter ? Number(draft.quarter) : undefined, facilityId: draft.facilityId || undefined, unitTypeId: draft.unitTypeId || undefined })
        closeMobileFilters()
      }}>
        <div className="br-filter-grid">
          <label>Facility<select aria-label="Facility" value={draft.facilityId} disabled={loading} onChange={event => setDraft({ ...draft, facilityId: event.target.value })}><option value="">All facilities</option>{facilities.map(item => <option key={item.facilityId} value={item.facilityId}>{item.name}{item.status === 'INACTIVE' ? ' (Inactive)' : ''}</option>)}</select></label>
          <label>Unit type<select aria-label="Unit type" value={draft.unitTypeId} disabled={loading} onChange={event => setDraft({ ...draft, unitTypeId: event.target.value })}><option value="">All unit types</option>{unitTypes.map(item => <option key={item.unitTypeId} value={item.unitTypeId}>{item.name}</option>)}</select></label>
          <label>Year<input type="number" min="1" max="9998" step="1" required value={draft.year} onChange={event => setDraft({ ...draft, year: event.target.value })} /></label>
          <label>Quarter<select aria-label="Quarter" value={draft.quarter} onChange={event => setDraft({ ...draft, quarter: event.target.value })}><option value="">Full year</option>{[1, 2, 3, 4].map(value => <option key={value} value={value}>Q{value}</option>)}</select></label>
        <div className="br-filter-actions"><button className="br-primary" type="submit" disabled={loading}><Check size={15} /> Apply filters</button><button type="button" onClick={reset}>Reset</button></div></div>
        <p className="br-filter-note">Year and quarter filter revenue only; physical inventory is current.</p>
        {validation && <p className="br-error" role="alert">{validation}</p>}
      </form>
      </details>
    </section>
    {errors.length > 0 && <div className="br-error" role="alert">{errors.map(error => <p key={error}>{error}</p>)}<button type="button" disabled={loading} onClick={() => setRevision(value => value + 1)}>Retry loading filters</button></div>}
    {!loading && errors.length === 0 && (facilities.length === 0 || unitTypes.length === 0) && <p className="br-notice" role="status">{facilities.length === 0 ? 'No facilities are available in the catalogue. ' : ''}{unitTypes.length === 0 ? 'No unit types are available in the catalogue.' : ''}</p>}
    <ReportContent key={JSON.stringify([tab, applied, revision])} view={tab} filters={applied} />
    <details className="br-definitions"><summary><Info size={16} /> How these reports are calculated</summary><div><p><strong>CSV export:</strong> Includes all matching rows across all pages, using the applied filters. Year and quarter select revenue in CSV, including the Rental Status export.</p><p><strong>Usage rate:</strong> In-use units divided by all physical units, including available, inspection and maintenance units. An empty inventory has a usage rate of 0%.</p><p><strong>Rental revenue:</strong> First-month rent recognized in the contract start month, plus successfully paid rental fee invoices from month two onward. Deposits and other fees are excluded.</p><p><strong>Time scope:</strong> Year and quarter filter rental revenue in the GMT+7 business calendar. Physical unit statuses always show the current snapshot. Available units are a physical status, not booking capacity.</p></div></details>
  </div>
}

function ReportContent({ view, filters }: { view: BusinessExportView; filters: BusinessReportFilters }) {
  const [page, setPage] = useState(1)
  const [retry, setRetry] = useState(0)
  const key = `${page}:${retry}`
  const [state, setState] = useState<{ key: string; data?: BusinessReportBreakdown | BusinessReportMonthly; error?: ApiErrorPresentation }>({ key: '' })
  const [exporting, setExporting] = useState(false)
  const [exportError, setExportError] = useState<ApiErrorPresentation | null>(null)
  const [exported, setExported] = useState(false)
  const exportController = useRef<AbortController | null>(null)
  useEffect(() => () => exportController.current?.abort(), [])
  useEffect(() => {
    const controller = new AbortController()
    const read = view === 'revenue' ? businessReportApi.monthly(filters, controller.signal) : businessReportApi.analysis(view, filters, page, controller.signal)
    read.then(response => {
      if (!controller.signal.aborted) setState({ key, data: response.data })
    }).catch(reason => {
      if (!controller.signal.aborted) setState({ key, error: presentApiError(reason) })
    })
    return () => controller.abort()
  }, [view, filters, page, key])
  const loading = state.key !== key
  const data = loading ? undefined : state.data
  const error = loading ? undefined : state.error
  const grouped = data && 'rows' in data ? data : undefined
  const monthly = data && 'months' in data ? data : undefined
  const summary = grouped?.summary
  const revenue = view === 'revenue'
  const snapshot = view === 'status'
  const period = periodLabel(filters)

  async function download() {
    if (exportController.current) return
    const controller = new AbortController()
    exportController.current = controller
    setExporting(true)
    setExportError(null)
    setExported(false)
    try {
      const blob = await businessReportApi.export(view, filters, controller.signal)
      if (controller.signal.aborted) return
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = `frms-${view}-${filters.year}-${filters.quarter ? `Q${filters.quarter}` : 'full-year'}.csv`
      document.body.append(link)
      link.click()
      link.remove()
      window.setTimeout(() => URL.revokeObjectURL(url), 1000)
      setExported(true)
    } catch (reason) {
      if (!controller.signal.aborted) setExportError(presentApiError(reason))
    } finally {
      exportController.current = null
      if (!controller.signal.aborted) setExporting(false)
    }
  }

  const metrics = summary ? snapshot
    ? [['Total units', number(summary.totalUnits)], ['In use', number(summary.inUseUnits)], ['Available', number(summary.availableUnits)], ['Usage rate', percent(summary.usageRate)]]
    : [[view === 'facility' ? 'Facilities' : 'Unit types', number(view === 'facility' ? summary.facilityCount : summary.unitTypeCount)], ['Rental revenue', formatMoney(summary.rentalRevenue)], ['Usage rate', percent(summary.usageRate)]]
    : monthly ? [['Rental revenue', formatMoney(monthly.rentalRevenue)], ['First-month revenue', formatMoney(monthly.firstMonthRecognizedRevenue)], ['Subsequent paid rent', formatMoney(monthly.subsequentPaidRentalRevenue)]] : []
  const columns = revenue ? ['Month', 'First-month revenue', 'Subsequent paid rent', 'Rental revenue']
    : [view === 'unitType' ? 'Unit type' : 'Facility', ...(snapshot ? ['Unit type'] : []), 'Total units', 'Available', 'In use', 'Inspection', 'Maintenance', 'Usage rate', ...(!snapshot ? ['Rental revenue'] : [])]

  return <section aria-label="Report results" aria-busy={loading}>
    <div className="br-result-heading"><p>{grouped ? `${snapshot ? 'Current inventory' : 'Inventory snapshot'} · ${formatUtcDateTime(grouped.asOf)} ICT${snapshot ? '' : ` · Revenue: ${period}`}.` : `Rental revenue · ${period} · GMT+7 business calendar`}</p><button type="button" className="br-primary" title={`Export all matching rows across all pages. Revenue period: ${period}.`} disabled={loading || !data || exporting} onClick={() => void download()}><Download size={16} />{exporting ? 'Exporting…' : 'Export CSV'}</button></div>
    <ApiErrorAlert error={exportError} />
    {exported && <p role="status" className="br-notice">CSV download started for the applied report.</p>}
    {loading ? <LoadingState label="Loading report…" /> : error ? <div className="br-report-error"><ApiErrorAlert error={error} /><button type="button" onClick={() => setRetry(value => value + 1)}>Retry report</button></div> : data && <>
      <section className={`br-metrics ${snapshot ? 'br-four' : ''}`} aria-label="Report summary">{metrics.map(([label, value]) => <article className="br-metric" key={label}><span>{label}</span><strong>{value}</strong></article>)}</section>
      <section className="br-card br-chart" aria-label="Report chart">
        <div className="br-card-heading"><h2>{snapshot ? 'Physical unit status' : revenue ? 'Monthly rental revenue' : `Rental revenue by ${view === 'facility' ? 'facility' : 'unit type'}`}</h2><span>{snapshot ? 'Complete applied scope' : revenue ? period : 'Current page only'}</span></div>
        {summary && snapshot ? <InventoryChart inventory={summary} /> : monthly ? <RevenueChart report={monthly} /> : grouped ? <GroupChart rows={grouped.rows} view={view} /> : null}
      </section>
      <section className="br-card br-detail"><div className="br-card-heading"><h2>{revenue ? 'Monthly revenue breakdown' : snapshot ? 'Current storage usage' : `${view === 'facility' ? 'Facility' : 'Unit type'} performance`}</h2><span>{snapshot ? 'Current snapshot' : period}</span></div><p>{revenue ? 'Rental revenue excludes deposits, late fees, extra fees and damage charges.' : snapshot ? 'Current physical units grouped by facility and unit type.' : 'Unit counts and usage rate reflect the current snapshot. Revenue follows the selected period.'}</p>
        <p className="br-scroll-hint" id="br-table-help">Scroll horizontally for more columns. The first column stays visible.</p>
        <div className="br-table-wrap" tabIndex={0} role="region" aria-label="Report detail table" aria-describedby="br-table-help"><table><caption className="br-sr-only">{tabs.find(item => item.id === view)?.label} report · {period}</caption><thead><tr>{columns.map(column => <th scope="col" key={column} className={['Facility', 'Unit type', 'Month'].includes(column) ? undefined : 'br-numeric'}>{column}</th>)}</tr></thead><tbody>
          {monthly?.months.map(row => <tr key={row.month}><th scope="row">{row.month}</th><td>{formatMoney(row.firstMonthRecognizedRevenue)}</td><td>{formatMoney(row.subsequentPaidRentalRevenue)}</td><td>{formatMoney(row.rentalRevenue)}</td></tr>)}
          {grouped?.rows.map(row => <tr key={`${row.facilityId}:${row.unitTypeId}`}><th scope="row">{view === 'unitType' ? groupLabel(row, view) : row.facilityName ?? 'Unnamed facility'}</th>{snapshot && <td className="br-text-cell">{row.unitTypeName ?? 'Unnamed unit type'}{row.unitTypeSize ? ` (${row.unitTypeSize})` : ''}</td>}{[row.totalUnits, row.availableUnits, row.inUseUnits, row.inspectionUnits, row.maintenanceUnits].map((value, index) => <td key={index}>{number(value)}</td>)}<td>{percent(row.usageRate)}</td>{!snapshot && <td>{formatMoney(row.rentalRevenue)}</td>}</tr>)}
          {(grouped?.rows.length === 0 || monthly?.months.length === 0) && <tr><td colSpan={columns.length}><EmptyState message="No report rows match the applied filters." /></td></tr>}
        </tbody></table></div>
        <div className="br-table-footer"><span>{grouped ? `Total results: ${number(grouped.pagination.totalItems)}` : `Monthly rows: ${monthly?.months.length ?? 0}`}</span>{grouped && <PaginationControls pagination={grouped.pagination} onPageChange={setPage} />}</div>
      </section>
    </>}
  </section>
}

function InventoryChart({ inventory }: { inventory: PhysicalInventory }) {
  if (!inventory.totalUnits) return <EmptyState message="No physical units in the selected scope. Usage rate is 0%." />
  return <div className="br-bars">{statuses.map(status => <div className="br-bar-row" key={status.key}><span>{status.label}</span><div className="br-bar-track"><i style={{ width: `${inventory[status.key] / inventory.totalUnits * 100}%`, background: status.color }} /></div><strong>{number(inventory[status.key])}</strong></div>)}</div>
}

function GroupChart({ rows, view }: { rows: BusinessReportGroup[]; view: BusinessExportView }) {
  if (!rows.length) return <EmptyState message="No groups match the applied filters." />
  return <ColumnChart items={rows.map(row => ({
    id: `${row.facilityId}:${row.unitTypeId}`, label: groupLabel(row, view),
    first: row.rentalRevenue, later: 0, value: formatMoney(row.rentalRevenue),
  }))} />
}

function RevenueChart({ report }: { report: BusinessReportMonthly }) {
  if (!report.months.length) return <EmptyState message="No monthly revenue rows match the applied filters." />
  return <><div className="br-legend"><span><i className="br-first" />First-month revenue</span><span><i className="br-later" />Subsequent paid rent</span></div>
    <ColumnChart items={report.months.map(row => ({
      id: row.month, label: row.month, first: row.firstMonthRecognizedRevenue,
      later: row.subsequentPaidRentalRevenue, value: formatMoney(row.rentalRevenue),
    }))} />
  </>
}

function ColumnChart({ items }: { items: { id: string; label: string; first: number; later: number; value: string }[] }) {
  const max = Math.max(1, ...items.map(item => item.first + item.later))
  const magnitude = 10 ** Math.floor(Math.log10(max / 4))
  const step = Math.max(1, ([1, 2, 5, 10].find(value => value * magnitude >= max / 4) ?? 10) * magnitude)
  const intervals = Math.ceil(max / step)
  const ceiling = intervals * step
  const ticks = Array.from({ length: intervals + 1 }, (_, index) => ceiling - index * step)
  const axisNumber = new Intl.NumberFormat('en-GB', { notation: 'compact', maximumFractionDigits: 2 })
  return <>
    <div className="br-column-chart">
      <div className="br-axis-unit">VND</div>
      <div className="br-chart-body">
        <div className="br-y-axis" aria-label="Revenue scale in VND">{ticks.map((tick, index) =>
          <span key={tick} style={{ top: `${index / intervals * 100}%` }} title={formatMoney(tick)}>{axisNumber.format(tick)}</span>
        )}</div>
        <div className="br-columns-scroll" tabIndex={0} role="region" aria-label="Revenue comparison chart in VND; scroll horizontally to see all groups">
          <div className="br-columns">
            <div className="br-chart-grid" aria-hidden="true">{ticks.map((tick, index) =>
              <span key={tick} style={{ top: `${index / intervals * 100}%` }} />
            )}</div>
            {items.map(item => <figure key={item.id} title={`${item.label}: ${item.value}`}>
              <div className="br-column-track" role="img" aria-label={`${item.label}: ${item.value}`}>
                <i className="br-later" style={{ height: `${item.later / ceiling * 100}%` }} />
                <i className="br-first" style={{ height: `${item.first / ceiling * 100}%` }} />
              </div>
              <figcaption>{item.label}</figcaption>
            </figure>)}
          </div>
        </div>
      </div>
    </div>
    <p className="br-chart-help">Exact values are in the table below. Scroll the chart horizontally for more groups.</p>
  </>
}
