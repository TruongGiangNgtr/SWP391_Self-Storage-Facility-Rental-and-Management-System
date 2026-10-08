import { Camera, CheckCircle2, RefreshCw, RotateCcw, ShieldAlert, WalletCards } from 'lucide-react'
import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import type { ApiCollectionResponse } from '../api/api.types'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { businessApi } from '../api/businessApi'
import { inspectionApi } from '../api/inspectionApi'
import { visitApi } from '../api/visitApi'
import { useAuth } from '../auth/auth.context'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { StatusBadge } from '../components/StatusBadge'
import type { StaffWorkItem } from '../features/handover/handover.types'
import type {
  ConfirmActualReturnResult,
  DamageTypeDetail,
  EvidenceType,
  ExtraFeeTypeDetail,
  FinalizeReturnResult,
  FinalStorageUnitStatus,
  InspectionDetail,
} from '../features/return-management/return.types'
import { formatMoney, formatUtcDateTime, getCurrentBusinessDate } from '../utils/formatters'
import '../styles/staff.css'

type ReturnTab = 'confirmation' | 'inspection' | 'fees'

const TAB_META: Record<ReturnTab, { feature: string; label: string; description: string }> = {
  confirmation: {
    feature: 'FWP-05',
    label: 'Return Confirmation',
    description: 'Check in the RETURN Visit and confirm the actual physical return date. The server derives Normal vs Early Return and creates the PENDING Inspection atomically.',
  },
  inspection: {
    feature: 'FWP-06',
    label: 'Return Inspection',
    description: 'Claim a PENDING Inspection, attach evidence and complete the inspection. Completing an Inspection keeps the StorageUnit in INSPECTION.',
  },
  fees: {
    feature: 'FWP-07',
    label: 'Fees & Finalize',
    description: 'Record Damage/Extra Fee data during the claimed inspection, then finalize only after Inspection completion and all Damage decisions are resolved.',
  },
}

function isReturnTab(value: string | null): value is ReturnTab {
  return value === 'confirmation' || value === 'inspection' || value === 'fees'
}

function shortId(value: string) {
  return value.length <= 18 ? value : `${value.slice(0, 8)}…${value.slice(-6)}`
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
  const [result, setResult] = useState<ConfirmActualReturnResult | null>(null)
  const [response, setResponse] = useState<ApiCollectionResponse<StaffWorkItem> | null>(null)
  const [loading, setLoading] = useState(true)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const next = await visitApi.listStaffWorkItems({ date, page })
      setResponse(next)
    } catch (caughtError) {
      setError(presentApiError(caughtError))
    } finally {
      setLoading(false)
    }
  }, [date, page])

  useEffect(() => { void load() }, [load, retry])

  const returns = useMemo(() => (response?.data ?? []).filter((item) => item.workType === 'RETURN_VISIT'), [response])

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
        <p className="fwp-toolbar-note">OPS-001 remains the Facility Staff work source. This tab only narrows RETURN_VISIT rows on the current API page.</p>
      </section>

      <ApiErrorAlert error={error} />

      {result && (
        <div className="fwp-banner fwp-banner-success">
          <CheckCircle2 size={18} />
          <div>
            <strong>{result.returnClassification} return confirmed.</strong>
            <span>Inspection {shortId(result.inspectionId)} is {result.inspectionStatus}; StorageUnit is {result.storageUnitStatus}.</span>
          </div>
          <button type="button" className="fwp-link-button" onClick={() => setSearchParams({ tab: 'inspection', inspectionId: result.inspectionId })}>Open Inspection</button>
        </div>
      )}

      {loading ? (
        <div className="fwp-state-grid">{Array.from({ length: 4 }).map((_, index) => <div className="fwp-skeleton" key={index} />)}</div>
      ) : returns.length === 0 ? (
        <div className="fwp-state-card"><strong>No RETURN visits on this page</strong><p>There are no current-page return visits for the selected date.</p></div>
      ) : (
        <section className="fwp-table-card">
          <div className="fwp-table-wrap">
            <table className="fwp-table">
              <thead><tr><th>Customer</th><th>Visit</th><th>Contract</th><th>Status</th><th>Actual return date</th><th>Action</th></tr></thead>
              <tbody>
                {returns.map((item) => (
                  <tr key={item.referenceId} className={requestedVisitId === item.referenceId ? 'fwp-row-selected' : ''}>
                    <td>{item.customer ? <div className="fwp-customer-cell"><strong>{item.customer.fullName}</strong><span>{item.customer.phoneNumber}</span></div> : <span className="fwp-muted">Customer context unavailable</span>}</td>
                    <td><code>{shortId(item.referenceId)}</code></td>
                    <td><code>{item.entityId ? shortId(item.entityId) : '—'}</code></td>
                    <td><StatusBadge status={item.status} /></td>
                    <td>
                      {item.status === 'CHECKED_IN' ? (
                        <input
                          className="fwp-inline-date"
                          type="date"
                          value={returnDates[item.referenceId] ?? date}
                          onChange={(event) => setReturnDates((current) => ({ ...current, [item.referenceId]: event.target.value }))}
                          disabled={busyId === item.referenceId}
                        />
                      ) : <span className="fwp-muted">Confirm after check-in</span>}
                    </td>
                    <td>
                      {item.status === 'SCHEDULED' && <button className="fwp-button fwp-button-primary" type="button" disabled={busyId !== null} onClick={() => void checkIn(item)}>{busyId === item.referenceId ? 'Checking in…' : 'Check In'}</button>}
                      {item.status === 'CHECKED_IN' && <button className="fwp-button fwp-button-primary" type="button" disabled={busyId !== null} onClick={() => void confirm(item)}>{busyId === item.referenceId ? 'Confirming…' : 'Confirm Return'}</button>}
                      {item.status !== 'SCHEDULED' && item.status !== 'CHECKED_IN' && <span className="fwp-muted">No action</span>}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {response && response.pagination.totalPages > 1 && (
            <footer className="fwp-pagination">
              <span>Page {response.pagination.page} of {response.pagination.totalPages}</span>
              <div>
                <button className="fwp-button fwp-button-secondary" type="button" disabled={response.pagination.page <= 1} onClick={() => setPage(response.pagination.page - 1)}>Previous</button>
                <button className="fwp-button fwp-button-secondary" type="button" disabled={response.pagination.page >= response.pagination.totalPages} onClick={() => setPage(response.pagination.page + 1)}>Next</button>
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
  const [list, setList] = useState<ApiCollectionResponse<InspectionDetail> | null>(null)
  const [loadingList, setLoadingList] = useState(true)
  const [selectedId, setSelectedId] = useState<string | null>(requestedInspectionId)
  const [detail, setDetail] = useState<InspectionDetail | null>(null)
  const [loadingDetail, setLoadingDetail] = useState(false)
  const [busy, setBusy] = useState<string | null>(null)
  const [error, setError] = useState<ApiErrorPresentation | null>(null)
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

  const loadList = useCallback(async () => {
    setLoadingList(true)
    setError(null)
    try {
      const next = await inspectionApi.list(page)
      setList(next)
      if (!selectedId && next.data.length > 0) setSelectedId(next.data[0].inspectionId)
    } catch (caughtError) {
      setError(presentApiError(caughtError))
    } finally {
      setLoadingList(false)
    }
  }, [page, selectedId])

  const loadDetail = useCallback(async (inspectionId: string) => {
    setLoadingDetail(true)
    setError(null)
    try {
      const next = await inspectionApi.get(inspectionId)
      setDetail(next.data)
      setConditionNote(next.data.conditionNote ?? '')
    } catch (caughtError) {
      setDetail(null)
      setError(presentApiError(caughtError))
    } finally {
      setLoadingDetail(false)
    }
  }, [])

  useEffect(() => { void loadList() }, [loadList, retry])
  useEffect(() => { if (selectedId) void loadDetail(selectedId) }, [loadDetail, selectedId, retry])

  useEffect(() => {
    if (mode !== 'fees') return
    let active = true
    Promise.all([inspectionApi.listDamageTypes(), businessApi.listExtraFeeTypes(1, 100)])
      .then(([damageResponse, feeResponse]) => {
        if (!active) return
        setDamageTypes(damageResponse.data.filter((item) => item.status === 'ACTIVE'))
        setExtraFeeTypes(feeResponse.data.filter((item) => item.status === 'ACTIVE'))
      })
      .catch((caughtError) => { if (active) setError(presentApiError(caughtError)) })
    return () => { active = false }
  }, [mode, retry])

  function chooseInspection(inspectionId: string) {
    setSelectedId(inspectionId)
    setFinalResult(null)
    setSearchParams({ tab: mode, inspectionId })
  }

  async function runAction(name: string, action: () => Promise<unknown>, refresh = true) {
    setBusy(name)
    setError(null)
    try {
      await action()
      if (refresh && selectedId) await loadDetail(selectedId)
      await loadList()
    } catch (caughtError) {
      setError(presentApiError(caughtError))
      if (selectedId) await loadDetail(selectedId)
    } finally {
      setBusy(null)
    }
  }

  async function uploadEvidence(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!selectedId || !evidenceFile) {
      setError(presentValidationError('Choose an evidence file before uploading.'))
      return
    }
    await runAction('evidence', () => inspectionApi.uploadEvidence(selectedId, evidenceFile, evidenceType))
    setEvidenceFile(null)
  }

  async function completeInspection(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!selectedId || !conditionNote.trim()) {
      setError(presentValidationError('Condition note is required to complete the Inspection.'))
      return
    }
    await runAction('complete', () => inspectionApi.complete(selectedId, { conditionNote: conditionNote.trim() }))
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
    await runAction('damage', () => inspectionApi.recordDamage(selectedId, {
      damageTypeId,
      damageAmount: amount,
      note: damageNote.trim() || null,
    }))
    setDamageTypeId('')
    setDamageAmount('')
    setDamageNote('')
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
    await runAction('extra-fee', () => inspectionApi.recordExtraFee(selectedId, {
      extraFeeTypeId,
      amount,
      reason: extraFeeReason.trim(),
    }))
    setExtraFeeTypeId('')
    setExtraFeeAmount('')
    setExtraFeeReason('')
  }

  async function finalizeReturn() {
    if (!detail) return
    setBusy('finalize')
    setError(null)
    setFinalResult(null)
    try {
      const response = await inspectionApi.finalizeReturn(detail.contractId, { storageUnitStatus: finalStatus })
      setFinalResult(response.data)
      await loadDetail(detail.inspectionId)
      await loadList()
    } catch (caughtError) {
      setError(presentApiError(caughtError))
      await loadDetail(detail.inspectionId)
    } finally {
      setBusy(null)
    }
  }

  const currentStaffOwnsInspection = Boolean(detail?.employeeId && user?.employeeId && detail.employeeId === user.employeeId)
  const canEditInspection = detail?.status === 'IN_PROGRESS' && currentStaffOwnsInspection
  const pendingDamageCount = detail?.damages.filter((item) => item.status === 'PENDING').length ?? 0

  return (
    <div className="fwp-inspection-layout">
      <section className="fwp-inspection-list-card" aria-label="Facility inspections">
        <div className="fwp-card-header">
          <div><strong>Facility Inspections</strong><span>{list?.pagination.totalItems ?? 0} total</span></div>
          <button className="fwp-icon-button" type="button" aria-label="Refresh inspections" disabled={loadingList} onClick={() => setRetry((value) => value + 1)}><RefreshCw size={16} /></button>
        </div>
        {loadingList ? <div className="fwp-state-grid compact">{Array.from({ length: 4 }).map((_, index) => <div className="fwp-skeleton" key={index} />)}</div> : (
          <div className="fwp-inspection-list">
            {(list?.data ?? []).map((item) => (
              <button key={item.inspectionId} type="button" className={selectedId === item.inspectionId ? 'fwp-inspection-item active' : 'fwp-inspection-item'} onClick={() => chooseInspection(item.inspectionId)}>
                <div><strong>{shortId(item.inspectionId)}</strong><span>Contract {shortId(item.contractId)}</span></div>
                <StatusBadge status={item.status} />
              </button>
            ))}
            {list?.data.length === 0 && <p className="fwp-muted">No inspections on this API page.</p>}
          </div>
        )}
        {list && list.pagination.totalPages > 1 && <div className="fwp-mini-pagination"><button type="button" disabled={list.pagination.page <= 1} onClick={() => setPage(list.pagination.page - 1)}>‹</button><span>{list.pagination.page}/{list.pagination.totalPages}</span><button type="button" disabled={list.pagination.page >= list.pagination.totalPages} onClick={() => setPage(list.pagination.page + 1)}>›</button></div>}
      </section>

      <section className="fwp-inspection-detail-card">
        <ApiErrorAlert error={error} />
        {loadingDetail ? <div className="fwp-state-card">Loading Inspection detail…</div> : !detail ? (
          <div className="fwp-state-card"><strong>Select an Inspection</strong><p>Choose a Facility-scoped Inspection from the list.</p></div>
        ) : (
          <>
            <div className="fwp-card-header fwp-detail-title">
              <div><span className="fwp-eyebrow">Inspection</span><h2>{shortId(detail.inspectionId)}</h2></div>
              <StatusBadge status={detail.status} />
            </div>
            <dl className="fwp-detail-grid">
              <div><dt>Contract</dt><dd>{detail.contractId}</dd></div>
              <div><dt>Storage Unit</dt><dd>{detail.storageUnitId}</dd></div>
              <div><dt>RETURN Visit</dt><dd>{detail.visitId ?? 'External recovery / no Visit'}</dd></div>
              <div><dt>Claimed by</dt><dd>{detail.employeeId ?? 'Unclaimed'}</dd></div>
              <div><dt>Completed</dt><dd>{detail.completedAt ? formatUtcDateTime(detail.completedAt) : 'Not completed'}</dd></div>
              <div><dt>Condition note</dt><dd>{detail.conditionNote ?? '—'}</dd></div>
            </dl>

            {mode === 'inspection' ? (
              <div className="fwp-workspace-stack">
                {detail.status === 'PENDING' && (
                  <section className="fwp-action-card">
                    <h3>Claim Inspection</h3>
                    <p>Claim is atomic. Another Staff member can win the claim before this request commits.</p>
                    <button className="fwp-button fwp-button-primary" type="button" disabled={busy !== null} onClick={() => void runAction('claim', () => inspectionApi.claim(detail.inspectionId))}>{busy === 'claim' ? 'Claiming…' : 'Claim Inspection'}</button>
                  </section>
                )}

                {detail.status === 'IN_PROGRESS' && !currentStaffOwnsInspection && <div className="fwp-banner fwp-banner-warning"><ShieldAlert size={18} /><span>This Inspection is already claimed by another Staff member. It is read-only for this account.</span></div>}

                {canEditInspection && (
                  <>
                    <section className="fwp-action-card">
                      <h3><Camera size={17} /> Evidence</h3>
                      <p>Upload IMAGE, VIDEO or DOCUMENT evidence. Binary content is sent as multipart/form-data.</p>
                      <form className="fwp-inline-form" onSubmit={uploadEvidence}>
                        <select value={evidenceType} onChange={(event) => setEvidenceType(event.target.value as EvidenceType)} disabled={busy !== null}>
                          <option value="IMAGE">IMAGE</option><option value="VIDEO">VIDEO</option><option value="DOCUMENT">DOCUMENT</option>
                        </select>
                        <input type="file" onChange={(event) => setEvidenceFile(event.target.files?.[0] ?? null)} disabled={busy !== null} />
                        <button className="fwp-button fwp-button-secondary" type="submit" disabled={busy !== null}>{busy === 'evidence' ? 'Uploading…' : 'Upload'}</button>
                      </form>
                    </section>
                    <section className="fwp-action-card">
                      <h3>Complete Inspection</h3>
                      <p>INS-007 records completion only. The StorageUnit remains INSPECTION until Finalize Return.</p>
                      <form className="fwp-stack-form" onSubmit={completeInspection}>
                        <label><span>Condition note</span><textarea rows={4} value={conditionNote} onChange={(event) => setConditionNote(event.target.value)} disabled={busy !== null} /></label>
                        <button className="fwp-button fwp-button-primary" type="submit" disabled={busy !== null}>{busy === 'complete' ? 'Completing…' : 'Complete Inspection'}</button>
                      </form>
                    </section>
                  </>
                )}

                {detail.status === 'COMPLETED' && <div className="fwp-banner fwp-banner-success"><CheckCircle2 size={18} /><div><strong>Inspection completed.</strong><span>The Unit is still INSPECTION. Continue to Fees & Finalize for the atomic release.</span></div><button className="fwp-link-button" type="button" onClick={() => setSearchParams({ tab: 'fees', inspectionId: detail.inspectionId })}>Open Fees & Finalize</button></div>}
              </div>
            ) : (
              <div className="fwp-workspace-stack">
                <section className="fwp-record-summary">
                  <div><span>Damage records</span><strong>{detail.damages.length}</strong></div>
                  <div><span>Pending decisions</span><strong>{pendingDamageCount}</strong></div>
                  <div><span>Extra fees</span><strong>{detail.extraFees.length}</strong></div>
                  <div><span>Evidence items</span><strong>{detail.evidence.length}</strong></div>
                </section>

                {canEditInspection && (
                  <div className="fwp-fee-grid">
                    <section className="fwp-action-card">
                      <h3>Record Damage</h3>
                      <p>Staff records Damage as PENDING. Same-Facility Manager decides APPROVED or REJECTED.</p>
                      <form className="fwp-stack-form" onSubmit={recordDamage}>
                        <label><span>Damage type</span><select value={damageTypeId} onChange={(event) => {
                          const id = event.target.value
                          setDamageTypeId(id)
                          const found = damageTypes.find((item) => item.damageTypeId === id)
                          setDamageAmount(found?.defaultAmount == null ? '' : String(found.defaultAmount))
                        }}><option value="">Select active DamageType</option>{damageTypes.map((item) => <option value={item.damageTypeId} key={item.damageTypeId}>{item.name}</option>)}</select></label>
                        <label><span>Damage amount (VND)</span><input type="number" min="0" step="0.01" value={damageAmount} onChange={(event) => setDamageAmount(event.target.value)} /></label>
                        <label><span>Note (optional)</span><textarea rows={3} value={damageNote} onChange={(event) => setDamageNote(event.target.value)} /></label>
                        <button className="fwp-button fwp-button-secondary" type="submit" disabled={busy !== null}>{busy === 'damage' ? 'Recording…' : 'Record Damage'}</button>
                      </form>
                    </section>

                    <section className="fwp-action-card">
                      <h3>Record Extra Fee</h3>
                      <p>Use the active server catalogue. The amount remains explicit and is not recalculated by the frontend.</p>
                      <form className="fwp-stack-form" onSubmit={recordExtraFee}>
                        <label><span>Extra fee type</span><select value={extraFeeTypeId} onChange={(event) => {
                          const id = event.target.value
                          setExtraFeeTypeId(id)
                          const found = extraFeeTypes.find((item) => item.extraFeeTypeId === id)
                          setExtraFeeAmount(found ? String(found.defaultAmount) : '')
                        }}><option value="">Select active ExtraFeeType</option>{extraFeeTypes.map((item) => <option value={item.extraFeeTypeId} key={item.extraFeeTypeId}>{item.name}</option>)}</select></label>
                        <label><span>Amount (VND)</span><input type="number" min="0" step="0.01" value={extraFeeAmount} onChange={(event) => setExtraFeeAmount(event.target.value)} /></label>
                        <label><span>Reason</span><textarea rows={3} value={extraFeeReason} onChange={(event) => setExtraFeeReason(event.target.value)} /></label>
                        <button className="fwp-button fwp-button-secondary" type="submit" disabled={busy !== null}>{busy === 'extra-fee' ? 'Recording…' : 'Record Extra Fee'}</button>
                      </form>
                    </section>
                  </div>
                )}

                {(detail.damages.length > 0 || detail.extraFees.length > 0) && (
                  <section className="fwp-action-card">
                    <h3>Recorded fees and Damage</h3>
                    <div className="fwp-record-list">
                      {detail.damages.map((item) => <div key={item.damageRecordId}><span>Damage · {shortId(item.damageTypeId)}</span><strong>{formatMoney(item.damageAmount)}</strong><StatusBadge status={item.status} /></div>)}
                      {detail.extraFees.map((item) => <div key={item.extraFeeId}><span>Extra Fee · {shortId(item.extraFeeTypeId)}</span><strong>{formatMoney(item.amount)}</strong><span>{item.reason}</span></div>)}
                    </div>
                  </section>
                )}

                <section className="fwp-action-card fwp-finalize-card">
                  <div><h3><WalletCards size={17} /> Atomic Finalize Return</h3><p>Settlement amounts are server-calculated. The chosen Unit state is a command parameter only and is committed together with terminal Contract/settlement state.</p></div>
                  {detail.status !== 'COMPLETED' && <div className="fwp-banner fwp-banner-warning"><ShieldAlert size={18} /><span>Inspection must be COMPLETED before Finalize Return.</span></div>}
                  {pendingDamageCount > 0 && <div className="fwp-banner fwp-banner-warning"><ShieldAlert size={18} /><span>{pendingDamageCount} Damage decision(s) are still PENDING. Manager resolution is required.</span></div>}
                  <label className="fwp-final-status"><span>Final StorageUnit status</span><select value={finalStatus} onChange={(event) => setFinalStatus(event.target.value as FinalStorageUnitStatus)} disabled={busy !== null}><option value="AVAILABLE">AVAILABLE</option><option value="MAINTENANCE">MAINTENANCE</option></select></label>
                  <button className="fwp-button fwp-button-primary" type="button" disabled={busy !== null || detail.status !== 'COMPLETED' || pendingDamageCount > 0} onClick={() => void finalizeReturn()}>{busy === 'finalize' ? 'Finalizing…' : 'Finalize Return'}</button>
                </section>

                {finalResult && (
                  <section className="fwp-settlement-card" aria-label="Authoritative settlement result">
                    <div className="fwp-card-header"><div><span className="fwp-eyebrow">Authoritative result</span><h3>Return Finalized</h3></div><StatusBadge status={finalResult.contractStatus} /></div>
                    <dl className="fwp-detail-grid">
                      <div><dt>StorageUnit status</dt><dd>{finalResult.storageUnitStatus}</dd></div>
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
    </div>
  )
}

export function StaffReturnsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const requestedTab = searchParams.get('tab')
  const tab: ReturnTab = isReturnTab(requestedTab) ? requestedTab : 'confirmation'
  const meta = TAB_META[tab]

  function changeTab(next: ReturnTab) {
    setSearchParams({ tab: next })
  }

  return (
    <div className="fwp-page fwp-returns-page" data-testid="handle-returns-page">
      <header className="fwp-page-header">
        <div>
          <p className="fwp-eyebrow">Facility Staff · FWP-05 → FWP-07</p>
          <h1>Handle Returns</h1>
          <p>One operational workspace for physical return confirmation, claim-based Inspection, fee recording and atomic return finalization.</p>
        </div>
        <RotateCcw size={30} className="fwp-header-icon" aria-hidden="true" />
      </header>

      <nav className="fwp-tabs" aria-label="Handle Returns stages">
        {(Object.keys(TAB_META) as ReturnTab[]).map((item) => (
          <button key={item} type="button" className={item === tab ? 'fwp-tab active' : 'fwp-tab'} onClick={() => changeTab(item)}>
            <span>{TAB_META[item].feature}</span>
            <strong>{TAB_META[item].label}</strong>
          </button>
        ))}
      </nav>

      <section className="fwp-stage-intro">
        <span>{meta.feature}</span>
        <div><strong>{meta.label}</strong><p>{meta.description}</p></div>
      </section>

      {tab === 'confirmation' && <ReturnConfirmationPanel />}
      {tab === 'inspection' && <InspectionWorkspace mode="inspection" />}
      {tab === 'fees' && <InspectionWorkspace mode="fees" />}
    </div>
  )
}
