import { ShieldCheck, X } from 'lucide-react'
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

const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i

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
      if (!facilityId.trim()) {
        nextErrors.facilityId = 'Facility ID is required for Facility Staff and Facility Manager.'
      } else if (!UUID_PATTERN.test(facilityId.trim())) {
        nextErrors.facilityId = 'Facility ID must be a valid UUID.'
      }
    }

    setClientErrors(nextErrors)
    if (Object.keys(nextErrors).length > 0) {
      return
    }

    await onSubmit({
      role,
      facilityId: facilityRequired ? facilityId.trim() : null,
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
            <label>
              <span>Facility ID</span>
              <input
                value={facilityId}
                onChange={(event: ChangeEvent<HTMLInputElement>) => {
                  setFacilityId(event.target.value)
                  setClientErrors((current) => {
                    if (!current.facilityId) return current
                    const next = { ...current }
                    delete next.facilityId
                    return next
                  })
                }}
                disabled={busy}
                aria-invalid={Boolean(fieldError('facilityId'))}
                placeholder="00000000-0000-0000-0000-000000000000"
              />
              {fieldError('facilityId') ? (
                <small className="awp03-field-error">{fieldError('facilityId')}</small>
              ) : null}
              <small>ADM-009 validates that the Facility exists before the assignment is persisted.</small>
            </label>
          ) : (
            <div className="awp03-field-readonly">
              <span>Facility</span>
              <strong>None — global Employee role</strong>
            </div>
          )}

          <div className="awp04-current-assignment" aria-label="Current assignment">
            <span>Current</span>
            <strong>{USER_ROLE_LABELS[account.role]}</strong>
            <code>{account.profile?.facilityId ?? 'Global / no Facility'}</code>
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
              disabled={busy}
            >
              {busy ? 'Saving assignment…' : 'Save assignment'}
            </button>
          </footer>
        </form>
      </div>
    </div>
  )
}
