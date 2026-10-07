import { X } from 'lucide-react'
import {
  useEffect,
  useMemo,
  useState,
  type FormEvent,
} from 'react'
import type {
  AdminApiErrorShape,
  AdminUserAccount,
} from '../../models/adminUser'
import {
  ADMIN_EMPLOYEE_ROLES,
  isFacilityScopedEmployeeRole,
  type CreateAdminEmployeeRequest,
  type EmployeeRole,
  type UpdateAdminEmployeeRequest,
} from '../../models/adminEmployee'
import { USER_ROLE_LABELS } from '../../models/adminUser'

const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

type CreateSubmit = (request: CreateAdminEmployeeRequest) => Promise<void>
type UpdateSubmit = (request: UpdateAdminEmployeeRequest) => Promise<void>

type Props =
  | {
      mode: 'create'
      account?: never
      busy: boolean
      error: AdminApiErrorShape | null
      onClose: () => void
      onSubmit: CreateSubmit
    }
  | {
      mode: 'edit'
      account: AdminUserAccount
      busy: boolean
      error: AdminApiErrorShape | null
      onClose: () => void
      onSubmit: UpdateSubmit
    }

interface CreateFields {
  fullName: string
  email: string
  phoneNumber: string
  role: EmployeeRole
  facilityId: string
}

function getServerFieldError(error: AdminApiErrorShape | null, field: string) {
  return error?.errors?.[field]?.[0] ?? null
}

export function EmployeeAccountFormDialog(props: Props) {
  const editing = props.mode === 'edit'
  const [fullName, setFullName] = useState(editing ? props.account.profile?.fullName ?? '' : '')
  const [createFields, setCreateFields] = useState<CreateFields>({
    fullName: '',
    email: '',
    phoneNumber: '',
    role: 'FACILITY_STAFF',
    facilityId: '',
  })
  const [clientErrors, setClientErrors] = useState<Record<string, string>>({})

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !props.busy) {
        props.onClose()
      }
    }

    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [props])

  const facilityRequired = useMemo(
    () => !editing && isFacilityScopedEmployeeRole(createFields.role),
    [createFields.role, editing],
  )

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (editing) {
      const nextErrors: Record<string, string> = {}
      if (!fullName.trim()) {
        nextErrors.fullName = 'Full name is required.'
      }
      setClientErrors(nextErrors)
      if (Object.keys(nextErrors).length > 0) {
        return
      }

      await props.onSubmit({ fullName: fullName.trim() })
      return
    }

    const nextErrors: Record<string, string> = {}
    if (!createFields.fullName.trim()) {
      nextErrors.fullName = 'Full name is required.'
    }
    if (!createFields.email.trim()) {
      nextErrors.email = 'Email is required.'
    } else if (!EMAIL_PATTERN.test(createFields.email.trim())) {
      nextErrors.email = 'Enter a valid email address.'
    }
    if (!createFields.phoneNumber.trim()) {
      nextErrors.phoneNumber = 'Phone number is required.'
    }
    if (facilityRequired) {
      if (!createFields.facilityId.trim()) {
        nextErrors.facilityId = 'Facility ID is required for Facility Staff and Facility Manager.'
      } else if (!UUID_PATTERN.test(createFields.facilityId.trim())) {
        nextErrors.facilityId = 'Facility ID must be a valid UUID.'
      }
    }

    setClientErrors(nextErrors)
    if (Object.keys(nextErrors).length > 0) {
      return
    }

    await props.onSubmit({
      fullName: createFields.fullName.trim(),
      email: createFields.email.trim(),
      phoneNumber: createFields.phoneNumber.trim(),
      role: createFields.role,
      facilityId: facilityRequired ? createFields.facilityId.trim() : null,
    })
  }

  const fieldError = (field: string) => clientErrors[field] ?? getServerFieldError(props.error, field)

  return (
    <div className="awp03-modal-layer" role="presentation">
      <div
        className="awp03-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="awp03-form-title"
      >
        <header className="awp03-modal__header">
          <div>
            <p className="awp01-eyebrow">{editing ? 'ADM-006' : 'ADM-005'}</p>
            <h2 id="awp03-form-title">{editing ? 'Edit employee profile' : 'Create employee account'}</h2>
          </div>
          <button
            type="button"
            className="awp01-icon-button"
            aria-label="Close employee form"
            onClick={props.onClose}
            disabled={props.busy}
          >
            <X size={18} />
          </button>
        </header>

        {!editing && (
          <div className="awp03-info-note">
            FRMS creates the account as INACTIVE, generates a 16-character initial password, and sends it by email. The password is never shown in this UI.
          </div>
        )}

        {editing && (
          <div className="awp03-info-note">
            AWP-03 updates the employee basic profile only. Email and phone are Release 1 login/contact identifiers and are not editable; role/facility changes belong to AWP-04.
          </div>
        )}

        {props.error && (
          <div className="awp03-inline-error" role="alert">
            <strong>{props.error.code}</strong>
            <span>{props.error.message}</span>
            {props.error.traceId ? <small>Trace: {props.error.traceId}</small> : null}
          </div>
        )}

        <form className="awp03-form" onSubmit={(event) => void submit(event)} noValidate>
          <label>
            <span>Full name</span>
            <input
              autoFocus
              value={editing ? fullName : createFields.fullName}
              onChange={(event) => {
                if (editing) {
                  setFullName(event.target.value)
                } else {
                  setCreateFields({ ...createFields, fullName: event.target.value })
                }
              }}
              disabled={props.busy}
              aria-invalid={Boolean(fieldError('fullName'))}
            />
            {fieldError('fullName') ? <small className="awp03-field-error">{fieldError('fullName')}</small> : null}
          </label>

          {!editing && (
            <>
              <div className="awp03-form__row">
                <label>
                  <span>Email</span>
                  <input
                    type="email"
                    value={createFields.email}
                    onChange={(event) => setCreateFields({ ...createFields, email: event.target.value })}
                    disabled={props.busy}
                    aria-invalid={Boolean(fieldError('email'))}
                    placeholder="employee@example.com"
                  />
                  {fieldError('email') ? <small className="awp03-field-error">{fieldError('email')}</small> : null}
                </label>

                <label>
                  <span>Phone number</span>
                  <input
                    value={createFields.phoneNumber}
                    onChange={(event) => setCreateFields({ ...createFields, phoneNumber: event.target.value })}
                    disabled={props.busy}
                    aria-invalid={Boolean(fieldError('phoneNumber'))}
                    placeholder="0900000002"
                  />
                  {fieldError('phoneNumber') ? <small className="awp03-field-error">{fieldError('phoneNumber')}</small> : null}
                </label>
              </div>

              <label>
                <span>Employee role</span>
                <select
                  value={createFields.role}
                  onChange={(event) => setCreateFields({
                    ...createFields,
                    role: event.target.value as EmployeeRole,
                    facilityId: isFacilityScopedEmployeeRole(event.target.value as EmployeeRole)
                      ? createFields.facilityId
                      : '',
                  })}
                  disabled={props.busy}
                >
                  {ADMIN_EMPLOYEE_ROLES.map((role) => (
                    <option key={role} value={role}>{USER_ROLE_LABELS[role]}</option>
                  ))}
                </select>
              </label>

              {facilityRequired ? (
                <label>
                  <span>Facility ID</span>
                  <input
                    value={createFields.facilityId}
                    onChange={(event) => setCreateFields({ ...createFields, facilityId: event.target.value })}
                    disabled={props.busy}
                    aria-invalid={Boolean(fieldError('facilityId'))}
                    placeholder="00000000-0000-0000-0000-000000000000"
                  />
                  {fieldError('facilityId') ? <small className="awp03-field-error">{fieldError('facilityId')}</small> : null}
                  <small>Required by BR-EMP-01 for Facility Staff and Facility Manager.</small>
                </label>
              ) : (
                <div className="awp03-field-readonly">
                  <span>Facility</span>
                  <strong>Not applicable for global employee roles</strong>
                </div>
              )}
            </>
          )}

          <footer className="awp03-modal__actions">
            <button
              className="awp01-button awp01-button--secondary"
              type="button"
              onClick={props.onClose}
              disabled={props.busy}
            >
              Cancel
            </button>
            <button
              className="awp01-button awp03-button--primary"
              type="submit"
              disabled={props.busy}
            >
              {props.busy ? 'Saving…' : editing ? 'Save changes' : 'Create employee'}
            </button>
          </footer>
        </form>
      </div>
    </div>
  )
}
