import {
  type SyntheticEvent,
  useEffect,
  useMemo,
  useState,
} from 'react'
import { businessApi } from '../../../src/api/businessApi'
import { ApiRequestError } from '../../../src/api/httpClient'
import type {
  Facility,
  FacilityStatus,
} from '../../../src/models/facility'
import { mockFacilities } from '../facility-management/data/facilityMock'
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
    useState<string | null>(null)
  const [currentPage, setCurrentPage] =
    useState(1)

  const pageSize = 5
  useEffect(() => {
  let cancelled = false

  async function loadFacilities() {
    setIsLoading(true)
    setPageError(null)

    try {
      const response =
        await businessApi.listFacilities(
          1,
          100,
        )

      if (cancelled) {
        return
      }

      setFacilities(response.data)
    } catch (error) {
      if (cancelled) {
        return
      }

      if (error instanceof ApiRequestError) {
        if (error.status === 501) {
          setFacilities(mockFacilities)
          setPageError(null)
          return
        }

        if (error.status === 401) {
          setFacilities([])
          setPageError(
            'Unauthorized. Please sign in as Business Operations Manager.',
          )
          return
        }

        if (error.status === 403) {
          setFacilities([])
          setPageError(
            'You do not have permission to access Facility Management.',
          )
          return
        }

        setFacilities([])
        setPageError(error.message)
        return
      }

      setFacilities([])
      setPageError(
        'Unable to load facilities.',
      )
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
}, [])
  const [facilities, setFacilities] =
  useState<Facility[]>([])
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
  
  const totalFacilities = facilities.length

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
  const totalPages = Math.max(
    1,
    Math.ceil(
      filteredFacilities.length / pageSize,
    ),
  )
  const paginatedFacilities = useMemo(() => {
  const startIndex =
    (currentPage - 1) * pageSize

    const endIndex =
      startIndex + pageSize

    return filteredFacilities.slice(
      startIndex,
      endIndex,
    )
  }, [
    filteredFacilities,
    currentPage,
    pageSize,
  ])
  function openViewModal(facility: Facility) {
    setSelectedFacility(facility)
  }
  function openEditModal(facility: Facility) {
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
  function handleEditFacility(
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

    setFacilities((current) =>
      current.map((facility) =>
        facility.facilityId ===
        editingFacility.facilityId
          ? {
              ...facility,
              name,
              address,
              contactInfo:
                editForm.contactInfo.trim() || null,
              description:
                editForm.description.trim() || null,
            }
          : facility,
      ),
    )

    setEditingFacility(null)
    setEditErrors({})
  }
  function closeEditModal() {
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
    setStatusActionFacility(facility)
    setStatusAction(action)
  }
  function closeStatusConfirmation() {
    setStatusActionFacility(null)
    setStatusAction(null)
  }
  function confirmStatusChange() {
    if (!statusActionFacility || !statusAction) {
      return
    }

    const nextStatus: FacilityStatus =
      statusAction === 'ACTIVATE'
        ? 'ACTIVE'
        : 'INACTIVE'

    setFacilities((current) =>
      current.map((facility) =>
        facility.facilityId ===
        statusActionFacility.facilityId
          ? {
              ...facility,
              status: nextStatus,
            }
          : facility,
      ),
    )

    closeStatusConfirmation()
  }
  const firstVisibleItem =
    filteredFacilities.length === 0
      ? 0
      : (currentPage - 1) * pageSize + 1

  const lastVisibleItem = Math.min(
    currentPage * pageSize,
    filteredFacilities.length,
  )
  function openCreateModal() {
        setCreateForm(emptyCreateFacilityForm)
        setCreateErrors({})
        setIsCreateOpen(true)
        }
        function closeCreateModal() {
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
        function handleCreateFacility(
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

            const newFacility: Facility = {
                facilityId: `facility-${Date.now()}`,
                name,
                address,
                contactInfo:
                createForm.contactInfo.trim() || null,
                description:
                createForm.description.trim() || null,
                status: 'INACTIVE',
            }

            setFacilities((current) => [
                newFacility,
                ...current,
            ])

            setSearchTerm('')
            setStatusFilter('ALL')

            setCreateForm(emptyCreateFacilityForm)
            setCreateErrors({})
            setIsCreateOpen(false)
            }

  return (
    <section className="facility-page">
      <div className="facility-breadcrumb">
        Business Operations / Facilities
      </div>

      <div className="facility-page-heading">
        <div>
          <span className="facility-eyebrow">
            FACILITY MANAGEMENT
          </span>

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

            <strong>{totalFacilities}</strong>

            <p>Facilities across the system</p>
          </div>

          <div className="facility-summary-icon">
            F
          </div>
        </article>

        <article className="facility-summary-card">
          <div>
            <span className="facility-summary-label">
              ACTIVE FACILITIES
            </span>

            <strong>{activeFacilities}</strong>

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
              INACTIVE FACILITIES
            </span>

            <strong>{inactiveFacilities}</strong>

            <p>New reservations disabled</p>
          </div>

          <div className="facility-summary-icon">
            ○
          </div>
        </article>
      </div>

      <div className="facility-toolbar">
        <div className="facility-search-wrapper">
          <span className="facility-search-icon">
            ⌕
          </span>

          <input
            type="search"
            value={searchTerm}
            onChange={(event) => {
              setSearchTerm(event.target.value)
              setCurrentPage(1)
            }}
            placeholder="Search by facility name, address or contact..."
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
            onClick={() => {
              setStatusFilter('ALL')
              setCurrentPage(1)
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
            onClick={() => {
            setStatusFilter('ACTIVE')
            setCurrentPage(1)
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
            onClick={() => {
              setStatusFilter('INACTIVE')
              setCurrentPage(1)
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
            setCurrentPage(1)
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
            Showing {filteredFacilities.length} facilities
          </span>
        </div>

        <div className="facility-table-scroll">
          <table className="facility-table">
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

                      <strong>Loading facilities...</strong>

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

                      <span>{pageError}</span>
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
                        title="Edit facility"
                        onClick={() => openEditModal(facility)}
                      >
                        ✎
                      </button>

                      <button
                        type="button"
                        className="facility-icon-button"
                        title="View facility"
                        onClick={() => openViewModal(facility)}
                      >
                        ◉
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
          <span>
            Rows per page: {pageSize}
          </span>

          <div className="facility-pagination">
            <span>
              Showing {firstVisibleItem}–
              {lastVisibleItem} of{' '}
              {filteredFacilities.length} facilities
            </span>

            <button
              type="button"
              className="facility-page-button"
              disabled={currentPage === 1}
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
                currentPage === totalPages ||
                filteredFacilities.length === 0
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
        <div className="facility-governance-icon">
          §
        </div>

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
      {isCreateOpen && (
  <div
    className="facility-modal-backdrop"
    onMouseDown={closeCreateModal}
  >
    <div
      className="facility-modal"
      role="dialog"
      aria-modal="true"
      aria-labelledby="create-facility-title"
      onMouseDown={(event) =>
        event.stopPropagation()
      }
    >
      <div className="facility-modal-header">
        <div>
          <span className="facility-eyebrow">
            FACILITY MANAGEMENT
          </span>

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
          onClick={closeCreateModal}
          aria-label="Close create facility dialog"
        >
          ×
        </button>
      </div>

      <div className="facility-modal-notice">
        New facilities start as{' '}
        <strong>INACTIVE</strong>. Activate the
        facility separately when it is ready to
        accept new reservations.
      </div>

      <form
        className="facility-form"
        onSubmit={handleCreateFacility}
      >
        <div className="facility-form-field">
          <label htmlFor="facility-name">
            Facility Name
            <span>*</span>
          </label>

          <input
            id="facility-name"
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
            <small className="facility-form-error">
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
            <small className="facility-form-error">
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
            onClick={closeCreateModal}
          >
            Cancel
          </button>

          <button
            type="submit"
            className="facility-primary-button"
          >
            Create Facility
          </button>
        </div>
      </form>
    </div>
  </div>
)}
     {selectedFacility && (
  <div
    className="facility-modal-backdrop"
    onMouseDown={closeViewModal}
  >
    <div
      className="facility-modal facility-detail-modal"
      role="dialog"
      aria-modal="true"
      aria-labelledby="facility-detail-title"
      onMouseDown={(event) =>
        event.stopPropagation()
      }
    >
      <div className="facility-modal-header">
        <div>
          <span className="facility-eyebrow">
            FACILITY DETAILS
          </span>

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
          ×
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
  </div>
)}
     {editingFacility && (
  <div
    className="facility-modal-backdrop"
    onMouseDown={closeEditModal}
  >
    <div
      className="facility-modal"
      role="dialog"
      aria-modal="true"
      aria-labelledby="edit-facility-title"
      onMouseDown={(event) =>
        event.stopPropagation()
      }
    >
      <div className="facility-modal-header">
        <div>
          <span className="facility-eyebrow">
            FACILITY MANAGEMENT
          </span>

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
          onClick={closeEditModal}
          aria-label="Close edit facility dialog"
        >
          ×
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
        className="facility-form"
        onSubmit={handleEditFacility}
      >
        <div className="facility-form-field">
          <label htmlFor="edit-facility-name">
            Facility Name
            <span>*</span>
          </label>

          <input
            id="edit-facility-name"
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
            <small className="facility-form-error">
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
            <small className="facility-form-error">
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
            onClick={closeEditModal}
          >
            Cancel
          </button>

          <button
            type="submit"
            className="facility-primary-button"
          >
            Save Changes
          </button>
        </div>
      </form>
    </div>
  </div>
)}   
     {statusActionFacility && statusAction && (
  <div
    className="facility-modal-backdrop"
    onMouseDown={closeStatusConfirmation}
  >
    <div
      className="facility-confirm-dialog"
      role="alertdialog"
      aria-modal="true"
      aria-labelledby="facility-status-dialog-title"
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
        <span className="facility-eyebrow">
          FACILITY LIFECYCLE
        </span>

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

      <div className="facility-confirm-actions">
        <button
          type="button"
          className="facility-secondary-button"
          onClick={closeStatusConfirmation}
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
          onClick={confirmStatusChange}
        >
          {statusAction === 'ACTIVATE'
            ? 'Activate Facility'
            : 'Deactivate Facility'}
        </button>
      </div>
    </div>
  </div>
)}
    </section>
  )
}