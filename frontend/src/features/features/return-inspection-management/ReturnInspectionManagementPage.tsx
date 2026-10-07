import {
  AlertTriangle,
  BadgeCheck,
  Check,
  CheckCircle2,
  ClipboardCheck,
  Eye,
  FileCheck2,
  FileText,
  RefreshCw,
  Search,
  ShieldAlert,
  ShieldCheck,
  X,
  XCircle,
} from 'lucide-react'
import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react'
import {
  inspectionApi,
  type DamageDecision,
  type DamageRecord,
  type DamageRecordStatus,
  type DamageType,
  type InspectionDetail,
  type InspectionListItem,
  type InspectionStatus,
} from '../../api/inspectionApi'
import { ApiRequestError } from '../../api/httpClient'
import {
  reportApi,
  type FacilityOperationsReport,
} from '../../api/reportApi'
import { useAuth } from '../../auth/auth.context'
import { formatMoney, formatUtcDateTime } from '../../utils/formatters'

type LinkageFilter = 'ALL' | 'RETURN_VISIT' | 'EXTERNAL_RECOVERY'

type UiMessage = {
  message: string
  traceId?: string
  details?: string[]
}

type PendingDecision = {
  damage: DamageRecord
  decision: DamageDecision
}

interface ManagerDialogProps {
  title: string
  eyebrow: string
  description?: string
  busy?: boolean
  onClose(): void
  children: ReactNode
}

const INSPECTION_STATUS_OPTIONS: Array<{
  value: 'ALL' | InspectionStatus
  label: string
}> = [
  { value: 'ALL', label: 'All statuses' },
  { value: 'PENDING', label: 'Pending' },
  { value: 'IN_PROGRESS', label: 'In progress' },
  { value: 'COMPLETED', label: 'Completed' },
]

const LINKAGE_OPTIONS: Array<{
  value: LinkageFilter
  label: string
}> = [
  { value: 'ALL', label: 'All return sources' },
  { value: 'RETURN_VISIT', label: 'RETURN Visit linked' },
  { value: 'EXTERNAL_RECOVERY', label: 'External recovery' },
]

function ManagerDialog({
  title,
  eyebrow,
  description,
  busy = false,
  onClose,
  children,
}: ManagerDialogProps) {
  const ref = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    ref.current?.showModal()
    return () => ref.current?.close()
  }, [])

  return (
    <dialog
      ref={ref}
      className="manager-dialog return-manager-dialog"
      aria-labelledby="return-manager-dialog-title"
      onCancel={(event) => {
        event.preventDefault()
        if (!busy) {
          onClose()
        }
      }}
    >
      <div className="manager-dialog-heading">
        <div>
          <span>{eyebrow}</span>
          <h2 id="return-manager-dialog-title">{title}</h2>
          {description && <p>{description}</p>}
        </div>
        <button
          type="button"
          className="manager-dialog-close"
          onClick={onClose}
          disabled={busy}
          aria-label="Close dialog"
        >
          <X size={18} />
        </button>
      </div>
      {children}
    </dialog>
  )
}

function shortId(value: string) {
  return value.length <= 14
    ? value
    : `${value.slice(0, 8)}…${value.slice(-4)}`
}

function inspectionStatusLabel(status: InspectionStatus) {
  return status === 'IN_PROGRESS' ? 'IN PROGRESS' : status
}

function inspectionStatusClass(status: InspectionStatus) {
  return `return-status return-status-${status.toLowerCase().replace('_', '-')}`
}

function damageStatusClass(status: DamageRecordStatus) {
  return `return-damage-status return-damage-status-${status.toLowerCase()}`
}

function friendlyDamageType(name: string) {
  return name
    .toLowerCase()
    .split('_')
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(' ')
}

function apiErrorMessage(error: unknown, fallback: string): UiMessage {
  if (!(error instanceof ApiRequestError)) {
    return { message: fallback }
  }

  const details = error.errors ? Object.values(error.errors).flat() : undefined
  const messages: Record<string, string> = {
    UNAUTHORIZED: 'Your session is invalid or has expired.',
    FORBIDDEN: 'This resource is outside your assigned Facility or Facility Manager permission.',
    RESOURCE_NOT_FOUND: 'The requested Inspection or DamageRecord was not found. Refresh the authoritative data.',
    INSPECTION_INVALID_STATUS: 'The Inspection changed state and this operation no longer matches its lifecycle.',
    DAMAGE_INVALID_STATUS: 'This DamageRecord is no longer PENDING. A terminal decision may already exist.',
    DAMAGE_DECISION_PENDING: 'Return finalization is blocked until every PENDING DamageRecord is decided.',
    ENDPOINT_NOT_IMPLEMENTED: 'This MWP-04 endpoint is not implemented by the current backend yet.',
    VALIDATION_ERROR: 'The request was rejected by server validation.',
  }

  if (error.status === 401) {
    return { message: messages.UNAUTHORIZED, traceId: error.traceId, details }
  }

  if (error.status === 403) {
    return { message: messages.FORBIDDEN, traceId: error.traceId, details }
  }

  return {
    message: messages[error.code]
      ?? (error.status === 409
        ? 'The server state changed before this action completed. Refresh and review the current lifecycle.'
        : error.status === 404
          ? messages.RESOURCE_NOT_FOUND
          : fallback),
    traceId: error.traceId,
    details,
  }
}

function MessageBlock({ message }: { message: UiMessage | null }) {
  if (!message) {
    return null
  }

  return (
    <div className="handover-alert handover-alert-error physical-form-error" role="alert">
      <strong>{message.message}</strong>
      {message.details?.map((detail) => <span key={detail}>{detail}</span>)}
      {message.traceId && <small>Trace: {message.traceId}</small>}
    </div>
  )
}

export function ReturnInspectionManagementPage() {
  const { user } = useAuth()
  const facilityId = user?.facilityId ?? null

  const [inspections, setInspections] = useState<InspectionListItem[]>([])
  const [damageTypes, setDamageTypes] = useState<DamageType[]>([])
  const [operationsReport, setOperationsReport] = useState<FacilityOperationsReport | null>(null)
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<'ALL' | InspectionStatus>('ALL')
  const [linkageFilter, setLinkageFilter] = useState<LinkageFilter>('ALL')
  const [isLoading, setIsLoading] = useState(true)
  const [pageError, setPageError] = useState<UiMessage | null>(null)
  const [catalogWarning, setCatalogWarning] = useState<UiMessage | null>(null)
  const [reportWarning, setReportWarning] = useState<UiMessage | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const [activeInspectionId, setActiveInspectionId] = useState<string | null>(null)
  const [selectedInspection, setSelectedInspection] = useState<InspectionDetail | null>(null)
  const [isLoadingDetail, setIsLoadingDetail] = useState(false)
  const [dialogError, setDialogError] = useState<UiMessage | null>(null)
  const [pendingDecision, setPendingDecision] = useState<PendingDecision | null>(null)
  const [isDeciding, setIsDeciding] = useState(false)

  const loadInspections = useCallback(async () => {
    if (!facilityId) {
      setInspections([])
      setPageError({
        message: 'Your Facility Manager account has no Facility assignment. MWP-04 requires a Facility-scoped Manager account.',
      })
      setIsLoading(false)
      return
    }

    setIsLoading(true)
    setPageError(null)

    try {
      const data = await inspectionApi.listAll()
      setInspections(data)
    } catch (error) {
      setInspections([])
      setPageError(apiErrorMessage(error, 'Unable to load Inspections for your assigned Facility.'))
    } finally {
      setIsLoading(false)
    }
  }, [facilityId])

  const loadDamageTypes = useCallback(async () => {
    if (!facilityId) {
      setDamageTypes([])
      return
    }

    setCatalogWarning(null)
    try {
      const response = await inspectionApi.listDamageTypes()
      setDamageTypes(response.data)
    } catch (error) {
      setDamageTypes([])
      setCatalogWarning(apiErrorMessage(
        error,
        'Damage Type catalogue is unavailable. Damage decisions remain visible by DamageType ID.',
      ))
    }
  }, [facilityId])

  const loadOperationsReport = useCallback(async () => {
    if (!facilityId) {
      setOperationsReport(null)
      return
    }

    setReportWarning(null)
    try {
      const response = await reportApi.getFacilityOperations(facilityId)
      setOperationsReport(response.data)
    } catch (error) {
      setOperationsReport(null)
      setReportWarning(apiErrorMessage(
        error,
        'Facility operations snapshot is unavailable. Inspection records can still be monitored.',
      ))
    }
  }, [facilityId])

  const refresh = useCallback(async () => {
    setSuccessMessage(null)
    await Promise.all([
      loadInspections(),
      loadDamageTypes(),
      loadOperationsReport(),
    ])
  }, [loadDamageTypes, loadInspections, loadOperationsReport])

  useEffect(() => {
    void refresh()
  }, [refresh])

  const damageTypeById = useMemo(() => {
    const map = new Map<string, DamageType>()
    for (const damageType of damageTypes) {
      map.set(damageType.damageTypeId, damageType)
    }
    return map
  }, [damageTypes])

  const statusCounts = useMemo(() => {
    const counts: Record<InspectionStatus, number> = {
      PENDING: 0,
      IN_PROGRESS: 0,
      COMPLETED: 0,
    }

    for (const inspection of inspections) {
      counts[inspection.status] += 1
    }

    return counts
  }, [inspections])

  const filteredInspections = useMemo(() => {
    const normalizedSearch = search.trim().toLowerCase()
    const priority: Record<InspectionStatus, number> = {
      PENDING: 0,
      IN_PROGRESS: 1,
      COMPLETED: 2,
    }

    return [...inspections]
      .filter((inspection) => {
        if (statusFilter !== 'ALL' && inspection.status !== statusFilter) {
          return false
        }

        if (linkageFilter === 'RETURN_VISIT' && !inspection.visitId) {
          return false
        }

        if (linkageFilter === 'EXTERNAL_RECOVERY' && inspection.visitId) {
          return false
        }

        if (!normalizedSearch) {
          return true
        }

        return [
          inspection.inspectionId,
          inspection.contractId,
          inspection.storageUnitId,
          inspection.visitId ?? '',
          inspection.employeeId ?? '',
          inspection.conditionNote ?? '',
        ].some((value) => value.toLowerCase().includes(normalizedSearch))
      })
      .sort((a, b) => {
        const byStatus = priority[a.status] - priority[b.status]
        if (byStatus !== 0) {
          return byStatus
        }
        return a.inspectionId.localeCompare(b.inspectionId)
      })
  }, [inspections, linkageFilter, search, statusFilter])

  async function openDetail(inspection: InspectionListItem) {
    setActiveInspectionId(inspection.inspectionId)
    setSelectedInspection(null)
    setPendingDecision(null)
    setDialogError(null)
    setIsLoadingDetail(true)

    try {
      const response = await inspectionApi.get(inspection.inspectionId)
      setSelectedInspection(response.data)
    } catch (error) {
      setDialogError(apiErrorMessage(error, 'Unable to load the latest Inspection detail.'))
    } finally {
      setIsLoadingDetail(false)
    }
  }

  function closeDialog() {
    if (isLoadingDetail || isDeciding) {
      return
    }

    setActiveInspectionId(null)
    setSelectedInspection(null)
    setPendingDecision(null)
    setDialogError(null)
  }

  function beginDecision(damage: DamageRecord, decision: DamageDecision) {
    if (damage.status !== 'PENDING') {
      setDialogError({
        message: 'Only a PENDING DamageRecord can be approved or rejected.',
      })
      return
    }

    setPendingDecision({ damage, decision })
    setDialogError(null)
  }

  async function reloadActiveInspection() {
    if (!activeInspectionId) {
      return
    }

    try {
      const response = await inspectionApi.get(activeInspectionId)
      setSelectedInspection(response.data)
    } catch (error) {
      setDialogError(apiErrorMessage(error, 'Unable to refresh the Inspection after the decision.'))
    }
  }

  async function handleDecision() {
    if (!pendingDecision || isDeciding) {
      return
    }

    setIsDeciding(true)
    setDialogError(null)

    try {
      await inspectionApi.decideDamage(
        pendingDecision.damage.damageRecordId,
        { decision: pendingDecision.decision },
      )

      setSuccessMessage(
        `Damage ${shortId(pendingDecision.damage.damageRecordId)} ${pendingDecision.decision === 'APPROVED' ? 'approved' : 'rejected'}.`,
      )
      setPendingDecision(null)

      await Promise.all([
        reloadActiveInspection(),
        loadInspections(),
        loadOperationsReport(),
      ])
    } catch (error) {
      const message = apiErrorMessage(error, 'Unable to save the Damage decision.')
      setDialogError(message)

      if (error instanceof ApiRequestError && error.status === 409) {
        setPendingDecision(null)
        await Promise.all([
          reloadActiveInspection(),
          loadInspections(),
        ])
      }
    } finally {
      setIsDeciding(false)
    }
  }

  const selectedDamages = selectedInspection?.damages ?? []
  const selectedExtraFees = selectedInspection?.extraFees ?? []
  const selectedEvidence = selectedInspection?.evidence ?? []
  const pendingDamageCount = selectedDamages.filter((damage) => damage.status === 'PENDING').length

  const dialogTitle = pendingDecision
    ? `${pendingDecision.decision === 'APPROVED' ? 'Approve' : 'Reject'} DamageRecord`
    : 'Inspection Detail'

  const dialogDescription = pendingDecision
    ? 'This is a terminal, audited Facility Manager decision. Staff-entered amount and note are not changed.'
    : 'Monitor return linkage and Inspection result, then decide any PENDING DamageRecord in the assigned Facility.'

  return (
    <section className="handover-page physical-page return-page">
      <div className="handover-heading-row">
        <div>
          <div className="handover-breadcrumb">
            Facility Manager / Returns &amp; Inspections
          </div>
          <span className="handover-eyebrow">MWP-04</span>
          <h1>Return &amp; Inspection Monitoring</h1>
          <p>
            Monitor RETURN-linked or external-recovery Inspections, review result data,
            and approve or reject PENDING DamageRecords for your assigned Facility.
          </p>
        </div>

        <div className="physical-heading-actions">
          <button
            type="button"
            className="handover-refresh-button"
            onClick={() => void refresh()}
            disabled={isLoading}
          >
            <RefreshCw size={17} />
            Refresh
          </button>
        </div>
      </div>

      {pageError && (
        <div className="handover-alert handover-alert-error physical-page-message" role="alert">
          <strong>{pageError.message}</strong>
          {pageError.details?.map((detail) => <span key={detail}>{detail}</span>)}
          {pageError.traceId && <small>Trace: {pageError.traceId}</small>}
        </div>
      )}

      {successMessage && (
        <div className="physical-success" role="status" aria-live="polite">
          <Check size={17} />
          <span>{successMessage}</span>
          <button
            type="button"
            onClick={() => setSuccessMessage(null)}
            aria-label="Dismiss success message"
          >
            <X size={15} />
          </button>
        </div>
      )}

      <div className="physical-summary-grid" aria-label="Inspection lifecycle summary">
        <article>
          <span>Inspection records</span>
          <strong>{inspections.length}</strong>
          <ClipboardCheck size={19} />
        </article>
        <article>
          <span>Pending</span>
          <strong>{statusCounts.PENDING}</strong>
          <AlertTriangle size={19} />
        </article>
        <article>
          <span>In progress</span>
          <strong>{statusCounts.IN_PROGRESS}</strong>
          <ShieldAlert size={19} />
        </article>
        <article>
          <span>Completed</span>
          <strong>{statusCounts.COMPLETED}</strong>
          <BadgeCheck size={19} />
        </article>
        <article>
          <span>Units in inspection</span>
          <strong>{operationsReport?.inspectionUnits ?? '—'}</strong>
          <FileCheck2 size={19} />
        </article>
      </div>

      {(catalogWarning || reportWarning) && (
        <div className="return-warning-stack" aria-live="polite">
          {catalogWarning && (
            <div className="return-soft-warning">
              <AlertTriangle size={15} />
              <span>{catalogWarning.message}</span>
            </div>
          )}
          {reportWarning && (
            <div className="return-soft-warning">
              <AlertTriangle size={15} />
              <span>{reportWarning.message}</span>
            </div>
          )}
        </div>
      )}

      <section className="physical-panel">
        <div className="physical-toolbar">
          <label className="handover-search physical-search">
            <Search size={17} />
            <input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search Inspection, Contract, Unit, Visit or Staff ID"
            />
          </label>

          <label className="physical-filter">
            <span>Inspection status</span>
            <select
              value={statusFilter}
              onChange={(event) => setStatusFilter(event.target.value as 'ALL' | InspectionStatus)}
            >
              {INSPECTION_STATUS_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>{option.label}</option>
              ))}
            </select>
          </label>

          <label className="physical-filter">
            <span>Return source</span>
            <select
              value={linkageFilter}
              onChange={(event) => setLinkageFilter(event.target.value as LinkageFilter)}
            >
              {LINKAGE_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>{option.label}</option>
              ))}
            </select>
          </label>
        </div>

        <div className="physical-panel-meta">
          <div>
            <strong>Facility Inspections</strong>
            <span>
              {filteredInspections.length} shown · {inspections.length} total
              {operationsReport?.asOf
                ? ` · Operations snapshot ${formatUtcDateTime(operationsReport.asOf)} GMT+7`
                : ''}
            </span>
          </div>
          <span className="physical-facility-id">
            Facility {facilityId ? shortId(facilityId) : 'unassigned'}
          </span>
        </div>

        {isLoading ? (
          <div className="handover-empty physical-empty">Loading Inspections…</div>
        ) : !pageError && filteredInspections.length === 0 ? (
          <div className="handover-empty physical-empty">
            <ClipboardCheck size={28} />
            <strong>No matching Inspection</strong>
            <span>
              {inspections.length === 0
                ? 'No return Inspection is currently visible for this Facility.'
                : 'Adjust the search or filters to see other Inspection records.'}
            </span>
          </div>
        ) : filteredInspections.length > 0 ? (
          <div className="physical-table-wrap">
            <table className="physical-table return-table">
              <thead>
                <tr>
                  <th>Inspection</th>
                  <th>Contract</th>
                  <th>StorageUnit</th>
                  <th>Return source</th>
                  <th>Status</th>
                  <th aria-label="Actions" />
                </tr>
              </thead>
              <tbody>
                {filteredInspections.map((inspection) => (
                  <tr key={inspection.inspectionId}>
                    <td>
                      <div className="physical-unit-cell">
                        <span><ClipboardCheck size={17} /></span>
                        <div>
                          <strong>{shortId(inspection.inspectionId)}</strong>
                          <small>
                            {inspection.employeeId
                              ? `Staff ${shortId(inspection.employeeId)}`
                              : 'Not claimed'}
                          </small>
                        </div>
                      </div>
                    </td>
                    <td>
                      <div className="physical-type-cell">
                        <strong>{shortId(inspection.contractId)}</strong>
                        <span>Contract</span>
                      </div>
                    </td>
                    <td>
                      <div className="physical-type-cell">
                        <strong>{shortId(inspection.storageUnitId)}</strong>
                        <span>Physical unit</span>
                      </div>
                    </td>
                    <td>
                      <div className="return-source-cell">
                        {inspection.visitId ? <FileText size={14} /> : <ShieldCheck size={14} />}
                        <div>
                          <strong>
                            {inspection.visitId ? 'RETURN Visit' : 'External recovery'}
                          </strong>
                          <span>
                            {inspection.visitId ? shortId(inspection.visitId) : 'VisitId = null'}
                          </span>
                        </div>
                      </div>
                    </td>
                    <td>
                      <span className={inspectionStatusClass(inspection.status)}>
                        {inspectionStatusLabel(inspection.status)}
                      </span>
                    </td>
                    <td>
                      <div className="physical-row-actions">
                        <button
                          type="button"
                          onClick={() => void openDetail(inspection)}
                          title="View latest Inspection and Damage decision state"
                        >
                          <Eye size={16} />
                          <span>View</span>
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : null}
      </section>

      <p className="physical-boundary-note">
        MWP-04 is a Facility Manager monitoring and Damage decision surface. Facility Staff owns Inspection claim,
        Damage/ExtraFee/evidence recording, Inspection completion, and Finalize Return. Completing an Inspection leaves
        the StorageUnit in INSPECTION; only the Staff Finalize Return transaction may release it to AVAILABLE or MAINTENANCE.
      </p>

      {activeInspectionId && (
        <ManagerDialog
          eyebrow={pendingDecision ? 'MWP-04 · INS-009' : 'MWP-04 · INS-002 / INS-009'}
          title={dialogTitle}
          description={dialogDescription}
          busy={isLoadingDetail || isDeciding}
          onClose={closeDialog}
        >
          {isLoadingDetail ? (
            <div className="physical-dialog-loading">Loading latest Inspection detail…</div>
          ) : pendingDecision ? (
            <DecisionPanel
              pendingDecision={pendingDecision}
              damageType={damageTypeById.get(pendingDecision.damage.damageTypeId)}
              error={dialogError}
              busy={isDeciding}
              onCancel={() => {
                if (!isDeciding) {
                  setPendingDecision(null)
                  setDialogError(null)
                }
              }}
              onConfirm={() => void handleDecision()}
            />
          ) : selectedInspection ? (
            <InspectionDetailPanel
              inspection={selectedInspection}
              damageTypeById={damageTypeById}
              pendingDamageCount={pendingDamageCount}
              error={dialogError}
              onDecide={beginDecision}
              onClose={closeDialog}
            />
          ) : (
            <div className="return-dialog-failure">
              <MessageBlock message={dialogError} />
              <div className="physical-dialog-actions">
                <button type="button" className="physical-secondary-button" onClick={closeDialog}>
                  Close
                </button>
              </div>
            </div>
          )}
        </ManagerDialog>
      )}
    </section>
  )
}

interface InspectionDetailPanelProps {
  inspection: InspectionDetail
  damageTypeById: Map<string, DamageType>
  pendingDamageCount: number
  error: UiMessage | null
  onDecide(damage: DamageRecord, decision: DamageDecision): void
  onClose(): void
}

function InspectionDetailPanel({
  inspection,
  damageTypeById,
  pendingDamageCount,
  error,
  onDecide,
  onClose,
}: InspectionDetailPanelProps) {
  const damages = inspection.damages ?? []
  const extraFees = inspection.extraFees ?? []
  const evidence = inspection.evidence ?? []

  return (
    <>
      <div className="physical-detail-grid return-detail-grid">
        <div>
          <span>Inspection</span>
          <strong>{inspection.inspectionId}</strong>
          <small>INS-002 authoritative detail</small>
        </div>
        <div>
          <span>Status</span>
          <strong className={`physical-status ${inspectionStatusClass(inspection.status)}`}>
            {inspectionStatusLabel(inspection.status)}
          </strong>
          <small>{inspection.employeeId ? `Claimed by ${shortId(inspection.employeeId)}` : 'Not claimed'}</small>
        </div>
        <div>
          <span>Contract</span>
          <strong>{inspection.contractId}</strong>
          <small>Return lifecycle contract</small>
        </div>
        <div>
          <span>StorageUnit</span>
          <strong>{inspection.storageUnitId}</strong>
          <small>Unit remains INSPECTION until Finalize Return</small>
        </div>
        <div>
          <span>Return source</span>
          <strong>{inspection.visitId ? 'RETURN Visit' : 'External recovery'}</strong>
          <small>{inspection.visitId ?? 'VisitId is null by approved external-recovery rule'}</small>
        </div>
        <div>
          <span>Completed</span>
          <strong>{inspection.completedAt ? formatUtcDateTime(inspection.completedAt) : 'Not completed'}</strong>
          <small>{inspection.completedAt ? 'Displayed in GMT+7' : 'Inspection lifecycle is not terminal yet'}</small>
        </div>
        <div className="physical-detail-wide">
          <span>Condition note</span>
          <strong>{inspection.conditionNote || 'No condition note recorded yet.'}</strong>
        </div>
      </div>

      <MessageBlock message={error} />

      <section className="return-detail-section">
        <div className="return-section-heading">
          <div>
            <span>Damage decision</span>
            <h3>DamageRecords</h3>
          </div>
          <span className={pendingDamageCount > 0 ? 'return-pending-chip' : 'return-clear-chip'}>
            {pendingDamageCount > 0 ? `${pendingDamageCount} pending` : 'No pending decision'}
          </span>
        </div>

        {damages.length === 0 ? (
          <div className="return-compact-empty">
            <ShieldCheck size={18} />
            <span>No DamageRecord has been recorded for this Inspection.</span>
          </div>
        ) : (
          <div className="return-damage-list">
            {damages.map((damage) => {
              const damageType = damageTypeById.get(damage.damageTypeId)
              return (
                <article key={damage.damageRecordId} className="return-damage-card">
                  <div className="return-damage-main">
                    <div className="return-damage-title-row">
                      <strong>
                        {damageType
                          ? friendlyDamageType(damageType.name)
                          : `DamageType ${shortId(damage.damageTypeId)}`}
                      </strong>
                      <span className={damageStatusClass(damage.status)}>{damage.status}</span>
                    </div>
                    <p>{damage.note || 'No Staff note.'}</p>
                    <small>DamageRecord {shortId(damage.damageRecordId)}</small>
                  </div>
                  <div className="return-damage-amount">
                    <span>Recorded amount</span>
                    <strong>{formatMoney(damage.damageAmount)}</strong>
                  </div>
                  <div className="return-damage-actions">
                    {damage.status === 'PENDING' ? (
                      <>
                        <button
                          type="button"
                          className="return-approve-button"
                          onClick={() => onDecide(damage, 'APPROVED')}
                        >
                          <CheckCircle2 size={15} />
                          Approve
                        </button>
                        <button
                          type="button"
                          className="return-reject-button"
                          onClick={() => onDecide(damage, 'REJECTED')}
                        >
                          <XCircle size={15} />
                          Reject
                        </button>
                      </>
                    ) : (
                      <span className="return-terminal-note">Terminal decision</span>
                    )}
                  </div>
                </article>
              )
            })}
          </div>
        )}
      </section>

      <div className="return-readonly-grid">
        <section className="return-detail-section">
          <div className="return-section-heading">
            <div>
              <span>Read-only monitoring</span>
              <h3>Extra fees</h3>
            </div>
            <span className="return-count-chip">{extraFees.length}</span>
          </div>
          {extraFees.length === 0 ? (
            <div className="return-compact-empty"><span>No ExtraFee recorded.</span></div>
          ) : (
            <div className="return-mini-list">
              {extraFees.map((fee) => (
                <div key={fee.extraFeeId}>
                  <div>
                    <strong>{formatMoney(fee.amount)}</strong>
                    <span>{fee.reason}</span>
                  </div>
                  <small>{shortId(fee.extraFeeTypeId)}</small>
                </div>
              ))}
            </div>
          )}
        </section>

        <section className="return-detail-section">
          <div className="return-section-heading">
            <div>
              <span>Read-only monitoring</span>
              <h3>Evidence metadata</h3>
            </div>
            <span className="return-count-chip">{evidence.length}</span>
          </div>
          {evidence.length === 0 ? (
            <div className="return-compact-empty"><span>No InspectionEvidence recorded.</span></div>
          ) : (
            <div className="return-mini-list">
              {evidence.map((item) => (
                <div key={item.inspectionEvidenceId}>
                  <div>
                    <strong>{item.evidenceType}</strong>
                    <span>{item.createdAt ? `${formatUtcDateTime(item.createdAt)} GMT+7` : 'Timestamp not exposed'}</span>
                  </div>
                  <small>{shortId(item.inspectionEvidenceId)}</small>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>

      <div className="physical-dialog-actions return-detail-actions">
        <button type="button" className="physical-secondary-button" onClick={onClose}>
          Close
        </button>
      </div>
    </>
  )
}

interface DecisionPanelProps {
  pendingDecision: PendingDecision
  damageType?: DamageType
  error: UiMessage | null
  busy: boolean
  onCancel(): void
  onConfirm(): void
}

function DecisionPanel({
  pendingDecision,
  damageType,
  error,
  busy,
  onCancel,
  onConfirm,
}: DecisionPanelProps) {
  const isApprove = pendingDecision.decision === 'APPROVED'

  return (
    <div className="physical-status-confirm return-decision-panel">
      <div className="return-decision-summary">
        <div>
          <span>Damage type</span>
          <strong>
            {damageType
              ? friendlyDamageType(damageType.name)
              : shortId(pendingDecision.damage.damageTypeId)}
          </strong>
        </div>
        <div>
          <span>Recorded amount</span>
          <strong>{formatMoney(pendingDecision.damage.damageAmount)}</strong>
        </div>
        <div className="return-decision-note">
          <span>Staff note</span>
          <strong>{pendingDecision.damage.note || 'No Staff note.'}</strong>
        </div>
      </div>

      <div className="physical-inline-note">
        <AlertTriangle size={16} />
        <span>
          {isApprove
            ? 'APPROVED Damage contributes its recorded amount to Deposit settlement.'
            : 'REJECTED Damage contributes zero to Deposit settlement.'}
          {' '}The terminal status cannot be reopened through MWP-04.
        </span>
      </div>

      <MessageBlock message={error} />

      <div className="physical-dialog-actions">
        <button
          type="button"
          className="physical-secondary-button"
          onClick={onCancel}
          disabled={busy}
        >
          Back
        </button>
        <button
          type="button"
          className={isApprove ? 'return-approve-button return-decision-button' : 'return-reject-button return-decision-button'}
          onClick={onConfirm}
          disabled={busy}
        >
          {isApprove ? <CheckCircle2 size={16} /> : <XCircle size={16} />}
          {busy
            ? 'Saving…'
            : isApprove
              ? 'Confirm approval'
              : 'Confirm rejection'}
        </button>
      </div>
    </div>
  )
}
