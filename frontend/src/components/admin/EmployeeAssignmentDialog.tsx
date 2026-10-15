import { ShieldCheck, X } from 'lucide-react'
import { listAllAdminFacilities, type AdminFacilityLookup } from '../../api/adminApi'
import { ApiRequestError } from '../../api/httpClient'
import '../../styles/adminRoleFacilityAssignment.css'
import {
  useEffect,
  useMemo,
  useState,
  type ChangeEvent,
  type FormEvent,
} from 'react'
import type {
  AssignAdminEmployeeRequest,
  EmployeeRole,
} from '../../models/adminEmployee'
import {
  ADMIN_EMPLOYEE_ROLES,
  isFacilityScopedEmployeeRole,
} from '../../models/adminEmployee'
import type {
  AdminApiErrorShape,
  AdminUserAccount,
} from '../../models/adminUser'
import { USER_ROLE_LABELS } from '../../models/adminUser'

interface Props {
  account: AdminUserAccount
  busy: boolean
  error: AdminApiErrorShape | null
  onClose: () => void
  onSubmit: (request: AssignAdminEmployeeRequest) => Promise<void>
}

function employeeRole(account: AdminUserAccount): EmployeeRole {
  return account.role === 'CUSTOMER'
    ? 'FACILITY_STAFF'
    : account.role
}

function getServerFieldError(error: AdminApiErrorShape | null, field: string) {
  return error?.errors?.[field]?.[0] ?? null
}

export function EmployeeAssignmentDialog({
  account,
  busy,
  error,
  onClose,
  onSubmit,
}: Props) {
  const [role, setRole] = useState<EmployeeRole>(() => employeeRole(account))
  const [facilityId, setFacilityId] = useState(account.profile?.facilityId ?? '')
  const [clientErrors, setClientErrors] = useState<Record<string, string>>({})
  const [facilities, setFacilities] = useState<AdminFacilityLookup[]>([])
  const [facilitiesLoading, setFacilitiesLoading] = useState(true)
  const [facilitiesError, setFacilitiesError] = useState<string | null>(null)
  const [facilitiesReload, setFacilitiesReload] = useState(0)

  // Load all pages from the System Administrator facility lookup.
  // Keep the original FacilityId for ADM-009; labels are Facility names only.
  useEffect(() => {
    let cancelled = false

    async function loadFacilities() {
      setFacilitiesLoading(true)
      setFacilitiesError(null)
      try {
        const result = await listAllAdminFacilities()
        if (!cancelled) {
          setFacilities(result.sort((a, b) => a.name.localeCompare(b.name, 'vi')))
        }
      } catch (loadError) {
        if (cancelled) return
        setFacilities([])
        if (loadError instanceof ApiRequestError && loadError.status === 403) {
          setFacilitiesError('You do not have permission to view Facilities. Verify your Facility list read access.')
        } else if (loadError instanceof ApiRequestError && loadError.status === 401) {
          setFacilitiesError('Your session has expired. Sign in again to load Facilities.')
        } else {
          setFacilitiesError('Unable to load Facilities. Try again.')
        }
      } finally {
        if (!cancelled) setFacilitiesLoading(false)
      }
    }

    void loadFacilities()
    return () => { cancelled = true }
  }, [facilitiesReload])

  const facilityRequired = useMemo(
    () => isFacilityScopedEmployeeRole(role),
    [role],
  )

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !busy) {
        onClose()
      }
    }

    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [busy, onClose])

  const fieldError = (field: string) =>
    clientErrors[field] ?? getServerFieldError(error, field)

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const nextErrors: Record<string, string> = {}

    if (facilityRequired) {
      if (facilitiesLoading || facilitiesError) {
        nextErrors.facilityId = 'Load the Facility list before saving this assignment.'
      } else if (!facilityId) {
        nextErrors.facilityId = 'Select a Facility for Facility Staff or Facility Manager.'
      } else if (!facilities.some((facility) => facility.facilityId === facilityId)) {
        nextErrors.facilityId = 'Select a Facility from the current list.'
      }
    }

    setClientErrors(nextErrors)
    if (Object.keys(nextErrors).length > 0) {
      return
    }

    await onSubmit({
      role,
      facilityId: facilityRequired ? facilityId : null,
    })
  }

  return (
    <div className="awp03-modal-layer" role="presentation">
      <div
        className="awp03-modal awp04-assignment-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="awp04-assignment-title"
        data-testid="awp04-assignment-dialog"
      >
        <header className="awp03-modal__header">
          <div>
            <p className="awp01-eyebrow">ADM-009 · BR-EMP-01</p>
            <h2 id="awp04-assignment-title">Assign role &amp; facility</h2>
          </div>
          <button
            className="awp01-icon-button"
            type="button"
            onClick={onClose}
            disabled={busy}
            aria-label="Close assignment dialog"
          >
            <X size={18} />
          </button>
        </header>

        <div className="awp04-assignment-person">
          <span className="awp04-assignment-person__icon" aria-hidden="true">
            <ShieldCheck size={18} />
          </span>
          <div>
            <strong>{account.profile?.fullName || 'Employee'}</strong>
            <span>{account.email}</span>
          </div>
        </div>

        <div className="awp03-info-note">
          Facility Staff and Facility Manager must have exactly one Facility. Business Operations Manager and System Administrator are global roles and must not carry a Facility assignment.
        </div>

        {error && (
          <div className="awp03-inline-error" role="alert">
            <strong>{error.code}</strong>
            <span>{error.message}</span>
            {error.traceId ? <small>Trace: {error.traceId}</small> : null}
          </div>
        )}

        <form className="awp03-form" onSubmit={(event: FormEvent<HTMLFormElement>) => void submit(event)} noValidate>
          <label>
            <span>Employee role</span>
            <select
              autoFocus
              value={role}
              onChange={(event: ChangeEvent<HTMLSelectElement>) => {
                const nextRole = event.target.value as EmployeeRole
                setRole(nextRole)
                setClientErrors({})
                if (!isFacilityScopedEmployeeRole(nextRole)) {
                  setFacilityId('')
                }
              }}
              disabled={busy}
              aria-invalid={Boolean(fieldError('role'))}
            >
              {ADMIN_EMPLOYEE_ROLES.map((value) => (
                <option key={value} value={value}>
                  {USER_ROLE_LABELS[value]}
                </option>
              ))}
            </select>
            {fieldError('role') ? (
              <small className="awp03-field-error">{fieldError('role')}</small>
            ) : null}
          </label>

          {facilityRequired ? (
            <>
              <label>
                <span>Facility</span>
                <select
                  value={facilitiesLoading || facilitiesError ? '' : facilityId}
                  onChange={(event: ChangeEvent<HTMLSelectElement>) => {
                    setFacilityId(event.target.value)
                    setClientErrors((current) => {
                      if (!current.facilityId) return current
                      const next = { ...current }
                      delete next.facilityId
                      return next
                    })
                  }}
                  disabled={busy || facilitiesLoading || Boolean(facilitiesError) || facilities.length === 0}
                  aria-invalid={Boolean(fieldError('facilityId'))}
                  aria-busy={facilitiesLoading}
                >
                  <option value="">
                    {facilitiesLoading ? 'Loading Facilities…' : facilitiesError ? 'Facilities unavailable' : facilities.length === 0 ? 'No Facilities available' : 'Select a Facility'}
                  </option>
                  {!facilitiesLoading && !facilitiesError && facilityId &&
                    !facilities.some((facility) => facility.facilityId === facilityId) && (
                      <option value={facilityId} disabled>Current Facility not found — select another</option>
                    )}
                  {facilities.map((facility) => (
                    <option key={facility.facilityId} value={facility.facilityId}>
                      {facility.name}{facility.status === 'INACTIVE' ? ' (Inactive)' : ''}
                    </option>
                  ))}
                </select>
                {fieldError('facilityId') ? (
                  <small className="awp03-field-error">{fieldError('facilityId')}</small>
                ) : null}
                <small>Facility names are loaded from the Admin facility directory. The selected Facility ID is sent to ADM-009.</small>
              </label>
              {facilitiesError ? (
                <div className="awp04-facility-load-error" role="alert">
                  <small>{facilitiesError}</small>
                  <button
                    className="awp01-button awp01-button--secondary"
                    type="button"
                    onClick={() => setFacilitiesReload((current) => current + 1)}
                    disabled={busy}
                  >
                    Retry
                  </button>
                </div>
              ) : null}
            </>
          ) : (
            <div className="awp03-field-readonly">
              <span>Facility</span>
              <strong>None — global Employee role</strong>
            </div>
          )}

          <div className="awp04-current-assignment" aria-label="Current assignment">
            <span>Current</span>
            <strong>{USER_ROLE_LABELS[account.role]}</strong>
            <code>{account.profile?.facilityId ? (facilities.find((facility) => facility.facilityId === account.profile?.facilityId)?.name ?? account.profile.facilityId) : 'Global / no Facility'}</code>
          </div>

          <footer className="awp03-modal__actions">
            <button
              className="awp01-button awp01-button--secondary"
              type="button"
              onClick={onClose}
              disabled={busy}
            >
              Cancel
            </button>
            <button
              className="awp01-button awp03-button--primary"
              type="submit"
              disabled={busy || (facilityRequired && (facilitiesLoading || Boolean(facilitiesError) || facilities.length === 0 || !facilities.some((facility) => facility.facilityId === facilityId)))}
            >
              {busy ? 'Saving assignment…' : 'Save assignment'}
            </button>
          </footer>
        </form>
      </div>
    </div>
  )
}
