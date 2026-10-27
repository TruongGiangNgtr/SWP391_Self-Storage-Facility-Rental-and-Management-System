import { useEffect, useState } from 'react'
import { storageUnitTimelineApi, type UnitTimeline, type UnitStatusPeriod } from '../../api/storageUnitTimelineApi'
import { presentApiError } from '../../api/apiErrorPresentation'
import './storageUnitTimeline.css'

const DAY = 86400000
const unitName = (unit: UnitTimeline) => `${unit.unitTypeName || 'Type name unavailable'} · ${unit.unitCode}`
const labels = { AVAILABLE: 'Available', IN_USE: 'In use', INSPECTION: 'Under inspection', MAINTENANCE: 'Maintenance' }
const businessMonth = () => { const parts = new Intl.DateTimeFormat('en-GB', { timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit' }).formatToParts(new Date()); return `${parts.find(p => p.type === 'year')?.value}-${parts.find(p => p.type === 'month')?.value}` }
const timestamp = (value: string) => new Date(value).toLocaleString('en-GB', { timeZone: 'Asia/Ho_Chi_Minh' })

export function StorageUnitTimeline({ facilityId, revision }: { facilityId: string; revision: number }) {
  const [month, setMonth] = useState(businessMonth)
  const [page, setPage] = useState(1)
  const [rows, setRows] = useState<UnitTimeline[]>([])
  const [pages, setPages] = useState(0)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [retry, setRetry] = useState(0)
  const [query, setQuery] = useState('')
  const [type, setType] = useState('')
  const [status, setStatus] = useState('')
  const [selected, setSelected] = useState<{ unit: UnitTimeline; day: number } | null>(null)
  const start = Date.parse(`${month}-01T00:00:00+07:00`)
  const end = start + new Date(Date.UTC(Number(month.slice(0, 4)), Number(month.slice(5, 7)), 0)).getUTCDate() * 86400000
  const days = Number.isFinite(start) && Number.isFinite(end) ? Array.from({ length: Math.round((end - start) / DAY) }, (_, index) => start + index * DAY) : []
  const valid = /^\d{4}-(0[1-9]|1[0-2])$/.test(month) && Number(month.slice(0, 4)) >= 1 && Number(month.slice(0, 4)) <= 9998 && Number.isFinite(start) && Number.isFinite(end) && end > start
  useEffect(() => {
    let active = true
    setRows([]); setPages(0); setSelected(null); setError('')
    if (!valid) { setLoading(false); return }
    setLoading(true)
    storageUnitTimelineApi.list(facilityId, month, page)
      .then(response => { if (active) { setRows(response.data); setPages(response.pagination.totalPages) } })
      .catch(reason => { if (active) { const issue = presentApiError(reason); setError(`${issue.message}${issue.traceId ? ` · Trace: ${issue.traceId}` : ''}`) } })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [facilityId, month, page, revision, retry, valid])
  const visible = rows.filter(unit => `${unit.unitCode} ${unit.unitTypeName} ${unit.locationInfo ?? ''}`.toLowerCase().includes(query.trim().toLowerCase()) && (!type || unit.unitTypeName === type) && (!status || unit.intervals.some(period => period.status === status && Date.parse(period.from) < end && Date.parse(period.to) > start)))
  return <section className="sut" aria-label="Storage unit status timeline">
    <div className="sut-filters">
      <label>Month<input type="month" min="0001-01" max="9998-12" value={month} onChange={e => { setMonth(e.target.value); setPage(1); setType('') }} /></label>
      <label>Search this page<input placeholder="Unit name, code or location" value={query} onChange={e => { setQuery(e.target.value); setSelected(null) }} /></label>
      <label>Unit type on this page<select value={type} onChange={e => { setType(e.target.value); setSelected(null) }}><option value="">All types</option>{[...new Set([type, ...rows.map(unit => unit.unitTypeName)].filter(Boolean))].map(name => <option key={name}>{name}</option>)}</select></label>
      <label>Status in period<select value={status} onChange={e => { setStatus(e.target.value); setSelected(null) }}><option value="">All statuses</option>{Object.entries(labels).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></label>
    </div>
    <p className="mop-small">GMT+7 · Filters apply to this page. Missing status history defaults to Available. Recorded status persists until the next change, including future dates. Scroll horizontally to see all days. Select a day to view exact status times. End times are exclusive.</p>
    <div className="sut-legend">{Object.entries(labels).map(([key, label]) => <span key={key}><i className={`sut-color sut-${key}`} />{label}</span>)}</div>
    {!valid ? <p role="alert" className="mop-alert">Select a valid month (YYYY-MM).</p> : loading ? <p className="mop-empty" role="status">Loading unit history…</p> : error ? <div className="mop-alert" role="alert"><strong>Unable to load unit history</strong><p>{error}</p><button type="button" onClick={() => setRetry(value => value + 1)}>Retry</button></div> : !visible.length ? <p className="mop-empty">{rows.length ? 'No units match the filters on this page.' : 'No unit history is available for this period.'}</p> : <div className="sut-scroll" role="region" aria-label="Daily timeline for the full month" tabIndex={0}><table className="sut-table" style={{ minWidth: 180 + days.length * 44 }}><caption className="mop-small">Storage unit history · {month} · Each column is one day (GMT+7)</caption><thead><tr><th scope="col">Storage unit / Code</th>{days.map((day, index) => <th scope="col" key={day}><span>{String(index + 1).padStart(2, '0')}</span></th>)}</tr></thead><tbody>{visible.map(unit => <tr key={unit.storageUnitId}><th scope="row"><strong>{unit.unitTypeName || 'Type name unavailable'}</strong><small>{unit.unitCode}</small><small>{unit.locationInfo}</small></th>{days.map(day => {
      const periods = unit.intervals.filter(period => Date.parse(period.from) < day + DAY && Date.parse(period.to) > day)
      const summary = periods.map(period => `${labels[period.status]}: ${timestamp(new Date(Math.max(day, Date.parse(period.from))).toISOString())} → ${timestamp(new Date(Math.min(day + DAY, Date.parse(period.to))).toISOString())}`).join('; ')
      return <td key={day}><button type="button" className="sut-day sut-track" aria-pressed={selected?.unit.storageUnitId === unit.storageUnitId && selected.day === day} title={summary || 'No status data'} aria-label={`${unitName(unit)}, ${timestamp(new Date(day).toISOString())}. ${summary || 'No status data'}`} onClick={() => setSelected({ unit, day })}>{periods.map((period, index) => {
        const left = Math.max(day, Date.parse(period.from))
        const right = Math.min(day + DAY, Date.parse(period.to))
        return <span key={index} className={`sut-segment sut-${period.status}`} style={{ left: `${(left - day) / DAY * 100}%`, width: `${(right - left) / DAY * 100}%` }} aria-hidden="true" />
      })}</button></td>
    })}</tr>)}</tbody></table></div>}

    <div className="mop-pagination"><button className="mop-btn mop-btn-secondary" type="button" disabled={loading || !valid || page <= 1} onClick={() => setPage(value => value - 1)}>Previous</button><span>Page {page} / {Math.max(1, pages)}</span><button className="mop-btn mop-btn-secondary" type="button" disabled={loading || !valid || page >= pages} onClick={() => setPage(value => value + 1)}>Next</button></div>
    {selected && <section className="sut-detail" aria-label="Selected day status details" aria-live="polite"><div><h3>{unitName(selected.unit)} · {new Date(selected.day).toLocaleDateString('en-GB', { timeZone: 'Asia/Ho_Chi_Minh' })}</h3><button className="mop-btn mop-btn-secondary" type="button" onClick={() => setSelected(null)}>Close details</button></div><dl><dt>Facility</dt><dd>{selected.unit.facilityName || 'Name unavailable'}</dd><dt>Location</dt><dd>{selected.unit.locationInfo || 'Location not specified'}</dd></dl>
      <p className="mop-small">All status periods for the selected day (GMT+7). Until is exclusive; midnight on the next date marks the end of this day.</p>
      {selected.unit.intervals.filter(period => Date.parse(period.from) < selected.day + DAY && Date.parse(period.to) > selected.day).map((period: UnitStatusPeriod, index) => <div className="sut-period" key={index}><strong className={`sut-period-status sut-${period.status}`}>{labels[period.status]}</strong><dl><dt>From (GMT+7)</dt><dd>{timestamp(new Date(Math.max(selected.day, Date.parse(period.from))).toISOString())}</dd><dt>Until (GMT+7)</dt><dd>{timestamp(new Date(Math.min(selected.day + DAY, Date.parse(period.to))).toISOString())}</dd><dt>Full status period</dt><dd>{timestamp(period.from)} → {timestamp(period.to)}</dd></dl></div>)}
      {!selected.unit.intervals.some(period => Date.parse(period.from) < selected.day + DAY && Date.parse(period.to) > selected.day) && <p>No status data for this day.</p>}
    </section>}
  </section>
}
