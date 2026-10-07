import { useEffect, useMemo, useState } from "react";
import type { ChangeEvent, FormEvent, MouseEvent } from "react";
import type { ApiErrorShape, SupportTicketDetail } from "../models/supportTicket";

const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

interface AssignSupportStaffDialogProps {
  open: boolean;
  ticket: SupportTicketDetail | null;
  submitting: boolean;
  error: ApiErrorShape | null;
  recentlyAssignedEmployeeIds?: string[];
  onClose: () => void;
  onAssign: (employeeId: string) => Promise<boolean>;
}

export function AssignSupportStaffDialog({
  open,
  ticket,
  submitting,
  error,
  recentlyAssignedEmployeeIds = [],
  onClose,
  onAssign,
}: AssignSupportStaffDialogProps) {
  const [employeeId, setEmployeeId] = useState("");
  const [validation, setValidation] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      setEmployeeId("");
      setValidation(null);
    }
  }, [open, ticket?.supportTicketId]);

  const suggestions = useMemo(
    () => Array.from(new Set(recentlyAssignedEmployeeIds.filter(Boolean))).slice(0, 5),
    [recentlyAssignedEmployeeIds],
  );

  if (!open || !ticket) return null;

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    const value = employeeId.trim();
    if (!UUID_PATTERN.test(value)) {
      setValidation("Enter a valid Facility Staff employee UUID.");
      return;
    }
    setValidation(null);
    const ok = await onAssign(value);
    if (ok) onClose();
  }

  return (
    <div className="mwp05-modal-backdrop" role="presentation" onMouseDown={onClose}>
      <section
        className="mwp05-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="mwp05-assign-title"
        onMouseDown={(event: MouseEvent<HTMLElement>) => event.stopPropagation()}
      >
        <div className="mwp05-modal__header">
          <div>
            <p className="mwp05-eyebrow">MWP-05 · OPEN support ticket</p>
            <h2 id="mwp05-assign-title">Assign responsible staff</h2>
          </div>
          <button className="mwp05-icon-button" type="button" onClick={onClose} aria-label="Close">
            ×
          </button>
        </div>

        <div className="mwp05-ticket-context">
          <div><span>Ticket</span><strong>{ticket.supportTicketId}</strong></div>
          <div><span>Contract</span><strong>{ticket.contractId}</strong></div>
          <div><span>Category</span><strong>{ticket.category.replaceAll("_", " ")}</strong></div>
        </div>

        <form onSubmit={handleSubmit}>
          <label className="mwp05-field">
            <span>Facility Staff employee ID</span>
            <input
              data-testid="mwp05-employee-id"
              autoFocus
              value={employeeId}
              onChange={(event: ChangeEvent<HTMLInputElement>) => setEmployeeId(event.target.value)}
              placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
              disabled={submitting}
              aria-invalid={Boolean(validation || error)}
            />
            <small>
              The server authoritatively validates ACTIVE FACILITY_STAFF role and same-Facility assignment.
            </small>
          </label>

          {suggestions.length > 0 && (
            <div className="mwp05-suggestions" aria-label="Recently assigned staff IDs">
              <span>Recently assigned in this visible Facility ticket set</span>
              <div>
                {suggestions.map((id) => (
                  <button key={id} type="button" onClick={() => setEmployeeId(id)} disabled={submitting}>
                    {id.slice(0, 8)}…
                  </button>
                ))}
              </div>
            </div>
          )}

          {validation && <div className="mwp05-inline-error">{validation}</div>}
          {error && (
            <div className="mwp05-inline-error" data-testid="mwp05-assign-error">
              <strong>{error.code}</strong>: {error.message}
              {error.traceId ? <span> · Trace {error.traceId}</span> : null}
            </div>
          )}

          <div className="mwp05-modal__actions">
            <button className="mwp05-button mwp05-button--secondary" type="button" onClick={onClose} disabled={submitting}>
              Cancel
            </button>
            <button className="mwp05-button mwp05-button--primary" type="submit" disabled={submitting} data-testid="mwp05-confirm-assign">
              {submitting ? "Assigning…" : "Assign staff"}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}
