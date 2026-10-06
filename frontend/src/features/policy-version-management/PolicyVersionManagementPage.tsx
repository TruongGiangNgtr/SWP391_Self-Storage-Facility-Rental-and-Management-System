import {
  type SyntheticEvent,
  useEffect,
  useMemo,
  useState,
} from 'react'
import { businessApi } from '../../api/businessApi'
import { ApiRequestError } from '../../api/httpClient'
import type {
  CreatePolicyVersionRequest,
  Policy,
  PolicyStatus,
} from '../../models/policy'
import { formatUtcDateTime } from '../../utils/formatters'

type PolicyStatusFilter = 'ALL' | PolicyStatus

type PolicyForm = Record<keyof CreatePolicyVersionRequest, string>
type PolicyFormErrors = Partial<Record<keyof CreatePolicyVersionRequest, string>>

const defaultPolicyForm: PolicyForm = {
  depositTimeoutHours: '1',
  reservationVisitStartDay: '1',
  reservationVisitEndDay: '5',
  monthlyPaymentDueDay: '5',
  overdueStartDay: '6',
  lateFeeDivisorDays: '31',
  earlyReturnWaiveFeeUntilDay: '5',
}

function formFromPolicy(policy: Policy | undefined): PolicyForm {
  if (!policy) {
    return defaultPolicyForm
  }

  return {
    depositTimeoutHours: String(policy.depositTimeoutHours),
    reservationVisitStartDay: String(policy.reservationVisitStartDay),
    reservationVisitEndDay: String(policy.reservationVisitEndDay),
    monthlyPaymentDueDay: String(policy.monthlyPaymentDueDay),
    overdueStartDay: String(policy.overdueStartDay),
    lateFeeDivisorDays: String(policy.lateFeeDivisorDays),
    earlyReturnWaiveFeeUntilDay: String(policy.earlyReturnWaiveFeeUntilDay),
  }
}

function formatEffectiveTo(value: string | null): string {
  return value ? formatUtcDateTime(value) : 'Current'
}

export function PolicyVersionManagementPage() {
  const [policies, setPolicies] = useState<Policy[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [pageError, setPageError] = useState<string | null>(null)
  const [statusFilter, setStatusFilter] =
    useState<PolicyStatusFilter>('ALL')
  const [searchTerm, setSearchTerm] = useState('')
  const [currentPage, setCurrentPage] = useState(1)
  const [selectedPolicy, setSelectedPolicy] =
    useState<Policy | null>(null)
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [form, setForm] = useState<PolicyForm>(defaultPolicyForm)
  const [formErrors, setFormErrors] =
    useState<PolicyFormErrors>({})
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)

  const pageSize = 5

  async function refreshPolicies() {
    const data = await businessApi.listAllPolicies()
    setPolicies(data)
  }

  useEffect(() => {
    let cancelled = false

    async function loadPolicies() {
      setIsLoading(true)
      setPageError(null)

      try {
        const data = await businessApi.listAllPolicies()

        if (!cancelled) {
          setPolicies(data)
        }
      } catch (error) {
        if (cancelled) {
          return
        }

        setPolicies([])

        if (error instanceof ApiRequestError) {
          if (error.status === 401) {
            setPageError(
              'Unauthorized. Please sign in as Business Operations Manager.',
            )
            return
          }

          if (error.status === 403) {
            setPageError(
              'You do not have permission to access Policy Version Management.',
            )
            return
          }

          setPageError(error.message)
          return
        }

        setPageError('Unable to load policy versions.')
      } finally {
        if (!cancelled) {
          setIsLoading(false)
        }
      }
    }

    void loadPolicies()

    return () => {
      cancelled = true
    }
  }, [])

  const activePolicy = useMemo(
    () => policies.find((policy) => policy.status === 'ACTIVE'),
    [policies],
  )

  const filteredPolicies = useMemo(() => {
    const normalizedSearch = searchTerm.trim().toLowerCase()

    return policies.filter((policy) => {
      const matchesStatus =
        statusFilter === 'ALL' || policy.status === statusFilter

      const matchesSearch =
        normalizedSearch.length === 0 ||
        `v${policy.version}`.includes(normalizedSearch) ||
        String(policy.version).includes(normalizedSearch) ||
        policy.policyId.toLowerCase().includes(normalizedSearch)

      return matchesStatus && matchesSearch
    })
  }, [policies, searchTerm, statusFilter])

  const totalPages = Math.max(
    1,
    Math.ceil(filteredPolicies.length / pageSize),
  )

  const paginatedPolicies = useMemo(() => {
    const startIndex = (currentPage - 1) * pageSize
    return filteredPolicies.slice(startIndex, startIndex + pageSize)
  }, [currentPage, filteredPolicies])

  const firstVisibleItem =
    filteredPolicies.length === 0
      ? 0
      : (currentPage - 1) * pageSize + 1

  const lastVisibleItem = Math.min(
    currentPage * pageSize,
    filteredPolicies.length,
  )

  function openCreateModal() {
    setForm(formFromPolicy(activePolicy))
    setFormErrors({})
    setSubmitError(null)
    setIsCreateOpen(true)
  }

  function closeCreateModal() {
    if (isSaving) {
      return
    }

    setIsCreateOpen(false)
    setFormErrors({})
    setSubmitError(null)
  }

  function updateField(field: keyof PolicyForm, value: string) {
    setForm((current) => ({ ...current, [field]: value }))
    setFormErrors((current) => ({ ...current, [field]: undefined }))
    setSubmitError(null)
  }

  function parsePositiveInteger(
    field: keyof PolicyForm,
    label: string,
    max?: number,
  ): number | null {
    const raw = form[field].trim()
    const value = Number(raw)

    if (!raw) {
      setFormErrors((current) => ({
        ...current,
        [field]: `${label} is required.`,
      }))
      return null
    }

    if (!Number.isInteger(value) || value < 1) {
      setFormErrors((current) => ({
        ...current,
        [field]: `${label} must be a positive whole number.`,
      }))
      return null
    }

    if (max !== undefined && value > max) {
      setFormErrors((current) => ({
        ...current,
        [field]: `${label} must be between 1 and ${max}.`,
      }))
      return null
    }

    return value
  }

  function validateForm(): CreatePolicyVersionRequest | null {
    setFormErrors({})

    const depositTimeoutHours = parsePositiveInteger(
      'depositTimeoutHours',
      'Deposit timeout',
    )
    const reservationVisitStartDay = parsePositiveInteger(
      'reservationVisitStartDay',
      'Reservation visit start day',
      31,
    )
    const reservationVisitEndDay = parsePositiveInteger(
      'reservationVisitEndDay',
      'Reservation visit end day',
      31,
    )
    const monthlyPaymentDueDay = parsePositiveInteger(
      'monthlyPaymentDueDay',
      'Monthly payment due day',
      31,
    )
    const overdueStartDay = parsePositiveInteger(
      'overdueStartDay',
      'Overdue start day',
      31,
    )
    const lateFeeDivisorDays = parsePositiveInteger(
      'lateFeeDivisorDays',
      'Late fee divisor days',
    )
    const earlyReturnWaiveFeeUntilDay = parsePositiveInteger(
      'earlyReturnWaiveFeeUntilDay',
      'Early return waive fee cutoff',
      31,
    )

    if (
      depositTimeoutHours === null ||
      reservationVisitStartDay === null ||
      reservationVisitEndDay === null ||
      monthlyPaymentDueDay === null ||
      overdueStartDay === null ||
      lateFeeDivisorDays === null ||
      earlyReturnWaiveFeeUntilDay === null
    ) {
      return null
    }

    if (reservationVisitStartDay > reservationVisitEndDay) {
      setFormErrors((current) => ({
        ...current,
        reservationVisitEndDay:
          'Reservation visit end day must be greater than or equal to start day.',
      }))
      return null
    }

    return {
      depositTimeoutHours,
      reservationVisitStartDay,
      reservationVisitEndDay,
      monthlyPaymentDueDay,
      overdueStartDay,
      lateFeeDivisorDays,
      earlyReturnWaiveFeeUntilDay,
    }
  }

  async function handleCreateVersion(
    event: SyntheticEvent<HTMLFormElement>,
  ) {
    event.preventDefault()

    if (isSaving) {
      return
    }

    const request = validateForm()
    if (!request) {
      return
    }

    setIsSaving(true)
    setSubmitError(null)

    try {
      await businessApi.createPolicyVersion(request)
      await refreshPolicies()
      setCurrentPage(1)
      setStatusFilter('ALL')
      setSearchTerm('')
      setIsCreateOpen(false)
      setFormErrors({})
    } catch (error) {
      if (error instanceof ApiRequestError) {
        if (error.code === 'VALIDATION_ERROR' && error.errors) {
          const serverErrors: PolicyFormErrors = {}

          for (const [field, messages] of Object.entries(error.errors)) {
            if (field in form && messages.length > 0) {
              serverErrors[field as keyof PolicyForm] = messages[0]
            }
          }

          setFormErrors(serverErrors)
          setSubmitError(
            Object.keys(serverErrors).length === 0
              ? error.message
              : null,
          )
          return
        }

        if (error.code === 'POLICY_VERSION_CONFLICT') {
          setSubmitError(
            'Another policy version was created at the same time. Reload the latest policy and try again.',
          )
          return
        }

        setSubmitError(error.message)
        return
      }

      setSubmitError('Unable to create policy version.')
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <section className="facility-page">
      <div className="facility-breadcrumb">
        Business Operations / Policies
      </div>

      <div className="facility-page-heading">
        <div>
          <span className="facility-eyebrow">
            POLICY VERSION MANAGEMENT
          </span>

          <h1>Policy Versions</h1>

          <p>
            Review the full policy history and create a new active
            configuration without overwriting previous versions.
          </p>
        </div>

        <div className="facility-heading-actions">
          <span className="facility-protection-note">
            Versioned Configuration · Historical References Protected
          </span>

          <button
            type="button"
            className="facility-create-button"
            onClick={openCreateModal}
            disabled={isLoading || Boolean(pageError)}
          >
            + Create Policy Version
          </button>
        </div>
      </div>

      <div className="facility-summary-grid">
        <article className="facility-summary-card">
          <div>
            <span className="facility-summary-label">
              TOTAL VERSIONS
            </span>
            <strong>{policies.length}</strong>
            <p>Immutable Policy rows in the registry</p>
          </div>
          <div className="facility-summary-icon">V</div>
        </article>

        <article className="facility-summary-card">
          <div>
            <span className="facility-summary-label">
              ACTIVE VERSION
            </span>
            <strong>
              {activePolicy ? `v${activePolicy.version}` : '—'}
            </strong>
            <p className="facility-summary-success">
              Used by new Reservations
            </p>
          </div>
          <div className="facility-summary-icon facility-summary-icon--active">
            A
          </div>
        </article>

        <article className="facility-summary-card">
          <div>
            <span className="facility-summary-label">
              HISTORICAL VERSIONS
            </span>
            <strong>
              {policies.filter((policy) => policy.status === 'INACTIVE').length}
            </strong>
            <p>Retained for captured lifecycle references</p>
          </div>
          <div className="facility-summary-icon">H</div>
        </article>
      </div>

      <div className="facility-toolbar">
        <div className="facility-search-wrapper">
          <span className="facility-search-icon">⌕</span>
          <input
            type="search"
            value={searchTerm}
            onChange={(event) => {
              setSearchTerm(event.target.value)
              setCurrentPage(1)
            }}
            placeholder="Search by version or Policy ID..."
            className="facility-search-input"
          />
        </div>

        <div className="facility-status-filters">
          {(['ALL', 'ACTIVE', 'INACTIVE'] as const).map((status) => (
            <button
              key={status}
              type="button"
              className={
                statusFilter === status
                  ? 'facility-filter-button facility-filter-button--active'
                  : 'facility-filter-button'
              }
              onClick={() => {
                setStatusFilter(status)
                setCurrentPage(1)
              }}
            >
              {status === 'ALL'
                ? 'All'
                : status === 'ACTIVE'
                  ? 'Active'
                  : 'Inactive'}
            </button>
          ))}
        </div>

        <button
          type="button"
          className="facility-clear-button"
          onClick={() => {
            setSearchTerm('')
            setStatusFilter('ALL')
            setCurrentPage(1)
          }}
        >
          Clear filters
        </button>
      </div>

      <div className="facility-table-card">
        <div className="facility-table-header">
          <div>
            <h2>Policy Version Registry</h2>
            <p>
              New versions are activated atomically while the previous
              active version becomes historical.
            </p>
          </div>
          <span>Showing {filteredPolicies.length} versions</span>
        </div>

        <div className="facility-table-scroll">
          <table className="facility-table policy-table">
            <thead>
              <tr>
                <th>Version</th>
                <th>Status</th>
                <th>Reservation Rules</th>
                <th>Billing Rules</th>
                <th>Effective Period</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {isLoading && (
                <tr>
                  <td colSpan={6} className="facility-loading-state">
                    <div className="facility-state-content">
                      <div className="facility-spinner" />
                      <strong>Loading policy versions...</strong>
                      <span>
                        Retrieving the versioned business-rule configuration.
                      </span>
                    </div>
                  </td>
                </tr>
              )}

              {pageError && (
                <tr>
                  <td colSpan={6} className="facility-error-state">
                    <div className="facility-state-content">
                      <div className="facility-error-icon">!</div>
                      <strong>Unable to load policy versions</strong>
                      <span>{pageError}</span>
                    </div>
                  </td>
                </tr>
              )}

              {!isLoading &&
                !pageError &&
                paginatedPolicies.map((policy) => (
                  <tr key={policy.policyId}>
                    <td>
                      <div className="facility-name-cell">
                        <strong>Version {policy.version}</strong>
                        <span>{policy.policyId}</span>
                      </div>
                    </td>
                    <td>
                      <span
                        className={
                          policy.status === 'ACTIVE'
                            ? 'facility-status facility-status--active'
                            : 'facility-status facility-status--inactive'
                        }
                      >
                        {policy.status}
                      </span>
                    </td>
                    <td>
                      <div className="facility-location-cell">
                        <strong>
                          Deposit timeout: {policy.depositTimeoutHours}h
                        </strong>
                        <span>
                          Visit window: day {policy.reservationVisitStartDay}–
                          {policy.reservationVisitEndDay}
                        </span>
                      </div>
                    </td>
                    <td>
                      <div className="facility-location-cell">
                        <strong>
                          Due day {policy.monthlyPaymentDueDay} · Overdue day{' '}
                          {policy.overdueStartDay}
                        </strong>
                        <span>
                          Late divisor {policy.lateFeeDivisorDays} · Early-return
                          cutoff day {policy.earlyReturnWaiveFeeUntilDay}
                        </span>
                      </div>
                    </td>
                    <td>
                      <div className="facility-location-cell">
                        <strong>{formatUtcDateTime(policy.effectiveFrom)}</strong>
                        <span>to {formatEffectiveTo(policy.effectiveTo)}</span>
                      </div>
                    </td>
                    <td>
                      <div className="facility-actions">
                        <button
                          type="button"
                          className="facility-action-button facility-action-button--activate"
                          onClick={() => setSelectedPolicy(policy)}
                        >
                          View Details
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}

              {!isLoading && !pageError && policies.length === 0 && (
                <tr>
                  <td colSpan={6} className="facility-empty-state">
                    <div className="facility-state-content">
                      <strong>No policy versions available</strong>
                      <span>
                        The system requires an approved Policy baseline before
                        version management can be used.
                      </span>
                    </div>
                  </td>
                </tr>
              )}

              {!isLoading &&
                !pageError &&
                policies.length > 0 &&
                filteredPolicies.length === 0 && (
                  <tr>
                    <td colSpan={6} className="facility-empty-state">
                      <div className="facility-state-content">
                        <strong>No matching policy versions</strong>
                        <span>
                          Try another version/Policy ID or clear the current
                          filters.
                        </span>
                        <button
                          type="button"
                          className="facility-secondary-button"
                          onClick={() => {
                            setSearchTerm('')
                            setStatusFilter('ALL')
                            setCurrentPage(1)
                          }}
                        >
                          Clear Filters
                        </button>
                      </div>
                    </td>
                  </tr>
                )}
            </tbody>
          </table>
        </div>

        <div className="facility-table-footer">
          <span>Rows per page: {pageSize}</span>
          <div className="facility-pagination">
            <span>
              Showing {firstVisibleItem}–{lastVisibleItem} of{' '}
              {filteredPolicies.length} versions
            </span>
            <button
              type="button"
              className="facility-page-button"
              disabled={currentPage === 1}
              onClick={() =>
                setCurrentPage((current) => Math.max(1, current - 1))
              }
            >
              Previous
            </button>
            <span className="facility-page-number">{currentPage}</span>
            <button
              type="button"
              className="facility-page-button"
              disabled={
                currentPage === totalPages || filteredPolicies.length === 0
              }
              onClick={() =>
                setCurrentPage((current) => Math.min(totalPages, current + 1))
              }
            >
              Next
            </button>
          </div>
        </div>
      </div>

      <div className="facility-governance-card">
        <div className="facility-governance-icon">§</div>
        <div>
          <h2>Versioning &amp; Historical Reference Protection</h2>
          <p>
            Policy changes never overwrite an existing Policy row. Creating a
            version inserts a new ACTIVE row and inactivates the previous one.
            Existing Reservations and Contracts continue using the PolicyId they
            already captured, so later configuration changes do not rewrite
            historical lifecycle behavior.
          </p>
        </div>
      </div>

      {isCreateOpen && (
        <div
          className="facility-modal-backdrop"
          onMouseDown={closeCreateModal}
        >
          <div
            className="facility-modal policy-modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="create-policy-version-title"
            onMouseDown={(event) => event.stopPropagation()}
          >
            <div className="facility-modal-header">
              <div>
                <span className="facility-eyebrow">POLICY VERSION</span>
                <h2 id="create-policy-version-title">
                  Create New Policy Version
                </h2>
                <p>
                  Start from the current active values, then change only the
                  configuration that should apply to new lifecycles.
                </p>
              </div>
              <button
                type="button"
                className="facility-modal-close"
                onClick={closeCreateModal}
                aria-label="Close create policy version dialog"
                disabled={isSaving}
              >
                ×
              </button>
            </div>

            <div className="facility-modal-notice">
              {activePolicy ? (
                <>
                  Creating from <strong>Version {activePolicy.version}</strong>.
                  The server will create the next version as ACTIVE and mark the
                  previous active row INACTIVE.
                </>
              ) : (
                <>
                  No active Policy is visible. The form uses the approved
                  baseline defaults and the server remains authoritative for
                  version creation.
                </>
              )}
            </div>

            <form className="facility-form" onSubmit={handleCreateVersion}>
              <div className="policy-form-grid">
                <PolicyNumberField
                  id="policy-deposit-timeout"
                  label="Deposit Timeout (hours)"
                  value={form.depositTimeoutHours}
                  error={formErrors.depositTimeoutHours}
                  onChange={(value) => updateField('depositTimeoutHours', value)}
                  disabled={isSaving}
                  min={1}
                />

                <PolicyNumberField
                  id="policy-late-divisor"
                  label="Late Fee Divisor (days)"
                  value={form.lateFeeDivisorDays}
                  error={formErrors.lateFeeDivisorDays}
                  onChange={(value) => updateField('lateFeeDivisorDays', value)}
                  disabled={isSaving}
                  min={1}
                />

                <PolicyNumberField
                  id="policy-visit-start"
                  label="Reservation Visit Start Day"
                  value={form.reservationVisitStartDay}
                  error={formErrors.reservationVisitStartDay}
                  onChange={(value) =>
                    updateField('reservationVisitStartDay', value)
                  }
                  disabled={isSaving}
                  min={1}
                  max={31}
                />

                <PolicyNumberField
                  id="policy-visit-end"
                  label="Reservation Visit End Day"
                  value={form.reservationVisitEndDay}
                  error={formErrors.reservationVisitEndDay}
                  onChange={(value) =>
                    updateField('reservationVisitEndDay', value)
                  }
                  disabled={isSaving}
                  min={1}
                  max={31}
                />

                <PolicyNumberField
                  id="policy-payment-due"
                  label="Monthly Payment Due Day"
                  value={form.monthlyPaymentDueDay}
                  error={formErrors.monthlyPaymentDueDay}
                  onChange={(value) => updateField('monthlyPaymentDueDay', value)}
                  disabled={isSaving}
                  min={1}
                  max={31}
                />

                <PolicyNumberField
                  id="policy-overdue-start"
                  label="Overdue Start Day"
                  value={form.overdueStartDay}
                  error={formErrors.overdueStartDay}
                  onChange={(value) => updateField('overdueStartDay', value)}
                  disabled={isSaving}
                  min={1}
                  max={31}
                />

                <PolicyNumberField
                  id="policy-early-return-cutoff"
                  label="Early Return Waive Fee Until Day"
                  value={form.earlyReturnWaiveFeeUntilDay}
                  error={formErrors.earlyReturnWaiveFeeUntilDay}
                  onChange={(value) =>
                    updateField('earlyReturnWaiveFeeUntilDay', value)
                  }
                  disabled={isSaving}
                  min={1}
                  max={31}
                />
              </div>

              {submitError && (
                <div className="policy-submit-error" role="alert">
                  {submitError}
                </div>
              )}

              <div className="facility-modal-actions">
                <button
                  type="button"
                  className="facility-secondary-button"
                  onClick={closeCreateModal}
                  disabled={isSaving}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="facility-primary-button"
                  disabled={isSaving}
                >
                  {isSaving ? 'Creating...' : 'Create & Activate Version'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {selectedPolicy && (
        <div
          className="facility-modal-backdrop"
          onMouseDown={() => setSelectedPolicy(null)}
        >
          <div
            className="facility-modal facility-detail-modal policy-detail-modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="policy-detail-title"
            onMouseDown={(event) => event.stopPropagation()}
          >
            <div className="facility-modal-header">
              <div>
                <span className="facility-eyebrow">POLICY VERSION</span>
                <h2 id="policy-detail-title">
                  Version {selectedPolicy.version}
                </h2>
                <p>
                  Read-only configuration. Historical policy values are never
                  edited in place.
                </p>
              </div>
              <button
                type="button"
                className="facility-modal-close"
                onClick={() => setSelectedPolicy(null)}
                aria-label="Close policy detail dialog"
              >
                ×
              </button>
            </div>

            <div className="facility-detail-body">
              <PolicyDetailRow label="Policy ID" value={selectedPolicy.policyId} />
              <PolicyDetailRow
                label="Status"
                value={selectedPolicy.status}
              />
              <PolicyDetailRow
                label="Effective From"
                value={formatUtcDateTime(selectedPolicy.effectiveFrom)}
              />
              <PolicyDetailRow
                label="Effective To"
                value={formatEffectiveTo(selectedPolicy.effectiveTo)}
              />
              <PolicyDetailRow
                label="Deposit Timeout"
                value={`${selectedPolicy.depositTimeoutHours} hour(s)`}
              />
              <PolicyDetailRow
                label="Reservation Visit Window"
                value={`Day ${selectedPolicy.reservationVisitStartDay} to day ${selectedPolicy.reservationVisitEndDay}`}
              />
              <PolicyDetailRow
                label="Monthly Payment Due Day"
                value={`Day ${selectedPolicy.monthlyPaymentDueDay}`}
              />
              <PolicyDetailRow
                label="Overdue Start Day"
                value={`Day ${selectedPolicy.overdueStartDay}`}
              />
              <PolicyDetailRow
                label="Late Fee Divisor"
                value={`${selectedPolicy.lateFeeDivisorDays} day(s)`}
              />
              <PolicyDetailRow
                label="Early Return Waive Cutoff"
                value={`Day ${selectedPolicy.earlyReturnWaiveFeeUntilDay}`}
              />
              <PolicyDetailRow
                label="Created At"
                value={formatUtcDateTime(selectedPolicy.createdAt)}
              />
            </div>

            <div className="facility-detail-footer">
              <button
                type="button"
                className="facility-secondary-button"
                onClick={() => setSelectedPolicy(null)}
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </section>
  )
}

interface PolicyNumberFieldProps {
  id: string
  label: string
  value: string
  error?: string
  disabled: boolean
  min: number
  max?: number
  onChange: (value: string) => void
}

function PolicyNumberField({
  id,
  label,
  value,
  error,
  disabled,
  min,
  max,
  onChange,
}: PolicyNumberFieldProps) {
  return (
    <div className="facility-form-field">
      <label htmlFor={id}>
        {label}<span>*</span>
      </label>
      <input
        id={id}
        type="number"
        step="1"
        min={min}
        max={max}
        inputMode="numeric"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        disabled={disabled}
      />
      {error && <small className="facility-form-error">{error}</small>}
    </div>
  )
}

function PolicyDetailRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="facility-detail-row">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  )
}
