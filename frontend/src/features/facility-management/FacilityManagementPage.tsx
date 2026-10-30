import { Eye, Pencil, Search, ShieldCheck, X } from 'lucide-react'
import { ApiErrorAlert } from '../../components/ApiErrorAlert'
import { presentApiError, type ApiErrorPresentation } from '../../api/apiErrorPresentation'
import type { Pagination } from '../../api/api.types'
import { EmployeeModal } from '../../components/EmployeeModal'
import {
  type SyntheticEvent,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react'
import { businessApi } from '../../../src/api/businessApi'

import type {
  Facility,
  FacilityStatus,
} from '../../../src/models/facility'


type FacilityStatusAction =
  | 'ACTIVATE'
  | 'DEACTIVATE'

interface CreateFacilityForm {
  name: string
  address: string
  contactInfo: string
  description: string
}
interface CreateFacilityErrors {
  name?: string
  address?: string
}
const emptyCreateFacilityForm: CreateFacilityForm = {
  name: '',
  address: '',
  contactInfo: '',
  description: '',
}



export function FacilityManagementPage() {
 const [isLoading, setIsLoading] =
    useState(true)

  const [pageError, setPageError] =
    useState<ApiErrorPresentation | null>(null)
  const [currentPage, setCurrentPage] =
    useState(1)

  const [pagination, setPagination] = useState<Pagination | null>(null)
  const [revision, setRevision] = useState(0)
  const [isSaving, setIsSaving] = useState(false)
  const saving = useRef(false)
  const [operationError, setOperationError] = useState<ApiErrorPresentation | null>(null)
  const [facilities, setFacilities] =
  useState<Facility[]>([])
  const pageSize = 5
  useEffect(() => {
  let cancelled = false

  async function loadFacilities() {
    setIsLoading(true)
    setPageError(null)

    try {
      const response =
        await businessApi.listFacilities(
          currentPage,
          pageSize,
        )

      if (cancelled) {
        return
      }

      if (currentPage > Math.max(1, response.pagination.totalPages)) {
        setCurrentPage(Math.max(1, response.pagination.totalPages))
        return
      }
      setPagination(response.pagination)
      setFacilities(response.data)
    } catch (error) {
      if (cancelled) {
        return
      }

      setFacilities([])
      setPagination(null)
      setPageError(presentApiError(error))
    } finally {
      if (!cancelled) {
        setIsLoading(false)
      }
    }
  }

  void loadFacilities()

  return () => {
    cancelled = true
  }
}, [currentPage, revision])
  const [isCreateOpen, setIsCreateOpen] =
    useState(false)
  const [selectedFacility, setSelectedFacility] =
    useState<Facility | null>(null)
  const [editingFacility, setEditingFacility] =
  useState<Facility | null>(null)
  const [statusActionFacility, setStatusActionFacility] =
    useState<Facility | null>(null)

  const [statusAction, setStatusAction] =
    useState<FacilityStatusAction | null>(null)
  const [editForm, setEditForm] =
    useState<CreateFacilityForm>(
      emptyCreateFacilityForm,
    )

const [editErrors, setEditErrors] =
  useState<CreateFacilityErrors>({})
  const [createForm, setCreateForm] =
    useState<CreateFacilityForm>(
    emptyCreateFacilityForm,
  )
  const [createErrors, setCreateErrors] =
  useState<CreateFacilityErrors>({})
  const [searchTerm, setSearchTerm] = useState('')
  const [statusFilter, setStatusFilter] = useState<
    'ALL' | FacilityStatus
  >('ALL')

  const totalFacilities = pagination?.totalItems ?? 0

  const activeFacilities = facilities.filter(
    (facility) => facility.status === 'ACTIVE',
  ).length

  const inactiveFacilities = facilities.filter(
    (facility) => facility.status === 'INACTIVE',
  ).length

  const filteredFacilities = useMemo(() => {
    const normalizedSearch = searchTerm
      .trim()
      .toLowerCase()

    return facilities.filter((facility) => {
      const matchesStatus =
        statusFilter === 'ALL' ||
        facility.status === statusFilter

      const matchesSearch =
        normalizedSearch.length === 0 ||
        facility.name
          .toLowerCase()
          .includes(normalizedSearch) ||
        facility.address
          .toLowerCase()
          .includes(normalizedSearch) ||
        facility.contactInfo
          ?.toLowerCase()
          .includes(normalizedSearch)

      return matchesStatus && matchesSearch
    })

  }, [facilities, searchTerm, statusFilter])
  const totalPages = Math.max(1, pagination?.totalPages ?? 1)
  const paginatedFacilities = filteredFacilities
  function openViewModal(facility: Facility) {
    setSelectedFacility(facility)
  }
  function openEditModal(facility: Facility) {
    setOperationError(null)
    setEditingFacility(facility)

    setEditForm({
      name: facility.name,
      address: facility.address,
      contactInfo: facility.contactInfo ?? '',
      description: facility.description ?? '',
    })

    setEditErrors({})
  }
  function updateEditField(
    field: keyof CreateFacilityForm,
    value: string,
  ) {
    setEditForm((current) => ({
      ...current,
      [field]: value,
    }))

    if (
      field === 'name' ||
      field === 'address'
    ) {
      setEditErrors((current) => ({
        ...current,
        [field]: undefined,
      }))
    }
  }
async function handleEditFacility(
    event: SyntheticEvent<HTMLFormElement>,
  ) {
    event.preventDefault()

    if (!editingFacility) {
      return
    }

    const name = editForm.name.trim()
    const address = editForm.address.trim()

    const nextErrors: CreateFacilityErrors = {}

    if (!name) {
      nextErrors.name =
        'Facility name is required.'
    }

    if (!address) {
      nextErrors.address =
        'Address is required.'
    }

    if (Object.keys(nextErrors).length > 0) {
      setEditErrors(nextErrors)
      return
    }

    if (saving.current) return
    saving.current = true
    setIsSaving(true)
    setOperationError(null)
    try {
      const response =
        await businessApi.updateFacility(
          editingFacility.facilityId,
          {
            name,
            address,
            contactInfo:
              editForm.contactInfo.trim() || null,
            description:
              editForm.description.trim() || null,
          },
        )

      setFacilities((current) =>
        current.map((facility) =>
          facility.facilityId ===
          editingFacility.facilityId
            ? response.data
            : facility,
        ),
      )

      setEditingFacility(null)
      setEditErrors({})
      setRevision(value => value + 1)
    } catch (error) {
      setOperationError(presentApiError(error))
    } finally {
      saving.current = false
      setIsSaving(false)
    }
  }
  function closeEditModal() {
    if (saving.current) return
    setOperationError(null)
    setEditingFacility(null)
    setEditErrors({})
  }

  function closeViewModal() {
    setSelectedFacility(null)
  }
  function openStatusConfirmation(
    facility: Facility,
    action: FacilityStatusAction,
  ) {
    setOperationError(null)
    setStatusActionFacility(facility)
    setStatusAction(action)
  }
  function closeStatusConfirmation() {
    if (saving.current) return
    setOperationError(null)
    setStatusActionFacility(null)
    setStatusAction(null)
  }
  async function confirmStatusChange() {
    if (!statusActionFacility || !statusAction) {
      return
    }

    if (saving.current) return
    saving.current = true
    setIsSaving(true)
    setOperationError(null)
    try {
      const response =
        statusAction === 'ACTIVATE'
          ? await businessApi.activateFacility(
              statusActionFacility.facilityId,
            )
          : await businessApi.deactivateFacility(
              statusActionFacility.facilityId,
            )

      setFacilities((current) =>
        current.map((facility) =>
          facility.facilityId ===
          statusActionFacility.facilityId
            ? response.data
            : facility,
        ),
      )

      setStatusActionFacility(null)
      setStatusAction(null)
      setRevision(value => value + 1)
    } catch (error) {
      setOperationError(presentApiError(error))
    } finally {
      saving.current = false
      setIsSaving(false)
    }
  }
  function openCreateModal() {
    setOperationError(null)
        setCreateForm(emptyCreateFacilityForm)
        setCreateErrors({})
        setIsCreateOpen(true)
        }
        function closeCreateModal() {
    if (saving.current) return
    setOperationError(null)
        setIsCreateOpen(false)
        setCreateErrors({})
        }
        function updateCreateField(
            field: keyof CreateFacilityForm,
            value: string,
            ) {
            setCreateForm((current) => ({
                ...current,
                [field]: value,
            }))

            if (
                field === 'name' ||
                field === 'address'
            ) {
                setCreateErrors((current) => ({
                ...current,
                [field]: undefined,
                }))
            }
            }
        async function handleCreateFacility(
            event: SyntheticEvent<HTMLFormElement>,
            ) {
            event.preventDefault()

            const name = createForm.name.trim()
            const address = createForm.address.trim()

            const nextErrors: CreateFacilityErrors = {}

            if (!name) {
                nextErrors.name =
                'Facility name is required.'
            }

            if (!address) {
                nextErrors.address =
                'Address is required.'
            }

            if (Object.keys(nextErrors).length > 0) {
                setCreateErrors(nextErrors)
                return
            }

    if (saving.current) return
    saving.current = true
    setIsSaving(true)
    setOperationError(null)
    try {
              const response =
                await businessApi.createFacility({
                  name,
                  address,
                  contactInfo:
                    createForm.contactInfo.trim() || null,
                  description:
                    createForm.description.trim() || null,
                })


              setFacilities((current) => [
                response.data,
                ...current,
              ])

              setSearchTerm('')
              setStatusFilter('ALL')

              setCreateForm(emptyCreateFacilityForm)
              setCreateErrors({})
              setIsCreateOpen(false)
      setCurrentPage(1)
        setRevision(value => value + 1)
    } catch (error) {
      setOperationError(presentApiError(error))
    } finally {
      saving.current = false
      setIsSaving(false)
    }
  }
  return (
    <section className="facility-page facility-registry">
      <div className="facility-breadcrumb">
        Business Operations / Facilities
      </div>

      <div className="facility-page-heading">
        <div>


          <h1>Facility Management</h1>

          <p>
            Manage facility information and operational status.
          </p>
        </div>

        <div className="facility-heading-actions">
          <span className="facility-protection-note">
            Facility History Protected · No Hard-Delete
          </span>

          <button
            type="button"
            className="facility-create-button"
            onClick={openCreateModal}
            >
            + Create Facility
            </button>
        </div>
      </div>

      <div className="facility-summary-grid">
        <article className="facility-summary-card">
          <div>
            <span className="facility-summary-label">
              TOTAL FACILITIES
            </span>

            <strong>{isLoading || pageError ? '—' : totalFacilities}</strong>

            <p>Facilities across the system</p>
          </div>

          <div className="facility-summary-icon">
            F
          </div>
        </article>

        <article className="facility-summary-card">
          <div>
            <span className="facility-summary-label">
              ACTIVE ON THIS PAGE
            </span>

            <strong>{isLoading || pageError ? '—' : activeFacilities}</strong>

            <p className="facility-summary-success">
              Accepting new reservations
            </p>
          </div>

          <div className="facility-summary-icon facility-summary-icon--active">
            ✓
          </div>
        </article>

        <article className="facility-summary-card">
          <div>
            <span className="facility-summary-label">
              INACTIVE ON THIS PAGE
            </span>

            <strong>{isLoading || pageError ? '—' : inactiveFacilities}</strong>

            <p>New reservations disabled</p>
          </div>

          <div className="facility-summary-icon">
            ○
          </div>
        </article>
      </div>

      <div className="facility-toolbar">
        <div className="facility-search-wrapper">
          <Search className="facility-search-icon" size={18} aria-hidden="true" />

          <input
            type="search"
            aria-label="Search this page by name, address or contact"
            value={searchTerm}
            onChange={(event) => {
              setSearchTerm(event.target.value)
            }}
            placeholder="Search this page by name, address or contact..."
            className="facility-search-input"
          />
        </div>

        <div className="facility-status-filters">
          <button
            type="button"
            className={
              statusFilter === 'ALL'
                ? 'facility-filter-button facility-filter-button--active'
                : 'facility-filter-button'
            }
            aria-pressed={statusFilter === 'ALL'}
            onClick={() => {
              setStatusFilter('ALL')
            }}
          >
            All
          </button>

          <button
            type="button"
            className={
              statusFilter === 'ACTIVE'
                ? 'facility-filter-button facility-filter-button--active'
                : 'facility-filter-button'
            }
            aria-pressed={statusFilter === 'ACTIVE'}
            onClick={() => {
            setStatusFilter('ACTIVE')
          }}
          >
            Active
          </button>

          <button
            type="button"
            className={
              statusFilter === 'INACTIVE'
                ? 'facility-filter-button facility-filter-button--active'
                : 'facility-filter-button'
            }
            aria-pressed={statusFilter === 'INACTIVE'}
            onClick={() => {
              setStatusFilter('INACTIVE')
            }}
          >
            Inactive
          </button>
        </div>

        <button
          type="button"
          className="facility-clear-button"
          onClick={() => {
            setSearchTerm('')
            setStatusFilter('ALL')
          }}
        >
          Clear filters
        </button>
      </div>

      <div className="facility-table-card">
        <div className="facility-table-header">
          <div>
            <h2>Facility Operational Registry</h2>

            <p>
              Live operational status and business
              governance across all portfolio branches.
            </p>
          </div>

          <span>
            {filteredFacilities.length} of {facilities.length} loaded facilities match. Search and filters cover this directory page only.
          </span>
        </div>

        <div className="facility-table-scroll" role="region" aria-label="Facility results, scroll horizontally for more columns" tabIndex={0}>
          <table className="facility-table" aria-label="Facility directory">
            <thead>
              <tr>
                <th>Facility</th>
                <th>Location & Contact</th>
                <th>Status</th>
                <th>Actions & Lifecycle</th>
              </tr>
            </thead>

            <tbody>
              {isLoading && (
                <tr>
                  <td
                    colSpan={4}
                    className="facility-loading-state"
                  >
                    <div className="facility-state-content">
                      <div className="facility-spinner" />

                      <strong role="status">Loading facilities...</strong>

                      <span>
                        Retrieving the latest facility data.
                      </span>
                    </div>
                  </td>
                </tr>
              )}
              {pageError && (
                <tr>
                  <td
                    colSpan={4}
                    className="facility-error-state"
                  >
                    <div className="facility-state-content">
                      <div className="facility-error-icon">
                        !
                      </div>

                      <strong>
                        Unable to load facilities
                      </strong>

                      <ApiErrorAlert error={pageError} />
                      <button type="button" className="facility-secondary-button" onClick={() => setRevision(value => value + 1)}>Retry</button>
                    </div>
                  </td>
                </tr>
              )}

              {!isLoading &&
                !pageError &&
                paginatedFacilities.map((facility) => (
                <tr key={facility.facilityId}>
                  <td>
                    <div className="facility-name-cell">
                      <strong>{facility.name}</strong>

                      <span>
                        {facility.description}
                      </span>
                    </div>
                  </td>

                  <td>
                    <div className="facility-location-cell">
                      <strong>
                        {facility.address}
                      </strong>

                      <span>
                        {facility.contactInfo ?? '—'}
                      </span>
                    </div>
                  </td>

                  <td>
                    <span
                      className={
                        facility.status === 'ACTIVE'
                          ? 'facility-status facility-status--active'
                          : 'facility-status facility-status--inactive'
                      }
                    >
                      {facility.status === 'ACTIVE'
                        ? 'Active'
                        : 'Inactive'}
                    </span>
                  </td>

                  <td>
                    <div className="facility-actions">
                      {facility.status === 'ACTIVE' ? (
                        <button
                          type="button"
                          className="facility-action-button facility-action-button--deactivate"
                          onClick={() =>
                            openStatusConfirmation(
                              facility,
                              'DEACTIVATE',
                            )
                          }
                        >
                          Deactivate
                        </button>
                      ) : (
                        <button
                          type="button"
                          className="facility-action-button facility-action-button--activate"
                          onClick={() =>
                            openStatusConfirmation(
                              facility,
                              'ACTIVATE',
                            )
                          }
                        >
                          Activate
                        </button>
                      )}

                     <button
                        type="button"
                        className="facility-icon-button"
                        title="Edit facility" aria-label={`Edit ${facility.name}`}
                        onClick={() => openEditModal(facility)}
                      >
                        <Pencil size={18} aria-hidden="true" />
                      </button>

                      <button
                        type="button"
                        className="facility-icon-button"
                        title="View facility" aria-label={`View ${facility.name}`}
                        onClick={() => openViewModal(facility)}
                      >
                        <Eye size={18} aria-hidden="true" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}

              {!isLoading &&
                !pageError &&
                facilities.length === 0 && (
                  <tr>
                    <td
                      colSpan={4}
                      className="facility-empty-state"
                    >
                      <div className="facility-state-content">
                        <strong>No facilities yet</strong>

                        <span>
                          Create the first facility to start
                          managing the storage portfolio.
                        </span>

                        <button
                          type="button"
                          className="facility-primary-button"
                          onClick={openCreateModal}
                        >
                          + Create Facility
                        </button>
                      </div>
                    </td>
                  </tr>
                )}
                {!isLoading &&
                  !pageError &&
                  facilities.length > 0 &&
                  filteredFacilities.length === 0 && (
                    <tr>
                      <td
                        colSpan={4}
                        className="facility-empty-state"
                      >
                        <div className="facility-state-content">
                          <strong>
                            No matching facilities
                          </strong>

                          <span>
                            Try another search term or clear
                            the current filters.
                          </span>

                          <button
                            type="button"
                            className="facility-secondary-button"
                            onClick={() => {
                              setSearchTerm('')
                              setStatusFilter('ALL')
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
          <span>
            Rows per page: {pageSize}
          </span>

          <div className="facility-pagination">
            <span>
              Directory page {currentPage} of {totalPages} / {totalFacilities} facilities
            </span>

            <button
              type="button"
              className="facility-page-button"
              disabled={isLoading || currentPage === 1}
              onClick={() =>
                setCurrentPage((current) =>
                  Math.max(1, current - 1),
                )
              }
            >
              Previous
            </button>

            <span className="facility-page-number">
              {currentPage}
            </span>

            <button
              type="button"
              className="facility-page-button"
              disabled={
                isLoading || currentPage >= totalPages
              }
              onClick={() =>
                setCurrentPage((current) =>
                  Math.min(
                    totalPages,
                    current + 1,
                  ),
                )
              }
            >
              Next
            </button>
          </div>
        </div>
      </div>

      <div className="facility-governance-card">
        <ShieldCheck className="facility-governance-icon" size={20} aria-hidden="true" />

        <div>
          <h2>
            BOM Governance &amp; Lifecycle Rules
          </h2>

          <p>
            Deactivation blocks new reservations while
            existing confirmed reservations and active
            contracts continue their lifecycle.
          </p>
        </div>
      </div>
      {isCreateOpen && (<EmployeeModal labelledBy="create-facility-title" onClose={closeCreateModal} busy={isSaving}>
  <div
    className="facility-modal-backdrop"
    onMouseDown={closeCreateModal}
  >
    <div
      className="facility-modal"
      onMouseDown={(event) =>
        event.stopPropagation()
      }
    >
      <div className="facility-modal-header">
        <div>


          <h2 id="create-facility-title">
            Create Facility
          </h2>

          <p>
            Add a new storage facility to the
            portfolio.
          </p>
        </div>

        <button
          type="button"
          className="facility-modal-close"
          disabled={isSaving} onClick={closeCreateModal}
          aria-label="Close create facility dialog"
        >
          <X size={20} aria-hidden="true" />
        </button>
      </div>

      <div className="facility-modal-notice">
        New facilities start as{' '}
        <strong>INACTIVE</strong>. Activate the
        facility separately when it is ready to
        accept new reservations.
      </div>

      <form
        className="facility-form" aria-busy={isSaving}
        onSubmit={handleCreateFacility}
      >
        <ApiErrorAlert error={operationError} />
        <fieldset className="employee-form-fields" disabled={isSaving}>
        <div className="facility-form-field">
          <label htmlFor="facility-name">
            Facility Name
            <span>*</span>
          </label>

          <input
            id="facility-name"
            required
            aria-invalid={Boolean(createErrors.name)}
            aria-describedby={createErrors.name ? "facility-name-error" : undefined}
            type="text"
            value={createForm.name}
            onChange={(event) =>
              updateCreateField(
                'name',
                event.target.value,
              )
            }
            placeholder="e.g. Go Vap Storage"
          />

          {createErrors.name && (
            <small id="facility-name-error" className="facility-form-error" role="alert">
              {createErrors.name}
            </small>
          )}
        </div>

        <div className="facility-form-field">
          <label htmlFor="facility-address">
            Address
            <span>*</span>
          </label>

          <input
            id="facility-address"
            required
            aria-invalid={Boolean(createErrors.address)}
            aria-describedby={createErrors.address ? "facility-address-error" : undefined}
            type="text"
            value={createForm.address}
            onChange={(event) =>
              updateCreateField(
                'address',
                event.target.value,
              )
            }
            placeholder="Facility address"
          />

          {createErrors.address && (
            <small id="facility-address-error" className="facility-form-error" role="alert">
              {createErrors.address}
            </small>
          )}
        </div>

        <div className="facility-form-field">
          <label htmlFor="facility-contact">
            Contact Information
          </label>

          <input
            id="facility-contact"
            type="text"
            value={createForm.contactInfo}
            onChange={(event) =>
              updateCreateField(
                'contactInfo',
                event.target.value,
              )
            }
            placeholder="Phone or contact details"
          />
        </div>

        <div className="facility-form-field">
          <label htmlFor="facility-description">
            Description
          </label>

          <textarea
            id="facility-description"
            rows={4}
            value={createForm.description}
            onChange={(event) =>
              updateCreateField(
                'description',
                event.target.value,
              )
            }
            placeholder="Short facility description"
          />
        </div>

        <div className="facility-modal-actions">
          <button
            type="button"
            className="facility-secondary-button"
            disabled={isSaving} onClick={closeCreateModal}
          >
            Cancel
          </button>

          <button
            type="submit"
            className="facility-primary-button"
          >
            {isSaving ? 'Creating facility...' : 'Create Facility'}
          </button>
        </div>
        </fieldset>
      </form>
    </div>
  </div></EmployeeModal>
)}
     {selectedFacility && (<EmployeeModal labelledBy="facility-detail-title" onClose={closeViewModal} busy={isSaving}>
  <div
    className="facility-modal-backdrop"
    onMouseDown={closeViewModal}
  >
    <div
      className="facility-modal facility-detail-modal"
      onMouseDown={(event) =>
        event.stopPropagation()
      }
    >
      <div className="facility-modal-header">
        <div>


          <h2 id="facility-detail-title">
            {selectedFacility.name}
          </h2>

          <p>
            View facility information and current
            operational status.
          </p>
        </div>

        <button
          type="button"
          className="facility-modal-close"
          onClick={closeViewModal}
          aria-label="Close facility detail"
        >
          <X size={20} aria-hidden="true" />
        </button>
      </div>

      <div className="facility-detail-body">
        <div className="facility-detail-row">
          <span>Facility Name</span>
          <strong>
            {selectedFacility.name}
          </strong>
        </div>

        <div className="facility-detail-row">
          <span>Status</span>

          <div>
            <span
              className={
                selectedFacility.status === 'ACTIVE'
                  ? 'facility-status facility-status--active'
                  : 'facility-status facility-status--inactive'
              }
            >
              {selectedFacility.status === 'ACTIVE'
                ? 'Active'
                : 'Inactive'}
            </span>
          </div>
        </div>

        <div className="facility-detail-row">
          <span>Address</span>
          <strong>
            {selectedFacility.address}
          </strong>
        </div>

        <div className="facility-detail-row">
          <span>Contact Information</span>
          <strong>
            {selectedFacility.contactInfo ?? '—'}
          </strong>
        </div>

        <div className="facility-detail-row facility-detail-row--description">
          <span>Description</span>

          <p>
            {selectedFacility.description ??
              'No description provided.'}
          </p>
        </div>
      </div>

      <div className="facility-detail-footer">
        <button
          type="button"
          className="facility-secondary-button"
          onClick={closeViewModal}
        >
          Close
        </button>
      </div>
    </div>
  </div></EmployeeModal>
)}
     {editingFacility && (<EmployeeModal labelledBy="edit-facility-title" onClose={closeEditModal} busy={isSaving}>
  <div
    className="facility-modal-backdrop"
    onMouseDown={closeEditModal}
  >
    <div
      className="facility-modal"
      onMouseDown={(event) =>
        event.stopPropagation()
      }
    >
      <div className="facility-modal-header">
        <div>


          <h2 id="edit-facility-title">
            Edit Facility
          </h2>

          <p>
            Update facility information. Operational
            status is managed separately.
          </p>
        </div>

        <button
          type="button"
          className="facility-modal-close"
          disabled={isSaving} onClick={closeEditModal}
          aria-label="Close edit facility dialog"
        >
          <X size={20} aria-hidden="true" />
        </button>
      </div>

      <div className="facility-modal-notice">
        Current status:{' '}
        <strong>
          {editingFacility.status}
        </strong>
        . Editing facility information does not change
        its operational status.
      </div>

      <form
        className="facility-form" aria-busy={isSaving}
        onSubmit={handleEditFacility}
      >
        <ApiErrorAlert error={operationError} />
        <fieldset className="employee-form-fields" disabled={isSaving}>
        <div className="facility-form-field">
          <label htmlFor="edit-facility-name">
            Facility Name
            <span>*</span>
          </label>

          <input
            id="edit-facility-name"
            required
            aria-invalid={Boolean(editErrors.name)}
            aria-describedby={editErrors.name ? "edit-facility-name-error" : undefined}
            type="text"
            value={editForm.name}
            onChange={(event) =>
              updateEditField(
                'name',
                event.target.value,
              )
            }
          />

          {editErrors.name && (
            <small id="edit-facility-name-error" className="facility-form-error" role="alert">
              {editErrors.name}
            </small>
          )}
        </div>

        <div className="facility-form-field">
          <label htmlFor="edit-facility-address">
            Address
            <span>*</span>
          </label>

          <input
            id="edit-facility-address"
            required
            aria-invalid={Boolean(editErrors.address)}
            aria-describedby={editErrors.address ? "edit-facility-address-error" : undefined}
            type="text"
            value={editForm.address}
            onChange={(event) =>
              updateEditField(
                'address',
                event.target.value,
              )
            }
          />

          {editErrors.address && (
            <small id="edit-facility-address-error" className="facility-form-error" role="alert">
              {editErrors.address}
            </small>
          )}
        </div>

        <div className="facility-form-field">
          <label htmlFor="edit-facility-contact">
            Contact Information
          </label>

          <input
            id="edit-facility-contact"
            type="text"
            value={editForm.contactInfo}
            onChange={(event) =>
              updateEditField(
                'contactInfo',
                event.target.value,
              )
            }
          />
        </div>

        <div className="facility-form-field">
          <label htmlFor="edit-facility-description">
            Description
          </label>

          <textarea
            id="edit-facility-description"
            rows={4}
            value={editForm.description}
            onChange={(event) =>
              updateEditField(
                'description',
                event.target.value,
              )
            }
          />
        </div>

        <div className="facility-modal-actions">
          <button
            type="button"
            className="facility-secondary-button"
            disabled={isSaving} onClick={closeEditModal}
          >
            Cancel
          </button>

          <button
            type="submit"
            className="facility-primary-button"
          >
            {isSaving ? 'Saving changes...' : 'Save Changes'}
          </button>
        </div>
        </fieldset>
      </form>
    </div>
  </div></EmployeeModal>
)}
     {statusActionFacility && statusAction && (<EmployeeModal labelledBy="facility-status-dialog-title" role="alertdialog" onClose={closeStatusConfirmation} busy={isSaving}>
  <div
    className="facility-modal-backdrop"
    onMouseDown={closeStatusConfirmation}
  >
    <div
      className="facility-confirm-dialog"
      onMouseDown={(event) =>
        event.stopPropagation()
      }
    >
      <div className="facility-confirm-icon">
        {statusAction === 'ACTIVATE'
          ? '✓'
          : '!'}
      </div>

      <div className="facility-confirm-content">


        <h2 id="facility-status-dialog-title">
          {statusAction === 'ACTIVATE'
            ? 'Activate Facility?'
            : 'Deactivate Facility?'}
        </h2>

        <p>
          You are about to{' '}
          <strong>
            {statusAction === 'ACTIVATE'
              ? 'activate'
              : 'deactivate'}
          </strong>{' '}
          <strong>
            {statusActionFacility.name}
          </strong>
          .
        </p>

        {statusAction === 'ACTIVATE' ? (
          <div className="facility-confirm-notice facility-confirm-notice--activate">
            Once activated, this facility can accept
            new reservations.
          </div>
        ) : (
          <div className="facility-confirm-notice facility-confirm-notice--deactivate">
            New reservations will be disabled.
            Existing confirmed reservations and active
            contracts must continue their current
            lifecycle.
          </div>
        )}
      </div>

      <ApiErrorAlert error={operationError} />
      <div className="facility-confirm-actions">
        <button
          type="button"
          className="facility-secondary-button"
          disabled={isSaving} onClick={closeStatusConfirmation}
        >
          Cancel
        </button>

        <button
          type="button"
          className={
            statusAction === 'ACTIVATE'
              ? 'facility-confirm-button facility-confirm-button--activate'
              : 'facility-confirm-button facility-confirm-button--deactivate'
          }
          disabled={isSaving} onClick={confirmStatusChange}
        >
          {isSaving ? 'Updating facility...' : statusAction === 'ACTIVATE'
            ? 'Activate Facility'
            : 'Deactivate Facility'}
        </button>
      </div>
    </div>
  </div></EmployeeModal>
)}
    </section>
  )
}
