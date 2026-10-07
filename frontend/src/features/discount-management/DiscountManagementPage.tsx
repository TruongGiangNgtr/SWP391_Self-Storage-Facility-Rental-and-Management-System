import {
  type SyntheticEvent,
  useMemo,
  useState,
} from 'react'
import { useSearchParams } from 'react-router-dom'
import { businessApi } from '../../api/businessApi'
import { ApiRequestError } from '../../api/httpClient'
import type {
  CreateCustomerDiscountRequest,
  Discount,
  DiscountStatus,
  UpdateDiscountRequest,
} from '../../models/discount'
import { formatUtcDateTime } from '../../utils/formatters'

interface DiscountForm {
  name: string
  percentage: string
  status: DiscountStatus
  effectiveFrom: string
  effectiveTo: string
}

interface DiscountFormErrors {
  name?: string
  percentage?: string
  effectiveFrom?: string
  effectiveTo?: string
}

const emptyForm: DiscountForm = {
  name: '',
  percentage: '',
  status: 'ACTIVE',
  effectiveFrom: '',
  effectiveTo: '',
}

const uuidPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i

function toUtcIso(localValue: string): string | null {
  if (!localValue) {
    return null
  }

  const valueWithSeconds = localValue.length === 16
    ? `${localValue}:00`
    : localValue
  const parsed = new Date(`${valueWithSeconds}+07:00`)

  return Number.isNaN(parsed.getTime()) ? null : parsed.toISOString()
}

function toBusinessDateTimeLocal(value: string | null): string {
  if (!value) {
    return ''
  }

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return ''
  }

  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Asia/Ho_Chi_Minh',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
    hourCycle: 'h23',
  }).formatToParts(date)

  const values = Object.fromEntries(
    parts.map((part) => [part.type, part.value]),
  )

  return `${values.year}-${values.month}-${values.day}T${values.hour}:${values.minute}`
}

function formFromDiscount(discount: Discount): DiscountForm {
  return {
    name: discount.name,
    percentage: String(discount.percentage),
    status: discount.status,
    effectiveFrom: toBusinessDateTimeLocal(discount.effectiveFrom),
    effectiveTo: toBusinessDateTimeLocal(discount.effectiveTo),
  }
}

function formatEffectivePeriod(discount: Discount): string {
  const start = formatUtcDateTime(discount.effectiveFrom)
  const end = discount.effectiveTo
    ? formatUtcDateTime(discount.effectiveTo)
    : 'No end date'

  return `${start} — ${end}`
}

export function DiscountManagementPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const initialCustomerId = searchParams.get('customerId') ?? ''

  const [customerIdInput, setCustomerIdInput] = useState(initialCustomerId)
  const [loadedCustomerId, setLoadedCustomerId] = useState('')
  const [discounts, setDiscounts] = useState<Discount[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [pageError, setPageError] = useState<string | null>(null)
  const [customerIdError, setCustomerIdError] = useState<string | null>(null)
  const [searchTerm, setSearchTerm] = useState('')
  const [statusFilter, setStatusFilter] =
    useState<'ALL' | DiscountStatus>('ALL')
  const [currentPage, setCurrentPage] = useState(1)
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [editingDiscount, setEditingDiscount] = useState<Discount | null>(null)
  const [form, setForm] = useState<DiscountForm>(emptyForm)
  const [formErrors, setFormErrors] = useState<DiscountFormErrors>({})
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  const [statusChangingId, setStatusChangingId] = useState<string | null>(null)

  const pageSize = 5

  const activeCount = discounts.filter(
    (discount) => discount.status === 'ACTIVE',
  ).length

  const inactiveCount = discounts.filter(
    (discount) => discount.status === 'INACTIVE',
  ).length

  const filteredDiscounts = useMemo(() => {
    const normalizedSearch = searchTerm.trim().toLowerCase()

    return discounts.filter((discount) => {
      const matchesStatus =
        statusFilter === 'ALL' || discount.status === statusFilter
      const matchesSearch =
        normalizedSearch.length === 0 ||
        discount.name.toLowerCase().includes(normalizedSearch) ||
        discount.discountId.toLowerCase().includes(normalizedSearch) ||
        String(discount.percentage).includes(normalizedSearch)

      return matchesStatus && matchesSearch
    })
  }, [discounts, searchTerm, statusFilter])

  const totalPages = Math.max(
    1,
    Math.ceil(filteredDiscounts.length / pageSize),
  )

  const paginatedDiscounts = useMemo(() => {
    const startIndex = (currentPage - 1) * pageSize
    return filteredDiscounts.slice(startIndex, startIndex + pageSize)
  }, [currentPage, filteredDiscounts])

  const firstVisibleItem =
    filteredDiscounts.length === 0
      ? 0
      : (currentPage - 1) * pageSize + 1

  const lastVisibleItem = Math.min(
    currentPage * pageSize,
    filteredDiscounts.length,
  )

  function validateCustomerId(): string | null {
    const value = customerIdInput.trim()

    if (!value) {
      setCustomerIdError('Customer ID is required.')
      return null
    }

    if (!uuidPattern.test(value)) {
      setCustomerIdError('Customer ID must be a valid UUID.')
      return null
    }

    setCustomerIdError(null)
    return value
  }

  async function loadCustomerDiscounts(customerId: string) {
    setIsLoading(true)
    setPageError(null)

    try {
      const data = await businessApi.listAllCustomerDiscounts(customerId)
      setDiscounts(data)
      setLoadedCustomerId(customerId)
      setSearchParams({ customerId })
      setCurrentPage(1)
      setSearchTerm('')
      setStatusFilter('ALL')
    } catch (error) {
      setDiscounts([])
      setLoadedCustomerId(customerId)

      if (error instanceof ApiRequestError) {
        if (error.status === 401) {
          setPageError(
            'Unauthorized. Please sign in as Business Operations Manager.',
          )
          return
        }

        if (error.status === 403) {
          setPageError(
            'You do not have permission to access Discount Management.',
          )
          return
        }

        if (error.status === 404) {
          setPageError('Customer or discount catalogue was not found.')
          return
        }

        setPageError(error.message)
        return
      }

      setPageError('Unable to load customer discounts.')
    } finally {
      setIsLoading(false)
    }
  }

  async function handleCustomerLookup(
    event: SyntheticEvent<HTMLFormElement>,
  ) {
    event.preventDefault()

    const customerId = validateCustomerId()
    if (!customerId) {
      return
    }

    await loadCustomerDiscounts(customerId)
  }

  function openCreateModal() {
    if (!loadedCustomerId || pageError) {
      return
    }

    setForm(emptyForm)
    setFormErrors({})
    setSubmitError(null)
    setEditingDiscount(null)
    setIsCreateOpen(true)
  }

  function openEditModal(discount: Discount) {
    setForm(formFromDiscount(discount))
    setFormErrors({})
    setSubmitError(null)
    setIsCreateOpen(false)
    setEditingDiscount(discount)
  }

  function closeFormModal() {
    if (isSaving) {
      return
    }

    setIsCreateOpen(false)
    setEditingDiscount(null)
    setForm(emptyForm)
    setFormErrors({})
    setSubmitError(null)
  }

  function updateField(
    field: keyof DiscountForm,
    value: string,
  ) {
    setForm((current) => ({ ...current, [field]: value }))
    setFormErrors((current) => ({ ...current, [field]: undefined }))
    setSubmitError(null)
  }

  function validateForm(): {
    name: string
    percentage: number
    effectiveFrom: string
    effectiveTo: string | null
  } | null {
    const nextErrors: DiscountFormErrors = {}
    const name = form.name.trim()
    const percentageText = form.percentage.trim()
    const percentage = Number(percentageText)
    const effectiveFrom = toUtcIso(form.effectiveFrom)
    const effectiveTo = form.effectiveTo
      ? toUtcIso(form.effectiveTo)
      : null

    if (!name) {
      nextErrors.name = 'Discount name is required.'
    }

    if (!percentageText) {
      nextErrors.percentage = 'Percentage is required.'
    } else if (!Number.isFinite(percentage)) {
      nextErrors.percentage = 'Percentage must be a valid number.'
    } else if (percentage < 0 || percentage > 100) {
      nextErrors.percentage = 'Percentage must be between 0 and 100.'
    }

    if (!form.effectiveFrom) {
      nextErrors.effectiveFrom = 'Effective from is required.'
    } else if (!effectiveFrom) {
      nextErrors.effectiveFrom = 'Effective from must be a valid date and time.'
    }

    if (form.effectiveTo && !effectiveTo) {
      nextErrors.effectiveTo = 'Effective to must be a valid date and time.'
    }

    if (Object.keys(nextErrors).length > 0 || !effectiveFrom) {
      setFormErrors(nextErrors)
      return null
    }

    return {
      name,
      percentage,
      effectiveFrom,
      effectiveTo,
    }
  }

  function applyServerValidation(error: ApiRequestError): boolean {
    if (error.code !== 'VALIDATION_ERROR' || !error.errors) {
      return false
    }

    const serverErrors: DiscountFormErrors = {}

    for (const [field, messages] of Object.entries(error.errors)) {
      if (messages.length === 0) {
        continue
      }

      if (field in form) {
        serverErrors[field as keyof DiscountFormErrors] = messages[0]
      }
    }

    setFormErrors(serverErrors)
    setSubmitError(
      Object.keys(serverErrors).length === 0 ? error.message : null,
    )
    return true
  }

  async function handleCreateDiscount(
    event: SyntheticEvent<HTMLFormElement>,
  ) {
    event.preventDefault()

    if (!loadedCustomerId || isSaving) {
      return
    }

    const values = validateForm()
    if (!values) {
      return
    }

    const request: CreateCustomerDiscountRequest = values

    setIsSaving(true)
    setSubmitError(null)

    try {
      const response = await businessApi.createCustomerDiscount(
        loadedCustomerId,
        request,
      )

      setDiscounts((current) => [response.data, ...current])
      setIsCreateOpen(false)
      setForm(emptyForm)
      setCurrentPage(1)
      setStatusFilter('ALL')
      setSearchTerm('')
    } catch (error) {
      if (error instanceof ApiRequestError) {
        if (applyServerValidation(error)) {
          return
        }

        setSubmitError(error.message)
        return
      }

      setSubmitError('Unable to create the discount.')
    } finally {
      setIsSaving(false)
    }
  }

  async function handleUpdateDiscount(
    event: SyntheticEvent<HTMLFormElement>,
  ) {
    event.preventDefault()

    if (!editingDiscount || isSaving) {
      return
    }

    const values = validateForm()
    if (!values) {
      return
    }

    const request: UpdateDiscountRequest = {
      ...values,
      status: form.status,
    }

    setIsSaving(true)
    setSubmitError(null)

    try {
      const response = await businessApi.updateDiscount(
        editingDiscount.discountId,
        request,
      )

      setDiscounts((current) =>
        current.map((discount) =>
          discount.discountId === editingDiscount.discountId
            ? response.data
            : discount,
        ),
      )
      closeFormModal()
    } catch (error) {
      if (error instanceof ApiRequestError) {
        if (applyServerValidation(error)) {
          return
        }

        if (error.status === 409) {
          setSubmitError(
            `${error.message} If this Discount is already referenced by a Contract, create a new Discount row for changed percentage/effective-period semantics.`,
          )
          return
        }

        setSubmitError(error.message)
        return
      }

      setSubmitError('Unable to update the discount.')
    } finally {
      setIsSaving(false)
    }
  }

  async function handleStatusChange(discount: Discount) {
    if (statusChangingId) {
      return
    }

    const nextStatus: DiscountStatus =
      discount.status === 'ACTIVE' ? 'INACTIVE' : 'ACTIVE'

    setStatusChangingId(discount.discountId)

    try {
      const response = await businessApi.updateDiscount(
        discount.discountId,
        {
          name: discount.name,
          percentage: discount.percentage,
          status: nextStatus,
          effectiveFrom: discount.effectiveFrom,
          effectiveTo: discount.effectiveTo,
        },
      )

      setDiscounts((current) =>
        current.map((item) =>
          item.discountId === discount.discountId ? response.data : item,
        ),
      )
    } catch (error) {
      if (error instanceof ApiRequestError) {
        window.alert(error.message)
      } else {
        window.alert('Unable to change Discount status.')
      }
    } finally {
      setStatusChangingId(null)
    }
  }

  const modalOpen = isCreateOpen || Boolean(editingDiscount)

  return (
    <section className="facility-page">
      <div className="facility-breadcrumb">
        Business Operations / Discounts
      </div>

      <div className="facility-page-heading">
        <div>
          <span className="facility-eyebrow">DISCOUNT MANAGEMENT</span>
          <h1>Customer Discounts</h1>
          <p>
            Manage percentage Discounts owned by a selected Customer while
            preserving Contract-captured financial semantics.
          </p>
        </div>

        <div className="facility-heading-actions">
          <span className="facility-protection-note">
            Customer-owned · Referenced Semantics Protected
          </span>
          <button
            type="button"
            className="facility-create-button"
            onClick={openCreateModal}
            disabled={
              !loadedCustomerId ||
              isLoading ||
              Boolean(pageError)
            }
          >
            + Create Discount
          </button>
        </div>
      </div>

      <form
        className="discount-customer-scope"
        onSubmit={handleCustomerLookup}
      >
        <div className="discount-customer-scope__copy">
          <span className="facility-summary-label">CUSTOMER SCOPE</span>
          <strong>Load discounts by Customer ID</strong>
          <p>
            BOM-010 is customer-scoped. Enter the target Customer UUID to load
            the authoritative Discount catalogue.
          </p>
        </div>

        <div className="discount-customer-scope__control">
          <label htmlFor="discount-customer-id">Customer ID</label>
          <div className="discount-customer-scope__row">
            <input
              id="discount-customer-id"
              type="text"
              value={customerIdInput}
              onChange={(event) => {
                setCustomerIdInput(event.target.value)
                setCustomerIdError(null)
              }}
              placeholder="00000000-0000-0000-0000-000000000000"
              disabled={isLoading}
              autoComplete="off"
            />
            <button
              type="submit"
              className="facility-primary-button"
              disabled={isLoading}
            >
              {isLoading ? 'Loading...' : 'Load Customer'}
            </button>
          </div>
          {customerIdError && (
            <span className="facility-form-error">{customerIdError}</span>
          )}
        </div>
      </form>

      <div className="facility-summary-grid">
        <article className="facility-summary-card">
          <div>
            <span className="facility-summary-label">TOTAL DISCOUNTS</span>
            <strong>{discounts.length}</strong>
            <p>Rows owned by the selected Customer</p>
          </div>
          <div className="facility-summary-icon">D</div>
        </article>

        <article className="facility-summary-card">
          <div>
            <span className="facility-summary-label">ACTIVE</span>
            <strong>{activeCount}</strong>
            <p className="facility-summary-success">
              Available for server-side eligibility checks
            </p>
          </div>
          <div className="facility-summary-icon facility-summary-icon--active">
            A
          </div>
        </article>

        <article className="facility-summary-card">
          <div>
            <span className="facility-summary-label">INACTIVE</span>
            <strong>{inactiveCount}</strong>
            <p>Retained for historical Contract references</p>
          </div>
          <div className="facility-summary-icon">I</div>
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
            placeholder="Search by name, percentage or Discount ID..."
            className="facility-search-input"
            disabled={!loadedCustomerId}
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
              disabled={!loadedCustomerId}
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
          disabled={!loadedCustomerId}
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
            <h2>Customer Discount Registry</h2>
            <p>
              {loadedCustomerId
                ? `Customer: ${loadedCustomerId}`
                : 'Select a Customer ID before loading Discount records.'}
            </p>
          </div>
          <span>Showing {filteredDiscounts.length} discounts</span>
        </div>

        <div className="facility-table-scroll">
          <table className="facility-table discount-table">
            <thead>
              <tr>
                <th>Discount</th>
                <th>Percentage</th>
                <th>Status</th>
                <th>Effective Period</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {!loadedCustomerId && !isLoading && (
                <tr>
                  <td colSpan={5} className="facility-empty-state">
                    <div className="facility-state-content">
                      <strong>No Customer selected</strong>
                      <span>
                        Enter a Customer UUID above to load BWP-04 Discount
                        Management data.
                      </span>
                    </div>
                  </td>
                </tr>
              )}

              {isLoading && (
                <tr>
                  <td colSpan={5} className="facility-loading-state">
                    <div className="facility-state-content">
                      <div className="facility-spinner" />
                      <strong>Loading customer discounts...</strong>
                      <span>Retrieving the authorized Discount catalogue.</span>
                    </div>
                  </td>
                </tr>
              )}

              {loadedCustomerId && pageError && !isLoading && (
                <tr>
                  <td colSpan={5} className="facility-error-state">
                    <div className="facility-state-content">
                      <div className="facility-error-icon">!</div>
                      <strong>Unable to load discounts</strong>
                      <span>{pageError}</span>
                    </div>
                  </td>
                </tr>
              )}

              {loadedCustomerId &&
                !isLoading &&
                !pageError &&
                paginatedDiscounts.map((discount) => (
                  <tr key={discount.discountId}>
                    <td>
                      <div className="facility-name-cell">
                        <strong>{discount.name}</strong>
                        <span>{discount.discountId}</span>
                      </div>
                    </td>
                    <td>
                      <strong className="discount-percentage">
                        {discount.percentage}%
                      </strong>
                    </td>
                    <td>
                      <span
                        className={
                          discount.status === 'ACTIVE'
                            ? 'facility-status facility-status--active'
                            : 'facility-status facility-status--inactive'
                        }
                      >
                        {discount.status}
                      </span>
                    </td>
                    <td>
                      <div className="facility-location-cell">
                        <strong>{formatEffectivePeriod(discount)}</strong>
                        <span>Displayed in GMT+7</span>
                      </div>
                    </td>
                    <td>
                      <div className="facility-actions discount-actions">
                        <button
                          type="button"
                          className="facility-action-button facility-action-button--activate"
                          onClick={() => openEditModal(discount)}
                        >
                          Edit
                        </button>
                        <button
                          type="button"
                          className={
                            discount.status === 'ACTIVE'
                              ? 'facility-action-button facility-action-button--deactivate'
                              : 'facility-action-button facility-action-button--activate'
                          }
                          disabled={statusChangingId === discount.discountId}
                          onClick={() => void handleStatusChange(discount)}
                        >
                          {statusChangingId === discount.discountId
                            ? 'Saving...'
                            : discount.status === 'ACTIVE'
                              ? 'Deactivate'
                              : 'Activate'}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}

              {loadedCustomerId &&
                !isLoading &&
                !pageError &&
                discounts.length === 0 && (
                  <tr>
                    <td colSpan={5} className="facility-empty-state">
                      <div className="facility-state-content">
                        <strong>No discounts for this Customer</strong>
                        <span>
                          Create the first Customer-owned percentage Discount.
                        </span>
                        <button
                          type="button"
                          className="facility-primary-button"
                          onClick={openCreateModal}
                        >
                          Create Discount
                        </button>
                      </div>
                    </td>
                  </tr>
                )}

              {loadedCustomerId &&
                !isLoading &&
                !pageError &&
                discounts.length > 0 &&
                filteredDiscounts.length === 0 && (
                  <tr>
                    <td colSpan={5} className="facility-empty-state">
                      <div className="facility-state-content">
                        <strong>No matching discounts</strong>
                        <span>
                          Try another search value or clear the current filters.
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
              {filteredDiscounts.length} discounts
            </span>
            <button
              type="button"
              className="facility-page-button"
              disabled={currentPage === 1 || filteredDiscounts.length === 0}
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
                currentPage === totalPages || filteredDiscounts.length === 0
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
        <div className="facility-governance-icon">%</div>
        <div>
          <h2>Discount Governance</h2>
          <p>
            A Customer may own multiple Discount rows, while each Contract may
            capture at most one. Once a Discount is referenced by any Contract,
            its Customer, percentage and effective-period semantics cannot be
            changed in place. Inactivation blocks new Contract selection without
            invalidating historical Contract usage or issued Invoice snapshots.
          </p>
        </div>
      </div>

      {modalOpen && (
        <div
          className="facility-modal-backdrop"
          onMouseDown={closeFormModal}
        >
          <div
            className="facility-modal discount-modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="discount-form-title"
            onMouseDown={(event) => event.stopPropagation()}
          >
            <div className="facility-modal-header">
              <div>
                <span className="facility-eyebrow">CUSTOMER DISCOUNT</span>
                <h2 id="discount-form-title">
                  {editingDiscount ? 'Edit Discount' : 'Create Discount'}
                </h2>
                <p>
                  {editingDiscount
                    ? `Update ${editingDiscount.name} for Customer ${editingDiscount.customerId}.`
                    : `Create a Discount for Customer ${loadedCustomerId}.`}
                </p>
              </div>
              <button
                type="button"
                className="facility-modal-close"
                onClick={closeFormModal}
                aria-label="Close discount dialog"
                disabled={isSaving}
              >
                ×
              </button>
            </div>

            {editingDiscount && (
              <div className="facility-modal-notice">
                If this Discount is already referenced by a Contract, the server
                will reject changes to percentage or effective dates. Change
                status to INACTIVE and create a new Discount row when financial
                semantics must change.
              </div>
            )}

            <form
              className="facility-form"
              onSubmit={
                editingDiscount ? handleUpdateDiscount : handleCreateDiscount
              }
            >
              <div className="discount-form-grid">
                <div className="facility-form-field discount-form-field--wide">
                  <label htmlFor="discount-name">
                    Name <span>*</span>
                  </label>
                  <input
                    id="discount-name"
                    type="text"
                    value={form.name}
                    onChange={(event) => updateField('name', event.target.value)}
                    placeholder="LOYALTY_10"
                    disabled={isSaving}
                  />
                  {formErrors.name && (
                    <span className="facility-form-error">
                      {formErrors.name}
                    </span>
                  )}
                </div>

                <div className="facility-form-field">
                  <label htmlFor="discount-percentage">
                    Percentage <span>*</span>
                  </label>
                  <input
                    id="discount-percentage"
                    type="number"
                    min="0"
                    max="100"
                    step="0.01"
                    value={form.percentage}
                    onChange={(event) =>
                      updateField('percentage', event.target.value)
                    }
                    placeholder="10"
                    disabled={isSaving}
                  />
                  {formErrors.percentage && (
                    <span className="facility-form-error">
                      {formErrors.percentage}
                    </span>
                  )}
                </div>

                {editingDiscount && (
                  <div className="facility-form-field">
                    <label htmlFor="discount-status">
                      Status <span>*</span>
                    </label>
                    <select
                      id="discount-status"
                      value={form.status}
                      onChange={(event) =>
                        updateField('status', event.target.value)
                      }
                      disabled={isSaving}
                    >
                      <option value="ACTIVE">ACTIVE</option>
                      <option value="INACTIVE">INACTIVE</option>
                    </select>
                  </div>
                )}

                <div className="facility-form-field">
                  <label htmlFor="discount-effective-from">
                    Effective From <span>*</span>
                  </label>
                  <input
                    id="discount-effective-from"
                    type="datetime-local"
                    value={form.effectiveFrom}
                    onChange={(event) =>
                      updateField('effectiveFrom', event.target.value)
                    }
                    disabled={isSaving}
                  />
                  {formErrors.effectiveFrom && (
                    <span className="facility-form-error">
                      {formErrors.effectiveFrom}
                    </span>
                  )}
                </div>

                <div className="facility-form-field">
                  <label htmlFor="discount-effective-to">
                    Effective To
                  </label>
                  <input
                    id="discount-effective-to"
                    type="datetime-local"
                    value={form.effectiveTo}
                    onChange={(event) =>
                      updateField('effectiveTo', event.target.value)
                    }
                    disabled={isSaving}
                  />
                  {formErrors.effectiveTo && (
                    <span className="facility-form-error">
                      {formErrors.effectiveTo}
                    </span>
                  )}
                  <span className="discount-field-hint">
                    Leave blank for no end date. Input is interpreted in GMT+7.
                  </span>
                </div>
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
                  onClick={closeFormModal}
                  disabled={isSaving}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="facility-primary-button"
                  disabled={isSaving}
                >
                  {isSaving
                    ? 'Saving...'
                    : editingDiscount
                      ? 'Save Changes'
                      : 'Create Discount'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </section>
  )
}
