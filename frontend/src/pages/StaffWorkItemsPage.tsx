import { EmployeeEntityLabel, useEmployeeLabelLookup } from '../components/EmployeeEntityLabel'
import {
  ArrowRight,
  CalendarDays,
  Clock,
  Filter,
  KeyRound,
  ListChecks,
  RefreshCw,
  Search,
} from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { presentApiError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { visitApi } from '../api/visitApi'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { DecryptedText } from '../components/react-bits/DecryptedText'
import { SpotlightCard } from '../components/react-bits/SpotlightCard'
import { StatusBadge } from '../components/StatusBadge'
import { isReservationWorkItem, type StaffWorkItem } from '../features/handover/handover.types'
import { isAccessWorkItem } from '../features/visits/accessValidation'
import { fetchAllEmployeePages } from '../utils/employeePresentation'
import { getCurrentBusinessDate } from '../utils/formatters'
import '../styles/staff.css'

const PAGE_SIZE = 20

type HandoverStage = 'reservation-check-in' | 'handover'
export type StaffQueueMode = 'daily' | HandoverStage | 'handle-handover' | 'access'

interface StaffWorkItemsPageProps {
  mode?: StaffQueueMode
}

const PAGE_COPY: Record<StaffQueueMode, { feature: string; title: string; description: string }> = {
  daily: {
    feature: 'FWP-01',
    title: 'Daily Work List',
    description: 'Today’s visits, inspections and assigned support tickets. Open an item to review its details and next action.',
  },
  'reservation-check-in': {
    feature: 'FWP-02',
    title: 'Reservation Check-in',
    description: 'Review scheduled reservation visits and verify arriving customers before check-in.',
  },
  handover: {
    feature: 'FWP-03',
    title: 'Handover Processing',
    description: 'Complete physical handover and acknowledge first-month rent for checked-in customers.',
  },
  'handle-handover': {
    feature: 'FWP-02-03',
    title: 'Handle Handover',
    description: 'Check in arriving customers, then complete their first storage handover.',
  },
  access: {
    feature: 'FWP-04',
    title: 'Access Visit Processing',
    description: 'Check customers in and out for access to their rented units.',
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
  const lookupLabel = useEmployeeLabelLookup()
  const [params, setParams] = useSearchParams()
  const isHandover = mode === 'handle-handover'
  const stage: HandoverStage = params.get('tab') === 'handover' ? 'handover' : 'reservation-check-in'
  const queueMode = isHandover ? stage : mode
  const [localDate, setLocalDate] = useState(getCurrentBusinessDate)
  const [localPage, setLocalPage] = useState(1)
  const date = isHandover ? params.get('date') || localDate : localDate
  const requestedPage = Number(params.get('page') ?? 1)
  const urlPage = Number.isSafeInteger(requestedPage) && requestedPage > 0 ? requestedPage : 1
  const page = isHandover ? urlPage : localPage
  function setPage(next: number) {
    if (!isHandover) { setLocalPage(next); return }
    setParams(current => { const query = new URLSearchParams(current); query.set('page', String(next)); return query })
  }
  const [search, setSearch] = useState('')
  const [retry, setRetry] = useState(0)
  const [snapshot, setSnapshot] = useState<{
    key: string
    items: StaffWorkItem[] | null
    error: ApiErrorPresentation | null
  } | null>(null)
  const key = `${date}:${retry}`
  const loading = snapshot?.key !== key
  const allItems = loading ? null : snapshot.items
  const error = loading ? null : snapshot.error

  useEffect(() => {
    let active = true
    fetchAllEmployeePages((nextPage, pageSize) => visitApi.listStaffWorkItems({ date, page: nextPage, pageSize }))
      .then((nextResult) => {
        if (active) setSnapshot({ key, items: nextResult.items, error: null })
      })
      .catch((caughtError: unknown) => {
        if (active) setSnapshot({ key, items: null, error: presentApiError(caughtError) })
      })
    return () => { active = false }
  }, [date, key])

  const filteredItems = useMemo(() => {
    const needle = search.trim().toLowerCase()
    return (allItems ?? []).filter((item) => {
      if (!matchesMode(item, queueMode)) return false
      if (!needle) return true
      return [
        item.workType,
        readableWorkType(item.workType),
        item.referenceId,
        item.entityId ?? '',
        item.status,
        item.scheduledDate,
        item.customer?.fullName ?? '',
        item.customer?.phoneNumber ?? '',
        lookupLabel('contract', item.entityId),
        lookupLabel('contractUnit', item.entityId),
      ].some((value) => value.toLowerCase().includes(needle))
    })
  }, [allItems, lookupLabel, queueMode, search])

  const scheduledCount = useMemo(() => {
    return (allItems ?? []).filter((item) => item.status === 'SCHEDULED').length
  }, [allItems])

  const checkedInCount = useMemo(() => {
    return (allItems ?? []).filter((item) => item.status === 'CHECKED_IN').length
  }, [allItems])

  const totalPages = Math.max(1, Math.ceil(filteredItems.length / PAGE_SIZE))
  const safePage = Math.min(page, totalPages)
  const visibleItems = useMemo(
    () => filteredItems.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE),
    [filteredItems, safePage],
  )

  return (
    <div className="fwp-page" data-testid={`${copy.feature.toLowerCase()}-page`}>
      <header className="fwp-page-header">
        <div>
          <h1>{copy.title}</h1>
          <p>{copy.description}</p>
        </div>
        <button className="fwp-button fwp-button-secondary" type="button" disabled={loading} onClick={() => setRetry((value) => value + 1)}>
          <RefreshCw size={16} />
          Refresh
        </button>
      </header>

      {isHandover && <>
        <nav className="fwp-tabs fwp-handover-tabs" aria-label="Handle Handover stages">
          {(['reservation-check-in', 'handover'] as const).map((item, index) => {
            const query = new URLSearchParams(params)
            query.set('tab', item === 'handover' ? 'handover' : 'check-in')
            query.delete('page')
            return <Link key={item} className={`fwp-tab${stage === item ? ' active' : ''}`} aria-current={stage === item ? 'page' : undefined} to={`?${query}`}>
              <span>Stage {index + 1}</span><strong>{PAGE_COPY[item].title}</strong>
            </Link>
          })}
        </nav>
        <p className="fwp-toolbar-note">{PAGE_COPY[stage].description}</p>
      </>}

      {/* Cyber-Command Operational Telemetry / Summary Cluster with React Bits SpotlightCard */}
      <section className="fwp-summary fwp-summary-command" aria-label="Work list summary">
        <SpotlightCard
          className="fwp-summary-card fwp-metric-total"
          spotlightColor="rgba(6, 182, 212, 0.18)"
          spotlightRadius={280}
        >
          <div className="fwp-metric-header">
            <span className="fwp-metric-label">Total work items</span>
            <div className="fwp-metric-icon-badge fwp-badge-cyan">
              <ListChecks size={16} aria-hidden="true" />
            </div>
          </div>
          <strong className="fwp-metric-value">{loading || !allItems ? '—' : allItems.length}</strong>
          <div className="fwp-metric-footer">
            <span className="fwp-metric-subtext">All operations today</span>
          </div>
        </SpotlightCard>

        <SpotlightCard
          className="fwp-summary-card fwp-metric-scheduled"
          spotlightColor="rgba(245, 158, 11, 0.18)"
          spotlightRadius={280}
        >
          <div className="fwp-metric-header">
            <span className="fwp-metric-label">Scheduled Visits</span>
            <div className="fwp-metric-icon-badge fwp-badge-amber">
              <Clock size={16} aria-hidden="true" />
            </div>
          </div>
          <strong className="fwp-metric-value">{loading || !allItems ? '—' : scheduledCount}</strong>
          <div className="fwp-metric-footer">
            <span className="fwp-status-dot fwp-pulse-amber" aria-hidden="true" />
            <span className="fwp-metric-subtext">Awaiting arrival</span>
          </div>
        </SpotlightCard>

        <SpotlightCard
          className="fwp-summary-card fwp-metric-handover"
          spotlightColor="rgba(16, 185, 129, 0.18)"
          spotlightRadius={280}
        >
          <div className="fwp-metric-header">
            <span className="fwp-metric-label">In Facility / Ready</span>
            <div className="fwp-metric-icon-badge fwp-badge-emerald">
              <KeyRound size={16} aria-hidden="true" />
            </div>
          </div>
          <strong className="fwp-metric-value">{loading || !allItems ? '—' : checkedInCount}</strong>
          <div className="fwp-metric-footer">
            <span className="fwp-status-dot fwp-pulse-emerald" aria-hidden="true" />
            <span className="fwp-metric-subtext">Ready for handover</span>
          </div>
        </SpotlightCard>

        <SpotlightCard
          className="fwp-summary-card fwp-metric-matching"
          spotlightColor="rgba(37, 99, 235, 0.18)"
          spotlightRadius={280}
        >
          <div className="fwp-metric-header">
            <span className="fwp-metric-label">Matching items</span>
            <div className="fwp-metric-icon-badge fwp-badge-blue">
              <Filter size={16} aria-hidden="true" />
            </div>
          </div>
          <strong className="fwp-metric-value">{loading ? '—' : filteredItems.length}</strong>
          <div className="fwp-metric-footer">
            <span className="fwp-metric-subtext">Active filter results</span>
          </div>
        </SpotlightCard>
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
                  if (isHandover) {
                    setParams(current => { const query = new URLSearchParams(current); query.set('date', event.target.value); query.delete('page'); return query })
                  } else {
                    setLocalDate(event.target.value)
                    setLocalPage(1)
                  }
                }
              }}
            />
          </div>
        </label>
        <label className="fwp-toolbar-search">
          <span>Search work items</span>
          <div className="fwp-input-with-icon">
            <Search size={16} aria-hidden="true" />
            <input
              value={search}
              onChange={(event) => {
                setSearch(event.target.value)
                setPage(1)
              }}
              placeholder="Customer, phone, unit or status…"
            />
          </div>
        </label>
        <p className="fwp-toolbar-note">Search and stage filters apply across all work items for the selected date.</p>
      </section>

      <ApiErrorAlert error={error} />

      {loading && (
        <div className="fwp-state-grid" aria-label="Loading work items">
          {Array.from({ length: 5 }).map((_, index) => <div className="fwp-skeleton" key={index} />)}
        </div>
      )}

      {!loading && !error && visibleItems.length === 0 && (
        <SpotlightCard
          className="fwp-state-card"
          spotlightColor="rgba(148, 163, 184, 0.15)"
          spotlightRadius={350}
        >
          <strong>No matching work items</strong>
          <p>No work items for the selected date match your filters.</p>
        </SpotlightCard>
      )}

      {!loading && !error && visibleItems.length > 0 && (
        <SpotlightCard
          className="fwp-table-card fwp-queue-container"
          aria-label={`${copy.title} table`}
          spotlightColor="rgba(6, 182, 212, 0.08)"
          spotlightRadius={550}
        >
          <div className="fwp-queue-header">
            <div className="fwp-queue-title">
              <span className="fwp-queue-badge">LIVE QUEUE</span>
              <span className="fwp-queue-count">{filteredItems.length} tasks scheduled</span>
            </div>
            <div className="fwp-queue-status">
              <span className="fwp-status-dot fwp-pulse-cyan" aria-hidden="true" />
              <span>Operational Feed (GMT+7)</span>
            </div>
          </div>
          <div className="fwp-table-wrap">
            <table className="fwp-table" aria-label={copy.title}>
              <thead>
                <tr>
                  <th>Work item</th>
                  <th>Customer</th>
                  <th>Scheduled</th>
                  <th>Status</th>
                  <th>Related rental</th>
                  <th aria-label="Actions" />
                </tr>
              </thead>
              <tbody>
                {visibleItems.map((item) => {
                  const target = workItemLink(item, safePage)
                  const link = target && isHandover ? `${target}&workTab=${stage === 'handover' ? 'handover' : 'check-in'}&workDate=${encodeURIComponent(date)}` : target
                  const isCheckedIn = item.status === 'CHECKED_IN'
                  const isScheduled = item.status === 'SCHEDULED'
                  return (
                    <tr
                      key={`${item.workType}-${item.referenceId}`}
                      className={`fwp-queue-row ${isCheckedIn ? 'fwp-row-priority-handover' : isScheduled ? 'fwp-row-scheduled' : ''}`}
                    >
                      <td>
                        <div className="fwp-work-type-cell">
                          <span
                            className={`fwp-row-indicator ${isCheckedIn ? 'indicator-handover' : isScheduled ? 'indicator-scheduled' : 'indicator-default'}`}
                            aria-hidden="true"
                          />
                          <div>
                            <strong>{readableWorkType(item.workType)}</strong>
                            <DecryptedText
                              text={item.referenceId}
                              speed={25}
                              maxIterations={6}
                              sequential
                              animateOn="view"
                              parentClassName="fwp-ref-code"
                            />
                          </div>
                        </div>
                      </td>
                      <td>
                        {item.customer ? (
                          <div className="fwp-customer-cell">
                            <strong>{item.customer.fullName}</strong>
                            <span className="fwp-phone-tag">{item.customer.phoneNumber}</span>
                          </div>
                        ) : <span className="fwp-muted">Derived work item</span>}
                      </td>
                      <td>
                        <div className="fwp-date-cell">
                          <CalendarDays size={13} className="fwp-cell-icon" aria-hidden="true" />
                          <span>{item.scheduledDate}</span>
                        </div>
                      </td>
                      <td>
                        <div className="fwp-status-cell">
                          {isCheckedIn && <span className="fwp-status-dot fwp-pulse-emerald" title="Customer checked in - ready for handover" />}
                          {isScheduled && <span className="fwp-status-dot fwp-pulse-amber" title="Visit scheduled" />}
                          <StatusBadge status={item.status} />
                        </div>
                      </td>
                      <td>
                        {item.entityId && item.workType !== 'RESERVATION_VISIT' ? (
                          <EmployeeEntityLabel kind="contract" id={item.entityId} />
                        ) : (
                          <span className="fwp-rental-tag">{readableWorkType(item.workType)} · {item.scheduledDate}</span>
                        )}
                      </td>
                      <td>
                        {link ? (
                          <Link className={`fwp-link-button ${isCheckedIn ? 'fwp-action-priority' : ''}`} to={link}>
                            <span>{actionLabel(item)}</span>
                            <ArrowRight size={14} aria-hidden="true" />
                          </Link>
                        ) : (
                          <span className="fwp-muted">Read only</span>
                        )}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        </SpotlightCard>
      )}
      {!loading && !error && totalPages > 1 && (
        <footer className="fwp-pagination">
          <span>Page {safePage} of {totalPages} · {filteredItems.length} matching work items</span>
          <div>
            <button className="fwp-button fwp-button-secondary" type="button" disabled={loading || safePage <= 1} onClick={() => setPage(safePage - 1)}>Previous</button>
            <button className="fwp-button fwp-button-secondary" type="button" disabled={loading || safePage >= totalPages} onClick={() => setPage(safePage + 1)}>Next</button>
          </div>
        </footer>
      )}
    </div>
  )
}
