export type SupportTicketStatus =
  | "OPEN"
  | "IN_PROGRESS"
  | "COMPLETED"
  | "CANCELLED";

export type SupportTicketCategory =
  | "UNIT_ISSUE"
  | "LOCK_KEY_ISSUE"
  | "ACCESS_CARD_CODE_ISSUE"
  | "PAYMENT_ISSUE"
  | "STORED_ITEM_ISSUE"
  | "DAMAGE_ISSUE"
  | "OTHER";

export interface SupportTicketDetail {
  supportTicketId: string;
  contractId: string;
  customerId: string;
  assignedEmployeeId: string | null;
  category: SupportTicketCategory;
  description: string;
  status: SupportTicketStatus;
  resultNote: string | null;
  createdAt: string;
  completedAt: string | null;
}

export interface PaginationMeta {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface CollectionEnvelope<T> {
  data: T[];
  pagination: PaginationMeta;
}

export interface DataEnvelope<T> {
  data: T;
  message?: string;
}

export interface ApiErrorShape {
  code: string;
  message: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

export interface AssignSupportStaffRequest {
  employeeId: string;
}

export interface SupportTicketListQuery {
  page?: number;
  pageSize?: number;
}

export const SUPPORT_TICKET_CATEGORIES: SupportTicketCategory[] = [
  "UNIT_ISSUE",
  "LOCK_KEY_ISSUE",
  "ACCESS_CARD_CODE_ISSUE",
  "PAYMENT_ISSUE",
  "STORED_ITEM_ISSUE",
  "DAMAGE_ISSUE",
  "OTHER",
];

export const SUPPORT_TICKET_STATUSES: SupportTicketStatus[] = [
  "OPEN",
  "IN_PROGRESS",
  "COMPLETED",
  "CANCELLED",
];
