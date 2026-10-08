import type { ApiErrorShape, SupportTicketDetail } from "../models/supportTicket";
import { SupportStatusBadge } from "./SupportStatusBadge";

interface Props {
  ticket: SupportTicketDetail | null;
  loading: boolean;
  error: ApiErrorShape | null;
  onClose: () => void;
  onAssign: () => void;
}

function formatDateTime(value: string | null) {
  if (!value) return "—";
  const date = new Date(value);
  return new Intl.DateTimeFormat("en-GB", {
    timeZone: "Asia/Ho_Chi_Minh",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).format(date);
}

export function SupportTicketDetailDrawer({ ticket, loading, error, onClose, onAssign }: Props) {
  return (
    <aside className="mwp05-drawer" aria-label="Support ticket detail" data-testid="mwp05-detail-drawer">
      <div className="mwp05-drawer__header">
        <div>
          <p className="mwp05-eyebrow">Support ticket detail</p>
          <h2>{ticket ? ticket.supportTicketId.slice(0, 12) : "Loading"}</h2>
        </div>
        <button className="mwp05-icon-button" type="button" onClick={onClose} aria-label="Close detail">×</button>
      </div>

      {loading && <div className="mwp05-state-card">Loading ticket detail…</div>}
      {error && !loading && (
        <div className="mwp05-state-card mwp05-state-card--error">
          <strong>{error.code}</strong>
          <p>{error.message}</p>
          {error.traceId ? <small>Trace: {error.traceId}</small> : null}
        </div>
      )}

      {ticket && !loading && (
        <>
          <div className="mwp05-drawer__status-row">
            <SupportStatusBadge status={ticket.status} />
            <span>{ticket.category.replaceAll("_", " ")}</span>
          </div>

          <dl className="mwp05-detail-grid">
            <div><dt>Contract</dt><dd>{ticket.contractId}</dd></div>
            <div><dt>Customer</dt><dd>{ticket.customerId}</dd></div>
            <div><dt>Assigned employee</dt><dd>{ticket.assignedEmployeeId ?? "Not assigned"}</dd></div>
            <div><dt>Created (GMT+7)</dt><dd>{formatDateTime(ticket.createdAt)}</dd></div>
            <div><dt>Completed (GMT+7)</dt><dd>{formatDateTime(ticket.completedAt)}</dd></div>
          </dl>

          <section className="mwp05-detail-section">
            <h3>Description</h3>
            <p>{ticket.description}</p>
          </section>

          {ticket.resultNote && (
            <section className="mwp05-detail-section">
              <h3>Result note</h3>
              <p>{ticket.resultNote}</p>
            </section>
          )}

          <div className="mwp05-drawer__actions">
            {ticket.status === "OPEN" ? (
              <button className="mwp05-button mwp05-button--primary" type="button" onClick={onAssign} data-testid="mwp05-open-assign">
                Assign Facility Staff
              </button>
            ) : (
              <div className="mwp05-action-note">
                Assignment is unavailable because this ticket is {ticket.status.replaceAll("_", " ").toLowerCase()}.
              </div>
            )}
          </div>
        </>
      )}
    </aside>
  );
}
