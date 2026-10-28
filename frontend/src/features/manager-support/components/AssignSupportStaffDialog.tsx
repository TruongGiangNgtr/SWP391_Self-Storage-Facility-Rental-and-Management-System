import { EmployeeEntityLabel } from '../../../components/EmployeeEntityLabel'
import { useEffect, useState } from "react";
import type { ChangeEvent, FormEvent, MouseEvent } from "react";
import type { ApiErrorShape, FacilityStaffLookupItem, PaginationMeta, SupportTicketDetail } from "../models/supportTicket";

import { listFacilitySupportStaff, normalizeApiError } from "../../../api/supportTicketApi";

interface AssignSupportStaffDialogProps {
  open: boolean;
  ticket: SupportTicketDetail | null;
  submitting: boolean;
  error: ApiErrorShape | null;
  facilityId: string | null;
  onClose: () => void;
  onAssign: (employeeId: string) => Promise<boolean>;
}

export function AssignSupportStaffDialog({
  open,
  ticket,
  submitting,
  error,
  facilityId,
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

  const [staff, setStaff] = useState<FacilityStaffLookupItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [lookupError, setLookupError] = useState<ApiErrorShape | null>(null);
  const [page, setPage] = useState(1);
  const [pagination, setPagination] = useState<PaginationMeta | null>(null);
  const [retry, setRetry] = useState(0);

  useEffect(() => { setPage(1); }, [open, facilityId, ticket?.supportTicketId]);
  useEffect(() => {
    if (!open) return;
    let active = true;
    setStaff([]);
    setEmployeeId("");
    setLoading(true);
    setLookupError(null);
    setPagination(null);
    if (!facilityId) {
      setLookupError({ code: "FACILITY_REQUIRED", message: "Your account has no assigned facility. Please contact your administrator." });
      setLoading(false);
      return;
    }
    void listFacilitySupportStaff(facilityId, page).then((result) => {
      if (!active) return;
      setStaff(result.data);
      setPagination(result.pagination);
    }).catch((err: unknown) => {
      if (active) setLookupError(normalizeApiError(err));
    }).finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [open, facilityId, page, retry, ticket?.supportTicketId]);

  if (!open || !ticket) return null;

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    const value = employeeId.trim();
    if (loading || lookupError || !staff.some((member) => member.employeeId === value)) {
      setValidation("Select a Facility Staff member from the list.");
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
            <p className="mwp05-eyebrow">Open support ticket</p>
            <h2 id="mwp05-assign-title">Assign responsible staff</h2>
          </div>
          <button className="mwp05-icon-button" type="button" onClick={onClose} aria-label="Close">
            ×
          </button>
        </div>

        <div className="mwp05-ticket-context">
          <div><span>Ticket</span><strong>{ticket.category.replaceAll('_', ' ')} · {new Date(ticket.createdAt).toLocaleString('en-GB', { timeZone: 'Asia/Ho_Chi_Minh' })}</strong></div>
          <div><span>Contract</span><strong><EmployeeEntityLabel kind="contract" id={ticket.contractId} /></strong></div>
          <div><span>Category</span><strong>{ticket.category.replaceAll("_", " ")}</strong></div>
        </div>

        <form onSubmit={handleSubmit}>
          <label className="mwp05-field">
            <span>Facility Staff</span>
            <select
              data-testid="mwp05-employee-id"
              autoFocus
              value={employeeId}
              onChange={(event: ChangeEvent<HTMLSelectElement>) => { setEmployeeId(event.target.value); setValidation(null); }}
              disabled={submitting || loading || Boolean(lookupError) || staff.length === 0}
              aria-invalid={Boolean(validation || error)}
            >
              <option value="">{loading ? "Loading staff..." : "Select a staff member"}</option>
              {staff.map((member) => (
                <option key={member.employeeId} value={member.employeeId}>{member.fullName}</option>
              ))}
            </select>
            <small>Choose an active staff member in your facility.</small>
          </label>
          {!loading && !lookupError && staff.length === 0 && <p role="status">No active Facility Staff found.</p>}
          {lookupError && (
            <div className="mwp05-inline-error" role="alert">
              <strong>{lookupError.code}</strong>: {lookupError.message}
              {lookupError.traceId && <span> - Trace {lookupError.traceId}</span>}
              {facilityId && <button type="button" className="mwp05-link-button" onClick={() => setRetry((value) => value + 1)}>Try again</button>}
            </div>
          )}
          {pagination && pagination.totalPages > 1 && (
            <div className="mwp05-pagination">
              <span>Staff page {pagination.page} of {pagination.totalPages}</span>
              <button type="button" className="mwp05-button mwp05-button--secondary" disabled={submitting || loading || page <= 1} onClick={() => setPage(page - 1)}>Previous</button>
              <button type="button" className="mwp05-button mwp05-button--secondary" disabled={submitting || loading || page >= pagination.totalPages} onClick={() => setPage(page + 1)}>Next</button>
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
            <button className="mwp05-button mwp05-button--primary" type="submit" disabled={submitting || loading || Boolean(lookupError) || !employeeId} data-testid="mwp05-confirm-assign">
              {submitting ? "Assigning…" : "Assign staff"}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}
