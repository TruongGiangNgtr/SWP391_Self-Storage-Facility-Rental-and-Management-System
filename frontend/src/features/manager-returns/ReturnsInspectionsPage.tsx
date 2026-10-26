import { EmployeeEntityLabel } from '../../components/EmployeeEntityLabel'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { AlertTriangle, ArrowUpRight, CheckCircle2, ClipboardCheck, Clock3, RefreshCw, RotateCcw, Search, ShieldCheck, X } from 'lucide-react'
import { useAuth } from '../../auth/auth.context'
import { inspectionApi } from '../../api/inspectionApi'
import { visitApi } from '../../api/visitApi'
import { contractApi } from '../../api/contractApi'
import { storageUnitApi, type StorageUnit } from '../../api/storageUnitApi'
import { facilityReportApi, type FacilityOperationsReport } from '../../api/facilityReportApi'
import { presentApiError } from '../../api/apiErrorPresentation'
import type { ApiCollectionResponse } from '../../api/api.types'
import type { InspectionDetail, InspectionListItem, DamageRecordDetail, DamageTypeDetail } from '../return-management/return.types'
import type { VisitDetail } from '../visits/visit.types'
import type { HandoverContractDetail } from '../handover/contract.types'
import '../manager-portal.css'

const PAGE_SIZE = 20
const date = (value?: string | null) => value ? value.slice(0, 10) : '—'
const money = (value: number) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(value)
const errorText = (e: unknown) => { const r = presentApiError(e); return r.message + (r.traceId ? ` (Trace: ${r.traceId})` : '') }
const badge = (s: string) => <span className={`mop-status mop-status-${s.toLowerCase().replace(/_/g, '-')}`}>{s.replace(/_/g, ' ')}</span>
type Filter = 'ALL' | 'PENDING' | 'IN_PROGRESS' | 'COMPLETED'
type View = 'inspections' | 'return-visits'
interface OpenInspection { inspection: InspectionDetail; contract: HandoverContractDetail | null; visit: VisitDetail | null; unit: StorageUnit | null }

export function ReturnsInspectionsPage() {
  const { user } = useAuth()
  const facilityId = user?.facilityId || null
  const [view, setView] = useState<View>('inspections')
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [filter, setFilter] = useState<Filter>('ALL')
  const [inspections, setInspections] = useState<InspectionListItem[]>([])
  const [visits, setVisits] = useState<VisitDetail[]>([])
  const [pages, setPages] = useState(0)
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [listError, setListError] = useState('')
  const [types, setTypes] = useState<DamageTypeDetail[]>([])
  const [typesError, setTypesError] = useState('')
  const [report, setReport] = useState<FacilityOperationsReport | null>(null)
  const [selected, setSelected] = useState<OpenInspection | null>(null)
  const [selectedVisit, setSelectedVisit] = useState<VisitDetail | null>(null)
  const [detailLoading, setDetailLoading] = useState(false)
  const [detailError, setDetailError] = useState('')
  const [decision, setDecision] = useState<{ damage: DamageRecordDetail; outcome: 'APPROVED' | 'REJECTED' } | null>(null)
  const [busy, setBusy] = useState(false)
  const [notice, setNotice] = useState('')
  const [revision, setRevision] = useState(0)
  const [detailTarget, setDetailTarget] = useState<{ view: View; id: string } | null>(null)
  const dialogRef = useRef<HTMLDialogElement>(null)
  const detailRequest = useRef(0)
  const refresh = () => { setRevision(v => v + 1); setNotice('') }

  const closeDetail = useCallback(() => {
    detailRequest.current += 1
    setDetailTarget(null)
    setSelected(null)
    setSelectedVisit(null)
    setDecision(null)
    setDetailLoading(false)
    setDetailError('')
    setNotice('')
  }, [])

  useEffect(() => {
    if (!detailTarget) return
    const dialog = dialogRef.current
    const previousFocus = document.activeElement
    dialog?.showModal()
    const overflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    return () => {
      dialog?.close()
      document.body.style.overflow = overflow
      if (previousFocus instanceof HTMLElement) previousFocus.focus()
    }
  }, [detailTarget])

  useEffect(() => () => { detailRequest.current += 1 }, [])

  useEffect(() => {
    let alive = true
    if (!facilityId) { setListError('No assigned Facility. Contact your System Administrator.'); setLoading(false); return }
    setLoading(true)
    setListError('')
    const request: Promise<ApiCollectionResponse<InspectionListItem | VisitDetail>> = view === 'inspections'
      ? inspectionApi.list(page, PAGE_SIZE)
      : visitApi.list(page, PAGE_SIZE)
    request.then(r => {
      if (!alive) return
      if (view === 'inspections') {
        // INS-001 carries authoritative FacilityId; never show another facility's row.
        setInspections((r.data as InspectionListItem[]).filter(i => i.facilityId === facilityId))
      } else {
        // VIS-003 is already scoped at the server and has no FacilityId in its DTO.
        setVisits((r.data as VisitDetail[]).filter(v => v.visitType === 'RETURN'))
      }
      setTotal(r.pagination.totalItems)
      setPages(r.pagination.totalPages)
    }).catch(e => { if (alive) { setListError(errorText(e)); setTotal(0); setPages(0); setInspections([]); setVisits([]) } })
      .finally(() => { if (alive) setLoading(false) })
    return () => { alive = false }
  }, [facilityId, page, view, revision])

  useEffect(() => {
    let alive = true
    inspectionApi.listDamageTypes().then(r => { if (alive) { setTypes(r.data); setTypesError('') } }).catch(e => { if (alive) setTypesError(errorText(e)) })
    return () => { alive = false }
  }, [revision])

  useEffect(() => {
    let alive = true
    if (facilityId) facilityReportApi.operations(facilityId).then(r => { if (alive) setReport(r.data) }).catch(() => { if (alive) setReport(null) })
    return () => { alive = false }
  }, [facilityId, revision])

  const visibleInspections = useMemo(() => inspections.filter(i => (filter === 'ALL' || i.status === filter) && JSON.stringify(i).toLowerCase().includes(search.trim().toLowerCase())), [inspections, filter, search])
  const visibleVisits = useMemo(() => visits.filter(v => JSON.stringify(v).toLowerCase().includes(search.trim().toLowerCase())), [visits, search])
  const pendingDecisions = selected?.inspection.damages.filter(d => d.status === 'PENDING') ?? []

  const openInspection = useCallback(async (id: string) => {
    const requestId = ++detailRequest.current
    setDetailTarget(current => current?.view === 'inspections' && current.id === id ? current : { view: 'inspections', id })
    setDetailLoading(true)
    setSelected(null)
    setSelectedVisit(null)
    setDetailError('')
    setNotice('')
    try {
      const detail = (await inspectionApi.get(id)).data
      if (requestId !== detailRequest.current) return
      setSelected({ inspection: detail, contract: null, visit: null, unit: null })
      setDetailLoading(false)
      // Inspect detail first: subordinate resources are resolved through its authoritative IDs.
      const [contract, visit, unit] = await Promise.allSettled([
        contractApi.get(detail.contractId),
        detail.visitId ? visitApi.get(detail.visitId) : Promise.resolve(null),
        storageUnitApi.get(detail.storageUnitId),
      ])
      if (requestId !== detailRequest.current) return
      setSelected(current => current ? { ...current,
        contract: contract.status === 'fulfilled' ? contract.value.data : null,
        visit: visit.status === 'fulfilled' ? visit.value?.data ?? null : null,
        unit: unit.status === 'fulfilled' ? unit.value.data : null,
      } : null)
      if ([contract, visit, unit].some(r => r.status === 'rejected')) setDetailError('Some related Contract, Visit or Unit details could not be loaded. Inspection data is still authoritative.')
    } catch (e) { if (requestId === detailRequest.current) { setDetailError(errorText(e)); setSelected(null) } }
    finally { if (requestId === detailRequest.current) setDetailLoading(false) }
  }, [])

  const openReturnVisit = async (visitId: string) => {
    const requestId = ++detailRequest.current
    setDetailTarget(current => current?.view === 'return-visits' && current.id === visitId ? current : { view: 'return-visits', id: visitId })
    setDetailLoading(true)
    setDetailError('')
    setNotice('')
    setSelected(null)
    setSelectedVisit(null)
    try { const r = await visitApi.get(visitId); if (requestId === detailRequest.current) setSelectedVisit(r.data) }
    catch (e) { if (requestId === detailRequest.current) { setDetailError(errorText(e)); setSelectedVisit(null) } }
    finally { if (requestId === detailRequest.current) setDetailLoading(false) }
  }

  async function confirmDecision() {
    if (!decision || !selected || busy) return
    const { damage, outcome } = decision
    setBusy(true)
    setNotice('')
    setDetailError('')
    try {
      // Re-read before submission; backend conditional update is the final concurrency guard.
      const latest = (await inspectionApi.get(selected.inspection.inspectionId)).data
      if (!latest.damages.some(d => d.damageRecordId === damage.damageRecordId && d.status === 'PENDING')) {
        setSelected({ ...selected, inspection: latest })
        setDecision(null)
        setDetailError('This Damage record was already decided. The latest Inspection has been loaded.')
        return
      }
      const result = await inspectionApi.decideDamage(damage.damageRecordId, outcome)
      setDecision(null)
      const refreshed = (await inspectionApi.get(selected.inspection.inspectionId)).data
      setSelected(current => current ? { ...current, inspection: refreshed } : null)
      setNotice(`Damage is now ${result.data.status}. The decision is final and audited.`)
      setRevision(v => v + 1)
    } catch (e) {
      setDecision(null)
      setDetailError(errorText(e))
      // Conflict or concurrent decision: reload persisted status, never assume success.
      try { const current = (await inspectionApi.get(selected.inspection.inspectionId)).data; setSelected(old => old ? { ...old, inspection: current } : null) } catch { /* original error remains */ }
    } finally { setBusy(false) }
  }

  const switchView = (next: View) => { closeDetail(); setView(next); setPage(1); setSearch(''); setFilter('ALL') }

  return <main className="mop-page" data-testid="manager-returns-inspections-page">
    <header className="mop-page-header"><div><p className="mop-eyebrow">FACILITY MANAGER · RETURNS</p><h1>Returns & Inspections</h1><p>Monitor physical returns, Inspection progress and approve or reject recorded Damage at your Facility.</p><span className="mop-facility-id"><EmployeeEntityLabel kind="facility" id={facilityId} /></span></div><button type="button" className="mop-btn mop-btn-secondary" onClick={refresh}><RefreshCw size={16}/> Refresh</button></header>
    <div className="mop-stats mop-return-stats"><div><span>Units under inspection</span><strong>{report ? report.inspectionUnits : '—'}</strong><small>Current facility snapshot</small></div><div><span>Inspections on this page</span><strong>{inspections.length}</strong><small>Page {page} · facility-scoped</small></div><div><span>In progress on this page</span><strong>{inspections.filter(i => i.status === 'IN_PROGRESS').length}</strong><small>Claimed by Facility Staff</small></div><div><span>Pending Damage (selected)</span><strong>{selected ? pendingDecisions.length : '—'}</strong><small>Open an Inspection to review</small></div></div>
    <section className="mop-section"><div className="mop-section-heading"><div><h2><RotateCcw size={19}/> Return work queue</h2><p>Review return visits and inspection findings. Managers decide damage charges; staff claim inspections and finalize returns.</p></div></div>
      <div role="tablist" aria-label="Return monitoring view" className="mop-tabs"><button type="button" role="tab" aria-selected={view === 'inspections'} className={`mop-tab ${view === 'inspections' ? 'active' : ''}`} onClick={() => switchView('inspections')}><ClipboardCheck size={16}/>Inspections</button><button type="button" role="tab" aria-selected={view === 'return-visits'} className={`mop-tab ${view === 'return-visits' ? 'active' : ''}`} onClick={() => switchView('return-visits')}><Clock3 size={16}/>RETURN Visits</button></div>
      <div className="mop-table-toolbar"><label className="mop-search"><Search size={16}/><input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search this page by status or note" aria-label="Search current page"/></label>{view === 'inspections' && <label className="mop-select-label">Status <select value={filter} onChange={e => setFilter(e.target.value as Filter)}><option value="ALL">All statuses</option><option value="PENDING">Pending</option><option value="IN_PROGRESS">In progress</option><option value="COMPLETED">Completed</option></select></label>}<span className="mop-small">{total} records across all statuses and types</span></div>
      {loading ? <p className="mop-empty">Loading {view}…</p> : listError ? <div role="alert" className="mop-alert">{listError}<button type="button" onClick={refresh}>Retry</button></div> : view === 'inspections' ? visibleInspections.length ? <div className="mop-table">{visibleInspections.map(i => <button key={i.inspectionId} className="mop-table-row" type="button" onClick={() => void openInspection(i.inspectionId)}><span className="mop-record"><strong>{i.status.replaceAll('_', ' ')} inspection</strong><small><EmployeeEntityLabel kind="contract" id={i.contractId} /> · <EmployeeEntityLabel kind="unit" id={i.storageUnitId} /></small></span><span className="mop-secondary">{i.completedAt ? `Completed ${date(i.completedAt)}` : i.employeeId ? <EmployeeEntityLabel kind="employee" id={i.employeeId} /> : 'Awaiting Staff claim'}</span>{badge(i.status)}<ArrowUpRight size={17}/></button>)}</div> : <p className="mop-empty">No Inspections match this page and filter.</p> : visibleVisits.length ? <div className="mop-table">{visibleVisits.map(v => <button key={v.visitId} type="button" className="mop-table-row" onClick={() => void openReturnVisit(v.visitId)}><span className="mop-record"><strong>Return visit · {date(v.visitDate)}</strong><small><EmployeeEntityLabel kind="contract" id={v.entityId} /> · Scheduled {date(v.visitDate)}</small></span><span className="mop-secondary">{v.actualReturnDate ? `Physical return ${date(v.actualReturnDate)}` : 'Return not yet confirmed'}</span>{badge(v.status)}<ArrowUpRight size={17}/></button>)}</div> : <p className="mop-empty">No return visits on this page. Try another page to review more visits.</p>}
      <div className="mop-pagination"><button className="mop-btn mop-btn-secondary" type="button" disabled={loading || page <= 1} onClick={() => setPage(p => p - 1)}>Previous</button><span>Page {page} / {Math.max(1, pages)}</span><button className="mop-btn mop-btn-secondary" type="button" disabled={loading || page >= pages} onClick={() => setPage(p => p + 1)}>Next</button></div>
    </section>
    {typesError && <p className="mop-alert" role="alert">DamageType catalogue unavailable: {typesError}</p>}
    {detailTarget && <dialog ref={dialogRef} className="mop-return-dialog" aria-labelledby="mop-return-detail-title" onCancel={e => { e.preventDefault(); if (busy) return; if (decision) setDecision(null); else closeDetail() }} onMouseDown={e => { if (e.target === e.currentTarget && !busy && !decision) closeDetail() }}>
    <div className="mop-drawer mop-return-drawer">
      <div className="mop-drawer-head"><div><small>Returns & Inspections</small><h2 id="mop-return-detail-title">{detailTarget.view === 'inspections' ? 'Inspection detail' : 'RETURN Visit detail'}</h2></div><button type="button" className="mop-icon-button" aria-label="Close record detail" disabled={busy || !!decision} onClick={closeDetail}><X size={20}/></button></div>
    {detailLoading && <p className="mop-alert" role="status">Loading record details…</p>}
    {detailError && <div className="mop-alert" role="alert">{detailError}<button type="button" disabled={busy} onClick={() => void (detailTarget.view === 'inspections' ? openInspection(detailTarget.id) : openReturnVisit(detailTarget.id))}>Reload detail</button></div>}
    {notice && <div role="status" className="mop-success"><CheckCircle2 size={18}/>{notice}</div>}
    {selectedVisit && <section className="mop-section" aria-label="RETURN Visit detail"><div className="mop-section-heading"><h2>RETURN Visit detail</h2></div><dl className="mop-detail-grid"><div className="mop-detail-field"><dt>Visit</dt><dd>Return visit · {date(selectedVisit.visitDate)}</dd></div><div className="mop-detail-field"><dt>Contract</dt><dd><EmployeeEntityLabel kind="contract" id={selectedVisit.entityId} /></dd></div><div className="mop-detail-field"><dt>Status</dt><dd>{selectedVisit.status}</dd></div><div className="mop-detail-field"><dt>Scheduled</dt><dd>{date(selectedVisit.visitDate)}</dd></div><div className="mop-detail-field"><dt>Actual physical return</dt><dd>{date(selectedVisit.actualReturnDate)}</dd></div><div className="mop-detail-field"><dt>Actual Staff actor</dt><dd><EmployeeEntityLabel kind="employee" id={selectedVisit.employeeId} empty={'—'} /></dd></div></dl></section>}
    {selected && <section className="mop-section" data-testid="manager-inspection-detail"><div className="mop-section-heading"><div><p className="mop-eyebrow">FACILITY INSPECTION</p><h2>Return inspection {badge(selected.inspection.status)}</h2><p>Read-only findings and terminal Damage decisions. Only Facility Staff can finalize Return.</p></div></div>
      <dl className="mop-detail-grid"><div className="mop-detail-field"><dt>Contract</dt><dd><EmployeeEntityLabel kind="contract" id={selected.inspection.contractId} /></dd></div><div className="mop-detail-field"><dt>Contract status / period</dt><dd>{selected.contract ? `${selected.contract.status} · ${selected.contract.startMonth} → ${selected.contract.endMonth}` : 'Unavailable'}</dd></div><div className="mop-detail-field"><dt>Storage unit</dt><dd>{selected.unit?.unitCode ?? <EmployeeEntityLabel kind="unit" id={selected.inspection.storageUnitId} />} · {selected.unit?.status ?? 'Unknown'}</dd></div><div className="mop-detail-field"><dt>RETURN Visit</dt><dd>{selected.visit ? `${date(selected.visit.visitDate)} · ${selected.visit.status}` : selected.inspection.visitId ? 'Visit details unavailable' : 'External recovery / no Visit'}</dd></div><div className="mop-detail-field"><dt>Actual return date</dt><dd>{date(selected.visit?.actualReturnDate)}</dd></div><div className="mop-detail-field"><dt>Claimed Staff</dt><dd><EmployeeEntityLabel kind="employee" id={selected.inspection.employeeId} empty={'Unclaimed'} /></dd></div><div className="mop-detail-field"><dt>Completed (GMT+7)</dt><dd>{selected.inspection.completedAt ? new Date(selected.inspection.completedAt).toLocaleString('en-GB', { timeZone: 'Asia/Ho_Chi_Minh' }) : '—'}</dd></div><div className="mop-detail-field"><dt>Condition note</dt><dd>{selected.inspection.conditionNote || 'No findings recorded'}</dd></div></dl>
      <div className="mop-detail-section"><div className="mop-section-heading"><div><h3>Damage decisions ({selected.inspection.damages.length})</h3><p>APPROVED values may be included in settlement. REJECTED values contribute zero; any PENDING decision blocks finalization.</p></div>{pendingDecisions.length ? <span className="mop-pending"><AlertTriangle size={15}/> {pendingDecisions.length} pending</span> : <span className="mop-status mop-status-approved">No pending decisions</span>}</div>
        {selected.inspection.damages.length === 0 ? <p className="mop-empty">No Damage records associated with this Inspection.</p> : <div className="mop-damage-list">{selected.inspection.damages.map(d => { const typeName = d.damageTypeName || types.find(t => t.damageTypeId === d.damageTypeId)?.name || 'Damage type unavailable'; return <article key={d.damageRecordId} className="mop-damage"><div className="mop-damage-heading"><div><strong>{typeName.replace(/_/g, ' ')}</strong><small>{date(d.createdAt)}</small></div>{badge(d.status)}</div><strong className="mop-amount">{money(d.damageAmount)}</strong><p>{d.note || 'No additional note provided.'}</p>{d.status === 'PENDING' && <div className="mop-decision-buttons"><button type="button" className="mop-btn mop-btn-primary" disabled={busy} onClick={() => setDecision({ damage: d, outcome: 'APPROVED' })}><ShieldCheck size={16}/>Approve</button><button type="button" className="mop-btn mop-btn-danger" disabled={busy} onClick={() => setDecision({ damage: d, outcome: 'REJECTED' })}><X size={16}/>Reject</button></div>}</article> })}</div>}
      </div>
      <div className="mop-return-extras"><section className="mop-detail-section"><h3>Extra fees ({selected.inspection.extraFees.length})</h3>{selected.inspection.extraFees.length ? selected.inspection.extraFees.map(f => <div className="mop-line" key={f.extraFeeId}><span>{f.reason}</span><strong>{money(f.amount)}</strong></div>) : <p className="mop-muted">No extra fees recorded.</p>}</section><section className="mop-detail-section"><h3>Evidence ({selected.inspection.evidence.length})</h3>{selected.inspection.evidence.length ? selected.inspection.evidence.map(e => <div key={e.inspectionEvidenceId} className="mop-line"><span>{e.evidenceType}</span><small>{date(e.createdAt)}</small></div>) : <p className="mop-muted">No uploaded evidence metadata.</p>}<p className="mop-muted">This view lists evidence details only.</p></section></div>
    </section>}
    </div>
    {decision && <div className="mop-confirm-backdrop" role="presentation" onMouseDown={e => { if (e.target === e.currentTarget && !busy) setDecision(null) }}><div role="alertdialog" aria-modal="true" aria-labelledby="mop-decision-heading" className="mop-confirm"><div className="mop-confirm-icon"><AlertTriangle size={24}/></div><h2 id="mop-decision-heading">{decision.outcome === 'APPROVED' ? 'Approve' : 'Reject'} this Damage?</h2><p>Damage <strong>{decision.damage.damageTypeName || types.find(type => type.damageTypeId === decision.damage.damageTypeId)?.name || 'Recorded damage'}</strong> · {money(decision.damage.damageAmount)}</p><p>This is a terminal audited decision. It cannot be edited or undone. The amount and note entered by Staff will not change.</p><div className="mop-confirm-actions"><button type="button" className="mop-btn mop-btn-secondary" disabled={busy} onClick={() => setDecision(null)}>Cancel</button><button type="button" className={`mop-btn ${decision.outcome === 'APPROVED' ? 'mop-btn-primary' : 'mop-btn-danger'}`} disabled={busy} onClick={() => void confirmDecision()}>{busy ? 'Submitting…' : `Confirm ${decision.outcome.toLowerCase()}`}</button></div></div></div>}
    </dialog>}
  </main>
}
