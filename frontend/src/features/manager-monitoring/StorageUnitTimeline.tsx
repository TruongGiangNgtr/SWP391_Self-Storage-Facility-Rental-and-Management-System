import { useEffect, useState } from 'react'
import { storageUnitTimelineApi, type UnitTimeline, type UnitStatusPeriod } from '../../api/storageUnitTimelineApi'
import { presentApiError } from '../../api/apiErrorPresentation'
import './storageUnitTimeline.css'

const labels = { AVAILABLE: 'Available', IN_USE: 'In use', INSPECTION: 'Under inspection', MAINTENANCE: 'Maintenance' }
const businessDate = () => new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date())
const timestamp = (value: string) => new Date(value).toLocaleString('en-GB', { timeZone: 'Asia/Ho_Chi_Minh' })

export function StorageUnitTimeline({ facilityId, revision }: { facilityId: string; revision: number }) {
  const [range, setRange] = useState(() => { const today = businessDate(); return { from: `${today.slice(0, 7)}-01`, to: today } })
  const [page, setPage] = useState(1)
  const [rows, setRows] = useState<UnitTimeline[]>([])
  const [pages, setPages] = useState(0)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [retry, setRetry] = useState(0)
  const [query, setQuery] = useState('')
  const [type, setType] = useState('')
  const [status, setStatus] = useState('')
  const [selected, setSelected] = useState<{ unit: UnitTimeline; period: UnitStatusPeriod } | null>(null)
  const start = Date.parse(`${range.from}T00:00:00+07:00`)
  const end = Date.parse(`${range.to}T00:00:00+07:00`) + 86400000
  const valid = Number.isFinite(start) && Number.isFinite(end) && end > start
  useEffect(() => {
    let active = true
    setRows([]); setPages(0); setSelected(null); setError('')
    if (!valid) { setLoading(false); return }
    setLoading(true)
    storageUnitTimelineApi.list(facilityId, range.from, range.to, page)
      .then(response => { if (active) { setRows(response.data); setPages(response.pagination.totalPages) } })
      .catch(reason => { if (active) { const issue = presentApiError(reason); setError(`${issue.message}${issue.traceId ? ` · Trace: ${issue.traceId}` : ''}`) } })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [facilityId, range.from, range.to, page, revision, retry, valid])
  const visible = rows.filter(unit => `${unit.unitCode} ${unit.unitTypeName} ${unit.locationInfo ?? ''}`.toLowerCase().includes(query.trim().toLowerCase()) && (!type || unit.unitTypeName === type) && (!status || unit.periods.some(period => period.status === status && Date.parse(period.from) < end && (period.to ? Date.parse(period.to) : Date.now()) > start)))
  return <section className="sut" aria-label="Storage unit status timeline">
    <div className="sut-filters">
      <label>From<input type="date" value={range.from} onChange={e => { setRange({ ...range, from: e.target.value }); setPage(1) }} /></label>
      <label>To<input type="date" value={range.to} onChange={e => { setRange({ ...range, to: e.target.value }); setPage(1) }} /></label>
      <label>Search this page<input placeholder="Unit code or location" value={query} onChange={e => setQuery(e.target.value)} /></label>
      <label>Unit type on this page<select value={type} onChange={e => setType(e.target.value)}><option value="">All types</option>{[...new Set(rows.map(unit => unit.unitTypeName))].map(name => <option key={name}>{name}</option>)}</select></label>
      <label>Status in period<select value={status} onChange={e => setStatus(e.target.value)}><option value="">All statuses</option>{Object.entries(labels).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></label>
    </div>
    <p className="mop-small">GMT+7 · Filters apply to this page. Blank intervals mean no historical data; future status is not predicted.</p>
    <div className="sut-legend">{Object.entries(labels).map(([key, label]) => <span key={key}><i className={`sut-color sut-${key}`} />{label}</span>)}<span><i className="sut-color sut-unknown" />No data</span></div>
    {!valid ? <p role="alert" className="mop-alert">Select a valid date range; the end date must be on or after the start date.</p> : loading ? <p className="mop-empty" role="status">Loading unit history…</p> : error ? <div className="mop-alert" role="alert"><strong>Unable to load unit history</strong><p>{error}</p><button type="button" onClick={() => setRetry(value => value + 1)}>Retry</button></div> : !visible.length ? <p className="mop-empty">{rows.length ? 'No units match the filters on this page.' : 'No unit history is available for this period.'}</p> : <div className="sut-scroll"><table className="sut-table"><caption className="mop-small">Storage unit history · {range.from} – {range.to}</caption><thead><tr><th scope="col">Unit / Type</th><th scope="col"><div className="sut-axis">{[0, 1, 2, 3, 4].map(tick => <span key={tick}>{new Date(start + (end - start - 86400000) * tick / 4).toLocaleDateString('en-GB', { timeZone: 'Asia/Ho_Chi_Minh', day: '2-digit', month: '2-digit' })}</span>)}</div></th></tr></thead><tbody>{visible.map(unit => <tr key={unit.unitCode}><th scope="row"><strong>{unit.unitCode}</strong><small>{unit.unitTypeName}</small><small>{unit.locationInfo}</small></th><td><div className="sut-track">{unit.periods.map((period, index) => {
      const left = Math.max(start, Date.parse(period.from))
      const right = Math.min(end, period.to ? Date.parse(period.to) : Date.now())
      if (!Number.isFinite(left) || !Number.isFinite(right) || right <= left) return null
      return <button type="button" key={index} className={`sut-segment sut-${period.status}`} style={{ left: `${(left - start) / (end - start) * 100}%`, width: `${(right - left) / (end - start) * 100}%` }} title={`${unit.unitCode}: ${labels[period.status]} · ${timestamp(period.from)} → ${period.to ? timestamp(period.to) : 'Ongoing'}`} aria-label={`${unit.unitCode}, ${labels[period.status]}, from ${timestamp(period.from)} to ${period.to ? timestamp(period.to) : 'ongoing'}`} onClick={() => setSelected({ unit, period })}>{labels[period.status]}</button>
    })}</div></td></tr>)}</tbody></table></div>}
    <div className="mop-pagination"><button className="mop-btn mop-btn-secondary" type="button" disabled={loading || !valid || page <= 1} onClick={() => setPage(value => value - 1)}>Previous</button><span>Page {page} / {Math.max(1, pages)}</span><button className="mop-btn mop-btn-secondary" type="button" disabled={loading || !valid || page >= pages} onClick={() => setPage(value => value + 1)}>Next</button></div>
    {selected && <section className="sut-detail" aria-label="Selected status period"><div><h3>{selected.unit.unitCode} · Status details</h3><button className="mop-btn mop-btn-secondary" type="button" onClick={() => setSelected(null)}>Close details</button></div><dl><dt>Facility</dt><dd>{selected.unit.facilityName || 'Name unavailable'}</dd><dt>Unit type</dt><dd>{selected.unit.unitTypeName || 'Name unavailable'}</dd><dt>Status</dt><dd>{labels[selected.period.status]}</dd><dt>From (GMT+7)</dt><dd>{timestamp(selected.period.from)}</dd><dt>Until (GMT+7)</dt><dd>{selected.period.to ? timestamp(selected.period.to) : 'Ongoing'}</dd></dl></section>}
  </section>
}
