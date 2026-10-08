import { CalendarDays, RefreshCw, Search } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import type { ApiCollectionResponse } from '../api/api.types'
import { presentApiError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { visitApi } from '../api/visitApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { StatusBadge } from '../components/StatusBadge'
import { isReservationWorkItem, type StaffWorkItem } from '../features/handover/handover.types'
import { isAccessWorkItem } from '../features/visits/accessValidation'
import { getCurrentBusinessDate } from '../utils/formatters'
import '../styles/staff.css'

export type StaffQueueMode = 'daily' | 'reservation-check-in' | 'handover' | 'access'

interface StaffWorkItemsPageProps {
  mode?: StaffQueueMode
}

const PAGE_COPY: Record<StaffQueueMode, { feature: string; title: string; description: string }> = {
  daily: {
    feature: 'FWP-01',
    title: 'Daily Work List',
    description: 'Facility-scoped work derived from Visits, Inspections and assigned Support Tickets. Open the authoritative resource before acting.',
  },
  'reservation-check-in': {
    feature: 'FWP-02',
    title: 'Reservation Check-in',
    description: 'Reservation visits awaiting Staff check-in. The server remains authoritative for visit date, Facility scope and lifecycle state.',
  },
  handover: {
    feature: 'FWP-03',
    title: 'Handover Processing',
    description: 'Checked-in reservation visits ready for the offline first-month receipt acknowledgement and atomic Complete Handover.',
  },
  access: {
    feature: 'FWP-04',
    title: 'Access Visit Processing',
    description: 'Process ACCESS visits without changing Contract ownership, rental period or StorageUnit assignment.',
  },
}

function matchesMode(item: StaffWorkItem, mode: StaffQueueMode) {
  if (mode === 'daily') return true
  if (mode === 'reservation-check-in') return item.workType === 'RESERVATION_VISIT' && item.status === 'SCHEDULED'
  if (mode === 'handover') return item.workType === 'RESERVATION_VISIT' && item.status === 'CHECKED_IN'
  return item.workType === 'ACCESS_VISIT'
}

function readableWorkType(workType: string) {
  return workType.replaceAll('_', ' ').toLowerCase().replace(/\b\w/g, (value) => value.toUpperCase())
}

function workItemLink(item: StaffWorkItem, page: number) {
  if (isReservationWorkItem(item)) {
    return `/staff/reservations/${encodeURIComponent(item.entityId)}/visits/${encodeURIComponent(item.referenceId)}?workPage=${page}`
  }
  if (isAccessWorkItem(item)) {
    return `/staff/contracts/${encodeURIComponent(item.entityId)}/access-visits/${encodeURIComponent(item.referenceId)}?workPage=${page}`
  }
  if (item.workType === 'RETURN_VISIT' && item.entityId) {
    return `/staff/returns?tab=confirmation&visitId=${encodeURIComponent(item.referenceId)}&contractId=${encodeURIComponent(item.entityId)}&date=${encodeURIComponent(item.scheduledDate)}`
  }
  if (item.workType.toUpperCase().includes('INSPECTION')) {
    return `/staff/returns?tab=inspection&inspectionId=${encodeURIComponent(item.referenceId)}`
  }
  if (item.workType.toUpperCase().includes('SUPPORT')) {
    return `/staff/support?ticketId=${encodeURIComponent(item.referenceId)}`
  }
  return null
}

function actionLabel(item: StaffWorkItem) {
  if (item.workType === 'RESERVATION_VISIT') {
    if (item.status === 'SCHEDULED') return 'Open Check-in'
    if (item.status === 'CHECKED_IN') return 'Open Handover'
    return 'View Visit'
  }
  if (item.workType === 'ACCESS_VISIT') {
    if (item.status === 'SCHEDULED') return 'Open Access Check-in'
    if (item.status === 'CHECKED_IN') return 'Open Access Check-out'
    return 'View Access Visit'
  }
  if (item.workType === 'RETURN_VISIT') return 'Open Handle Returns'
  if (item.workType.toUpperCase().includes('INSPECTION')) return 'Open Inspection'
  if (item.workType.toUpperCase().includes('SUPPORT')) return 'Open Support Ticket'
  return 'Open Work Item'
}

export function StaffWorkItemsPage({ mode = 'daily' }: StaffWorkItemsPageProps) {
  const copy = PAGE_COPY[mode]
  const [date, setDate] = useState(getCurrentBusinessDate)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [retry, setRetry] = useState(0)
  const [snapshot, setSnapshot] = useState<{
    key: string
    response: ApiCollectionResponse<StaffWorkItem> | null
    error: ApiErrorPresentation | null
  } | null>(null)
  const key = `${date}:${page}:${retry}`
  const loading = snapshot?.key !== key
  const response = loading ? null : snapshot.response
  const error = loading ? null : snapshot.error

  useEffect(() => {
    let active = true
    visitApi.listStaffWorkItems({ date, page })
      .then((nextResponse) => {
        if (active) setSnapshot({ key, response: nextResponse, error: null })
      })
      .catch((caughtError: unknown) => {
        if (active) setSnapshot({ key, response: null, error: presentApiError(caughtError) })
      })
    return () => { active = false }
  }, [date, page, key])

  const visibleItems = useMemo(() => {
    const needle = search.trim().toLowerCase()
    return (response?.data ?? []).filter((item) => {
      if (!matchesMode(item, mode)) return false
      if (!needle) return true
      return [
        item.workType,
        item.referenceId,
        item.entityId ?? '',
        item.status,
        item.customer?.fullName ?? '',
        item.customer?.phoneNumber ?? '',
      ].some((value) => value.toLowerCase().includes(needle))
    })
  }, [mode, response?.data, search])

  return (
    <div className="fwp-page" data-testid={`${copy.feature.toLowerCase()}-page`}>
      <header className="fwp-page-header">
        <div>
          <p className="fwp-eyebrow">Facility Staff · {copy.feature}</p>
          <h1>{copy.title}</h1>
          <p>{copy.description}</p>
        </div>
        <button className="fwp-button fwp-button-secondary" type="button" disabled={loading} onClick={() => setRetry((value) => value + 1)}>
          <RefreshCw size={16} />
          Refresh
        </button>
      </header>

      <section className="fwp-summary" aria-label="Work list summary">
        <div className="fwp-summary-card">
          <span>Server total</span>
          <strong>{response?.pagination.totalItems ?? '—'}</strong>
        </div>
        <div className="fwp-summary-card">
          <span>Current API page</span>
          <strong>{response?.pagination.page ?? page}</strong>
        </div>
        <div className="fwp-summary-card">
          <span>Matching rows</span>
          <strong>{loading ? '—' : visibleItems.length}</strong>
        </div>
      </section>

      <section className="fwp-toolbar" aria-label="Work-list filters">
        <label>
          <span>Business date (GMT+7)</span>
          <div className="fwp-input-with-icon">
            <CalendarDays size={16} aria-hidden="true" />
            <input
              type="date"
              value={date}
              onChange={(event) => {
                if (event.target.value) {
                  setDate(event.target.value)
                  setPage(1)
                }
              }}
            />
          </div>
        </label>
        <label className="fwp-toolbar-search">
          <span>Search current page</span>
          <div className="fwp-input-with-icon">
            <Search size={16} aria-hidden="true" />
            <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Customer, phone, Visit/Contract ID…" />
          </div>
        </label>
        <p className="fwp-toolbar-note">Date and pagination are server-authoritative. The queue/search view only narrows rows already returned on this API page.</p>
      </section>

      <ApiErrorAlert error={error} />

      {loading && (
        <div className="fwp-state-grid" aria-label="Loading work items">
          {Array.from({ length: 5 }).map((_, index) => <div className="fwp-skeleton" key={index} />)}
        </div>
      )}

      {!loading && !error && visibleItems.length === 0 && (
        <div className="fwp-state-card">
          <strong>No matching work items</strong>
          <p>No rows on this API page match the selected Facility Staff queue and current-page search.</p>
        </div>
      )}

      {!loading && !error && visibleItems.length > 0 && (
        <section className="fwp-table-card" aria-label={`${copy.title} table`}>
          <div className="fwp-table-wrap">
            <table className="fwp-table">
              <thead>
                <tr>
                  <th>Work item</th>
                  <th>Customer</th>
                  <th>Scheduled</th>
                  <th>Status</th>
                  <th>Reference</th>
                  <th aria-label="Actions" />
                </tr>
              </thead>
              <tbody>
                {visibleItems.map((item) => {
                  const link = workItemLink(item, response?.pagination.page ?? page)
                  return (
                    <tr key={`${item.workType}-${item.referenceId}`}>
                      <td><strong>{readableWorkType(item.workType)}</strong></td>
                      <td>
                        {item.customer ? (
                          <div className="fwp-customer-cell">
                            <strong>{item.customer.fullName}</strong>
                            <span>{item.customer.phoneNumber}</span>
                          </div>
                        ) : <span className="fwp-muted">Derived work item</span>}
                      </td>
                      <td>{item.scheduledDate}</td>
                      <td><StatusBadge status={item.status} /></td>
                      <td><code>{item.referenceId}</code></td>
                      <td>
                        {link ? <Link className="fwp-link-button" to={link}>{actionLabel(item)}</Link> : <span className="fwp-muted">Read only</span>}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          {response && response.pagination.totalPages > 1 && (
            <footer className="fwp-pagination">
              <span>Page {response.pagination.page} of {response.pagination.totalPages} · {response.pagination.totalItems} total work items</span>
              <div>
                <button className="fwp-button fwp-button-secondary" type="button" disabled={loading || response.pagination.page <= 1} onClick={() => setPage(response.pagination.page - 1)}>Previous</button>
                <button className="fwp-button fwp-button-secondary" type="button" disabled={loading || response.pagination.page >= response.pagination.totalPages} onClick={() => setPage(response.pagination.page + 1)}>Next</button>
              </div>
            </footer>
          )}
        </section>
      )}
    </div>
  )
}
