import { EmployeeEntityLabel } from '../../components/EmployeeEntityLabel'
import { StorageUnitCode } from '../../components/StorageUnitCode'
import { NeonUnitGrid } from './NeonUnitGrid'
import { LayoutGrid, List } from 'lucide-react'
import { EmployeeModal } from '../../components/EmployeeModal'
import {
  AlertTriangle,
  Check,
  CircleOff,
  Eye,
  MapPin,
  Pencil,
  Plus,
  RefreshCw,
  Search,
  ShieldCheck,
  Warehouse,
  Wrench,
  X,
} from 'lucide-react'
import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type FormEvent,
  type ReactNode,
} from 'react'
import { catalogApi } from '../../api/catalogApi'
import { ApiRequestError } from '../../api/httpClient'
import {
  storageUnitApi,
  type CreateStorageUnitRequest,
  type StorageUnit,
  type StorageUnitStatus,
  type UpdateStorageUnitRequest,
} from '../../api/storageUnitApi'
import { useAuth } from '../../auth/auth.context'
import type { UnitTypeAvailability } from '../catalog/catalog.types'
import { formatMoney } from '../../utils/formatters'

type DialogMode = 'create' | 'edit' | 'detail' | 'status' | null

type UiMessage = {
  message: string
  traceId?: string
  details?: string[]
}

interface UnitFormState {
  unitCode: string
  unitTypeId: string
  locationInfo: string
}

interface ManagerDialogProps {
  title: string
  description?: string
  busy?: boolean
  onClose(): void
  children: ReactNode
}

const EMPTY_FORM: UnitFormState = {
  unitCode: '',
  unitTypeId: '',
  locationInfo: '',
}

const STATUS_OPTIONS: Array<{ value: 'ALL' | StorageUnitStatus; label: string }> = [
  { value: 'ALL', label: 'All statuses' },
  { value: 'AVAILABLE', label: 'Available' },
  { value: 'IN_USE', label: 'In use' },
  { value: 'INSPECTION', label: 'Inspection' },
  { value: 'MAINTENANCE', label: 'Maintenance' },
]

function ManagerDialog({
  title,
  description,
  busy = false,
  onClose,
  children,
}: ManagerDialogProps) {
  // EmployeeModal manages native dialog lifecycle, focus entry/trap/restoration, and Escape.

  useEffect(() => {
  // showModal is handled by EmployeeModal
    return undefined
  }, [])

  return (
    <EmployeeModal
      role="dialog"
      className="manager-dialog"
      labelledBy="manager-dialog-title" busy={busy}
      onClose={() => { /*
        event.preventDefault()
        */ if (!busy) {
          onClose()
        }
      }}
    >
      <div className="manager-dialog-heading">
        <div>
          <h2 id="manager-dialog-title">{title}</h2>
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
    </EmployeeModal>
  )
}


function statusLabel(status: StorageUnitStatus) {
  switch (status) {
    case 'IN_USE':
      return 'IN USE'
    default:
      return status
  }
}

function statusClass(status: StorageUnitStatus) {
  return `physical-status physical-status-${status.toLowerCase().replace('_', '-')}`
}

function apiErrorMessage(error: unknown, fallback: string): UiMessage {
  if (!(error instanceof ApiRequestError)) {
    return { message: fallback }
  }

  const details = error.errors ? Object.values(error.errors).flat() : undefined
  const messages: Record<string, string> = {
    UNAUTHORIZED: 'Your session is invalid or has expired.',
    FORBIDDEN: 'This action is outside your assigned Facility or Facility Manager permission.',
    VALIDATION_ERROR: 'Some StorageUnit information is invalid. Check the highlighted input and try again.',
    RESOURCE_NOT_FOUND: 'The requested resource was not found. Refresh the authoritative data.',
    FACILITY_NOT_FOUND: 'Your assigned Facility could not be found.',
    UNIT_TYPE_REQUIRED: 'Unit Type is required.',
    UNIT_TYPE_NOT_FOUND: 'The selected Unit Type does not exist.',
    UNIT_CODE_REQUIRED: 'Unit Code is required.',
    UNIT_CODE_ALREADY_EXISTS: 'A StorageUnit with this Unit Code already exists in the Facility.',
    STORAGE_UNIT_NOT_FOUND: 'The StorageUnit no longer exists. Refresh the authoritative data.',
    UNIT_TYPE_CHANGE_NOT_ALLOWED: 'Unit Type cannot be changed while this StorageUnit is occupied, IN USE, or under INSPECTION.',
    STORAGE_UNIT_STATUS_REQUIRED: 'A target StorageUnit status is required.',
    STORAGE_UNIT_STATUS_NOT_ALLOWED: 'Facility Manager may directly manage only AVAILABLE and MAINTENANCE operational states.',
    STORAGE_UNIT_STATUS_TRANSITION_INVALID: 'The requested operational status transition is not legal from the current StorageUnit state.',
    STORAGE_UNIT_OCCUPIED: 'This StorageUnit has an ACTIVE Contract and its status cannot be changed manually.',
    ENDPOINT_NOT_IMPLEMENTED: 'This StorageUnit operation is not implemented by the current backend yet.',
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
        ? 'The StorageUnit changed on the server and this action conflicts with its current lifecycle. Refresh and try again.'
        : error.status === 404
          ? 'The requested StorageUnit or Unit Type was not found.'
          : fallback),
    traceId: error.traceId,
    details,
  }
}

function managerStatusTarget(status: StorageUnitStatus): StorageUnitStatus | null {
  if (status === 'AVAILABLE') {
    return 'MAINTENANCE'
  }

  if (status === 'MAINTENANCE') {
    return 'AVAILABLE'
  }

  return null
}

function unitTypeDisplay(unitType?: UnitTypeAvailability | null) {
  if (!unitType) {
    return null
  }

  return `${unitType.name} · ${unitType.mode} · ${unitType.size}`
}

export function PhysicalUnitManagementPage() {
  const { user } = useAuth()
  const facilityId = user?.facilityId ?? null

  const [units, setUnits] = useState<StorageUnit[]>([])
  const [facilityUnitTypes, setFacilityUnitTypes] = useState<UnitTypeAvailability[]>([])
  const [unitTypeCache, setUnitTypeCache] = useState<Record<string, UnitTypeAvailability>>({})
  const [search, setSearch] = useState('')
  const [viewMode, setViewMode] = useState<'grid' | 'table'>('grid')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(12)
  const [statusFilter, setStatusFilter] = useState<'ALL' | StorageUnitStatus>('ALL')
  const [unitTypeFilter, setUnitTypeFilter] = useState('ALL')
  const [isLoading, setIsLoading] = useState(true)
  const [isLoadingUnitTypes, setIsLoadingUnitTypes] = useState(false)
  const [pageError, setPageError] = useState<UiMessage | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const [dialogMode, setDialogMode] = useState<DialogMode>(null)
  const [selectedUnit, setSelectedUnit] = useState<StorageUnit | null>(null)
  const [form, setForm] = useState<UnitFormState>(EMPTY_FORM)
  const [verifiedUnitType, setVerifiedUnitType] = useState<UnitTypeAvailability | null>(null)
  const [formError, setFormError] = useState<UiMessage | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isVerifyingUnitType, setIsVerifyingUnitType] = useState(false)
  const [isLoadingDetail, setIsLoadingDetail] = useState(false)
  const detailRequest = useRef(0)

  const loadUnitTypes = useCallback(async () => {
    if (!facilityId) {
      setFacilityUnitTypes([])
      return
    }

    setIsLoadingUnitTypes(true)
    try {
      const data = await catalogApi.listAllFacilityUnitTypes(facilityId)
      setFacilityUnitTypes(data)
      setUnitTypeCache((current) => {
        const next = { ...current }
        for (const unitType of data) {
          next[unitType.unitTypeId] = unitType
        }
        return next
      })
    } catch {
      // Retain cached named options; Refresh retries the Facility catalogue.
      setFacilityUnitTypes([])
    } finally {
      setIsLoadingUnitTypes(false)
    }
  }, [facilityId])

  const loadUnits = useCallback(async () => {
    if (!facilityId) {
      setUnits([])
      setPageError({
        message: 'Your Facility Manager account has no Facility assignment. An assigned Facility is required to manage physical units.',
      })
      setIsLoading(false)
      return
    }

    setIsLoading(true)
    setPageError(null)

    try {
      const data = await storageUnitApi.listAllByFacility(facilityId)
      setUnits(data)
    } catch (error) {
      setUnits([])
      setPageError(apiErrorMessage(error, 'Unable to load StorageUnits for your Facility.'))
    } finally {
      setIsLoading(false)
    }
  }, [facilityId])

  const refresh = useCallback(async () => {
    setSuccessMessage(null)
    await Promise.all([loadUnits(), loadUnitTypes()])
  }, [loadUnitTypes, loadUnits])

  useEffect(() => {
    void refresh()
  }, [refresh])

  const knownUnitTypes = useMemo(() => {
    const byId = new Map<string, UnitTypeAvailability>()
    for (const unitType of facilityUnitTypes) {
      byId.set(unitType.unitTypeId, unitType)
    }
    for (const unitType of Object.values(unitTypeCache)) {
      byId.set(unitType.unitTypeId, unitType)
    }
    return [...byId.values()].sort((a, b) => a.name.localeCompare(b.name))
  }, [facilityUnitTypes, unitTypeCache])

  // UNIT-001 is already authoritative for the units in the Manager's Facility.
  // Keep the filter usable even if the optional CAT-003 catalogue read is temporarily
  // unavailable: every UnitTypeId present in the loaded StorageUnits remains selectable.
  const unitTypeFilterOptions = useMemo(() => {
    const ids = new Set(units.map((unit) => unit.unitTypeId))

    return [...ids]
      .map((unitTypeId) => {
        const detail = unitTypeCache[unitTypeId]
        return {
          unitTypeId,
          label: detail
            ? `${detail.name} · ${detail.mode}`
            : 'Unit type name unavailable',
        }
      })
      .sort((a, b) => a.label.localeCompare(b.label))
  }, [unitTypeCache, units])

  const filteredUnits = useMemo(() => {
    const normalizedSearch = search.trim().toLowerCase()

    return [...units]
      .filter((unit) => {
        if (statusFilter !== 'ALL' && unit.status !== statusFilter) {
          return false
        }

        if (unitTypeFilter !== 'ALL' && unit.unitTypeId !== unitTypeFilter) {
          return false
        }

        if (!normalizedSearch) {
          return true
        }

        const unitType = unitTypeCache[unit.unitTypeId]
        return [
          unit.unitCode,
          unit.locationInfo ?? '',
          unit.storageUnitId,
          unit.unitTypeId,
          unitType?.name ?? '',
          unitType?.mode ?? '',
          unitType?.size ?? '',
        ].some((value) => value.toLowerCase().includes(normalizedSearch))
      })
      .sort((a, b) => a.unitCode.localeCompare(b.unitCode))
  }, [search, statusFilter, unitTypeCache, unitTypeFilter, units])

  const statusCounts = useMemo(() => {
    const counts: Record<StorageUnitStatus, number> = {
      AVAILABLE: 0,
      IN_USE: 0,
      INSPECTION: 0,
      MAINTENANCE: 0,
    }

    for (const unit of units) {
      counts[unit.status] += 1
    }

    return counts
  }, [units])

  const totalPages = Math.max(1, Math.ceil(filteredUnits.length / pageSize))
  const safePage = Math.min(page, totalPages)
  const visibleUnits = useMemo(() => {
    const start = (safePage - 1) * pageSize
    return filteredUnits.slice(start, start + pageSize)
  }, [filteredUnits, safePage, pageSize])

  function closeDialog() {
    if (isSubmitting) {
      return
    }

    detailRequest.current += 1; setIsLoadingDetail(false); setIsVerifyingUnitType(false); setDialogMode(null)
    setSelectedUnit(null)
    setForm(EMPTY_FORM)
    setFormError(null)
    setVerifiedUnitType(null)
  }

  function openCreate() {
    setSelectedUnit(null)
    setForm(EMPTY_FORM)
    setVerifiedUnitType(null)
    setFormError(null)
    detailRequest.current += 1; setIsLoadingDetail(false); setIsVerifyingUnitType(false); setDialogMode('create')
  }

  async function fetchUnitType(unitTypeId: string, requestId = detailRequest.current): Promise<UnitTypeAvailability | null> {
    const normalized = unitTypeId.trim()
    if (!normalized) {
      setVerifiedUnitType(null)
      setFormError({ message: 'Unit Type is required.' })
      return null
    }

    const cached = unitTypeCache[normalized]
    if (cached) {
      setVerifiedUnitType(cached)
      setFormError(null)
      return cached
    }

    setIsVerifyingUnitType(true)
    setFormError(null)

    try {
      const response = await catalogApi.getUnitType(normalized)
      if (requestId !== detailRequest.current) return null
      const unitType = response.data
      setVerifiedUnitType(unitType)
      setUnitTypeCache((current) => ({ ...current, [unitType.unitTypeId]: unitType }))
      return unitType
    } catch (error) {
      setVerifiedUnitType(null)
      if (requestId === detailRequest.current) setFormError(apiErrorMessage(error, 'Unable to verify this Unit Type.'))
      return null
    } finally {
      if (requestId === detailRequest.current) setIsVerifyingUnitType(false)
    }
  }

  async function openDetail(unit: StorageUnit) {
    const requestId = ++detailRequest.current
    setDialogMode('detail')
    setSelectedUnit(unit)
    setIsLoadingDetail(true)
    setFormError(null)

    try {
      const response = await storageUnitApi.get(unit.storageUnitId)
      if (requestId !== detailRequest.current) return
      const current = response.data
      setSelectedUnit(current)
      await fetchUnitType(current.unitTypeId, requestId)
    } catch (error) {
      if (requestId === detailRequest.current) setFormError(apiErrorMessage(error, 'Unable to load the latest StorageUnit detail.'))
    } finally {
      if (requestId === detailRequest.current) setIsLoadingDetail(false)
    }
  }

  async function openEdit(unit: StorageUnit) {
    const requestId = ++detailRequest.current
    setDialogMode('edit')
    setSelectedUnit(unit)
    setForm({
      unitCode: unit.unitCode,
      unitTypeId: unit.unitTypeId,
      locationInfo: unit.locationInfo ?? '',
    })
    setFormError(null)
    setVerifiedUnitType(unitTypeCache[unit.unitTypeId] ?? null)
    setIsLoadingDetail(true)

    try {
      const response = await storageUnitApi.get(unit.storageUnitId)
      if (requestId !== detailRequest.current) return
      const current = response.data
      setSelectedUnit(current)
      setForm({
        unitCode: current.unitCode,
        unitTypeId: current.unitTypeId,
        locationInfo: current.locationInfo ?? '',
      })
      const currentType = unitTypeCache[current.unitTypeId]
      setVerifiedUnitType(currentType ?? null)

      // IN_USE / INSPECTION cannot reassign Unit Type in MWP-02, so a catalogue
      // verification failure must not block a valid location-only edit.
      if (current.status !== 'IN_USE' && current.status !== 'INSPECTION' && !currentType) {
        await fetchUnitType(current.unitTypeId, requestId)
      }
    } catch (error) {
      if (requestId === detailRequest.current) setFormError(apiErrorMessage(error, 'Unable to load the latest StorageUnit before editing.'))
    } finally {
      if (requestId === detailRequest.current) setIsLoadingDetail(false)
    }
  }

  async function openStatus(unit: StorageUnit) {
    const requestId = ++detailRequest.current
    setDialogMode('status')
    setSelectedUnit(unit)
    setFormError(null)
    setIsLoadingDetail(true)

    try {
      const response = await storageUnitApi.get(unit.storageUnitId)
      if (requestId !== detailRequest.current) return
      setSelectedUnit(response.data)
    } catch (error) {
      if (requestId === detailRequest.current) setFormError(apiErrorMessage(error, 'Unable to load the latest StorageUnit status.'))
    } finally {
      if (requestId === detailRequest.current) setIsLoadingDetail(false)
    }
  }

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!facilityId || isSubmitting) {
      return
    }

    const unitCode = form.unitCode.trim()
    const unitTypeId = form.unitTypeId.trim()

    if (!unitCode) {
      setFormError({ message: 'Unit Code is required.' })
      return
    }

    if (!unitTypeId) {
      setFormError({ message: 'Unit Type is required.' })
      return
    }

    const request: CreateStorageUnitRequest = {
      unitTypeId,
      unitCode,
      locationInfo: form.locationInfo.trim() || null,
    }

    setIsSubmitting(true)
    setFormError(null)

    try {
      await storageUnitApi.create(facilityId, request)
      setSuccessMessage(`Storage unit ${unitCode} created as AVAILABLE.`)
      closeDialogAfterSubmit()
      await loadUnits()
      await loadUnitTypes()
    } catch (error) {
      const message = apiErrorMessage(error, 'Unable to create the StorageUnit.')
      setFormError(message)
      if (error instanceof ApiRequestError && error.status === 409) {
        await loadUnits()
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleEdit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!selectedUnit || isSubmitting) {
      return
    }

    const unitTypeChanged = form.unitTypeId.trim() !== selectedUnit.unitTypeId
    const unitTypeLocked = selectedUnit.status === 'IN_USE' || selectedUnit.status === 'INSPECTION'

    if (unitTypeChanged && unitTypeLocked) {
      setFormError({
        message: `Unit Type cannot be reassigned while the StorageUnit is ${statusLabel(selectedUnit.status)}.`,
      })
      return
    }

    const unitTypeId = form.unitTypeId.trim() || selectedUnit.unitTypeId

    const request: UpdateStorageUnitRequest = {
      unitTypeId,
      locationInfo: form.locationInfo.trim() || null,
    }

    setIsSubmitting(true)
    setFormError(null)

    try {
      const response = await storageUnitApi.update(selectedUnit.storageUnitId, request)
      setSuccessMessage(`StorageUnit ${response.data.unitCode} updated.`)
      closeDialogAfterSubmit()
      await loadUnits()
      await loadUnitTypes()
    } catch (error) {
      const message = apiErrorMessage(error, 'Unable to update the StorageUnit.')
      setFormError(message)
      if (error instanceof ApiRequestError && error.status === 409) {
        await refreshSelectedUnit(selectedUnit.storageUnitId)
        await loadUnits()
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleStatusTransition() {
    if (!selectedUnit || isSubmitting) {
      return
    }

    const target = managerStatusTarget(selectedUnit.status)
    if (!target) {
      setFormError({
        message: 'This status is controlled by handover and return workflows and cannot be changed here.',
      })
      return
    }

    setIsSubmitting(true)
    setFormError(null)

    try {
      const response = await storageUnitApi.updateStatus(selectedUnit.storageUnitId, { status: target })
      setSuccessMessage(`StorageUnit ${response.data.unitCode} changed to ${statusLabel(response.data.status)}.`)
      closeDialogAfterSubmit()
      await loadUnits()
    } catch (error) {
      setFormError(apiErrorMessage(error, 'Unable to change the StorageUnit operational status.'))
      if (error instanceof ApiRequestError && error.status === 409) {
        await refreshSelectedUnit(selectedUnit.storageUnitId)
        await loadUnits()
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  function closeDialogAfterSubmit() {
    detailRequest.current += 1; setIsLoadingDetail(false); setIsVerifyingUnitType(false); setDialogMode(null)
    setSelectedUnit(null)
    setForm(EMPTY_FORM)
    setFormError(null)
    setVerifiedUnitType(null)
  }

  async function refreshSelectedUnit(storageUnitId: string) {
    try {
      const response = await storageUnitApi.get(storageUnitId)
      setSelectedUnit(response.data)
      setForm((current) => ({
        ...current,
        unitTypeId: response.data.unitTypeId,
        locationInfo: response.data.locationInfo ?? '',
      }))
    } catch {
      // The original operation error remains the primary message.
    }
  }

  const selectedStatusTarget = selectedUnit ? managerStatusTarget(selectedUnit.status) : null
  const canReassignUnitType = selectedUnit
    ? selectedUnit.status !== 'IN_USE' && selectedUnit.status !== 'INSPECTION'
    : true

  return (
    <section className="handover-page physical-page">
      <div className="handover-heading-row">
        <div>
          <div className="handover-breadcrumb">
            Facility Manager / Physical Unit Management
          </div>
          <span className="handover-eyebrow">Facility inventory</span>
          <h1>Manage Physical Units</h1>
          <p>
            Manage unit types, physical locations and maintenance availability at your facility.
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
          <button
            type="button"
            className="physical-primary-button"
            onClick={openCreate}
            disabled={!facilityId}
          >
            <Plus size={17} />
            Add StorageUnit
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
          <button type="button" onClick={() => setSuccessMessage(null)} aria-label="Dismiss success message">
            <X size={15} />
          </button>
        </div>
      )}

      <div className="physical-summary-grid" aria-label="StorageUnit status summary">
        <article>
          <span>Total units</span>
          <strong>{units.length}</strong>
          <Warehouse size={19} />
        </article>
        <article>
          <span>Available</span>
          <strong>{statusCounts.AVAILABLE}</strong>
          <ShieldCheck size={19} />
        </article>
        <article>
          <span>In use</span>
          <strong>{statusCounts.IN_USE}</strong>
          <Warehouse size={19} />
        </article>
        <article>
          <span>Inspection</span>
          <strong>{statusCounts.INSPECTION}</strong>
          <AlertTriangle size={19} />
        </article>
        <article>
          <span>Maintenance</span>
          <strong>{statusCounts.MAINTENANCE}</strong>
          <Wrench size={19} />
        </article>
      </div>

      <section className="physical-panel">
        <div className="physical-toolbar">
          <label className="handover-search physical-search">
            <Search size={17} />
            <input
              value={search}
              onChange={(event) => {
                setSearch(event.target.value)
                setPage(1)
              }}
              placeholder="Search unit code, location or unit type"
            />
          </label>

          <label className="physical-filter">
            <span>Status</span>
            <select
              value={statusFilter}
              onChange={(event) => {
                setStatusFilter(event.target.value as 'ALL' | StorageUnitStatus)
                setPage(1)
              }}
            >
              {STATUS_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>{option.label}</option>
              ))}
            </select>
          </label>

          <label className="physical-filter">
            <span>Unit Type</span>
            <select
              value={unitTypeFilter}
              onChange={(event) => {
                setUnitTypeFilter(event.target.value)
                setPage(1)
              }}
            >
              <option value="ALL">All Unit Types</option>
              {unitTypeFilterOptions.map((unitType) => (
                <option key={unitType.unitTypeId} value={unitType.unitTypeId}>
                  {unitType.label}
                </option>
              ))}
            </select>
          </label>

          <div className="physical-view-toggle">
            <button
              type="button"
              className={`physical-view-btn ${viewMode === 'grid' ? 'active' : ''}`}
              onClick={() => setViewMode('grid')}
              title="Sơ đồ Holographic Grid"
            >
              <LayoutGrid size={15} />
              <span>Sơ đồ</span>
            </button>
            <button
              type="button"
              className={`physical-view-btn ${viewMode === 'table' ? 'active' : ''}`}
              onClick={() => setViewMode('table')}
              title="Danh sách bảng"
            >
              <List size={15} />
              <span>Bảng</span>
            </button>
          </div>
        </div>

        <div className="physical-panel-meta">
          <div>
            <strong>Facility StorageUnits</strong>
            <span>{filteredUnits.length} shown · {units.length} total</span>
          </div>
          <span className="physical-facility-id">
            <EmployeeEntityLabel kind="facility" id={facilityId} />
          </span>
        </div>

        {isLoading ? (
          <div className="handover-empty physical-empty">Loading StorageUnits…</div>
        ) : !pageError && filteredUnits.length === 0 ? (
          <div className="handover-empty physical-empty">
            <Warehouse size={28} />
            <strong>No matching StorageUnit</strong>
            <span>
              {units.length === 0
                ? 'Create the first physical unit for this Facility.'
                : 'Adjust the search or filters to see other units.'}
            </span>
          </div>
        ) : filteredUnits.length > 0 ? (
          viewMode === 'grid' ? (
            <NeonUnitGrid
              units={units}
              filteredUnits={filteredUnits}
              visibleUnits={visibleUnits}
              page={safePage}
              totalPages={totalPages}
              pageSize={pageSize}
              onPageChange={setPage}
              onPageSizeChange={(newSize) => {
                setPageSize(newSize)
                setPage(1)
              }}
              unitTypeCache={unitTypeCache}
              onOpenDetail={openDetail}
              onOpenEdit={openEdit}
              onOpenStatus={openStatus}
              managerStatusTarget={managerStatusTarget}
              statusLabel={statusLabel}
            />
          ) : (
            <div className="physical-table-wrap">
              <table className="physical-table">
              <thead>
                <tr>
                  <th>Unit</th>
                  <th>Unit Type</th>
                  <th>Location</th>
                  <th>Status</th>
                  <th aria-label="Actions" />
                </tr>
              </thead>
              <tbody>
                {visibleUnits.map((unit) => {
                  const unitType = unitTypeCache[unit.unitTypeId]
                  const target = managerStatusTarget(unit.status)

                  return (
                    <tr key={unit.storageUnitId}>
                      <td>
                        <div className="physical-unit-cell">
                          <span><Warehouse size={17} /></span>
                          <div>
                            <StorageUnitCode code={unit.unitCode} />
                          </div>
                        </div>
                      </td>
                      <td>
                        <div className="physical-type-cell">
                          <strong>{unitType?.name ?? 'Unit type name unavailable'}</strong>
                          <span>
                            {unitType ? `${unitType.mode} · ${unitType.size} · ${formatMoney(unitType.rentalPrice)}` : 'Unit type details unavailable'}
                          </span>
                        </div>
                      </td>
                      <td>
                        <div className="physical-location-cell">
                          <MapPin size={15} />
                          <span>{unit.locationInfo || 'Not specified'}</span>
                        </div>
                      </td>
                      <td><span className={statusClass(unit.status)}>{statusLabel(unit.status)}</span></td>
                      <td>
                        <div className="physical-row-actions">
                          <button type="button" onClick={() => void openDetail(unit)} title="View latest detail">
                            <Eye size={16} />
                            <span>View</span>
                          </button>
                          <button type="button" onClick={() => void openEdit(unit)} title="Edit Unit Type or location">
                            <Pencil size={16} />
                            <span>Edit</span>
                          </button>
                          <button
                            type="button"
                            onClick={() => void openStatus(unit)}
                            disabled={!target}
                            title={target ? `Change status to ${statusLabel(target)}` : 'Status is controlled by handover/return lifecycle'}
                          >
                            {target === 'MAINTENANCE' ? <Wrench size={16} /> : target === 'AVAILABLE' ? <ShieldCheck size={16} /> : <CircleOff size={16} />}
                            <span>{target === 'MAINTENANCE' ? 'Maintain' : target === 'AVAILABLE' ? 'Restore' : 'Lifecycle'}</span>
                          </button>
                        </div>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
            {totalPages > 1 && (
              <div className="fwp-pagination" style={{ padding: '12px 16px' }}>
                <span>Trang {safePage} / {totalPages} · {filteredUnits.length} ô kho</span>
                <div style={{ display: 'flex', gap: '8px' }}>
                  <button
                    type="button"
                    className="fwp-button fwp-button-secondary"
                    disabled={safePage <= 1}
                    onClick={() => setPage(safePage - 1)}
                  >
                    Trang trước
                  </button>
                  <button
                    type="button"
                    className="fwp-button fwp-button-secondary"
                    disabled={safePage >= totalPages}
                    onClick={() => setPage(safePage + 1)}
                  >
                    Trang sau
                  </button>
                </div>
              </div>
            )}
          </div>
        ) ) : null}
      </section>

      <p className="physical-boundary-note">
        Unit type definitions and rental prices are read-only here. Handover and return processing update occupancy and inspection status.
      </p>

      {dialogMode === 'create' && (
        <ManagerDialog
          title="Add StorageUnit"
          description="New StorageUnits start as AVAILABLE. Choose an existing Unit Type; unit type definitions cannot be created or modified here."
          busy={isSubmitting || isVerifyingUnitType}
          onClose={closeDialog}
        >
          <form className="physical-form" onSubmit={(event) => void handleCreate(event)}>
            <UnitFormFields
              form={form}
              onChange={setForm}
              knownUnitTypes={knownUnitTypes}
              verifiedUnitType={verifiedUnitType}
              verifying={isVerifyingUnitType}
              onVerify={() => void fetchUnitType(form.unitTypeId)}
              showUnitCode
            />
            <FormError error={formError} />
            <div className="physical-dialog-actions">
              <button type="button" className="physical-secondary-button" onClick={closeDialog} disabled={isSubmitting}>Cancel</button>
              <button type="submit" className="physical-primary-button" disabled={isSubmitting || isVerifyingUnitType}>
                <Plus size={16} />
                {isSubmitting ? 'Creating…' : 'Create StorageUnit'}
              </button>
            </div>
          </form>
        </ManagerDialog>
      )}

      {dialogMode === 'edit' && selectedUnit && (
        <ManagerDialog
          title={`Edit ${selectedUnit.unitCode}`}
          description="Location may be maintained operationally. Unit Type reassignment is blocked while the unit is IN_USE or INSPECTION and remains server-authoritative."
          busy={isSubmitting}
          onClose={closeDialog}
        >
          {isLoadingDetail ? (
            <div className="physical-dialog-loading">Loading latest StorageUnit…</div>
          ) : (
            <form className="physical-form" onSubmit={(event) => void handleEdit(event)}>
              <div className="physical-current-state">
                <span>Current status</span>
                <strong className={statusClass(selectedUnit.status)}>{statusLabel(selectedUnit.status)}</strong>
              </div>
              <UnitFormFields
                form={form}
                onChange={setForm}
                knownUnitTypes={knownUnitTypes}
                verifiedUnitType={verifiedUnitType}
                verifying={isVerifyingUnitType}
                onVerify={() => void fetchUnitType(form.unitTypeId)}
                unitTypeDisabled={!canReassignUnitType}
              />
              {!canReassignUnitType && (
                <div className="physical-inline-note">
                  <AlertTriangle size={16} />
                  Unit Type is locked while this unit is {statusLabel(selectedUnit.status)}. You can still update its location information.
                </div>
              )}
              <FormError error={formError} />
              <div className="physical-dialog-actions">
                <button type="button" className="physical-secondary-button" onClick={closeDialog} disabled={isSubmitting}>Cancel</button>
                <button type="submit" className="physical-primary-button" disabled={isSubmitting || isVerifyingUnitType}>
                  <Check size={16} />
                  {isSubmitting ? 'Saving…' : 'Save changes'}
                </button>
              </div>
            </form>
          )}
        </ManagerDialog>
      )}

      {dialogMode === 'detail' && selectedUnit && (
        <ManagerDialog
          title={selectedUnit.unitCode}
          description="Latest server-authoritative StorageUnit detail."
          busy={false}
          onClose={closeDialog}
        >
          {isLoadingDetail ? (
            <div className="physical-dialog-loading">Loading latest StorageUnit…</div>
          ) : (
            <>
              <div className="physical-detail-grid">
                <div><span>Status</span><strong className={statusClass(selectedUnit.status)}>{statusLabel(selectedUnit.status)}</strong></div>
                <div><span>StorageUnit</span><StorageUnitCode code={selectedUnit.unitCode} /></div>
                <div><span>Facility</span><strong><EmployeeEntityLabel kind="facility" id={selectedUnit.facilityId} /></strong></div>
                <div><span>Location</span><strong>{selectedUnit.locationInfo || 'Not specified'}</strong></div>
                <div className="physical-detail-wide">
                  <span>Unit Type</span>
                  <strong>{unitTypeDisplay(verifiedUnitType) ?? 'Unit type name unavailable'}</strong>
                  {verifiedUnitType && <small>{formatMoney(verifiedUnitType.rentalPrice)} / month · read-only master price</small>}
                </div>
              </div>
              <FormError error={formError} />
              <div className="physical-dialog-actions">
                <button type="button" className="physical-secondary-button" onClick={closeDialog}>Close</button>
                <button type="button" className="physical-primary-button" onClick={() => void openEdit(selectedUnit)}>
                  <Pencil size={16} /> Edit unit
                </button>
              </div>
            </>
          )}
        </ManagerDialog>
      )}

      {dialogMode === 'status' && selectedUnit && (
        <ManagerDialog
          title="Change operational status"
          description="Only preventive-maintenance status changes are available here. Handover and return status changes must follow their respective workflows."
          busy={isSubmitting}
          onClose={closeDialog}
        >
          {isLoadingDetail ? (
            <div className="physical-dialog-loading">Checking latest status…</div>
          ) : (
            <div className="physical-status-confirm">
              <div className="physical-status-transition">
                <span className={statusClass(selectedUnit.status)}>{statusLabel(selectedUnit.status)}</span>
                <strong>→</strong>
                {selectedStatusTarget ? (
                  <span className={statusClass(selectedStatusTarget)}>{statusLabel(selectedStatusTarget)}</span>
                ) : (
                  <span className="physical-status physical-status-locked">LIFECYCLE CONTROLLED</span>
                )}
              </div>
              <p>
                {selectedStatusTarget === 'MAINTENANCE'
                  ? 'Use this for preventive maintenance only when the unit has no ACTIVE Contract. The backend remains authoritative.'
                  : selectedStatusTarget === 'AVAILABLE'
                    ? 'Return this maintenance unit to AVAILABLE only when the legal StorageUnit transition is allowed.'
                    : 'This StorageUnit state is managed through handover or return processing and cannot be changed here.'}
              </p>
              <FormError error={formError} />
              <div className="physical-dialog-actions">
                <button type="button" className="physical-secondary-button" onClick={closeDialog} disabled={isSubmitting}>Cancel</button>
                <button
                  type="button"
                  className="physical-primary-button"
                  onClick={() => void handleStatusTransition()}
                  disabled={isSubmitting || !selectedStatusTarget}
                >
                  {selectedStatusTarget === 'MAINTENANCE' ? <Wrench size={16} /> : <ShieldCheck size={16} />}
                  {isSubmitting ? 'Updating…' : selectedStatusTarget ? `Set ${statusLabel(selectedStatusTarget)}` : 'Unavailable'}
                </button>
              </div>
            </div>
          )}
        </ManagerDialog>
      )}

      {isLoadingUnitTypes && <span className="physical-sr-only">Loading Unit Type suggestions</span>}
    </section>
  )
}

interface UnitFormFieldsProps {
  form: UnitFormState
  onChange(next: UnitFormState): void
  knownUnitTypes: UnitTypeAvailability[]
  verifiedUnitType: UnitTypeAvailability | null
  verifying: boolean
  onVerify(): void
  showUnitCode?: boolean
  unitTypeDisabled?: boolean
}

function UnitFormFields({
  form,
  onChange,
  knownUnitTypes,
  verifiedUnitType,
  verifying,
  onVerify,
  showUnitCode = false,
  unitTypeDisabled = false,
}: UnitFormFieldsProps) {
  return (
    <>
      {showUnitCode && (
        <label className="physical-field">
          <span>Unit Code <strong>*</strong></span>
          <input
            value={form.unitCode}
            onChange={(event) => onChange({ ...form, unitCode: event.target.value })}
            placeholder="e.g. D7-A-001"
            autoComplete="off"
            required
          />
          <small>Operational code for the physical StorageUnit.</small>
        </label>
      )}

      <label className="physical-field">
        <span>Unit Type <strong>*</strong></span>
        <div className="physical-verify-row">
          <select
            value={form.unitTypeId}
            onChange={(event) => onChange({ ...form, unitTypeId: event.target.value })}
            disabled={unitTypeDisabled}
            required
          >
            <option value="">Choose a unit type</option>
            {form.unitTypeId && !knownUnitTypes.some(type => type.unitTypeId === form.unitTypeId) &&
              <option value={form.unitTypeId} disabled>{verifiedUnitType?.name || 'Current unit type unavailable'}</option>}
            {knownUnitTypes.map(type => <option key={type.unitTypeId} value={type.unitTypeId}>{type.name} · {type.mode} · {type.size}</option>)}
          </select>
          <button type="button" onClick={onVerify} disabled={unitTypeDisabled || verifying || !form.unitTypeId.trim()}>
            {verifying ? 'Checking…' : 'Verify'}
          </button>
        </div>
        <small>
          Manager may read existing Unit Types but cannot create or change Unit Type master data or Rental Price.
        </small>
      </label>

      {verifiedUnitType && verifiedUnitType.unitTypeId === form.unitTypeId.trim() && (
        <div className="physical-unit-type-preview">
          <div>
            <span>{verifiedUnitType.mode}</span>
            <strong>{verifiedUnitType.name}</strong>
          </div>
          <dl>
            <div><dt>Size</dt><dd>{verifiedUnitType.size}</dd></div>
            <div><dt>Current price</dt><dd>{formatMoney(verifiedUnitType.rentalPrice)}</dd></div>
          </dl>
        </div>
      )}

      <label className="physical-field">
        <span>Location information</span>
        <textarea
          value={form.locationInfo}
          onChange={(event) => onChange({ ...form, locationInfo: event.target.value })}
          placeholder="e.g. Floor 1 - Zone A"
          rows={3}
        />
        <small>Physical location/operational note for the unit.</small>
      </label>
    </>
  )
}

function FormError({ error }: { error: UiMessage | null }) {
  if (!error) {
    return null
  }

  return (
    <div className="handover-alert handover-alert-error physical-form-error" role="alert">
      <strong>{error.message}</strong>
      {error.details?.map((detail) => <span key={detail}>{detail}</span>)}
      {error.traceId && <small>Trace: {error.traceId}</small>}
    </div>
  )
}
