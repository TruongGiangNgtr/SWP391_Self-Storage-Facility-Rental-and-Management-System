import { EmployeeEntityLabel } from '../components/EmployeeEntityLabel'
import { EmployeeModal } from '../components/EmployeeModal'
import { Camera, CheckCircle2, Eye, RefreshCw, RotateCcw, ShieldAlert, WalletCards } from 'lucide-react'
import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { businessApi } from '../api/businessApi'
import { inspectionApi } from '../api/inspectionApi'
import { visitApi } from '../api/visitApi'
import { useAuth } from '../auth/auth.context'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { StatusBadge } from '../components/StatusBadge'
import type { StaffWorkItem } from '../features/handover/handover.types'
import {
  formatEvidenceSize,
  getEvidencePreview,
  saveEvidencePreview,
} from '../features/return-management/evidencePreviewStore'
import type {
  ConfirmActualReturnResult,
  DamageTypeDetail,
  EvidenceType,
  ExtraFeeTypeDetail,
  FinalizeReturnResult,
  FinalStorageUnitStatus,
  InspectionDetail,
  InspectionEvidenceDetail,
  InspectionListItem,
} from '../features/return-management/return.types'
import { fetchAllEmployeePages } from '../utils/employeePresentation'
import { formatMoney, formatUtcDateTime, getCurrentBusinessDate } from '../utils/formatters'
import '../styles/staff.css'

const PAGE_SIZE = 20

type ReturnTab = 'confirmation' | 'inspection' | 'fees'

const TAB_META: Record<ReturnTab, { label: string; description: string }> = {
  confirmation: {
    label: 'Return Confirmation',
    description: 'Check in the return visit and confirm the physical return date to begin inspection.',
  },
  inspection: {
    label: 'Return Inspection',
    description: 'Claim a pending inspection, attach condition evidence, record any damage or extra fees, and complete the inspection.',
  },
  fees: {
    label: 'Fees & Finalize',
    description: 'Review recorded damage and extra fees, verify Manager damage decisions, and finalize the return settlement.',
  },
}

function isReturnTab(value: string | null): value is ReturnTab {
  return value === 'confirmation' || value === 'inspection' || value === 'fees'
}

function ReturnConfirmationPanel() {
  const [searchParams, setSearchParams] = useSearchParams()
  const requestedVisitId = searchParams.get('visitId')
  const initialDate = searchParams.get('date') || getCurrentBusinessDate()
  const [date, setDate] = useState(initialDate)
  const [page, setPage] = useState(1)
  const [retry, setRetry] = useState(0)
  const [busyId, setBusyId] = useState<string | null>(null)
  const [returnDates, setReturnDates] = useState<Record<string, string>>({})
  const [error, setError] = useState<ApiErrorPresentation | null>(null)
  const [loadError, setLoadError] = useState<ApiErrorPresentation | null>(null)
  const [result, setResult] = useState<ConfirmActualReturnResult | null>(null)
  const [workItems, setWorkItems] = useState<StaffWorkItem[]>([])
  const [loading, setLoading] = useState(true)

  const load = useCallback(async () => {
    setLoading(true)
    setLoadError(null)
    try {
      const next = await fetchAllEmployeePages((nextPage, pageSize) => visitApi.listStaffWorkItems({ date, page: nextPage, pageSize }))
      setWorkItems(next.items)
    } catch (caughtError) {
      setLoadError(presentApiError(caughtError))
    } finally {
      setLoading(false)
    }
  }, [date])

  useEffect(() => { void load() }, [load, retry])

  const returns = useMemo(() => workItems.filter((item) => item.workType === 'RETURN_VISIT'), [workItems])
  const totalPages = Math.max(1, Math.ceil(returns.length / PAGE_SIZE))
  const safePage = Math.min(page, totalPages)
  const visibleReturns = useMemo(
    () => returns.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE),
    [returns, safePage],
  )

  async function checkIn(item: StaffWorkItem) {
    setBusyId(item.referenceId)
    setError(null)
    setResult(null)
    try {
      await visitApi.checkIn(item.referenceId)
    } catch (caughtError) {
      setError(presentApiError(caughtError))
    } finally {
      setBusyId(null)
      await load()
    }
  }

  async function confirm(item: StaffWorkItem) {
    const actualReturnDate = returnDates[item.referenceId] || date
    if (!actualReturnDate) {
      setError(presentValidationError('Choose the actual physical return date.'))
      return
    }

    setBusyId(item.referenceId)
    setError(null)
    setResult(null)
    try {
      const next = await visitApi.confirmReturn(item.referenceId, actualReturnDate)
      setResult(next.data)
    } catch (caughtError) {
      setError(presentApiError(caughtError))
    } finally {
      setBusyId(null)
      await load()
    }
  }

  return (
    <div className="fwp-return-panel">
      <section className="fwp-toolbar fwp-toolbar-return" aria-label="Return confirmation filters">
        <label>
          <span>Work date (GMT+7)</span>
          <input type="date" value={date} onChange={(event) => { setDate(event.target.value); setPage(1) }} />
        </label>
        <button className="fwp-button fwp-button-secondary" type="button" disabled={loading} onClick={() => setRetry((value) => value + 1)}>
          <RefreshCw size={16} /> Refresh
        </button>
        <p className="fwp-toolbar-note">Showing all return visits scheduled for the selected date.</p>
      </section>

      <ApiErrorAlert error={error ?? loadError} />

      {result && (
        <div className="fwp-banner fwp-banner-success">
          <CheckCircle2 size={18} />
          <div>
            <strong>{result.returnClassification} return confirmed.</strong>
            <span>Inspection is {result.inspectionStatus}; storage unit status is {result.storageUnitStatus}.</span>
          </div>
          <button type="button" className="fwp-link-button" onClick={() => setSearchParams({ tab: 'inspection', inspectionId: result.inspectionId })}>Open Inspection</button>
        </div>
      )}

      {loading ? (
        <div className="fwp-state-grid">{Array.from({ length: 4 }).map((_, index) => <div className="fwp-skeleton" key={index} />)}</div>
      ) : returns.length === 0 ? (
        <div className="fwp-state-card"><strong>No RETURN visits for this date</strong><p>There are no return visits scheduled for the selected date.</p></div>
      ) : (
        <section className="fwp-table-card">
          <div className="fwp-table-wrap">
            <table className="fwp-table" aria-label="Scheduled return visits">
              <thead><tr><th>Customer</th><th>Visit</th><th>Contract</th><th>Status</th><th>Actual return date</th><th>Action</th></tr></thead>
              <tbody>
                {visibleReturns.map((item) => (
                  <tr key={item.referenceId} className={requestedVisitId === item.referenceId ? 'fwp-row-selected' : ''}>
                    <td>{item.customer ? <div className="fwp-customer-cell"><strong>{item.customer.fullName}</strong><span>{item.customer.phoneNumber}</span></div> : <span className="fwp-muted">Customer context unavailable</span>}</td>
                    <td>Return visit · {item.scheduledDate}</td>
                    <td><EmployeeEntityLabel kind="contract" id={item.entityId} /></td>
                    <td><StatusBadge status={item.status} /></td>
                    <td>
                      {item.actualReturnDate ? <span>{item.actualReturnDate}</span> : item.status === 'CHECKED_IN' ? (
                        <input
                          className="fwp-inline-date"
                          type="date"
                          aria-label={`Actual return date for ${item.customer?.fullName ?? 'visit'}`}
                          value={returnDates[item.referenceId] ?? date}
                          onChange={(event) => setReturnDates((current) => ({ ...current, [item.referenceId]: event.target.value }))}
                          disabled={busyId === item.referenceId}
                        />
                      ) : <span className="fwp-muted">Confirm after check-in</span>}
                    </td>
                    <td>
                      {item.actualReturnDate ? (
                        <button className="fwp-button fwp-button-secondary" type="button" onClick={() => setSearchParams(item.inspectionId ? { tab: 'inspection', inspectionId: item.inspectionId } : { tab: 'inspection' })}>Confirmed — {item.inspectionId ? 'Open Inspection' : 'Review Inspections'}</button>
                      ) : item.status === 'SCHEDULED' ? (
                        <button className="fwp-button fwp-button-primary" type="button" disabled={busyId !== null} onClick={() => void checkIn(item)}>{busyId === item.referenceId ? 'Checking in…' : 'Check In'}</button>
                      ) : item.status === 'CHECKED_IN' ? (
                        <button className="fwp-button fwp-button-primary" type="button" disabled={busyId !== null} onClick={() => void confirm(item)}>{busyId === item.referenceId ? 'Confirming…' : 'Confirm Return'}</button>
                      ) : <span className="fwp-muted">No action</span>}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {totalPages > 1 && (
            <footer className="fwp-pagination">
              <span>Page {safePage} of {totalPages} · {returns.length} return visits</span>
              <div>
                <button className="fwp-button fwp-button-secondary" type="button" disabled={safePage <= 1} onClick={() => setPage(safePage - 1)}>Previous</button>
                <button className="fwp-button fwp-button-secondary" type="button" disabled={safePage >= totalPages} onClick={() => setPage(safePage + 1)}>Next</button>
              </div>
            </footer>
          )}
        </section>
      )}
    </div>
  )
}

interface InspectionWorkspaceProps {
  mode: 'inspection' | 'fees'
}

function InspectionWorkspace({ mode }: InspectionWorkspaceProps) {
  const { user } = useAuth()
  const [searchParams, setSearchParams] = useSearchParams()
  const requestedInspectionId = searchParams.get('inspectionId')
  const [page, setPage] = useState(1)
  const [retry, setRetry] = useState(0)
  const [inspections, setInspections] = useState<InspectionListItem[]>([])
  const [loadingList, setLoadingList] = useState(true)
  const [selectedId, setSelectedId] = useState<string | null>(requestedInspectionId)
  const [detail, setDetail] = useState<InspectionDetail | null>(null)
  const [loadingDetail, setLoadingDetail] = useState(false)
  const [busy, setBusy] = useState<string | null>(null)
  const [error, setError] = useState<ApiErrorPresentation | null>(null)
  const [loadError, setLoadError] = useState<ApiErrorPresentation | null>(null)
  const [conditionNote, setConditionNote] = useState('')
  const [evidenceType, setEvidenceType] = useState<EvidenceType>('IMAGE')
  const [evidenceFile, setEvidenceFile] = useState<File | null>(null)
  const [damageTypes, setDamageTypes] = useState<DamageTypeDetail[]>([])
  const [extraFeeTypes, setExtraFeeTypes] = useState<ExtraFeeTypeDetail[]>([])
  const [damageTypeId, setDamageTypeId] = useState('')
  const [damageAmount, setDamageAmount] = useState('')
  const [damageNote, setDamageNote] = useState('')
  const [extraFeeTypeId, setExtraFeeTypeId] = useState('')
  const [extraFeeAmount, setExtraFeeAmount] = useState('')
  const [extraFeeReason, setExtraFeeReason] = useState('')
  const [finalStatus, setFinalStatus] = useState<FinalStorageUnitStatus>('AVAILABLE')
  const [finalResult, setFinalResult] = useState<FinalizeReturnResult | null>(null)
  const [confirmCompleteOpen, setConfirmCompleteOpen] = useState(false)
  const [previewEvidenceItem, setPreviewEvidenceItem] = useState<InspectionEvidenceDetail | null>(null)

  const loadList = useCallback(async () => {
    setLoadingList(true)
    setLoadError(null)
    try {
      const next = await fetchAllEmployeePages((nextPage, pageSize) => inspectionApi.list(nextPage, pageSize))
      setInspections(next.items)
      if (!selectedId && next.items.length > 0) setSelectedId(next.items[0].inspectionId)
    } catch (caughtError) {
      setLoadError(presentApiError(caughtError))
    } finally {
      setLoadingList(false)
    }
  }, [selectedId])

  const loadDetail = useCallback(async (inspectionId: string) => {
    setLoadingDetail(true)
    setLoadError(null)
    try {
      const next = await inspectionApi.get(inspectionId)
      setDetail(next.data)
      setConditionNote(next.data.conditionNote ?? '')
    } catch (caughtError) {
      setDetail(null)
      setLoadError(presentApiError(caughtError))
    } finally {
      setLoadingDetail(false)
    }
  }, [])

  useEffect(() => { void loadList() }, [loadList, retry])
  useEffect(() => { if (selectedId) void loadDetail(selectedId) }, [loadDetail, selectedId, retry])

  useEffect(() => {
    let active = true
    Promise.all([inspectionApi.listDamageTypes(), businessApi.listExtraFeeTypes(1, 100)])
      .then(([damageResponse, feeResponse]) => {
        if (!active) return
        setDamageTypes(damageResponse.data.filter((item) => item.status === 'ACTIVE'))
        setExtraFeeTypes(feeResponse.data.filter((item) => item.status === 'ACTIVE'))
      })
      .catch((caughtError) => { if (active) setLoadError(presentApiError(caughtError)) })
    return () => { active = false }
  }, [retry])

  const totalPages = Math.max(1, Math.ceil(inspections.length / PAGE_SIZE))
  const safePage = Math.min(page, totalPages)
  const visibleInspections = useMemo(
    () => inspections.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE),
    [inspections, safePage],
  )

  function chooseInspection(inspectionId: string) {
    setSelectedId(inspectionId)
    setError(null)
    setFinalResult(null)
    setConfirmCompleteOpen(false)
    setPreviewEvidenceItem(null)
    setSearchParams({ tab: mode, inspectionId })
  }

  async function runAction(name: string, action: () => Promise<unknown>, refresh = true): Promise<boolean> {
    setBusy(name)
    setError(null)
    let succeeded = false
    try {
      await action()
      succeeded = true
    } catch (caughtError) {
      setError(presentApiError(caughtError))
    } finally {
      // Refresh errors must never replace or hide the mutation error.
      if (refresh && selectedId) await loadDetail(selectedId)
      await loadList()
      setBusy(null)
    }
    return succeeded
  }

  async function uploadEvidence(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!selectedId || !evidenceFile) {
      setError(presentValidationError('Choose an evidence file before uploading.'))
      return
    }
    const fileToUpload = evidenceFile
    if (await runAction('evidence', async () => {
      const response = await inspectionApi.uploadEvidence(selectedId, fileToUpload, evidenceType)
      await saveEvidencePreview(response.data, fileToUpload)
    })) {
      setEvidenceFile(null)
    }
  }

  function requestCompleteInspection(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!selectedId || !conditionNote.trim()) {
      setError(presentValidationError('Condition note is required to complete the Inspection.'))
      return
    }
    setError(null)
    setConfirmCompleteOpen(true)
  }

  async function confirmCompleteInspection() {
    if (!selectedId || !conditionNote.trim()) return
    const ok = await runAction('complete', () => inspectionApi.complete(selectedId, { conditionNote: conditionNote.trim() }))
    if (ok) setConfirmCompleteOpen(false)
  }

  async function recordDamage(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!selectedId || !damageTypeId || damageAmount.trim() === '') {
      setError(presentValidationError('Damage type and amount are required.'))
      return
    }
    const amount = Number(damageAmount)
    if (!Number.isFinite(amount) || amount < 0) {
      setError(presentValidationError('Damage amount must be a non-negative number.'))
      return
    }
    if (await runAction('damage', () => inspectionApi.recordDamage(selectedId, {
      damageTypeId,
      damageAmount: amount,
      note: damageNote.trim() || null,
    }))) {
      setDamageTypeId('')
      setDamageAmount('')
      setDamageNote('')
    }
  }

  async function recordExtraFee(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!selectedId || !extraFeeTypeId || extraFeeAmount.trim() === '' || !extraFeeReason.trim()) {
      setError(presentValidationError('Extra fee type, amount and reason are required.'))
      return
    }
    const amount = Number(extraFeeAmount)
    if (!Number.isFinite(amount) || amount < 0) {
      setError(presentValidationError('Extra fee amount must be a non-negative number.'))
      return
    }
    if (await runAction('extra-fee', () => inspectionApi.recordExtraFee(selectedId, {
      extraFeeTypeId,
      amount,
      reason: extraFeeReason.trim(),
    }))) {
      setExtraFeeTypeId('')
      setExtraFeeAmount('')
      setExtraFeeReason('')
    }
  }

  async function finalizeReturn() {
    if (!detail) return
    setBusy('finalize')
    setError(null)
    setFinalResult(null)
    try {
      const response = await inspectionApi.finalizeReturn(detail.contractId, { storageUnitStatus: finalStatus })
      setFinalResult(response.data)
    } catch (caughtError) {
      setError(presentApiError(caughtError))
    } finally {
      // Keep the authoritative operation error and settlement result through refresh.
      await loadDetail(detail.inspectionId)
      await loadList()
      setBusy(null)
    }
  }

  const currentStaffOwnsInspection = Boolean(detail?.employeeId && user?.employeeId && detail.employeeId === user.employeeId)
  const canEditInspection = detail?.status === 'IN_PROGRESS' && currentStaffOwnsInspection
  const pendingDamageCount = detail?.damages.filter((item) => item.status === 'PENDING').length ?? 0
  const previewRecord = previewEvidenceItem ? getEvidencePreview(previewEvidenceItem.inspectionEvidenceId) : null
  const previewUrl = previewRecord?.objectUrl || previewRecord?.dataUrl || null

  function renderChargeEntryCards() {
    if (!canEditInspection) return null
    return (
      <div className="fwp-fee-grid">
        <section className="fwp-action-card">
          <h3>Record Damage</h3>
          <p>Recorded damage starts as PENDING until a Facility Manager approves or rejects it.</p>
          <form className="fwp-stack-form" onSubmit={recordDamage}>
            <label><span>Damage type</span><select value={damageTypeId} onChange={(event) => {
              const id = event.target.value
              setDamageTypeId(id)
              const found = damageTypes.find((item) => item.damageTypeId === id)
              setDamageAmount(found?.defaultAmount == null ? '' : String(found.defaultAmount))
            }}><option value="">Select damage type</option>{damageTypes.map((item) => <option value={item.damageTypeId} key={item.damageTypeId}>{item.name}</option>)}</select></label>
            <label><span>Damage amount (VND)</span><input type="number" min="0" step="0.01" value={damageAmount} onChange={(event) => setDamageAmount(event.target.value)} /></label>
            <label><span>Note (optional)</span><textarea rows={3} value={damageNote} onChange={(event) => setDamageNote(event.target.value)} /></label>
            <button className="fwp-button fwp-button-secondary" type="submit" disabled={busy !== null}>{busy === 'damage' ? 'Recording…' : 'Record Damage'}</button>
          </form>
        </section>

        <section className="fwp-action-card">
          <h3>Record Extra Fee</h3>
          <p>Select a fee from the active catalogue and enter the exact VND charge and reason.</p>
          <form className="fwp-stack-form" onSubmit={recordExtraFee}>
            <label><span>Extra fee type</span><select value={extraFeeTypeId} onChange={(event) => {
              const id = event.target.value
              setExtraFeeTypeId(id)
              const found = extraFeeTypes.find((item) => item.extraFeeTypeId === id)
              setExtraFeeAmount(found ? String(found.defaultAmount) : '')
            }}><option value="">Select extra fee type</option>{extraFeeTypes.map((item) => <option value={item.extraFeeTypeId} key={item.extraFeeTypeId}>{item.name}</option>)}</select></label>
            <label><span>Amount (VND)</span><input type="number" min="0" step="0.01" value={extraFeeAmount} onChange={(event) => setExtraFeeAmount(event.target.value)} /></label>
            <label><span>Reason</span><textarea rows={3} value={extraFeeReason} onChange={(event) => setExtraFeeReason(event.target.value)} /></label>
            <button className="fwp-button fwp-button-secondary" type="submit" disabled={busy !== null}>{busy === 'extra-fee' ? 'Recording…' : 'Record Extra Fee'}</button>
          </form>
        </section>
      </div>
    )
  }

  function renderRecordedChargesList() {
    if (!detail || (detail.damages.length === 0 && detail.extraFees.length === 0)) return null
    return (
      <section className="fwp-action-card">
        <h3>Recorded fees and Damage</h3>
        <div className="fwp-record-list">
          {detail.damages.map((item) => (
            <div key={item.damageRecordId}>
              <span>Damage · {item.damageTypeName || damageTypes.find(type => type.damageTypeId === item.damageTypeId)?.name || 'Damage type unavailable'}{item.note ? ` — ${item.note}` : ''}</span>
              <strong>{formatMoney(item.damageAmount)}</strong>
              <StatusBadge status={item.status} />
            </div>
          ))}
          {detail.extraFees.map((item) => (
            <div key={item.extraFeeId}>
              <span>Extra Fee · {extraFeeTypes.find(type => type.extraFeeTypeId === item.extraFeeTypeId)?.name.replaceAll('_', ' ') || 'Fee type unavailable'}</span>
              <strong>{formatMoney(item.amount)}</strong>
              <span>{item.reason}</span>
            </div>
          ))}
        </div>
      </section>
    )
  }

  function renderEvidenceItemsList() {
    if (!detail || detail.evidence.length === 0) return null
    return (
      <div className="fwp-evidence-list" aria-label="Uploaded inspection evidence">
        {detail.evidence.map((item, index) => {
          const preview = getEvidencePreview(item.inspectionEvidenceId)
          const thumbUrl = preview?.objectUrl || preview?.dataUrl || null
          return (
            <div key={item.inspectionEvidenceId} className="fwp-evidence-row">
              <div className="fwp-evidence-meta">
                {item.evidenceType === 'IMAGE' && thumbUrl && (
                  <img className="fwp-evidence-thumb" src={thumbUrl} alt={preview?.fileName || `Evidence ${index + 1}`} />
                )}
                <div>
                  <strong>{preview?.fileName || `${item.evidenceType} evidence #${index + 1}`}</strong>
                  <span>
                    {item.evidenceType} · Uploaded {formatUtcDateTime(item.createdAt)}
                    {preview ? ` · ${formatEvidenceSize(preview.fileSize)}` : ''}
                  </span>
                </div>
              </div>
              <button
                type="button"
                className="fwp-button fwp-button-secondary"
                onClick={() => setPreviewEvidenceItem(item)}
              >
                <Eye size={14} /> Preview
              </button>
            </div>
          )
        })}
      </div>
    )
  }

  function renderCompleteInspectionCard() {
    if (!canEditInspection) return null
    return (
      <section className="fwp-action-card">
        <h3>Complete Inspection</h3>
        <p>Ensure all evidence, damage records, and extra fees are added above before completing. Completing locks the inspection against further charge entries; the unit stays under inspection until you finalize the return.</p>
        <form className="fwp-stack-form" onSubmit={requestCompleteInspection}>
          <label><span>Condition note</span><textarea rows={4} value={conditionNote} onChange={(event) => setConditionNote(event.target.value)} disabled={busy !== null} placeholder="Describe the physical condition of the unit upon return..." /></label>
          <button className="fwp-button fwp-button-primary" type="submit" disabled={busy !== null}>{busy === 'complete' ? 'Completing…' : 'Complete Inspection'}</button>
        </form>
      </section>
    )
  }

  return (
    <div className="fwp-inspection-layout">
      <section className="fwp-inspection-list-card" aria-label="Facility inspections">
        <div className="fwp-card-header">
          <div><strong>Facility Inspections</strong><span>{inspections.length} total</span></div>
          <button className="fwp-icon-button" type="button" aria-label="Refresh inspections" disabled={loadingList} onClick={() => setRetry((value) => value + 1)}><RefreshCw size={16} /></button>
        </div>
        {loadingList ? <div className="fwp-state-grid compact">{Array.from({ length: 4 }).map((_, index) => <div className="fwp-skeleton" key={index} />)}</div> : (
          <div className="fwp-inspection-list">
            {visibleInspections.map((item) => (
              <button key={item.inspectionId} type="button" className={selectedId === item.inspectionId ? 'fwp-inspection-item active' : 'fwp-inspection-item'} onClick={() => chooseInspection(item.inspectionId)}>
                <div><strong>{item.status.replaceAll('_', ' ')} inspection</strong><span><EmployeeEntityLabel kind="contract" id={item.contractId} /></span></div>
                <StatusBadge status={item.status} />
              </button>
            ))}
            {visibleInspections.length === 0 && <p className="fwp-muted">No inspections found.</p>}
          </div>
        )}
        {totalPages > 1 && <div className="fwp-mini-pagination"><button type="button" disabled={safePage <= 1} onClick={() => setPage(safePage - 1)}>‹</button><span>{safePage}/{totalPages}</span><button type="button" disabled={safePage >= totalPages} onClick={() => setPage(safePage + 1)}>›</button></div>}
      </section>

      <section className="fwp-inspection-detail-card">
        <ApiErrorAlert error={error ?? loadError} />
        {loadingDetail ? <div className="fwp-state-card">Loading Inspection detail…</div> : !detail ? (
          <div className="fwp-state-card"><strong>Select an Inspection</strong><p>Choose a Facility-scoped Inspection from the list.</p></div>
        ) : (
          <>
            <div className="fwp-card-header fwp-detail-title">
              <div><span className="fwp-eyebrow">Inspection</span><h2>Inspection findings</h2></div>
              <StatusBadge status={detail.status} />
            </div>
            <dl className="fwp-detail-grid">
              <div><dt>Contract</dt><dd><EmployeeEntityLabel kind="contract" id={detail.contractId} /></dd></div>
              <div><dt>Storage Unit</dt><dd><EmployeeEntityLabel kind="contractUnit" id={detail.contractId} /></dd></div>
              <div><dt>RETURN Visit</dt><dd>{detail.visitId ?? 'External recovery / no Visit'}</dd></div>
              <div><dt>Claimed by</dt><dd><EmployeeEntityLabel kind="employee" id={detail.employeeId} empty={'Unclaimed'} /></dd></div>
              <div><dt>Completed</dt><dd>{detail.completedAt ? formatUtcDateTime(detail.completedAt) : 'Not completed'}</dd></div>
              <div><dt>Condition note</dt><dd>{detail.conditionNote ?? '—'}</dd></div>
            </dl>

            {mode === 'inspection' ? (
              <div className="fwp-workspace-stack">
                {detail.status === 'PENDING' && (
                  <section className="fwp-action-card">
                    <h3>Claim Inspection</h3>
                    <p>Claim this inspection before recording findings. If another staff member claims it first, refresh to see the current owner.</p>
                    <button className="fwp-button fwp-button-primary" type="button" disabled={busy !== null} onClick={() => void runAction('claim', () => inspectionApi.claim(detail.inspectionId))}>{busy === 'claim' ? 'Claiming…' : 'Claim Inspection'}</button>
                  </section>
                )}

                {detail.status === 'IN_PROGRESS' && !currentStaffOwnsInspection && <div className="fwp-banner fwp-banner-warning"><ShieldAlert size={18} /><span>This Inspection is already claimed by another Staff member. It is read-only for this account.</span></div>}

                {canEditInspection && (
                  <>
                    <div className="fwp-banner fwp-banner-warning">
                      <ShieldAlert size={18} />
                      <div><strong>Step 1: Evidence &amp; charges · Step 2: Complete inspection.</strong><span>Attach evidence and record any damage or extra fees below while the inspection is IN_PROGRESS. Once completed, charges can no longer be added.</span></div>
                    </div>
                    <section className="fwp-action-card">
                      <h3><Camera size={17} /> Evidence ({detail.evidence.length})</h3>
                      <p>Attach photos, videos, or documents of the unit’s condition. Uploaded files are saved immediately to this inspection.</p>
                      <form className="fwp-inline-form" onSubmit={uploadEvidence}>
                        <label className="form-field">
                          <span>Evidence type</span>
                          <select value={evidenceType} onChange={(event) => setEvidenceType(event.target.value as EvidenceType)} disabled={busy !== null}>
                            <option value="IMAGE">Photo (IMAGE)</option><option value="VIDEO">Video (VIDEO)</option><option value="DOCUMENT">Document (DOCUMENT)</option>
                          </select>
                        </label>
                        <label className="form-field">
                          <span>Evidence file</span>
                          <input type="file" onChange={(event) => setEvidenceFile(event.target.files?.[0] ?? null)} disabled={busy !== null} />
                        </label>
                        <button className="fwp-button fwp-button-secondary" type="submit" disabled={busy !== null}>{busy === 'evidence' ? 'Uploading…' : 'Upload'}</button>
                      </form>
                      {renderEvidenceItemsList()}
                    </section>
                    {renderChargeEntryCards()}
                    {renderRecordedChargesList()}
                    {renderCompleteInspectionCard()}
                  </>
                )}

                {!canEditInspection && detail.evidence.length > 0 && (
                  <section className="fwp-action-card">
                    <h3><Camera size={17} /> Evidence ({detail.evidence.length})</h3>
                    {renderEvidenceItemsList()}
                  </section>
                )}

                {detail.status === 'COMPLETED' && <div className="fwp-banner fwp-banner-success"><CheckCircle2 size={18} /><div><strong>Inspection completed.</strong><span>The unit remains under inspection. Review existing charges and Manager decisions before finalizing; no new fees can be recorded.</span></div><button className="fwp-link-button" type="button" onClick={() => setSearchParams({ tab: 'fees', inspectionId: detail.inspectionId })}>Open Fees &amp; Finalize</button></div>}
              </div>
            ) : (
              <div className="fwp-workspace-stack">
                {canEditInspection && <div className="fwp-banner fwp-banner-warning"><ShieldAlert size={18} /><div><strong>Inspection is still IN_PROGRESS.</strong><span>Record damage and extra fees below, then complete the inspection before finalizing the return.</span></div><button type="button" className="fwp-link-button" onClick={() => setSearchParams({ tab: 'inspection', inspectionId: detail.inspectionId })}>Open Return Inspection</button></div>}
                <section className="fwp-record-summary">
                  <div><span>Damage records</span><strong>{detail.damages.length}</strong></div>
                  <div><span>Pending decisions</span><strong>{pendingDamageCount}</strong></div>
                  <div><span>Extra fees</span><strong>{detail.extraFees.length}</strong></div>
                  <div><span>Evidence items</span><strong>{detail.evidence.length}</strong></div>
                </section>

                {renderChargeEntryCards()}
                {renderRecordedChargesList()}
                {detail.evidence.length > 0 && (
                  <section className="fwp-action-card">
                    <h3><Camera size={17} /> Evidence ({detail.evidence.length})</h3>
                    {renderEvidenceItemsList()}
                  </section>
                )}
                {renderCompleteInspectionCard()}

                <section className="fwp-action-card fwp-finalize-card">
                  <div><h3><WalletCards size={17} /> Finalize Return</h3><p>Review recorded charges and choose the unit’s next status. Finalizing records the settlement and closes the return together.</p></div>
                  {detail.status !== 'COMPLETED' && <div className="fwp-banner fwp-banner-warning"><ShieldAlert size={18} /><span>Inspection must be COMPLETED before Finalize Return.</span></div>}
                  {pendingDamageCount > 0 && <div className="fwp-banner fwp-banner-warning"><ShieldAlert size={18} /><span>{pendingDamageCount} Damage decision(s) are still PENDING. Manager resolution is required.</span></div>}
                  <label className="fwp-final-status"><span>Next storage unit status</span><select value={finalStatus} onChange={(event) => setFinalStatus(event.target.value as FinalStorageUnitStatus)} disabled={busy !== null}><option value="AVAILABLE">AVAILABLE</option><option value="MAINTENANCE">MAINTENANCE</option></select></label>
                  <button className="fwp-button fwp-button-primary" type="button" disabled={busy !== null || detail.status !== 'COMPLETED' || pendingDamageCount > 0} onClick={() => void finalizeReturn()}>{busy === 'finalize' ? 'Finalizing…' : 'Finalize Return'}</button>
                </section>

                {finalResult && (
                  <section className="fwp-settlement-card" aria-label="Authoritative settlement result">
                    <div className="fwp-card-header"><div><span className="fwp-eyebrow">Authoritative result</span><h3>Return Finalized</h3></div><StatusBadge status={finalResult.contractStatus} /></div>
                    <dl className="fwp-detail-grid">
                      <div><dt>Storage unit status</dt><dd>{finalResult.storageUnitStatus}</dd></div>
                      <div><dt>Settlement status</dt><dd>{finalResult.settlement.status}</dd></div>
                      <div><dt>Total deduction</dt><dd>{formatMoney(finalResult.settlement.totalDeduction)}</dd></div>
                      <div><dt>Refund amount</dt><dd>{formatMoney(finalResult.settlement.refundAmount)}</dd></div>
                      <div><dt>Additional amount due</dt><dd>{formatMoney(finalResult.settlement.additionalAmountDue)}</dd></div>
                    </dl>
                    <p className="fwp-muted">Actual deduction/refund/collection occurs outside FRMS. These values are recorded obligations only.</p>
                  </section>
                )}
              </div>
            )}
          </>
        )}
      </section>

      {confirmCompleteOpen && detail && (
        <EmployeeModal
          role="alertdialog"
          labelledBy="fwp-confirm-complete-title"
          busy={busy === 'complete'}
          onClose={() => setConfirmCompleteOpen(false)}
        >
          <div
            className="fwp-modal-backdrop"
            role="presentation"
            onMouseDown={(event) => {
              if (event.target === event.currentTarget && busy !== 'complete') setConfirmCompleteOpen(false)
            }}
          >
            <div className="fwp-modal-card">
              <h2 id="fwp-confirm-complete-title">Complete this inspection and lock charge entries?</h2>
              <p>
                Once you complete the inspection, no additional evidence, damage records, or extra fees can be added.
              </p>
              <div className="fwp-modal-summary">
                <div><span>Evidence files</span><strong>{detail.evidence.length}</strong></div>
                <div><span>Damage records</span><strong>{detail.damages.length}</strong></div>
                <div><span>Extra fees</span><strong>{detail.extraFees.length}</strong></div>
              </div>
              {detail.damages.length === 0 && detail.extraFees.length === 0 && (
                <div className="fwp-banner fwp-banner-warning">
                  <ShieldAlert size={18} />
                  <div>
                    <strong>No damage or extra fees recorded yet.</strong>
                    <span>If the unit has any damage or extra charges, cancel and record them before completing.</span>
                  </div>
                </div>
              )}
              <dl className="fwp-detail-grid">
                <div><dt>Contract</dt><dd><EmployeeEntityLabel kind="contract" id={detail.contractId} /></dd></div>
                <div><dt>Condition note</dt><dd>{conditionNote.trim()}</dd></div>
              </dl>
              <div className="fwp-modal-actions">
                <button
                  type="button"
                  className="fwp-button fwp-button-secondary"
                  disabled={busy === 'complete'}
                  onClick={() => setConfirmCompleteOpen(false)}
                >
                  Go back to edit
                </button>
                <button
                  type="button"
                  className="fwp-button fwp-button-primary"
                  disabled={busy === 'complete'}
                  onClick={() => void confirmCompleteInspection()}
                >
                  {busy === 'complete' ? 'Completing…' : 'Confirm & Complete Inspection'}
                </button>
              </div>
            </div>
          </div>
        </EmployeeModal>
      )}

      {previewEvidenceItem && detail && (
        <EmployeeModal
          labelledBy="fwp-evidence-preview-title"
          onClose={() => setPreviewEvidenceItem(null)}
        >
          <div
            className="fwp-modal-backdrop"
            role="presentation"
            onMouseDown={(event) => {
              if (event.target === event.currentTarget) setPreviewEvidenceItem(null)
            }}
          >
            <div className="fwp-modal-card">
              <h2 id="fwp-evidence-preview-title">
                {previewRecord?.fileName || `${previewEvidenceItem.evidenceType} Evidence Preview`}
              </h2>
              <p>
                Uploaded {formatUtcDateTime(previewEvidenceItem.createdAt)} · Type: {previewEvidenceItem.evidenceType}
                {previewRecord ? ` · ${formatEvidenceSize(previewRecord.fileSize)}` : ''}
              </p>
              <div className="fwp-evidence-preview-frame">
                {previewEvidenceItem.evidenceType === 'IMAGE' && previewUrl ? (
                  <img src={previewUrl} alt={previewRecord?.fileName || 'Inspection evidence preview'} />
                ) : previewEvidenceItem.evidenceType === 'VIDEO' && previewUrl ? (
                  <video src={previewUrl} controls />
                ) : previewUrl ? (
                  <a className="fwp-button fwp-button-secondary" href={previewUrl} download={previewRecord?.fileName || 'evidence'}>
                    Download / Open Document
                  </a>
                ) : (
                  <p className="fwp-muted">
                    Visual file preview is available in the uploading browser session. Server detail responses protect raw binary files and provide verified metadata below.
                  </p>
                )}
              </div>
              <dl className="fwp-detail-grid">
                <div><dt>Contract</dt><dd><EmployeeEntityLabel kind="contract" id={detail.contractId} /></dd></div>
                <div><dt>Storage Unit</dt><dd><EmployeeEntityLabel kind="contractUnit" id={detail.contractId} /></dd></div>
                <div><dt>Condition note</dt><dd>{detail.conditionNote || conditionNote || '—'}</dd></div>
                <div><dt>Claimed by</dt><dd><EmployeeEntityLabel kind="employee" id={detail.employeeId} empty={'Unclaimed'} /></dd></div>
              </dl>
              <div className="fwp-modal-actions">
                <button type="button" className="fwp-button fwp-button-secondary" onClick={() => setPreviewEvidenceItem(null)}>
                  Close preview
                </button>
              </div>
            </div>
          </div>
        </EmployeeModal>
      )}
    </div>
  )
}

export function StaffReturnsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const requestedTab = searchParams.get('tab')
  const tab: ReturnTab = isReturnTab(requestedTab) ? requestedTab : 'confirmation'
  const meta = TAB_META[tab]

  function changeTab(next: ReturnTab) {
    // Preserve the selected Inspection when toggling FWP-06/FWP-07.
    setSearchParams(current => {
      const nextParams = new URLSearchParams(current)
      nextParams.set('tab', next)
      if (next === 'confirmation') nextParams.delete('inspectionId')
      return nextParams
    })
  }

  return (
    <div className="fwp-page fwp-returns-page" data-testid="handle-returns-page">
      <header className="fwp-page-header">
        <div>
          <h1>Handle Returns</h1>
          <p>Confirm the physical return, inspect the unit, record fees and finalize the return.</p>
        </div>
        <RotateCcw size={30} className="fwp-header-icon" aria-hidden="true" />
      </header>

      <nav className="fwp-tabs" aria-label="Handle Returns stages">
        {(Object.keys(TAB_META) as ReturnTab[]).map((item) => (
          <button key={item} type="button" className={item === tab ? 'fwp-tab active' : 'fwp-tab'} onClick={() => changeTab(item)}>
            <strong>{TAB_META[item].label}</strong>
          </button>
        ))}
      </nav>

      <section className="fwp-stage-intro">
        <div><strong>{meta.label}</strong><p>{meta.description}</p></div>
      </section>

      {tab === 'confirmation' && <ReturnConfirmationPanel />}
      {tab === 'inspection' && <InspectionWorkspace mode="inspection" />}
      {tab === 'fees' && <InspectionWorkspace mode="fees" />}
    </div>
  )
}
