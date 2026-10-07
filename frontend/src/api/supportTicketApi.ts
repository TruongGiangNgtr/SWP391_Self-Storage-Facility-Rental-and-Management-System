import type {
  ApiErrorShape,
  AssignSupportStaffRequest,
  CollectionEnvelope,
  DataEnvelope,
  SupportTicketDetail,
  SupportTicketListQuery,
} from "../features/manager-support/models/supportTicket";
import { ApiRequestError, httpClient } from "./httpClient";

/**
 * MWP-05 API surface from SRS V10 FINAL:
 * SUP-002 GET  /api/v1/support-tickets
 * SUP-003 GET  /api/v1/support-tickets/{ticketId}
 * SUP-005 POST /api/v1/support-tickets/{ticketId}/assign
 *
 * The shared httpClient already prefixes VITE_API_BASE_URL (default: /api/v1),
 * so paths in this module MUST NOT repeat /api/v1.
 */
export function listSupportTickets(
  query: SupportTicketListQuery = {},
): Promise<CollectionEnvelope<SupportTicketDetail>> {
  const page = query.page ?? 1;
  const pageSize = query.pageSize ?? 20;

  return httpClient.get(
    `/support-tickets?page=${encodeURIComponent(String(page))}&pageSize=${encodeURIComponent(String(pageSize))}`,
  );
}

export function getSupportTicket(
  ticketId: string,
): Promise<DataEnvelope<SupportTicketDetail>> {
  return httpClient.get(
    `/support-tickets/${encodeURIComponent(ticketId)}`,
  );
}

export async function assignSupportTicketStaff(
  ticketId: string,
  request: AssignSupportStaffRequest,
): Promise<void> {
  await httpClient.post<unknown>(
    `/support-tickets/${encodeURIComponent(ticketId)}/assign`,
    request,
  );
}

export function normalizeApiError(error: unknown): ApiErrorShape {
  if (error instanceof ApiRequestError) {
    return {
      code: error.code,
      message: error.message,
      traceId: error.traceId,
      errors: error.errors,
    };
  }

  return {
    code: "UNEXPECTED_ERROR",
    message: error instanceof Error
      ? error.message
      : "An unexpected error occurred.",
  };
}
