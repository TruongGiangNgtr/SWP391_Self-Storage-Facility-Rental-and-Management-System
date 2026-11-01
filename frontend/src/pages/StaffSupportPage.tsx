import { EmployeeEntityLabel, useEmployeeLabelLookup } from '../components/EmployeeEntityLabel'
import { CheckCircle2, LifeBuoy, RefreshCw, Search, ShieldAlert } from 'lucide-react'
import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import { presentApiError, presentValidationError, type ApiErrorPresentation } from '../api/apiErrorPresentation'
import { completeSupportTicket, getSupportTicket, listSupportTickets } from '../api/supportTicketApi'
import { useAuth } from '../auth/auth.context'
import { ApiErrorAlert } from '../components/ApiErrorAlert'
import { SpotlightCard } from '../components/react-bits/SpotlightCard'
import { StatusBadge } from '../components/StatusBadge'
import {
  SUPPORT_TICKET_CATEGORIES,
  SUPPORT_TICKET_STATUSES,
  type SupportTicketCategory,
  type SupportTicketDetail,
  type SupportTicketStatus,
} from '../features/manager-support/models/supportTicket'
import { fetchAllEmployeePages } from '../utils/employeePresentation'
import { formatUtcDateTime } from '../utils/formatters'
import '../styles/staff.css'

const PAGE_SIZE = 20

function readable(value: string) {
  return value.replaceAll('_', ' ').toLowerCase().replace(/\b\w/g, (part) => part.toUpperCase())
}

export function StaffSupportPage() {
  const { user } = useAuth()
  const lookupLabel = useEmployeeLabelLookup()
  const [searchParams, setSearchParams] = useSearchParams()
  const requestedTicketId = searchParams.get('ticketId')
  const [page, setPage] = useState(1)
  const [retry, setRetry] = useState(0)
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<'ALL' | SupportTicketStatus>('ALL')
  const [categoryFilter, setCategoryFilter] = useState<'ALL' | SupportTicketCategory>('ALL')
  const [tickets, setTickets] = useState<SupportTicketDetail[]>([])
  const [selectedId, setSelectedId] = useState<string | null>(requestedTicketId)
  const [detail, setDetail] = useState<SupportTicketDetail | null>(null)
  const [loadingList, setLoadingList] = useState(true)
  const [loadingDetail, setLoadingDetail] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [resultNote, setResultNote] = useState('')
  const [error, setError] = useState<ApiErrorPresentation | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  const loadList = useCallback(async () => {
    setLoadingList(true)
    setError(null)
    try {
      const nextTickets = await fetchAllEmployeePages((nextPage, pageSize) => listSupportTickets({ page: nextPage, pageSize }))
      setTickets(nextTickets.items)
      setSelectedId((current) => current ?? nextTickets.items[0]?.supportTicketId ?? null)
    } catch (caughtError) {
      setError(presentApiError(caughtError))
    } finally {
      setLoadingList(false)
    }
  }, [])

  const loadDetail = useCallback(async (ticketId: string) => {
    setLoadingDetail(true)
    setError(null)
    try {
      const response = await getSupportTicket(ticketId)
      setDetail(response.data)
      setResultNote(response.data.resultNote ?? '')
    } catch (caughtError) {
      setDetail(null)
      setError(presentApiError(caughtError))
    } finally {
      setLoadingDetail(false)
    }
  }, [])

  useEffect(() => { void loadList() }, [loadList, retry])
  useEffect(() => { if (selectedId) void loadDetail(selectedId) }, [loadDetail, selectedId, retry])

  const filteredTickets = useMemo(() => {
    const needle = search.trim().toLowerCase()
    return tickets.filter((ticket) => {
      if (statusFilter !== 'ALL' && ticket.status !== statusFilter) return false
      if (categoryFilter !== 'ALL' && ticket.category !== categoryFilter) return false
      if (!needle) return true
      return [
        ticket.supportTicketId,
        ticket.contractId,
        ticket.customerId,
        ticket.category,
        readable(ticket.category),
        ticket.status,
        ticket.description,
        ticket.assignedEmployeeId ?? '',
        lookupLabel('contract', ticket.contractId),
        lookupLabel('contractUnit', ticket.contractId),
        lookupLabel('customer', ticket.customerId),
        lookupLabel('employee', ticket.assignedEmployeeId),
      ].some((value) => value.toLowerCase().includes(needle))
    })
  }, [categoryFilter, lookupLabel, search, statusFilter, tickets])

  const totalPages = Math.max(1, Math.ceil(filteredTickets.length / PAGE_SIZE))
  const safePage = Math.min(page, totalPages)
  const visibleTickets = useMemo(
    () => filteredTickets.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE),
    [filteredTickets, safePage],
  )

  const assignedToCurrentStaff = Boolean(
    detail?.assignedEmployeeId && user?.employeeId && detail.assignedEmployeeId === user.employeeId,
  )
  const canComplete = detail?.status === 'IN_PROGRESS' && assignedToCurrentStaff

  function selectTicket(ticketId: string) {
    setSuccess(null)
    setSelectedId(ticketId)
    setSearchParams({ ticketId })
  }

  async function submitCompletion(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!detail) return
    if (!canComplete) {
      setError(presentValidationError('Only the assigned Facility Staff can complete an IN_PROGRESS Support Ticket.'))
      return
    }
    if (!resultNote.trim()) {
      setError(presentValidationError('Result note is required before completing the Support Ticket.'))
      return
    }

    setSubmitting(true)
    setError(null)
    setSuccess(null)
    try {
      await completeSupportTicket(detail.supportTicketId, resultNote.trim())
      setSuccess('Support Ticket completed. The recorded result is now authoritative server state.')
      await Promise.all([loadDetail(detail.supportTicketId), loadList()])
    } catch (caughtError) {
      setError(presentApiError(caughtError))
      await loadDetail(detail.supportTicketId)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="fwp-page fwp-support-page" data-testid="fwp-08-page">
      <header className="fwp-page-header">
        <div>
          <h1>Support Processing</h1>
          <p>Review accessible Contract-related Support Tickets and complete only tickets assigned to this Staff account. Issue handling remains outside FRMS; this screen records the result.</p>
        </div>
        <LifeBuoy size={30} className="fwp-header-icon" aria-hidden="true" />
      </header>

      <section className="fwp-summary" aria-label="Support work summary">
        <SpotlightCard className="fwp-summary-card" spotlightColor="rgba(6, 182, 212, 0.16)">
          <span>Server total</span>
          <strong>{tickets.length}</strong>
        </SpotlightCard>
        <SpotlightCard className="fwp-summary-card" spotlightColor="rgba(37, 99, 235, 0.16)">
          <span>Matching tickets</span>
          <strong>{filteredTickets.length}</strong>
        </SpotlightCard>
        <SpotlightCard className="fwp-summary-card" spotlightColor="rgba(16, 185, 129, 0.16)">
          <span>Assigned to me</span>
          <strong>{tickets.filter((ticket) => ticket.assignedEmployeeId === user?.employeeId).length}</strong>
        </SpotlightCard>
      </section>

      <section className="fwp-toolbar fwp-support-toolbar" aria-label="Support ticket filters">
        <label className="fwp-input-with-icon">
          <Search size={16} />
          <input
            value={search}
            onChange={(event) => {
              setSearch(event.target.value)
              setPage(1)
            }}
            placeholder="Search customer, unit, staff or description"
          />
        </label>
        <label>
          <span>Status</span>
          <select
            value={statusFilter}
            onChange={(event) => {
              setStatusFilter(event.target.value as 'ALL' | SupportTicketStatus)
              setPage(1)
            }}
          >
            <option value="ALL">All statuses</option>
            {SUPPORT_TICKET_STATUSES.map((status) => <option key={status} value={status}>{readable(status)}</option>)}
          </select>
        </label>
        <label>
          <span>Category</span>
          <select
            value={categoryFilter}
            onChange={(event) => {
              setCategoryFilter(event.target.value as 'ALL' | SupportTicketCategory)
              setPage(1)
            }}
          >
            <option value="ALL">All categories</option>
            {SUPPORT_TICKET_CATEGORIES.map((category) => <option key={category} value={category}>{readable(category)}</option>)}
          </select>
        </label>
        <button className="fwp-button fwp-button-secondary" type="button" disabled={loadingList} onClick={() => setRetry((value) => value + 1)}>
          <RefreshCw size={16} /> Refresh
        </button>
      </section>

      <ApiErrorAlert error={error} />
      {success && <div className="fwp-banner fwp-banner-success"><CheckCircle2 size={18} /><span>{success}</span></div>}

      <div className="fwp-support-layout">
        <SpotlightCard className="fwp-table-card fwp-support-table-card" spotlightColor="rgba(37, 99, 235, 0.08)">
          {loadingList ? (
            <div className="fwp-state-grid compact">{Array.from({ length: 5 }).map((_, index) => <div className="fwp-skeleton" key={index} />)}</div>
          ) : visibleTickets.length === 0 ? (
            <div className="fwp-state-card"><strong>No matching Support Tickets</strong><p>No accessible tickets in your facility match the current filters.</p></div>
          ) : (
            <div className="fwp-table-wrap">
              <table className="fwp-table" aria-label="Staff support tickets">
                <thead><tr><th>Ticket</th><th>Category</th><th>Status</th><th>Assigned</th><th>Created</th><th /></tr></thead>
                <tbody>
                  {visibleTickets.map((ticket) => (
                    <tr key={ticket.supportTicketId} className={selectedId === ticket.supportTicketId ? 'fwp-row-selected' : ''}>
                      <td><div className="fwp-customer-cell"><strong>{ticket.category.replaceAll('_', ' ')} · {formatUtcDateTime(ticket.createdAt)}</strong><span><EmployeeEntityLabel kind="contract" id={ticket.contractId} /></span></div></td>
                      <td>{readable(ticket.category)}</td>
                      <td><StatusBadge status={ticket.status} /></td>
                      <td>{ticket.assignedEmployeeId === user?.employeeId ? <strong>You</strong> : ticket.assignedEmployeeId ? <EmployeeEntityLabel kind="employee" id={ticket.assignedEmployeeId} /> : <span className="fwp-muted">Unassigned</span>}</td>
                      <td>{formatUtcDateTime(ticket.createdAt)}</td>
                      <td><button className="fwp-link-button" type="button" onClick={() => selectTicket(ticket.supportTicketId)}>Open</button></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {totalPages > 1 && (
            <footer className="fwp-pagination">
              <span>Page {safePage} of {totalPages} · {filteredTickets.length} matching tickets</span>
              <div>
                <button className="fwp-button fwp-button-secondary" type="button" disabled={safePage <= 1} onClick={() => setPage(safePage - 1)}>Previous</button>
                <button className="fwp-button fwp-button-secondary" type="button" disabled={safePage >= totalPages} onClick={() => setPage(safePage + 1)}>Next</button>
              </div>
            </footer>
          )}
        </SpotlightCard>

        <aside className="fwp-support-detail-card" aria-label="Support Ticket detail">
          {loadingDetail ? <div className="fwp-state-card">Loading Support Ticket detail…</div> : !detail ? (
            <div className="fwp-state-card"><strong>Select a Support Ticket</strong><p>Open a Facility-scoped ticket to review responsibility and result state.</p></div>
          ) : (
            <>
              <div className="fwp-card-header fwp-detail-title">
                <div><span className="fwp-eyebrow">Support Ticket</span><h2>{detail.category.replaceAll('_', ' ')}</h2></div>
                <StatusBadge status={detail.status} />
              </div>

              <dl className="fwp-detail-grid fwp-support-detail-grid">
                <div><dt>Contract</dt><dd><EmployeeEntityLabel kind="contract" id={detail.contractId} /></dd></div>
                <div><dt>Customer</dt><dd><EmployeeEntityLabel kind="customer" id={detail.customerId} /></dd></div>
                <div><dt>Category</dt><dd>{readable(detail.category)}</dd></div>
                <div><dt>Assigned employee</dt><dd><EmployeeEntityLabel kind="employee" id={detail.assignedEmployeeId} empty={'Not assigned'} /></dd></div>
                <div><dt>Created (GMT+7)</dt><dd>{formatUtcDateTime(detail.createdAt)}</dd></div>
                <div><dt>Completed (GMT+7)</dt><dd>{detail.completedAt ? formatUtcDateTime(detail.completedAt) : 'Not completed'}</dd></div>
              </dl>

              <section className="fwp-action-card">
                <h3>Description</h3>
                <p className="fwp-support-copy">{detail.description}</p>
              </section>

              {detail.resultNote && detail.status === 'COMPLETED' && (
                <section className="fwp-action-card">
                  <h3>Recorded result</h3>
                  <p className="fwp-support-copy">{detail.resultNote}</p>
                </section>
              )}

              {detail.status === 'IN_PROGRESS' && !assignedToCurrentStaff && (
                <div className="fwp-banner fwp-banner-warning">
                  <ShieldAlert size={18} />
                  <span>This ticket is assigned to another Facility Staff member. It is read-only for this account.</span>
                </div>
              )}

              {canComplete && (
                <section className="fwp-action-card fwp-support-complete-card">
                  <h3>Complete assigned Support Ticket</h3>
                  <p>Record the operational result. Completing this ticket does not mutate Contract, Payment or StorageUnit lifecycle.</p>
                  <form className="fwp-stack-form" onSubmit={submitCompletion}>
                    <label>
                      <span>Result note</span>
                      <textarea rows={5} value={resultNote} onChange={(event) => setResultNote(event.target.value)} disabled={submitting} placeholder="Describe the issue handling result…" />
                    </label>
                    <button className="fwp-button fwp-button-primary" type="submit" disabled={submitting}>{submitting ? 'Completing…' : 'Complete Support Ticket'}</button>
                  </form>
                </section>
              )}

              {detail.status === 'OPEN' && (
                <div className="fwp-banner fwp-banner-warning"><ShieldAlert size={18} /><span>This ticket is OPEN and must be assigned by the Facility Manager before Staff completion.</span></div>
              )}
            </>
          )}
        </aside>
      </div>
    </div>
  )
}
