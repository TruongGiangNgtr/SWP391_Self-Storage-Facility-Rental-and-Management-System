import type { SupportTicketStatus } from "../models/supportTicket";

export function SupportStatusBadge({ status }: { status: SupportTicketStatus }) {
  return (
    <span className={`mwp05-status mwp05-status--${status.toLowerCase()}`}>
      {status.replaceAll("_", " ")}
    </span>
  );
}
