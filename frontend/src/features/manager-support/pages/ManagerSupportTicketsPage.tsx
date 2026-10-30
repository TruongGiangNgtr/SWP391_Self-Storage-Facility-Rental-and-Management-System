import { EmployeeModal } from '../../../components/EmployeeModal'
import { EmployeeEntityLabel } from '../../../components/EmployeeEntityLabel'
import type { ChangeEvent, MouseEvent } from "react";
import { useState } from "react";
import { useAuth } from "../../../auth/auth.context";
import { AssignSupportStaffDialog } from "../components/AssignSupportStaffDialog";
import { SupportStatusBadge } from "../components/SupportStatusBadge";
import { SupportTicketDetailDrawer } from "../components/SupportTicketDetailDrawer";
import { useManagerSupportTicketDetail, useManagerSupportTickets } from "../hooks/useManagerSupportTickets";
import {
  SUPPORT_TICKET_CATEGORIES,
  SUPPORT_TICKET_STATUSES,
  type SupportTicketDetail,
} from "../models/supportTicket";
import "../styles/managerSupport.css";

function formatCreatedAt(value: string) {
  const date = new Date(value);
  return new Intl.DateTimeFormat("en-GB", {
    timeZone: "Asia/Ho_Chi_Minh",
    year: "numeric",
    month: "short",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).format(date);
}


export default function ManagerSupportTicketsPage() {
  const { user } = useAuth();
  const {
    tickets,
    pagination,
    loading,
    error,
    filters,
    setFilters,
    loadPage,
    refresh,
  } = useManagerSupportTickets();

  const [selectedTicketId, setSelectedTicketId] = useState<string | null>(null);
  const [assignOpen, setAssignOpen] = useState(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const detail = useManagerSupportTicketDetail(selectedTicketId);

  async function assign(employeeId: string) {
    const result = await detail.assign(employeeId);
    if (result.ok) {
      setSuccessMessage("Support ticket assigned. Authoritative ticket state was refreshed.");
      await refresh();
      return true;
    }
    return false;
  }

  function openTicket(ticket: SupportTicketDetail) {
    setSuccessMessage(null);
    setSelectedTicketId(ticket.supportTicketId);
  }

  return (
    <div className="mwp05-page" data-testid="mwp05-page">
      <header className="mwp05-page__header">
        <div>
          <p className="mwp05-eyebrow">Facility Manager · Flow 7</p>
          <h1>Support staff assignment</h1>
          <p>
            Review support tickets in your Facility and assign one responsible Facility Staff member to an OPEN ticket.
          </p>
        </div>
        <button className="mwp05-button mwp05-button--secondary" type="button" onClick={() => void refresh()} disabled={loading}>
          Refresh
        </button>
      </header>

      {successMessage && <div className="mwp05-banner mwp05-banner--success">{successMessage}</div>}

      <section className="mwp05-toolbar" aria-label="Support ticket filters">
        <label>
          <span>Status</span>
          <select value={filters.status} onChange={(e: ChangeEvent<HTMLSelectElement>) => setFilters({ ...filters, status: e.target.value as typeof filters.status })}>
            <option value="ALL">All statuses</option>
            {SUPPORT_TICKET_STATUSES.map((status) => <option key={status} value={status}>{status.replaceAll("_", " ")}</option>)}
          </select>
        </label>
        <label>
          <span>Category</span>
          <select value={filters.category} onChange={(e: ChangeEvent<HTMLSelectElement>) => setFilters({ ...filters, category: e.target.value as typeof filters.category })}>
            <option value="ALL">All categories</option>
            {SUPPORT_TICKET_CATEGORIES.map((category) => <option key={category} value={category}>{category.replaceAll("_", " ")}</option>)}
          </select>
        </label>
        <label className="mwp05-toolbar__search">
          <span>Search this page</span>
          <input
            value={filters.search}
            onChange={(e: ChangeEvent<HTMLInputElement>) => setFilters({ ...filters, search: e.target.value })}
            placeholder="Search ticket description…"
          />
        </label>
      </section>

      {loading && (
        <div className="mwp05-state-grid" aria-label="Loading support tickets">
          {Array.from({ length: 5 }).map((_, index) => <div className="mwp05-skeleton" key={index} />)}
        </div>
      )}

      {!loading && error && (
        <div className="mwp05-state-card mwp05-state-card--error">
          <strong>{error.code}</strong>
          <p>{error.message}</p>
          {error.traceId ? <small>Trace: {error.traceId}</small> : null}
          <button className="mwp05-button mwp05-button--secondary" type="button" onClick={() => void refresh()}>Try again</button>
        </div>
      )}

      {!loading && !error && tickets.length === 0 && (
        <div className="mwp05-state-card">
          <strong>No support tickets to show</strong>
          <p>Change the filters or refresh the authoritative ticket list.</p>
        </div>
      )}

      {!loading && !error && tickets.length > 0 && (
        <section className="mwp05-table-card">
          <div className="mwp05-table-wrap">
            <table className="mwp05-table">
              <thead>
                <tr>
                  <th>Ticket</th>
                  <th>Category</th>
                  <th>Description</th>
                  <th>Status</th>
                  <th>Responsible staff</th>
                  <th>Created (GMT+7)</th>
                  <th aria-label="Actions" />
                </tr>
              </thead>
              <tbody>
                {tickets.map((ticket) => (
                  <tr key={ticket.supportTicketId} data-testid={`mwp05-ticket-${ticket.supportTicketId}`}>
                    <td><span>{ticket.category.replaceAll("_", " ")} · {formatCreatedAt(ticket.createdAt)}</span></td>
                    <td>{ticket.category.replaceAll("_", " ")}</td>
                    <td className="mwp05-table__description" title={ticket.description}>{ticket.description}</td>
                    <td><SupportStatusBadge status={ticket.status} /></td>
                    <td>{ticket.assignedEmployeeId ? <EmployeeEntityLabel kind="employee" id={ticket.assignedEmployeeId} /> : <span className="mwp05-muted">Unassigned</span>}</td>
                    <td>{formatCreatedAt(ticket.createdAt)}</td>
                    <td>
                      <button className="mwp05-link-button" type="button" onClick={() => openTicket(ticket)}>View</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <footer className="mwp05-pagination">
            <span>Page {pagination.page} of {Math.max(pagination.totalPages, 1)} · {pagination.totalItems} tickets</span>
            <div>
              <button type="button" className="mwp05-button mwp05-button--secondary" disabled={pagination.page <= 1} onClick={() => void loadPage(pagination.page - 1)}>Previous</button>
              <button type="button" className="mwp05-button mwp05-button--secondary" disabled={pagination.page >= pagination.totalPages} onClick={() => void loadPage(pagination.page + 1)}>Next</button>
            </div>
          </footer>
        </section>
      )}

      {selectedTicketId && (<EmployeeModal label="Support ticket detail" busy={assignOpen} onClose={() => setSelectedTicketId(null)}>
        <div className="mwp05-drawer-layer" role="presentation" onMouseDown={() => { if (!assignOpen) setSelectedTicketId(null) }}>
          <div onMouseDown={(event: MouseEvent<HTMLDivElement>) => event.stopPropagation()}>
            <SupportTicketDetailDrawer
              ticket={detail.ticket}
              loading={detail.loading}
              error={detail.error}
              onClose={() => setSelectedTicketId(null)}
              onAssign={() => setAssignOpen(true)}
            />
          </div>
        </div></EmployeeModal>
      )}

      <AssignSupportStaffDialog
        open={assignOpen}
        ticket={detail.ticket}
        submitting={detail.assigning}
        error={detail.error}
        facilityId={user?.facilityId ?? null}
        onClose={() => setAssignOpen(false)}
        onAssign={assign}
      />
    </div>
  );
}
