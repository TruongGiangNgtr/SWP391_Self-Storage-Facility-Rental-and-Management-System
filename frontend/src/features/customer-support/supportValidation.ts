import { isUuid } from '../handover/handoverValidation'
import {
  SUPPORT_TICKET_CATEGORIES, SUPPORT_TICKET_STATUSES,
  type SupportTicketDetail,
  type PaginationMeta,
} from '../manager-support/models/supportTicket'

export function isSupportTicket(value: unknown): value is SupportTicketDetail {
  if (!value || typeof value !== 'object') return false
  const ticket = value as SupportTicketDetail
  return isUuid(ticket.supportTicketId) && isUuid(ticket.contractId) && isUuid(ticket.customerId) &&
    (ticket.assignedEmployeeId === null || isUuid(ticket.assignedEmployeeId)) &&
    SUPPORT_TICKET_CATEGORIES.includes(ticket.category) && SUPPORT_TICKET_STATUSES.includes(ticket.status) &&
    typeof ticket.description === 'string' && (ticket.resultNote === null || typeof ticket.resultNote === 'string') &&
    typeof ticket.createdAt === 'string' && Number.isFinite(Date.parse(ticket.createdAt)) &&
    (ticket.completedAt === null || (typeof ticket.completedAt === 'string' && Number.isFinite(Date.parse(ticket.completedAt))))
}

export function canCancelSupportTicket(ticket: SupportTicketDetail): boolean {
  return ticket.status === 'OPEN' || ticket.status === 'IN_PROGRESS'
}

export function isSupportPagination(value: unknown): value is PaginationMeta {
  if (!value || typeof value !== 'object') return false
  const pagination = value as PaginationMeta
  return Number.isInteger(pagination.page) && pagination.page >= 1 &&
    Number.isInteger(pagination.pageSize) && pagination.pageSize >= 1 && pagination.pageSize <= 100 &&
    Number.isInteger(pagination.totalItems) && pagination.totalItems >= 0 &&
    Number.isInteger(pagination.totalPages) && pagination.totalPages >= 0
}
